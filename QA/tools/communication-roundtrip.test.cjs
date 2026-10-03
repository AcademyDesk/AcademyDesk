// Reuse the same actual TSX regression cases; shared controlled harness adds deferred/fault responses.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const {channels,fields,base,nodes,expected,page}=require('./communication-page-harness.cjs');
for(const channel of channels)for(const edit of ['name','provider','address'])test(`${channel} ${edit} edit retains every unedited optional field`,async()=>{
 const initial=structuredClone(base),p=await page({rows:initial}),row=initial.find(x=>x.channel===channel);
 const key=edit==='name'?'senderName':edit==='provider'?'provider':channel==='WhatsApp'?'phoneNumber':channel==='Meeting'?'externalAccountReference':'senderAddress';
 const value=key==='provider'?(channel==='WhatsApp'?'BSP':'Microsoft365'):key==='phoneNumber'?'+910000000002':key==='senderAddress'?'changed@example.invalid':'Changed '+channel;
 p.edit(channel,key,value);await p.save(channel);const payload=JSON.parse(p.writes()[0].body);assert.deepEqual(Object.keys(payload).sort(),[...fields].sort());
 for(const field of fields)assert.equal(payload[field],field===key?value:row[field]);assert.equal(p.rows().find(x=>x.channel===channel).hasSecureConnection,true);
 for(const other of initial.filter(x=>x.channel!==channel))assert.deepEqual(p.rows().find(x=>x.channel===other.channel),other);
 console.log('CHANNELROUNDTRIP PAYLOAD '+JSON.stringify({label:channel+'-'+edit,channel,initial,payload,expected:expected(payload),secureConnection:true}));
});
for(const channel of channels)test(`${channel} existing null optionals remain null after API normalization`,async()=>{
 const initial=structuredClone(base),row=initial.find(x=>x.channel===channel);for(const key of fields.filter(k=>!['provider','status','messagesEnabled'].includes(k)))row[key]=null;row.status='NotConfigured';row.messagesEnabled=false;
 const p=await page({rows:initial});p.edit(channel,'senderName','Changed '+channel);await p.save(channel);const payload=JSON.parse(p.writes()[0].body);
 for(const key of ['senderAddress','replyToAddress','phoneNumber','externalAccountReference'])assert.equal(expected(payload)[key],null);
 console.log('CHANNELROUNDTRIP PAYLOAD '+JSON.stringify({label:channel+'-null-optionals',channel,initial,payload,expected:expected(payload),secureConnection:true}));
});
for(const channel of channels)test(`${channel} new channel uses documented blank defaults`,async()=>{
 const initial=[],p=await page({rows:initial});p.edit(channel,'senderName','New '+channel);await p.save(channel);const payload=JSON.parse(p.writes()[0].body);
 assert.equal(payload.status,'NotConfigured');assert.equal(payload.messagesEnabled,false);assert.equal(expected(payload).replyToAddress,null);assert.equal(p.rows().find(x=>x.channel===channel).hasSecureConnection,false);
 console.log('CHANNELROUNDTRIP PAYLOAD '+JSON.stringify({label:channel+'-new-defaults',channel,initial,payload,expected:expected(payload),secureConnection:false}));
});
for(const channel of channels)test(`${channel} explicit visible clear changes only that field`,async()=>{
 const initial=structuredClone(base),p=await page({rows:initial});p.edit(channel,'senderName','   ');await p.save(channel);const payload=JSON.parse(p.writes()[0].body);assert.equal(expected(payload).senderName,null);
 for(const key of ['replyToAddress','phoneNumber','externalAccountReference'])assert.equal(payload[key],initial.find(x=>x.channel===channel)[key]);
 console.log('CHANNELROUNDTRIP PAYLOAD '+JSON.stringify({label:channel+'-explicit-clear',channel,initial,payload,expected:expected(payload),secureConnection:true}));
});
for(const channel of channels)for(const status of ['NotConfigured','Disabled'])test(`${channel} ${status} preserves the API messagesEnabled normalization`,async()=>{
 const initial=structuredClone(base);initial.find(x=>x.channel===channel).status=status;const p=await page({rows:initial});p.edit(channel,'senderName','Changed '+channel);await p.save(channel);const payload=JSON.parse(p.writes()[0].body);assert.equal(payload.messagesEnabled,true);assert.equal(expected(payload).messagesEnabled,false);
 console.log('CHANNELROUNDTRIP PAYLOAD '+JSON.stringify({label:channel+'-'+status,channel,initial,payload,expected:expected(payload),secureConnection:true}));
});
for(const getStatus of [403,500])test(`Failed initial settings GET ${getStatus} disables and guards all saves`,async()=>{const p=await page({getStatus});for(const channel of channels){assert.equal(channel==='Meeting'?p.meeting().props.disabled:nodes(p.form(channel)).find(n=>n.type==='button').props.disabled,true);await p.save(channel);}assert.equal(p.writes().length,0);});
const malformed=[null,{},[null],[{...base[0],replyToAddress:undefined}],[{...base[0],phoneNumber:1}],[{...base[0],messagesEnabled:null}],[{...base[0],hasSecureConnection:undefined}],[base[0],base[0]],[{...base[0],channel:'SMS'}]];
for(const [i,rows]of malformed.entries())test(`Incomplete settings response ${i} cannot overwrite saved configuration`,async()=>{const p=await page({rows});for(const channel of channels)await p.save(channel);assert.equal(p.writes().length,0);assert.equal(p.meeting().props.disabled,true);});
test('Unresolved initial load stays disabled and rejects direct save handler',async()=>{let resolve;const defer=new Promise(r=>resolve=r),p=await page({defer});assert.equal(p.meeting().props.disabled,true);await p.save('Meeting');assert.equal(p.writes().length,0);resolve();await p.flush();assert.equal(p.meeting().props.disabled,false);});
if(process.env.QA_CHANNEL_SQL==='1')for(const channel of channels)test(`${channel} captured real HTTP settings retain hidden fields in actual editor payload`,async()=>{const log=fs.readFileSync('QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log','utf8'),fixture=JSON.parse(log.match(/^CHANNELROUNDTRIP FIXTURE (.+)$/m)[1]),p=await page({rows:fixture.rows});p.edit(channel,'senderName','Captured edit');await p.save(channel);const payload=JSON.parse(p.writes()[0].body),row=fixture.rows.find(x=>x.channel===channel);for(const key of ['replyToAddress','phoneNumber','externalAccountReference'])assert.equal(expected(payload)[key],row[key]);});
