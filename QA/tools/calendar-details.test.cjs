// Actual calendar TSX projection/grid/agenda with controlled hooks/API, not browser/device evidence.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const ts=require('../../apps/web/node_modules/typescript'),jsx=require('../../apps/web/node_modules/react/jsx-runtime');
const source=fs.readFileSync('apps/web/src/app/calendar/page.tsx','utf8');
const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,jsx:ts.JsxEmit.ReactJSX}}).outputText;
const fixedTime='2026-10-08T10:00:00Z';
const batches=[{id:'b',name:'Synthetic class',courseId:'c',teacherId:'a',meetingLink:'https://batch.example.invalid/meeting',meetingPattern:'Thursday'}];
const teachers=[{id:'a',firstName:'Batch',lastName:'Teacher'},{id:'z',firstName:'Session',lastName:'Teacher'}];
const session={id:'x',batchId:'b',teacherId:'z',startUtc:fixedTime,deliveryMode:'Online',roomName:'https://session.example.invalid/meeting'};
function nodes(n){return Array.isArray(n)?n.flatMap(nodes):!n||typeof n!=='object'?[]:[n,...nodes(n.props?.children)];}
function text(n){return Array.isArray(n)?n.map(text).join(''):n==null||typeof n==='boolean'?'':typeof n==='object'?text(n.props?.children):String(n);}
async function page(config={}){
 let index=0,effectIndex=0;const state=new Map(),deps=[],effects=[],calls=[],module={exports:{}};
 const react={useState:v=>{const k=index++;if(!state.has(k))state.set(k,typeof v==='function'?v():v);return[state.get(k),v=>state.set(k,typeof v==='function'?v(state.get(k)):v)];},useMemo:fn=>fn(),useEffect:(fn,next)=>{const k=effectIndex++;if(!deps[k]||next.some((v,i)=>v!==deps[k][i])){deps[k]=next;effects.push(fn);}}};
 const data={batches:config.batches??batches,sessions:config.sessions??[{...session,...config.session}],events:config.events??[], 'makeup-classes':config.makeups??[],students:[{id:'s',firstName:'Synthetic',lastName:'Student'}],teachers:config.teachers??teachers,courses:[{id:'c',name:'Synthetic subject'}],enrollments:[{batchId:'b',studentId:'s',status:'Active'}]};
 const api=async(url,init={})=>{calls.push({url,...init});const key=url.split('/').at(-1);return{ok:true,status:200,json:async()=>url==='/api/academies'?[{id:'owned'}]:data[key]};};
 const clock=config.now?class extends Date{constructor(...args){super(...(args.length?args:[config.now]));}}:Date;
 new Function('require','module','exports','Date',code)(n=>n==='react/jsx-runtime'?jsx:n==='react'?react:n==='@/lib/api'?{academyApi:api}:(()=>{throw Error(n)})(),module,module.exports,clock);
 const render=()=>{index=effectIndex=0;return module.exports.default();};
 const initial=render();for(let i=0;i<4;i++){for(const fn of effects.splice(0))fn();await new Promise(r=>setImmediate(r));render();}
 if(!config.keepCurrentMonth)state.set(8,new Date(config.month??'2026-10-01T00:00:00Z'));
 const rows=()=>nodes(render()).filter(n=>typeof n.type==='function'&&n.type.name==='AgendaItem');
 const item=()=>rows()[0]?.props.item;
 const grid=()=>nodes(render()).filter(n=>n.props?.className?.startsWith('calendar-event '));
 const agenda=(id)=>{const row=rows().find(n=>n.props.item.id===(id??item().id));index=100;return row.type(row.props);};
 return{state,data,calls,render,rows,item,grid,agenda,initial};
}
test('Session teacher overrides a different batch teacher in agenda text',async()=>{const p=await page();assert.equal(p.item().teacher,'Session Teacher');assert.match(text(p.agenda()),/TeacherSession Teacher/);assert.equal(p.item().subject,'Synthetic subject');});
test('Grid and rendered agenda Open share the exact session URL',async()=>{const p=await page();assert.equal(p.item().href,session.roomName);for(const link of [p.grid()[0],nodes(p.agenda()).find(n=>n.type==='a')]){assert.equal(link.props.href,session.roomName);assert.equal(link.props.target,'_blank');assert.equal(link.props.rel,'noreferrer');}assert.match(text(p.agenda()),/Locationhttps:\/\/session.example.invalid\/meeting/);const location=nodes(p.agenda()).find(n=>n.type==='dd'&&n.props.title===session.roomName);assert.equal(location.props.style.whiteSpace,'normal');assert.equal(location.props.style.overflowWrap,'anywhere');});
for(const teacherId of [null,undefined,'missing'])test(`Missing/unresolved session teacher ${teacherId} stays Unassigned, not the batch teacher`,async()=>{const p=await page({session:{teacherId}});assert.equal(p.item().teacher,'Unassigned');});
test('Missing teacher lookup does not substitute another teacher',async()=>{const p=await page({teachers:[]});assert.equal(p.item().teacher,'Unassigned');});
for(const roomName of [null,undefined,'','  '])test(`Absent location ${JSON.stringify(roomName)} uses valid legacy batch link`,async()=>{const p=await page({session:{roomName}});assert.equal(p.item().href,batches[0].meetingLink);assert.equal(p.item().location,batches[0].meetingLink);assert.equal(p.grid()[0].props.href,batches[0].meetingLink);});
for(const deliveryMode of ['Online','Hybrid','online',' hybrid '])test(`${deliveryMode} uses the explicit session location`,async()=>{const p=await page({session:{deliveryMode}});assert.equal(p.item().href,session.roomName);assert.equal(p.item().deliveryMode,deliveryMode);});
test('In-person URL does not become a meeting action or inherit batch link',async()=>{const p=await page({session:{deliveryMode:'InPerson'}});assert.equal(p.item().href,undefined);assert.equal(p.grid()[0].type,'div');assert.ok(!nodes(p.agenda()).some(n=>n.type==='a'));assert.equal(p.item().location,session.roomName);});
test('In-person missing location remains unspecified',async()=>{const p=await page({session:{deliveryMode:'InPerson',roomName:null}});assert.equal(p.item().location,undefined);assert.match(text(p.agenda()),/LocationNot specified/);});
for(const roomName of ['Room A','javascript:alert(1)','data:text/html,test','//meeting.example.invalid','https://','https://invalid host/','https://user:password@meeting.example.invalid/'])test(`Supplied unsafe/non-URL location is displayed but not replaced/opened: ${roomName}`,async()=>{const p=await page({session:{roomName}});assert.equal(p.item().href,undefined);assert.equal(p.item().location,roomName);assert.equal(p.grid()[0].type,'div');assert.ok(!nodes(p.agenda()).some(n=>n.type==='a'));});
test('Whitespace surrounding a valid URL is trimmed consistently',async()=>{const p=await page({session:{roomName:'  '+session.roomName+'  '}});assert.equal(p.item().href,session.roomName);assert.equal(p.item().location,session.roomName);});
test('HTTP link is allowed without changing its path/query/fragment',async()=>{const url='http://session.example.invalid/path?a=1#lesson',p=await page({session:{roomName:url}});assert.equal(p.item().href,url);});
test('Unsafe batch fallback is never clickable',async()=>{const p=await page({session:{roomName:null},batches:[{...batches[0],meetingLink:'javascript:alert(1)'}]});assert.equal(p.item().href,undefined);});
test('Missing batch retains explicit session teacher/location',async()=>{const p=await page({batches:[]});assert.equal(p.item().title,'Class');assert.equal(p.item().teacher,'Session Teacher');assert.equal(p.item().href,session.roomName);assert.equal(p.item().subject,'Not assigned');});
test('Two sessions from one batch retain separate teacher/location values',async()=>{const p=await page({sessions:[session,{...session,id:'y',teacherId:'a',roomName:'https://other.example.invalid/meeting'}]});assert.deepEqual(p.rows().map(x=>[x.props.item.teacher,x.props.item.href]),[['Session Teacher',session.roomName],['Batch Teacher','https://other.example.invalid/meeting']]);});
test('Events/make-ups retain internal actions and do not inherit meeting links',async()=>{const p=await page({events:[{id:'e',title:'Synthetic event',type:'Recital',startUtc:fixedTime,venue:'Hall'}],makeups:[{id:'m',studentId:'s',batchId:'b',startUtc:fixedTime,venue:'Room'}]});const event=p.rows().find(x=>x.props.item.type==='Event'),makeup=p.rows().find(x=>x.props.item.type==='Make-up');assert.equal(event.props.item.href,'/events');assert.equal(makeup.props.item.href,'/makeup');for(const id of ['e','m']){const link=nodes(p.agenda(id)).find(n=>n.type==='a');assert.equal(link.props.target,undefined);}});
test('Filters and agenda collapse/expand retain session-specific projection',async()=>{const p=await page();const button=(label)=>nodes(p.render()).find(n=>n.type==='button'&&text(n)===label);button('Event').props.onClick();assert.equal(p.rows().length,0);button('Class').props.onClick();assert.equal(p.item().teacher,'Session Teacher');const toggle=()=>nodes(p.render()).find(n=>n.props?.['aria-controls']==='calendar-month-agenda-list');toggle().props.onClick();assert.equal(toggle().props['aria-expanded'],false);assert.equal(p.rows().length,0);toggle().props.onClick();assert.equal(p.item().href,session.roomName);assert.equal(toggle().props['aria-expanded'],true);assert.ok(p.calls.every(x=>!x.method));});
if(process.env.QA_CALENDAR_SQL==='1')test('Captured real HTTP/SQL fixture reaches actual calendar grid/agenda unchanged',async()=>{
 const log=fs.readFileSync('QA/EVIDENCE/logs/phase-2b-calendar-details-sql.log','utf8');const capture=log.match(/^CALENDARDETAILS FIXTURE (.+)$/m);assert.ok(capture);const fixture=JSON.parse(capture[1]);
 const p=await page(fixture);const row=p.rows().find(x=>x.props.item.id===fixture.expected.sessionId);assert.ok(row);assert.equal(row.props.item.teacher,fixture.expected.teacher);assert.equal(row.props.item.href,fixture.expected.location);const grid=p.grid().find(x=>x.key==='Class-'+fixture.expected.sessionId);assert.equal(grid.props.href,fixture.expected.location);const agenda=p.agenda(fixture.expected.sessionId);assert.match(text(agenda),new RegExp(fixture.expected.teacher));assert.equal(nodes(agenda).find(x=>x.type==='a').props.href,fixture.expected.location);
 for(const session of fixture.sessions){const rendered=p.rows().find(x=>x.props.item.id===session.id).props.item;if(session.teacherId===null){assert.equal(rendered.teacher,'Unassigned');assert.equal(rendered.href,fixture.batches.find(x=>x.id===session.batchId).meetingLink);}if(session.deliveryMode==='InPerson'){assert.equal(rendered.href,undefined);assert.equal(rendered.location,session.roomName);}}
});

