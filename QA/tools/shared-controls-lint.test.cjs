// Controlled React-hook tests of actual TS/TSX. Not browser or physical-device proof.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const ts = require('../../apps/web/node_modules/typescript');
const jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const React = require('../../apps/web/node_modules/react');
const { renderToStaticMarkup } = require('../../apps/web/node_modules/react-dom/server');
const root = path.resolve(__dirname, '../..');

function load(file, dependencies, globals = {}) {
  const code = ts.transpileModule(fs.readFileSync(path.join(root, file), 'utf8'), {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022, jsx: ts.JsxEmit.ReactJSX },
  }).outputText;
  const module = { exports: {} };
  new Function('require', 'module', 'exports', ...Object.keys(globals), code)(name => {
    if (name === 'react/jsx-runtime') return jsx;
    if (name in dependencies) return dependencies[name];
    throw new Error(`Unexpected dependency: ${name}`);
  }, module, module.exports, ...Object.values(globals));
  return module.exports;
}

function nodes(node, predicate) {
  if (Array.isArray(node)) return node.flatMap(n => nodes(n, predicate));
  if (!node || typeof node !== 'object') return [];
  return [...(predicate(node) ? [node] : []), ...nodes(node.props?.children, predicate)];
}

function calendar(value = '', required = false) {
  let now = new Date('2026-10-28T12:00:00Z').getTime();
  class ClockDate extends Date {
    constructor(...args) { super(...(args.length ? args : [now])); }
    static now() { return now; }
  }
  const state = [];
  let cursor, dirty;
  const react = {
    useState(initial) {
      const key = cursor++;
      if (!(key in state)) state[key] = typeof initial === 'function' ? initial() : initial;
      return [state[key], next => {
        next = typeof next === 'function' ? next(state[key]) : next;
        if (!Object.is(next, state[key])) { state[key] = next; dirty = true; }
      }];
    },
    useRef: () => ({ current: null }), useEffect() {}, useMemo: callback => callback(),
  };
  const { StandardDateField } = load('apps/web/src/components/standard-date-field.tsx', { react }, { Date: ClockDate });
  const changes = [];
  const props = { name: 'qa-date', label: 'Date', value, required, onChange: next => changes.push(next) };
  function render() {
    for (let attempt = 0; attempt < 10; attempt++) {
      cursor = 0; dirty = false;
      const tree = StandardDateField(props);
      if (!dirty) return tree;
    }
    throw new Error('Calendar render did not settle');
  }
  const find = predicate => {
    const matches = nodes(render(), predicate);
    assert.equal(matches.length, 1);
    return matches[0];
  };
  return { render, find, props, changes, advance: () => { now += 86_400_000; },
    open: () => find(n => n.props?.className === 'standard-date-trigger').props.onClick() };
}

test('blank calendar opens on today, preserving hidden-field value', () => {
  const c = calendar(); c.open();
  assert.equal(c.find(n => n.props?.['aria-label'] === 'Select month').props.value, 9);
  assert.equal(c.find(n => n.props?.['aria-label'] === 'Select year').props.value, 2026);
  assert.equal(c.find(n => n.props?.['data-today'] === true).props.children, 28);
  assert.equal(c.find(n => n.type === 'input' && n.props.type === 'hidden').props.value, '');
  assert.deepEqual(c.changes, []);
});

test('calendar browsing survives same-value rerenders, new controlled value resets month', () => {
  const c = calendar('2026-10-10'); c.open();
  c.find(n => n.props?.['aria-label'] === 'Previous month').props.onClick();
  assert.equal(c.find(n => n.props?.['aria-label'] === 'Select month').props.value, 8);
  c.props.value = '2027-02-03';
  assert.equal(c.find(n => n.props?.['aria-label'] === 'Select month').props.value, 1);
  assert.equal(c.find(n => n.props?.['aria-label'] === 'Select year').props.value, 2027);
  assert.equal(c.find(n => n.props?.['data-selected'] === true).props.children, 3);
  assert.deepEqual(c.changes, []);
});

test('selecting a day sends its ISO date and closes the popover', () => {
  const c = calendar('2026-10-10'); c.open();
  c.find(n => n.props?.['data-current'] === true && n.props.children === 15).props.onClick();
  assert.deepEqual(c.changes, ['2026-10-15']);
  assert.equal(nodes(c.render(), n => n.props?.role === 'dialog').length, 0);
});

