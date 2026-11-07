// Actual TSX with synthetic APIs. Not live Identity, SQL or device acceptance.
const {test}=require('node:test'),a=require('node:assert/strict');
const {mount,nodes,text,response}=require('./frontend-page-runtime.cjs');
const statuses=['Open','In progress','Resolved','Closed'];
const row=(priority,status='Open',id=priority+'-'+status)=>({id,academyId:'a',academyName:'Synthetic academy',subject:id,priority,status,description:'Synthetic support',createdAtUtc:'2026-11-07T00:00:00Z'});
async function owner(rows){const writes=[];const h=mount('apps/web/src/app/platform/control/page.tsx',async(url,init={})=>{
 if(init.method){writes.push({url,body:JSON.parse(init.body)});return response({});}
 return response(url==='/api/platform/support-cases'?rows:url==='/api/platform/academies'?[{id:'a',name:'Synthetic academy',subscriptionPlan:'Trial',subscriptionStatus:'Trial',enabledModulesJson:'["Core"]',studentLimit:10,staffLimit:5}]:url.endsWith('/health')?{api:'Healthy',database:'Healthy'}:url.endsWith('/settings')?{}:[]);
});await h.flush();h.query.set('tab','Support');await h.flush();return{h,writes};}
const summary=(h,label)=>nodes(h.render()).find(n=>n.type==='button'&&nodes(n).some(x=>x.type==='span'&&text(x)===label));
const count=(h,label)=>Number(text(nodes(summary(h,label)).find(n=>n.type==='b')));
const dialog=h=>nodes(h.render()).find(n=>n.props?.role==='dialog');
const subjects=h=>nodes(dialog(h)).filter(n=>n.type==='b').map(text);
for(const priority of ['Low','Normal','High','Urgent','Critical'])for(const status of statuses)test(`${priority}/${status} triage count and detail membership agree`,async()=>{
 const item=row(priority,status),f=await owner([item]);try{const elevated=['High','Urgent','Critical'].includes(priority)&&!['Resolved','Closed'].includes(status);a.equal(count(f.h,'High priority'),elevated?1:0);summary(f.h,'High priority').props.onClick();await f.h.flush();a.deepEqual(subjects(f.h),elevated?[item.subject]:[]);a.equal(f.writes.length,0);}finally{f.h.unmount();}
});
for(const priority of ['critical',' CRITICAL ',' urgent ','high'])test(`existing API priority ${JSON.stringify(priority)} keeps its triage importance`,async()=>{const f=await owner([row(priority)]);try{a.equal(count(f.h,'High priority'),1);summary(f.h,'High priority').props.onClick();await f.h.flush();a.equal(subjects(f.h).length,1);a.equal(f.writes.length,0);}finally{f.h.unmount();}});
test('all/open/resolved views retain every applicable row without promoting low or unknown priority',async()=>{
 const rows=['Low','Normal','High','Urgent','Critical'].flatMap(p=>statuses.map(s=>row(p,s))).concat(row('Deferred'));const f=await owner(rows);try{
  for(const[label,expected]of[['All cases',rows],['Open cases',rows.filter(x=>!['Resolved','Closed'].includes(x.status))],['Resolved',rows.filter(x=>['Resolved','Closed'].includes(x.status))]]){a.equal(count(f.h,label),expected.length);summary(f.h,label).props.onClick();await f.h.flush();a.deepEqual(subjects(f.h).sort(),expected.map(x=>x.subject).sort());nodes(dialog(f.h)).find(n=>n.props?.['aria-label']==='Close support case details').props.onClick();await f.h.flush();a.equal(dialog(f.h),undefined);}
  a.equal(f.writes.length,0);
 }finally{f.h.unmount();}
});
test('Critical academy intake payload appears in owner urgent details without changing its priority',async()=>{
 const cases=[],writes=[];const h=mount('apps/web/src/app/platform-services/page.tsx',async(url,init={})=>{
  if(init.method){const body=JSON.parse(init.body);writes.push({url,body});cases.push(row(body.priority,'Open','new-critical'));return response({id:'new-critical'});}
  return response(url==='/api/academies'?[{id:'a'}]:url.endsWith('/support-cases')?cases:[]);
 });let f;try{await h.flush();const select=nodes(h.render()).find(n=>n.type==='select-control'&&n.props.name==='priority');a.ok(select.props.options.some(x=>x.value==='Critical'));select.props.onChange('Critical');await h.flush();let resets=0;
  await nodes(h.render()).find(n=>n.type==='form'&&n.props.className.includes('platform-support-form')).props.onSubmit({preventDefault(){},currentTarget:{values:{subject:'Critical issue',description:'Synthetic urgent request'},reset(){resets++;}}});await h.flush();
  a.deepEqual(writes,[{url:'/api/academies/a/platform-services/support-cases',body:{subject:'Critical issue',priority:'Critical',description:'Synthetic urgent request'}}]);a.equal(resets,1);
  f=await owner(cases);a.equal(count(f.h,'High priority'),1);summary(f.h,'High priority').props.onClick();await f.h.flush();a.deepEqual(subjects(f.h),['new-critical']);a.equal(cases[0].priority,'Critical');a.equal(f.writes.length,0);
 }finally{h.unmount();f?.h.unmount();}
});
