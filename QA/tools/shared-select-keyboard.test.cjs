'use strict';
// Actual select TSX with controlled hooks/DOM focus. Native navigation is tested separately.
const test = require('node:test'), a = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const code = ts.transpileModule(fs.readFileSync('apps/web/src/components/standard-select-field.tsx', 'utf8'), { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText;
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];
function select(extra = {}) {
  const state = [], refs = [], deps = [], effects = [], changes = [], document = { activeElement: null };
  let si = 0, ri = 0, ei = 0, tree, buttons;
  const module = { exports: {} }, react = {
    useId: () => 'select-test', useCallback: fn => fn,
    useState: initial => { const k = si++; if (!(k in state)) state[k] = initial; return [state[k], next => { state[k] = typeof next === 'function' ? next(state[k]) : next; }]; },
    useRef: initial => { const k = ri++; return refs[k] ?? (refs[k] = { current: initial }); },
    useEffect: (fn, next) => { const k = ei++; if (!deps[k] || next.some((v, i) => v !== deps[k][i])) { deps[k] = next; effects.push(fn); } },
  };
  new Function('require', 'module', 'exports', 'document', code)(name => name === 'react' ? react : name === 'react/jsx-runtime' ? jsx : name.endsWith('.module.css') ? { default: {} } : name.endsWith('anchored-popup') ? { AnchoredPopup: 'popup' } : name.endsWith('required-control-value') ? { RequiredControlValue: 'validation' } : (() => { throw Error(name); })(), module, module.exports, document);
  const props = { name: 'studentId', value: 'b', label: 'Student', placeholder: 'Select student', options: [{ value: 'a', label: 'Alice' }, { value: 'b', label: 'Bob' }, { value: 'c', label: 'Charlie' }, { value: 'd', label: 'Chloe' }], onChange: v => changes.push(v), ...extra };
  const trigger = { focus() { document.activeElement = trigger; } };
  const render = () => {
    si = ri = ei = 0; tree = module.exports.StandardSelectField(props);
    buttons = nodes(tree).filter(n => n.props?.role === 'option').map(n => ({ node: n, textContent: n.props.children, focus() { document.activeElement = this; }, scrollIntoView() {} }));
    tree.props.ref.current = { querySelector: selector => selector.includes('role="option"') ? buttons.find(b => selector.includes('aria-selected') ? b.node.props['aria-selected'] : true) : trigger, querySelectorAll: () => buttons };
    for (const effect of effects.splice(0)) effect();
    return tree;
  };
  render();
  const open = () => { nodes(tree).find(n => n.type === 'button').props.onClick(); render(); };
  const key = (key, extra = {}) => { const event = { key, nativeEvent: {}, defaultPrevented: false, preventDefault() { this.defaultPrevented = true; }, ...extra }; tree.props.onKeyDown?.(event); return event; };
  return { render, open, key, changes, trigger, document, props, get tree() { return tree; }, get buttons() { return buttons; } };
}
test('opening moves focus to current selected option without committing a change', () => { const s = select(); s.open(); a.equal(s.document.activeElement?.textContent, 'Bob'); a.deepEqual(s.changes, []); });
test('empty or stale selection focuses first available option', () => { for (const value of ['', 'missing']) { const s = select({ value }); s.open(); a.equal(s.document.activeElement?.textContent, 'Select student'); a.deepEqual(s.changes, []); } });
test('options do not create a tab stop for every record', () => { const s = select(); s.open(); a.equal(s.buttons.length, 5); a.ok(s.buttons.every(b => b.node.props.tabIndex === -1)); });
test('Tab and Shift+Tab dismiss without selection and allow browser default from trigger', () => { for (const shiftKey of [false, true]) { const s = select(); s.open(); const e = s.key('Tab', { shiftKey }); a.equal(s.document.activeElement, s.trigger); a.equal(e.defaultPrevented, false); s.render(); a.equal(nodes(s.tree).some(n => n.type === 'popup'), false); a.deepEqual(s.changes, []); } });
test('type-ahead supports case-insensitive prefix and repeated-character cycling', () => { const s = select(); s.open(); a.equal(s.key('C').defaultPrevented, true); a.equal(s.document.activeElement.textContent, 'Charlie'); s.key('c'); a.equal(s.document.activeElement.textContent, 'Chloe'); s.key('h'); a.equal(s.document.activeElement.textContent, 'Chloe'); a.deepEqual(s.changes, []); });
test('modifier shortcuts and Space retain their native meaning', () => { const s = select(); s.open(); const before = s.document.activeElement; for (const event of [{ key: 'a', ctrlKey: true }, { key: 'c', metaKey: true }, { key: 'b', altKey: true }, { key: ' ' }]) a.equal(s.key(event.key, event).defaultPrevented, false); a.equal(s.document.activeElement, before); });
test('option activation commits once, collapses and restores trigger focus', () => { const s = select(); s.open(); s.buttons.find(b => b.textContent === 'Charlie').node.props.onClick(); a.deepEqual(s.changes, ['c']); a.equal(s.document.activeElement, s.trigger); s.render(); a.equal(nodes(s.tree).some(n => n.type === 'popup'), false); });
test('visible label and selected value both contribute to labelled trigger', () => { const s = select(); const trigger = nodes(s.tree).find(n => n.type === 'button'); const ids = trigger.props['aria-labelledby'].split(' '); a.equal(ids.length, 2); a.ok(ids.every(id => nodes(s.tree).some(n => n.props?.id === id))); });
test('disabled select never focuses or renders its popup', () => { const s = select({ disabled: true }); s.open(); a.equal(nodes(s.tree).some(n => n.type === 'popup'), false); a.equal(s.document.activeElement, null); });
