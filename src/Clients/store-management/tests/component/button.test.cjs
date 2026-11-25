/* eslint-disable @typescript-eslint/no-require-imports -- Node's built-in test runner uses CommonJS here. */
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const React = require('react');
const ts = require('typescript');
const { renderToStaticMarkup } = require('react-dom/server');

const source = fs.readFileSync(path.join(__dirname, '../../components/ui/button.tsx'), 'utf8');
const compiled = ts.transpileModule(source, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX, target: ts.ScriptTarget.ES2022 },
}).outputText;
const moduleExports = {};
vm.runInNewContext(compiled, {
  exports: moduleExports,
  require: name => {
    if (name === '@base-ui/react/button') return { Button: props => React.createElement('button', props, props.children) };
    if (name === '@/lib/utils') return { cn: (...values) => values.filter(Boolean).join(' ') };
    return require(name);
  },
});

test('Button renders its label and variant classes', () => {
  const markup = renderToStaticMarkup(React.createElement(moduleExports.Button, { variant: 'outline' }, 'Save changes'));

  assert.match(markup, /Save changes/);
  assert.match(markup, /outline/);
});
