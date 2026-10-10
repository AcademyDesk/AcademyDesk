// Executes actual page handlers with synthetic React hooks and API transport; not SQL/RBAC acceptance.
const test = require('node:test'), assert = require('node:assert/strict'), fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript'), jsx = require('../../apps/web/node_modules/react/jsx-runtime');
const tick = () => new Promise(r => setImmediate(r));
const nodes = n => Array.isArray(n) ? n.flatMap(nodes) : !n || typeof n !== 'object' ? [] : [n, ...nodes(n.props?.children)];
const defs = {
  enrollments: { route: 'enrollments', notice: 8, saving: 10, drafts: [[4, 'student'], [5, 'source'], [6, '2026-10-20'], [7, 'Waitlisted']], actions: ['create', 'status'] },
  promotions: { route: 'batch-promotions', notice: 5, saving: 6, drafts: [[7, 'student'], [8, 'source'], [9, 'target'], [10, '2026-10-20']], actions: ['create', 'approve', 'reject'] },
};
async function page(domain, config = {}) {
  const d = defs[domain], promo = domain === 'promotions';
  const source = process.env.QA_UI_BATCH_BASELINE === '1'
    ? require('node:child_process').execFileSync('git', ['show', 'c0d348a1592a2946b398639e8fcb17c2e2d5d359:apps/web/src/app/' + d.route + '/page.tsx'], { encoding: 'utf8' })
    : fs.readFileSync('apps/web/src/app/' + d.route + '/page.tsx', 'utf8');
  const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX } }).outputText;
  let i = 0, r = 0, e = 0;
  const state = new Map(), refs = [], deps = [], effects = [], calls = [], module = { exports: {} };
  const data = {
    '/api/academies/owned/students': [{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }],
    '/api/academies/owned/batches': [{ id: 'source', name: 'Source', capacity: 10 }, { id: 'target', name: 'Target', capacity: 10 }],
    '/api/academies/owned/enrollments': [{ id: 'enrollment', studentId: 'student', batchId: 'source', startDate: '2026-10-01', endDate: null, status: 'Active' }],
    '/api/academies/owned/batch-promotions': [{ id: 'promotion', studentId: 'student', sourceBatchId: 'source', targetBatchId: 'target', effectiveDate: '2026-10-20', status: 'Pending' }],
  };
  const forms = [{ resets: 0, fields: { notes: config.blank ? '' : ' Draft note ' }, reset() { this.resets++; } }];
  let prompts = 0;
  const react = {
    useMemo: fn => fn(),
    useState: v => { const k = i++; if (!state.has(k)) state.set(k, v); return [state.get(k), v => state.set(k, typeof v === 'function' ? v(state.get(k)) : v)]; },
    useRef: v => { const k = r++; return refs[k] ?? (refs[k] = { current: v }); },
    useEffect: (fn, next) => { const k = e++; if (!deps[k] || next.some((v, i) => v !== deps[k][i])) { deps[k] = next; effects.push(fn); } },
  };
  const api = async (url, init = {}) => {
    calls.push({ url, ...init });
    if (init.method) {
      if (config.writeGate) await config.writeGate;
      if (config.network) throw Error('Synthetic network');
      return { ok: !config.status, status: config.status || 201, json: async () => ({ message: config.message || 'Synthetic policy refusal.' }) };
    }
    if (url === '/api/academies') return { ok: !config.academyStatus, json: async () => config.noAcademy ? [] : [{ id: 'owned' }] };
    assert.ok(Object.hasOwn(data, url), url);
    if (calls.some(c => c.method)) { if (config.readGate) await config.readGate; if (config.refreshFailure) return { ok: false, status: 503 }; }
    return { ok: !config.initialFailure, json: async () => config.malformed ? {} : data[url] };
  };
  new Function('require', 'module', 'exports', 'FormData', 'window', code)(n => n === 'react/jsx-runtime' ? jsx : n === 'react' ? react
    : n === 'next/link' ? { __esModule: true, default: 'a' }
    : n === '@/lib/api' ? { academyApi: api, apiHeaders: () => ({}) }
    : n === '@/components/workspace-nav' ? { WorkspaceNav: 'nav' }
    : n === '@/components/standard-date-field' ? { StandardDateField: 'date' }
    : n === '@/components/design-system/controls' ? { StandardSelectField: 'select', StandardDateField: 'date' }
    : (() => { throw Error(n); })(), module, module.exports, class { constructor(form) { this.fields = { ...form.fields }; } get(k) { return this.fields[k] ?? null; } }, { location: { search: '?studentId=student' }, prompt() { prompts++; return Object.hasOwn(config, 'prompt') ? config.prompt : ' Decision note '; } });
  const render = () => { i = r = e = 0; return module.exports.default(); };
  async function settle() { for (let i = 0; i < 5; i++) { for (const fn of effects.splice(0)) fn(); await tick(); render(); } }
  render(); await settle(); for (const [k, v] of d.drafts) state.set(k, v);
  function handler(action) {
    const tree = nodes(render());
    if (action === 'create') {
      const h = tree.find(n => n.type === 'form').props.onSubmit;
      return () => { let released = false; const event = { preventDefault() {}, get currentTarget() { if (released) throw Error('Released currentTarget'); return forms[0]; } }; const result = h(event); released = true; return result; };
    }
    if (!promo) return () => {
      const select = nodes(render()).find(n => n.type === 'select' && n.props.id === 'status-enrollment');
      if (!select) return tree.find(n => n.type === 'select' && n.props.value === 'Active').props.onChange({ target: { value: 'Paused' } });
      select.props.onChange({ target: { value: 'Paused' } });
      nodes(render()).find(n => n.type === 'textarea' && n.props.id === 'reason-enrollment')?.props.onChange({ target: { value: ' Decision note ' } });
      return nodes(render()).filter(n => n.type === 'form')[1].props.onSubmit({ preventDefault() {} });
    };
    return () => tree.find(n => n.type === 'button' && n.props.children === (action === 'approve' ? 'Approve' : 'Reject')).props.onClick();
  }
  return { d, state, calls, forms, render, settle, handler, prompts: () => prompts, writes: () => calls.filter(c => c.method), notice: () => state.get(d.notice) };
}

