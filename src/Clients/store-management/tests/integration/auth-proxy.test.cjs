/* eslint-disable @typescript-eslint/no-require-imports -- CommonJS VM harness loads the server route with isolated dependencies. */
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');
const { createCipheriv, randomBytes } = require('node:crypto');

function load(file, dependencies, globals = {}) {
  const code = ts.transpileModule(fs.readFileSync(path.join(__dirname, '..', '..', file), 'utf8'), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
  }).outputText;
  const exports = {};
  vm.runInNewContext(code, { exports, require: name => dependencies[name] ?? require(name),
    Buffer, Response, Request, Headers, URL, AbortSignal, setTimeout, clearTimeout, ...globals });
  return exports;
}

function harness(fetch) {
  const values = new Map();
  const jar = { get: name => values.has(name) ? { value: values.get(name) } : undefined,
    set: (name, value) => values.set(name, value), delete: ({ name }) => values.delete(name) };
  const coordinator = load('lib/session-refresh.ts', {});
  const route = load('app/api/identity/[...path]/route.ts', {
    'next/headers': { cookies: async () => jar }, '@/lib/session-refresh': coordinator,
  }, { fetch, process: { env: { IDENTITY_COOKIE_ENCRYPTION_KEY: Buffer.alloc(32, 7).toString('base64') } } });
  async function call(resource, method = 'GET', data) {
    const request = new Request(`http://localhost/api/identity/${resource}`, {
      method, headers: { host: 'localhost', origin: 'http://localhost', 'content-type': 'application/json', 'user-agent': 'RegressionBrowser/1.0' },
      ...(data ? { body: JSON.stringify(data) } : {}),
    });
    return route[method](request, { params: Promise.resolve({ path: resource.split('/') }) });
  }
  return { call, expireAccess: () => values.delete('app_7f2a'), values };
}

const identity = token => ({ accessToken: `access-${token}`, refreshToken: token,
  userId: 'user-1', sessionId: 'session-1', refreshTokenExpiryTime: new Date(Date.now() + 86_400_000).toISOString() });
const json = (value, status = 200) => Response.json(value, { status });
const credentials = { email: 'member@example.test', password: 'test-password' };

test('parallel requests rotate once and preserve the decrypted email', async () => {
  let refreshes = 0;
  const h = harness(async (url, init) => {
    if (url.endsWith('/login')) { assert.equal(init.headers['User-Agent'], 'RegressionBrowser/1.0'); return json(identity('original')); }
    if (url.endsWith('/refresh')) { refreshes++; await new Promise(resolve => setTimeout(resolve, 20)); return json(identity('rotated')); }
    return json({ ok: true });
  });
  assert.equal((await h.call('auths/login', 'POST', credentials)).status, 200);
  h.expireAccess();
  const responses = await Promise.all(Array.from({ length: 8 }, () => h.call('auths/me')));
  assert.ok(responses.every(response => response.ok));
  assert.equal(refreshes, 1);
  assert.equal((await (await h.call('session')).json()).email, credentials.email);
});

test('logout with expired access revokes using the refresh credential without rotating', async () => {
  const calls = [];
  const h = harness(async (url, init) => {
    calls.push(url);
    if (url.endsWith('/login')) return json(identity('original'));
    assert.ok(url.endsWith('/logout'));
    assert.deepEqual(JSON.parse(init.body), { userId: 'user-1', sessionId: 'session-1', token: 'original' });
    return new Response(null, { status: 204 });
  });
  await h.call('auths/login', 'POST', credentials);
  h.expireAccess();
  assert.equal((await h.call('auths/logout', 'POST', {})).status, 204);
  assert.equal(calls.filter(url => url.endsWith('/refresh')).length, 0);
  assert.equal((await (await h.call('session')).json()).authenticated, false);
});

test('legacy email cookies with repeated encryption are repaired', async () => {
  const h = harness(async () => json(identity('original')));
  await h.call('auths/login', 'POST', credentials);
  for (let i = 0; i < 3; i++) {
    const iv = randomBytes(12);
    const cipher = createCipheriv('aes-256-gcm', Buffer.alloc(32, 7), iv);
    const ciphertext = Buffer.concat([cipher.update(h.values.get('app_2a5f'), 'utf8'), cipher.final()]);
    h.values.set('app_2a5f', `v1.${iv.toString('base64url')}.${cipher.getAuthTag().toString('base64url')}.${ciphertext.toString('base64url')}`);
  }
  assert.equal((await (await h.call('session')).json()).email, credentials.email);
  h.expireAccess();
  assert.equal((await h.call('auths/me')).status, 200);
  assert.equal((await (await h.call('session')).json()).email, credentials.email);
});

test('temporary refresh outage preserves cookies and allows a retry', async () => {
  let unavailable = true;
  const h = harness(async url => {
    if (url.endsWith('/login')) return json(identity('original'));
    if (url.endsWith('/refresh')) return unavailable ? json({}, 503) : json(identity('rotated'));
    return json({ ok: true });
  });
  await h.call('auths/login', 'POST', credentials);
  h.expireAccess();
  assert.equal((await h.call('auths/me')).status, 503);
  assert.equal((await (await h.call('session')).json()).authenticated, true);
  unavailable = false;
  assert.equal((await h.call('auths/me')).status, 200);
});

test('a mutation body survives the 401 refresh-and-retry path', async () => {
  const bodies = [];
  const h = harness(async (url, init) => {
    if (url.endsWith('/login')) return json(identity('original'));
    if (url.endsWith('/refresh')) return json(identity('rotated'));
    bodies.push(init.body);
    return bodies.length === 1 ? json({}, 401) : new Response(null, { status: 204 });
  });
  await h.call('auths/login', 'POST', credentials);
  assert.equal((await h.call('admin/users/member', 'PUT', { firstName: 'Updated' })).status, 204);
  assert.equal(bodies.length, 2);
  assert.equal(bodies[0], bodies[1]);
});

test('invalid refresh clears the session; logout outages are reported and clear local cookies', async () => {
  let failingLogout = false;
  const h = harness(async url => url.endsWith('/login') ? json(identity('original')) : json({}, failingLogout ? 503 : 400));
  await h.call('auths/login', 'POST', credentials);
  h.expireAccess();
  assert.equal((await h.call('auths/me')).status, 401);
  assert.equal((await (await h.call('session')).json()).authenticated, false);
  await h.call('auths/login', 'POST', credentials);
  failingLogout = true;
  assert.equal((await h.call('auths/logout', 'POST', {})).status, 502);
  assert.equal((await (await h.call('session')).json()).authenticated, false);
});
