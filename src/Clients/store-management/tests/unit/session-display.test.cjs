/* eslint-disable @typescript-eslint/no-require-imports -- Node's built-in test runner uses CommonJS here. */
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const ts = require('typescript');

const source = fs.readFileSync(path.join(__dirname, '../../features/identity-admin/utils/session-display.ts'), 'utf8');
const compiled = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
}).outputText;
const moduleExports = {};
vm.runInNewContext(compiled, { exports: moduleExports, Intl, Date, RegExp, Number });

test('detects browser and operating system from a user agent', () => {
  const label = moduleExports.getSessionDeviceLabel(
    'Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/120.0 Safari/537.36',
    'Unknown',
  );
  assert.equal(label.browser, 'Google Chrome');
  assert.equal(label.operatingSystem, 'Windows');
});

test('uses the unknown label for missing agents and invalid dates', () => {
  const label = moduleExports.getSessionDeviceLabel(null, 'Unknown');
  assert.equal(label.browser, 'Unknown');
  assert.equal(label.operatingSystem, 'Unknown');
  assert.equal(moduleExports.formatSessionDate('not-a-date', 'en', 'Unknown'), 'Unknown');
});