function select(p,status){nodes(p.render()).find(n=>n.props?.id==='status-enrollment').props.onChange({target:{value:status}});}
function reason(p,value){nodes(p.render()).find(n=>n.props?.id==='reason-enrollment').props.onChange({target:{value}});}
function save(p){return nodes(p.render()).filter(n=>n.type==='form')[1].props.onSubmit({preventDefault(){}});}
function cancel(p){nodes(p.render()).find(n=>n.type==='button'&&n.props.children==='Cancel change').props.onClick();}
for(const status of ['Paused','Completed','Waitlisted','Withdrawn','Cancelled'])test('Review then confirmed '+status+' saves trimmed reason and matching draft only',async()=>{const p=await page('enrollments');select(p,status);assert.equal(p.writes().length,0);reason(p,'  Synthetic lifecycle explanation  ');p.state.set(11,{...p.state.get(11),other:{status:'Paused',reason:'Unrelated reason'}});save(p);await p.settle();assert.equal(p.notice(),'Enrolment updated.');assert.deepEqual(JSON.parse(p.writes()[0].body),{status,endDate:null,lifecycleReason:'Synthetic lifecycle explanation'});assert.equal(p.writes().length,1);assert.deepEqual(p.state.get(11),{other:{status:'Paused',reason:'Unrelated reason'}});for(const[k,v]of p.d.drafts)assert.equal(p.state.get(k),v);});
for(const value of['','   '])test('Missing/whitespace reason blocks even direct handler '+JSON.stringify(value),async()=>{const p=await page('enrollments');select(p,'Paused');reason(p,value);save(p);await p.settle();assert.equal(p.writes().length,0);assert.match(p.notice(),/reason is required/);assert.equal(p.state.get(11).enrollment.reason,value);assert.equal(p.state.get(10),false);});
test('Active review sends null reason and clears end date only on explicit save',async()=>{const p=await page('enrollments');p.state.set(3,[{...p.state.get(3)[0],status:'Paused',endDate:'2026-10-05'}]);select(p,'Active');assert.equal(p.writes().length,0);assert.ok(!nodes(p.render()).some(n=>n.props?.id==='reason-enrollment'));save(p);await p.settle();assert.deepEqual(JSON.parse(p.writes()[0].body),{status:'Active',endDate:null,lifecycleReason:null});});
test('Non-Active existing end date is retained, not invented',async()=>{const p=await page('enrollments');p.state.set(3,[{...p.state.get(3)[0],endDate:'2026-10-05'}]);select(p,'Completed');reason(p,'Done');save(p);await p.settle();assert.equal(JSON.parse(p.writes()[0].body).endDate,'2026-10-05');});
test('Cancel sends no request, restores authoritative selector and retains creation draft',async()=>{const p=await page('enrollments');select(p,'Cancelled');reason(p,'Draft');cancel(p);await p.settle();assert.equal(p.writes().length,0);assert.deepEqual(p.state.get(11),{});assert.equal(nodes(p.render()).find(n=>n.props?.id==='status-enrollment').props.value,'Active');for(const[k,v]of p.d.drafts)assert.equal(p.state.get(k),v);});
test('Changing target retains reason; active hides rather than leaks reason in request',async()=>{const p=await page('enrollments');select(p,'Paused');reason(p,'Retain this');select(p,'Completed');assert.equal(p.state.get(11).enrollment.reason,'Retain this');select(p,'Active');save(p);await p.settle();assert.equal(JSON.parse(p.writes()[0].body).lifecycleReason,null);});
for(const status of[400,403,404,500,503])test('Denied/uncertain '+status+' retains proposed status and reason',async()=>{const p=await page('enrollments',{status});select(p,'Withdrawn');reason(p,'  Draft reason  ');save(p);await p.settle();assert.deepEqual(p.state.get(11).enrollment,{status:'Withdrawn',reason:'  Draft reason  '});assert.equal(p.writes().length,1);assert.match(p.notice(),status>=500?/could not be confirmed/:/Synthetic policy refusal/);});
test('Network uncertainty retains review draft',async()=>{const p=await page('enrollments',{network:true});select(p,'Paused');reason(p,'Draft');save(p);await p.settle();assert.match(p.notice(),/could not be confirmed/);assert.equal(p.state.get(11).enrollment.reason,'Draft');});
test('Confirmed write/failed readback clears matching review without false failed save',async()=>{const p=await page('enrollments',{refreshFailure:true});select(p,'Paused');reason(p,'Done');save(p);await p.settle();assert.match(p.notice(),/^Enrolment updated.*could not be refreshed.*Do not repeat/);assert.deepEqual(p.state.get(11),{});assert.equal(p.writes().length,1);});
for(const stage of['write','readback'])test('Pending '+stage+' guards cancel/status/reason/create and duplicate submit',async()=>{let release;const gate=new Promise(r=>release=r),p=await page('enrollments',stage==='write'?{writeGate:gate}:{readGate:gate});select(p,'Paused');reason(p,'Pending reason');const tree=nodes(p.render()),submit=tree.filter(n=>n.type==='form')[1].props.onSubmit,cancelPending=tree.find(n=>n.type==='button'&&n.props.children==='Cancel change').props.onClick,changePending=tree.find(n=>n.props?.id==='status-enrollment').props.onChange,reasonPending=tree.find(n=>n.props?.id==='reason-enrollment').props.onChange;submit({preventDefault(){}});await tick();try{submit({preventDefault(){}});cancelPending();changePending({target:{value:'Completed'}});reasonPending({target:{value:'Changed'}});p.handler('create')();assert.equal(p.writes().length,1);assert.equal(p.state.get(10),true);assert.ok(nodes(p.render()).filter(n=>n.type==='fieldset').every(n=>n.props.disabled));if(stage==='write')assert.deepEqual(p.state.get(11).enrollment,{status:'Paused',reason:'Pending reason'});}finally{release();}await p.settle();assert.equal(p.writes().length,1);assert.equal(p.state.get(10),false);});
