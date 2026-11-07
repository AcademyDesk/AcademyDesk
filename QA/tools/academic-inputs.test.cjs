// Actual TSX with synthetic APIs and explicit timezones; not live SQL/browser acceptance.
const { test } = require('node:test'), a = require('node:assert/strict');
const { mount, nodes, text, response } = require('./frontend-page-runtime.cjs');

for(const zone of ['Pacific/Honolulu','Pacific/Kiritimati'])for(const page of ['academic-periods','holidays'])test(`${page} civil date stays 1 January in ${zone}`,async()=>{
 const previous=process.env.TZ;process.env.TZ=zone;let h;
 try{
  h=mount(`apps/web/src/app/${page}/page.tsx`,async url=>response(url==='/api/academies'?[{id:'a'}]:page==='holidays'?[{id:'h',name:'Synthetic holiday',holidayDate:'2026-01-01'}]:{years:[{id:'y',name:'Synthetic year',startDate:'2026-01-01',endDate:'2026-12-31',isCurrent:true,isClosed:false}],terms:[]}));
  await h.flush();const t=text(h.render());a.match(t,/01-Jan-2026|1 Jan 2026/);a.doesNotMatch(t,/02-Jan-2026|2 Jan 2026/);
 }finally{h?.unmount();if(previous===undefined)delete process.env.TZ;else process.env.TZ=previous;}
});

for(const sequence of ['','0','1.5','2'])test(`curriculum sequence ${JSON.stringify(sequence)} respects the whole-number minimum`,async()=>{
 const writes=[];const h=mount('apps/web/src/app/curriculum/page.tsx',async(url,init={})=>{
  if(init.method){writes.push(JSON.parse(init.body));return response({id:'saved'});}
  return response(url==='/api/academies'?[{id:'a'}]:url.endsWith('/courses')?[{id:'c',name:'Synthetic course'}]:[]);
 });
 try{
  await h.flush();const all=()=>nodes(h.render());
  all().find(n=>n.type==='input'&&n.props.placeholder?.includes('Module')).props.onChange({target:{value:'Synthetic module'}});
  all().find(n=>n.type==='input'&&n.props.type==='number').props.onChange({target:{value:sequence}});
  await h.flush();await all().find(n=>n.type==='form').props.onSubmit({preventDefault(){}});await h.flush();
  a.equal(writes.length,sequence==='2'?1:0);if(writes.length)a.equal(writes[0].sequence,2);
  else a.match(text(h.render()),/whole.number|sequence/i);
 }finally{h.unmount();}
});

test('shared subject-fee number input permits exact positive paisa increments',async()=>{
 const h=mount('apps/web/src/components/student-fee-arrangements.tsx',async()=>response([]),{academyId:'a',studentId:'s'},'StudentFeeArrangements');
 try{await h.flush();const n=nodes(h.render()).find(n=>n.type==='input'&&n.props.type==='number');a.equal(n.props.step,'0.01');a.equal(n.props.min,'0.01');}
 finally{h.unmount();}
});
for(const amount of ['1.50','0.50','0.01'])test(`subject-fee payload preserves ${amount}`,async()=>{
 const writes=[];const h=mount('apps/web/src/components/student-fee-arrangements.tsx',async(_,init={})=>{if(init.method)writes.push(JSON.parse(init.body));return response(init.method?{id:'saved'}:[]);},{academyId:'a',studentId:'s'},'StudentFeeArrangements');
 try{
  await h.flush();const all=()=>nodes(h.render());all().find(n=>n.type==='input'&&n.props.placeholder?.includes('Subject')).props.onChange({target:{value:'Synthetic subject'}});
  all().find(n=>n.type==='input'&&n.props.type==='number').props.onChange({target:{value:amount}});await h.flush();
  await all().find(n=>n.type==='form').props.onSubmit({preventDefault(){}});await h.flush();a.equal(writes.length,1);a.equal(writes[0].amount,Number(amount));
 }finally{h.unmount();}
});
