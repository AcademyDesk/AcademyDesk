// Actual page handlers with controlled hooks/API. Not live React/browser/device evidence.
const test=require('node:test'),assert=require('node:assert/strict'),fs=require('node:fs');
const {channels,base,page}=require('./communication-page-harness.cjs');
const deferred=()=>{let resolve;const promise=new Promise(r=>resolve=r);return{promise,resolve};};
for(const channel of channels)for(const mode of ['clean','dirty','missing'])test(`${channel} preserves other ${mode} panels`,async()=>{
 const p=await page({rows:mode==='missing'?base.filter(x=>x.channel===channel):base});
 if(mode!=='clean')for(const other of channels.filter(x=>x!==channel)){p.edit(other,'senderName','Unsaved '+other);p.edit(other,other==='Email'?'senderAddress':other==='WhatsApp'?'phoneNumber':'externalAccountReference','Pending '+other);}
 const untouched=Object.fromEntries(channels.filter(x=>x!==channel).map(x=>[x,structuredClone(p.draft(x))]));
 p.edit(channel,'senderName','Saved '+channel);await p.save(channel);
 for(const [other,draft]of Object.entries(untouched))assert.deepEqual(p.draft(other),draft);
 assert.equal(p.writes().length,1);assert.equal(p.rows().find(x=>x.channel===channel).senderName,'Saved '+channel);
});
for(const channel of channels)test(`${channel} save has persistent panel-specific success without whole-page readback`,async()=>{
 const p=await page({onGet:n=>{if(n>1)throw Error('Readback unavailable');}});p.edit(channel,'senderName','  Trimmed  ');await p.save(channel);
 assert.equal(p.draft(channel)[channel==='Meeting'?'organizerName':'senderName'],'Trimmed');
 assert.match(p.notice(channel),new RegExp(channel+'.*saved'));for(const other of channels.filter(x=>x!==channel))assert.equal(p.notice(other),'');
 assert.equal(p.calls.filter(x=>x.url.endsWith('communication-settings')&&x.method!=='PUT').length,1);
 assert.equal(p.disabled(channel),false);
});
for(const order of [['Email','WhatsApp','Meeting'],['Email','Meeting','WhatsApp'],['WhatsApp','Email','Meeting'],['WhatsApp','Meeting','Email'],['Meeting','Email','WhatsApp'],['Meeting','WhatsApp','Email']])test(`Concurrent channel replies ${order.join('/')} remain isolated`,async()=>{
 const gates=Object.fromEntries(channels.map(x=>[x,deferred()]));const p=await page({onPut:async({channel,commit})=>{await gates[channel].promise;return commit();}});
 for(const channel of channels){p.edit(channel,'senderName','Saved '+channel);await p.save(channel);assert.equal(p.disabled(channel),true);}
 for(const channel of channels)p.edit(channel,'senderName','Newer '+channel);
 for(const channel of order){gates[channel].resolve();await p.flush();assert.match(p.notice(channel),new RegExp(channel+'.*saved'));for(const other of channels)assert.equal(p.draft(other)[other==='Meeting'?'organizerName':'senderName'],'Newer '+other);}
 assert.equal(p.writes().length,3);for(const channel of channels){assert.equal(p.disabled(channel),false);assert.equal(p.rows().find(x=>x.channel===channel).senderName,'Saved '+channel);}
});
for(const channel of channels)test(`${channel} rejects duplicate pending handler and keeps other panels enabled`,async()=>{
 const gate=deferred(),p=await page({onPut:async({commit})=>{await gate.promise;return commit();}});p.edit(channel,'senderName','Submitted');
 const trigger=channel==='Meeting'?p.meeting().props.onSave:()=>p.form(channel).props.onSubmit({preventDefault(){}});trigger();trigger();await p.flush();
 assert.equal(p.writes().length,1);assert.equal(p.disabled(channel),true);for(const other of channels.filter(x=>x!==channel))assert.equal(p.disabled(other),false);
 gate.resolve();await p.flush();assert.equal(p.disabled(channel),false);
});
for(const channel of channels)test(`${channel} retains in-flight edits but hydrates unchanged submitted fields`,async()=>{
 const gate=deferred(),p=await page({onPut:async({commit})=>{await gate.promise;return commit();}});p.edit(channel,'senderName','  Submitted  ');await p.save(channel);
 const key=channel==='Email'?'senderAddress':channel==='WhatsApp'?'phoneNumber':'externalAccountReference';p.edit(channel,key,'Newer value');gate.resolve();await p.flush();
 assert.equal(p.draft(channel)[channel==='Meeting'?'organizerName':'senderName'],'Submitted');assert.equal(p.draft(channel)[channel==='Meeting'?'reference':key],'Newer value');assert.match(p.notice(channel),/newer edits.*not.*saved/i);
 await p.save(channel);assert.equal(p.writes().length,2);assert.equal(JSON.parse(p.writes()[1].body)[key],'Newer value');
});
for(const channel of channels)for(const fault of ['validation','server','network'])test(`${channel} ${fault} failure keeps every draft and supports retry`,async()=>{
 let fail=true;const p=await page({onPut:({commit})=>{if(!fail)return commit();if(fault==='network')throw Error('disconnected');return{ok:false,status:fault==='validation'?400:500,json:async()=>({message:'Synthetic rejection'})};}});
 for(const x of channels)p.edit(x,'senderName','Draft '+x);const before=channels.map(x=>structuredClone(p.draft(x)));await p.save(channel);
 assert.deepEqual(channels.map(x=>p.draft(x)),before);assert.doesNotMatch(p.notice(channel),/settings saved/);assert.match(p.notice(channel),/kept|retained/i);assert.equal(p.disabled(channel),false);
 fail=false;await p.save(channel);assert.match(p.notice(channel),new RegExp(channel+'.*saved'));for(const [i,other]of channels.entries())if(other!==channel)assert.deepEqual(p.draft(other),before[i]);
});
for(const channel of channels)for(const malformed of ['null','wrong-channel','missing-hidden'])test(`${channel} ${malformed} success cannot replace drafts or claim confirmation`,async()=>{
 const p=await page({onPut:({stored,commit})=>{commit();const row=malformed==='null'?null:malformed==='wrong-channel'?{...stored,channel:channel==='Email'?'WhatsApp':'Email'}:{...stored,replyToAddress:undefined};return{ok:true,status:200,json:async()=>row};}});
 for(const x of channels)p.edit(x,'senderName','Draft '+x);const before=channels.map(x=>structuredClone(p.draft(x)));await p.save(channel);
 assert.deepEqual(channels.map(x=>p.draft(x)),before);assert.doesNotMatch(p.notice(channel),/settings saved/);assert.match(p.notice(channel),/not.*confirm|could not.*confirm/i);for(const x of channels)assert.equal(p.disabled(x),true);
});
for(const channel of channels)test(`${channel} existing optional clear/defaults stay supported with other dirty drafts`,async()=>{
 const p=await page({rows:base.map(x=>({...x,senderName:null,status:'NotConfigured',messagesEnabled:false}))});for(const other of channels.filter(x=>x!==channel))p.edit(other,'senderName','Keep '+other);
 p.edit(channel,'senderName','   ');await p.save(channel);assert.equal(p.draft(channel)[channel==='Meeting'?'organizerName':'senderName'],'');for(const other of channels.filter(x=>x!==channel))assert.equal(p.draft(other)[other==='Meeting'?'organizerName':'senderName'],'Keep '+other);
});
for(const channel of channels)test(`${channel} historical real HTTP summary renders and preserves independent drafts`,async()=>{
 const log=fs.readFileSync(process.env.QA_CHANNEL_HISTORY_LOG||'QA/EVIDENCE/logs/phase-2b-communication-roundtrip-sql.log','utf8'),rows=JSON.parse(log.match(/^CHANNELROUNDTRIP FIXTURE (.+)$/m)[1]).rows,p=await page({rows});
 for(const other of channels.filter(x=>x!==channel))p.edit(other,'senderName','Keep captured '+other);p.edit(channel,'senderName','Saved captured '+channel);await p.save(channel);
 for(const other of channels.filter(x=>x!==channel))assert.equal(p.draft(other)[other==='Meeting'?'organizerName':'senderName'],'Keep captured '+other);
 const payload=JSON.parse(p.writes()[0].body),row=rows.find(x=>x.channel===channel);for(const key of ['replyToAddress','phoneNumber','externalAccountReference'])assert.equal(payload[key],row[key]);
});