test('required date disables Clear; reopening refreshes today after midnight', () => {
  const c = calendar('2026-10-10', true); c.open();
  assert.equal(c.find(n => n.type === 'button' && n.props.children === 'Clear').props.disabled, true);
  c.open(); c.advance(); c.open();
  assert.equal(c.find(n => n.props?.['data-today'] === true).props.children, 29);
});

test('theme toggle omits pressed state before hydration and exposes both client choices', () => {
  for (const hydrated of [false, true]) for (const theme of ['light', 'dark']) {
    const selections = [];
    const { ThemeToggle } = load('apps/web/src/components/theme-toggle.tsx', {
      '@/components/theme-provider': { useTheme: () => ({ theme, setTheme: next => selections.push(next) }) },
      '@/lib/use-hydrated': { useHydrated: () => hydrated },
    });
    const html = renderToStaticMarkup(React.createElement(ThemeToggle));
    assert.equal((html.match(/aria-pressed=/g) ?? []).length, hydrated ? 2 : 0);
    const buttons = nodes(ThemeToggle(), n => n.type === 'button');
    assert.equal(buttons[0].props['aria-pressed'], hydrated ? theme === 'light' : undefined);
    assert.equal(buttons[1].props['aria-pressed'], hydrated ? theme === 'dark' : undefined);
    buttons.forEach(button => button.props.onClick());
    assert.deepEqual(selections, ['light', 'dark']);
  }
});

test('hydration hook keeps actual server-rendered output stable', () => {
  const { useHydrated } = load('apps/web/src/lib/use-hydrated.ts', { react: React });
  function Probe() { return jsx.jsx('span', { children: String(useHydrated()) }); }
  assert.equal(renderToStaticMarkup(React.createElement(Probe)), '<span>false</span>');
});

test('clock is stable between ticks and releases its timer on unmount', () => {
  let now = 100, state, cleanup, callback, period, clears = 0;
  const { useClock } = load('apps/web/src/lib/use-clock.ts', {
    react: { useState: initial => { state ??= initial(); return [state, next => { state = next; }]; },
      useEffect: effect => { cleanup = effect(); } },
  }, { Date: { now: () => now }, window: {
    setInterval: (cb, ms) => { callback = cb; period = ms; return 7; },
    clearInterval: id => { assert.equal(id, 7); clears++; },
  } });
  assert.equal(useClock(), 100); assert.equal(period, 60_000);
  now = 200;
  assert.equal(state, 100);
  callback(); assert.equal(state, 200);
  cleanup(); assert.equal(clears, 1);
});

test('shell keeps menus open on the same route and closes them on navigation', () => {
  const state = [];
  let cursor, dirty, pathname = '/dashboard';
  const { EnterpriseShell } = load('apps/web/src/components/enterprise-shell.tsx', {
    react: { createContext: React.createContext, useContext: React.useContext,
      useMemo: callback => callback(), useEffect() {}, useRef: () => ({ current: null }),
      useState: initial => {
        const key = cursor++;
        if (!(key in state)) state[key] = typeof initial === 'function' ? initial() : initial;
        return [state[key], next => {
          next = typeof next === 'function' ? next(state[key]) : next;
          if (!Object.is(state[key], next)) { state[key] = next; dirty = true; }
        }];
      },
    },
    'next/link': { default: 'a' },
    'next/navigation': { usePathname: () => pathname, useRouter: () => ({}), useSearchParams: () => new URLSearchParams() },
    '@/components/theme-toggle': { ThemeToggle: 'theme-toggle' },
    '@/lib/api': { academyApi() { throw Error('Unexpected network access'); }, apiUrl: '', clearPortalTokens() {} },
  });
  function render() {
    for (let i = 0; i < 10; i++) {
      cursor = 0; dirty = false;
      const tree = EnterpriseShell({ children: null });
      if (!dirty) return tree;
    }
    throw Error('Shell render did not settle');
  }
  function button(label) {
    const matches = nodes(render(), n => n.type === 'button' && n.props['aria-label'] === label);
    assert.equal(matches.length, 1, label);
    return matches[0];
  }
  // The profile button's accessible name is defined by the component itself.
  const profile = () => button('Open account menu');
  assert.ok(profile());
  profile().props.onClick();
  button('Open notifications').props.onClick();
  assert.equal(profile().props['aria-expanded'], true);
  assert.equal(button('Open notifications').props['aria-expanded'], true);
  pathname = '/students';
  assert.equal(profile().props['aria-expanded'], false);
  assert.equal(button('Open notifications').props['aria-expanded'], false);
});