// Run the unchanged projection cases and this boundary matrix in each browser
// timezone's Node process. These are actual TSX checks, not browser acceptance.
const boundaryCases=[
 ['month-before','2026-09-30T18:29:59Z','2026-09-01',30,'September 2026'],
 ['month-after','2026-09-30T18:30:00Z','2026-10-01',1,'October 2026'],
 ['year-before','2026-12-31T18:29:59Z','2026-12-01',31,'December 2026'],
 ['year-after','2026-12-31T18:30:00Z','2027-01-01',1,'January 2027'],
 ['leap-day','2028-02-28T18:30:00Z','2028-02-01',29,'February 2028'],
 ['US-DST-start','2026-03-08T10:00:00Z','2026-03-01',8,'March 2026'],
 ['US-DST-end','2026-11-01T09:00:00Z','2026-11-01',1,'November 2026'],
 ['Sydney-DST-start','2026-10-03T18:30:00Z','2026-10-01',4,'October 2026'],
];
for(const [label,instant,month,day,heading] of boundaryCases)test(`India calendar ${label} grid/agenda/month/Today agree (${process.env.TZ??'system'})`,async()=>{
 const p=await page({session:{startUtc:instant},month:month+'T00:00:00Z',now:instant});
 const cells=()=>nodes(p.render()).filter(n=>n.props?.className?.startsWith('calendar-day '));
 const cell=cells().find(n=>nodes(n).some(x=>x.props?.className?.startsWith('calendar-event ')));
 assert.ok(cell,'Event appears in the grid');
 assert.equal(text(nodes(cell).find(n=>n.props?.className==='calendar-date')),String(day));
 assert.ok(!cell.props.className.includes('outside'));
 assert.ok(cell.props.className.includes('today'));
 assert.equal(p.rows().length,1,'Agenda includes the same event');
 assert.ok(nodes(p.render()).some(n=>n.type==='h2'&&text(n)===heading));
 const navigate=label=>nodes(p.render()).find(n=>n.props?.['aria-label']===label).props.onClick();
 navigate('Next month');assert.equal(p.rows().length,0);navigate('Previous month');assert.equal(p.rows().length,1);
 navigate('Previous month');assert.equal(p.rows().length,0);
 nodes(p.render()).find(n=>n.type==='button'&&text(n)==='Today').props.onClick();
 assert.ok(nodes(p.render()).some(n=>n.type==='h2'&&text(n)===heading));assert.equal(p.rows().length,1);
 assert.equal(cells().length,42);assert.equal(p.calls.length,9,'Navigation does not refetch or write');
});
test('Initial month is India current month, not browser month',async()=>{
 const p=await page({now:'2026-09-30T19:00:00Z',keepCurrentMonth:true});
 assert.equal(p.initial.props['aria-busy'],'true','Prerender is independent of build/client date');
 assert.ok(!nodes(p.initial).some(n=>n.type==='h2'),'No build-time month in prerender');
 assert.ok(nodes(p.render()).some(n=>n.type==='h2'&&text(n)==='October 2026'));
});
test('Month navigation crosses December/January and leap February without DST drift',async()=>{
 const p=await page({month:'2027-12-01T00:00:00Z'});
 const next=()=>nodes(p.render()).find(n=>n.props?.['aria-label']==='Next month').props.onClick();
 for(const heading of ['January 2028','February 2028','March 2028']){next();assert.ok(nodes(p.render()).some(n=>n.type==='h2'&&text(n)===heading));}
});
test('Class, make-up and event share one India day and agenda despite UTC prior day',async()=>{
 const startUtc='2026-09-30T19:00:00Z',p=await page({session:{startUtc},events:[{id:'e',title:'Boundary event',type:'Recital',startUtc}],makeups:[{id:'m',studentId:'s',batchId:'b',startUtc}]});
 const cells=nodes(p.render()).filter(n=>n.props?.className?.startsWith('calendar-day '));
 const cell=cells.find(n=>nodes(n).filter(x=>x.props?.className?.startsWith('calendar-event ')).length===3);
 assert.ok(cell);assert.equal(text(nodes(cell).find(n=>n.props?.className==='calendar-date')),'1');
 assert.equal(p.rows().length,3);for(const id of ['e','m'])assert.match(text(p.agenda(id)),/01 Oct,? 2026/);
});

