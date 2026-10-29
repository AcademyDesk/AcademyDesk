// Actual WorkspaceFrame/helper, controlled lifecycle delivery; not native Identity/SQL.
const fs = require('node:fs'), vm = require('node:vm'), assert = require('node:assert/strict');
const { test } = require('node:test');
const ts = require('../../apps/web/node_modules/typescript');
const jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const compile = file => ts.transpileModule(fs.readFileSync(file, 'utf8'), { compilerOptions: {
  target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX,
} }).outputText;
const apiCode = compile('apps/web/src/lib/api.ts');
const frameCode = compile('apps/web/src/components/workspace-frame.tsx');
const paths = { AcademyAdmin: '/dashboard', Teacher: '/teacher', Portal: '/portal', Platform: '/platform/control' };
function fixture(workspace, initial = true) {
  const data = new Map(), listeners = new Map(), states = [], effects = [];
  let cursor = 0, mounted = false;
  const storage = { getItem: key => data.get(key) ?? null, setItem: (key,value) => data.set(key,String(value)), removeItem: key => data.delete(key) };
  function target(prefix) { return {
    addEventListener: (name,fn) => { const key=prefix+name; if(!listeners.has(key))listeners.set(key,new Set());listeners.get(key).add(fn); },
    removeEventListener: (name,fn) => listeners.get(prefix+name)?.delete(fn),
    dispatchEvent: event => { for(const fn of [...listeners.get(prefix+event.type) ?? []])fn(event); return true; },
  }; }
  const window = { ...target('window:'), location: { pathname: paths[workspace] }, localStorage: storage };
  const document = { ...target('document:'), visibilityState: 'visible' };
  const api = { exports: {} };
  vm.runInNewContext(apiCode, { module: api, exports: api.exports, process: { env: {} }, window, Headers, AbortController, CustomEvent, fetch: () => { throw Error('No network allowed'); } });
  if(initial)api.exports.savePortalTokens(workspace,'synthetic-access','synthetic-refresh');
  const frame = { exports: {} };
  new Function('require','module','exports','window','document',frameCode)(name => {
    if(name==='react/jsx-runtime')return jsx;
    if(name==='react')return { useState: value => { const index=cursor++; if(!(index in states))states[index]=value;return [states[index],value=>states[index]=value]; }, useEffect: effect => { if(!mounted)effects.push(effect()); } };
    if(name==='next/navigation')return { usePathname: () => window.location.pathname };
    if(name==='@/lib/api')return api.exports;
    return { EnterpriseShell:'shell', RoleManualModeBar:'manual-bar' };
  },frame,frame.exports,window,document);
  const resolve = node => node && typeof node.type==='function' ? resolve(node.type(node.props)) : node;
  const render = () => { cursor=0;const node=resolve(frame.exports.WorkspaceFrame({children:'PRIVATE-CONTENT'}));mounted=true;return node; };
  const nodes = value => value==null ? [] : Array.isArray(value) ? value.flatMap(nodes) : typeof value==='object' ? [value,...nodes(value.props?.children)] : [value];
  const privateVisible = () => nodes(render()).includes('PRIVATE-CONTENT');
  const emit = (type, detail) => window.dispatchEvent({type,detail});
  const remove = (targetWorkspace=workspace) => { storage.removeItem('academydesk.accessToken.'+targetWorkspace);storage.removeItem('academydesk.refreshToken.'+targetWorkspace); };
  return { api:api.exports, data, window, document, render, nodes, privateVisible, emit, remove, dispose:()=>effects.forEach(cleanup=>cleanup?.()) };
}
for(const workspace of Object.keys(paths)) {
  for(const event of ['focus','pageshow','visibilitychange'])test(`${workspace}: missed account-replacement marker gates content on ${event}`,()=>{
    const f=fixture(workspace);f.data.set('academydesk.sessionEpoch.'+workspace,'synthetic-old-epoch');f.render();
    f.data.set('academydesk.sessionEpoch.'+workspace,'synthetic-new-epoch');
    if(event==='visibilitychange')f.document.dispatchEvent({type:event});else f.emit(event);
    assert.equal(f.privateVisible(),false);assert.equal(f.api.portalAccessToken(workspace),'synthetic-access');
  });
  for(const event of ['focus','pageshow','visibilitychange'])test(`${workspace}: missed logout gates content on ${event}`,()=>{
    const f=fixture(workspace);assert.equal(f.privateVisible(),true);f.remove();
    // Deliberately no StorageEvent: test the missing-event recovery path.
    if(event==='visibilitychange')f.document.dispatchEvent({type:event});else f.emit(event);
    assert.equal(f.privateVisible(),false);const nodes=f.nodes(f.render());
    assert.ok(nodes.some(node=>node?.props?.role==='alert'));assert.ok(nodes.some(node=>node?.props?.href==='/login'));
    assert.equal(f.data.size,0,'Resume must not restore credentials');
  });
  test(`${workspace}: normal renewal is not logout`,()=>{
    const f=fixture(workspace);f.render();f.api.savePortalTokens(workspace,'renewed','renewed-refresh');f.emit('focus');f.emit('pageshow');f.document.dispatchEvent({type:'visibilitychange'});assert.equal(f.privateVisible(),true);
  });
  test(`${workspace}: another workspace logout is isolated`,()=>{
    const f=fixture(workspace);f.render();const other=Object.keys(paths).find(value=>value!==workspace);f.api.savePortalTokens(other,'other','other-refresh');f.api.clearPortalTokens(other);f.emit('focus');assert.equal(f.privateVisible(),true);
  });
  test(`${workspace}: same-document logout gates content`,()=>{
    const f=fixture(workspace);f.render();f.api.clearPortalTokens(workspace);assert.equal(f.privateVisible(),false);
  });
  test(`${workspace}: lifecycle listeners removed on unmount`,()=>{
    const f=fixture(workspace);f.render();f.dispose();f.remove();f.emit('focus');f.emit('pageshow');f.document.dispatchEvent({type:'visibilitychange'});f.emit(f.api.portalSignOutEvent,workspace);assert.equal(f.privateVisible(),true);
  });
}
test('public login stays usable without a session',()=>{const f=fixture('AcademyAdmin');f.window.location.pathname='/login';f.render();f.remove();f.emit('focus');assert.equal(f.privateVisible(),true);});
test('an initially unsigned-in role page is not falsely called a peer logout',()=>{const f=fixture('Teacher',false);f.render();f.emit('focus');assert.equal(f.privateVisible(),true);});
test('newly observed login is checked on subsequent resume',()=>{const f=fixture('Teacher',false);f.render();f.api.savePortalTokens('Teacher','new','new-refresh');f.emit('focus');f.remove();f.emit('pageshow');assert.equal(f.privateVisible(),false);});
test('legacy administrator token remains supported',()=>{const f=fixture('AcademyAdmin',false);f.data.set('academydesk.accessToken','legacy');f.render();f.emit('focus');assert.equal(f.privateVisible(),true);f.data.delete('academydesk.accessToken');f.emit('pageshow');assert.equal(f.privateVisible(),false);});
test('a hidden-tab visibility event defers the check until visible',()=>{const f=fixture('Teacher');f.render();f.remove();f.document.visibilityState='hidden';f.document.dispatchEvent({type:'visibilitychange'});assert.equal(f.privateVisible(),true);f.document.visibilityState='visible';f.document.dispatchEvent({type:'visibilitychange'});assert.equal(f.privateVisible(),false);});