for(const deliveryMode of ['Offline','InPerson','Online','Hybrid',' online ',' hybrid '])test(`Make-up ${deliveryMode} calendar uses only its relevant location and retains internal action`,async()=>{
 const virtual=['online','hybrid'].includes(deliveryMode.trim().toLowerCase()),row={id:'m',studentId:'s',batchId:'b',startUtc:fixedTime,deliveryMode,venue:'Studio A',meetingLink:'https://meeting.example.invalid/makeup'};
 const p=await page({sessions:[],makeups:[row]}),item=p.item();assert.equal(item.location,virtual?row.meetingLink:row.venue);assert.equal(item.detail,item.location);assert.equal(item.href,'/makeup');assert.equal(item.opensExternally,undefined);assert.ok(text(p.agenda()).includes(item.location));const link=nodes(p.agenda()).find(n=>n.type==='a');assert.equal(link.props.href,'/makeup');assert.equal(link.props.target,undefined);
});
for(const deliveryMode of ['Offline','Online','Hybrid'])test(`Make-up ${deliveryMode} missing correct location never substitutes opposite field`,async()=>{const p=await page({sessions:[],makeups:[{id:'m',studentId:'s',batchId:'b',startUtc:fixedTime,deliveryMode,venue:deliveryMode==='Offline'?null:'Wrong room',meetingLink:deliveryMode==='Offline'?'Wrong link':null}]});assert.equal(p.item().location,undefined);assert.equal(p.item().detail,'Make-up class');});
if(process.env.QA_MAKEUP_SQL==='1')test('Captured make-up SQL fixture reaches actual calendar location projections',async()=>{const log=fs.readFileSync('QA/EVIDENCE/logs/phase-2b-makeup-location-sql.log','utf8'),fixture=JSON.parse(log.match(/^MAKEUPLOCATION FIXTURE (.+)$/m)[1]);const p=await page({sessions:[],makeups:fixture.makeups,now:fixture.makeups[0].startUtc,keepCurrentMonth:true});for(const row of fixture.makeups){const item=p.rows().find(x=>x.props.item.id===row.id)?.props.item;assert.ok(item);assert.equal(item.location,(row.deliveryMode==='Offline'?row.venue:row.meetingLink)||undefined);assert.equal(item.href,'/makeup');}});
