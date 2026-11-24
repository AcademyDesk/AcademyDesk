'use strict';
// Default is inspection ONLY. Execution needs a NEW approval bound to this packet
// and these exact evaluator bytes; the completed D65 approval is not accepted.
const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process');
const crypto=require('node:crypto'),os=require('node:os'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const mini=process.env.QA_MINI_WORKTREE||'D:/PENTA-Models/operations/mini-academy-conversation-20261123';
const packetFile=path.join(root,'QA/HANDOFF/D73_CONNECTED_4B_PACKET_2026-11-24.json');
const packet=JSON.parse(fs.readFileSync(packetFile));
const probeFile=path.join(__dirname,'penta-connected-query-probe.py');
const digest=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const command=(file,args,options={})=>cp.execFileSync(file,args,{encoding:'utf8',windowsHide:true,timeout:30000,stdio:['ignore','pipe','pipe'],...options}).trim();
const docker=args=>command('docker',args);
const git=(directory,args)=>command('git',args,{cwd:directory});
const leaseFile='D:/AcademyDesk-codex-p0/.build-check/qa-validation-window.lock';
const read=file=>JSON.parse(fs.readFileSync(file));
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
const fail=message=>{throw Error(message);};
const protect=directory=>command('icacls.exe',[directory,'/inheritance:r','/grant:r',command('whoami',[])+':(OI)(CI)(F)','*S-1-5-18:(OI)(CI)(F)']);
const running=()=>docker(['ps','--no-trunc','--format','{{.ID}}']).split(/\r?\n/).filter(Boolean).sort();
async function hashFile(file){const hash=crypto.createHash('sha256');for await(const bytes of fs.createReadStream(file))hash.update(bytes);return hash.digest('hex');}
function bindings(){return {scope:packet.scope,packetSha256:digest(fs.readFileSync(packetFile)),
  evaluatorSha256:digest(fs.readFileSync(__filename)),probeSha256:digest(fs.readFileSync(probeFile)),miniSha:packet.mini_source};}
async function preflight(){
  assert.equal(packet.may_execute,false);assert.equal(packet.contract,'penta-connected-query-evaluation.v1');
  assert.equal(git(root,['branch','--show-current']),'codex/penta-host-integration-20261030');
  git(root,['merge-base','--is-ancestor',packet.academy_code_source,'HEAD']);
  assert.equal(git(root,['diff',packet.academy_code_source,'--','apps/api','apps/web','QA/tools/SqlHarness']), '', 'Host runtime changed since packet');
  assert.equal(git(mini,['rev-parse','HEAD']),packet.mini_source);
  assert.equal(git(mini,['status','--porcelain','--untracked-files=no']),'','Mini tracked changes require a new packet');
  const weight=path.join('D:/PENTA-Models',packet.model.relative_path);
  assert.equal(fs.lstatSync(weight).isSymbolicLink(),false);assert.equal(fs.statSync(weight).size,packet.model.bytes);
  assert.equal(await hashFile(weight),packet.model.sha256,'Cached model hash changed');
  for(const image of [packet.model.image,packet.model.core_image])assert.equal(JSON.parse(docker(['image','inspect',image]))[0].Id,image);
  const active=read(path.join(root,'.build-check/ACTIVE_DEMO.json')),demo=read(path.join(active.folder,'result.json'));
  assert.equal(demo.ready,true);assert.equal(demo.synthetic,true);assert.equal(demo.azure,false);
  assert.equal(demo.source,packet.demo_backend_source);assert.equal(demo.run,active.run);
  assert.equal(demo.fixture.api,'http://127.0.0.1:54999');
  assert.ok(Date.parse(demo.expiresAtUtc)-Date.now()>packet.limits.max_automated_seconds*1000,'Demo expires too soon');
  assert.equal(fs.existsSync(active.stop),false,'Owner demo is stopping');
  const leaseBytes=fs.readFileSync(leaseFile),lease=JSON.parse(leaseBytes);assert.equal(lease.scope,'interactive-owner-demo');process.kill(lease.pid,0);
  const existing=running();assert.ok(existing.length>0,'Expected owner SQL is not running');
  const sqlName='academydesk-qa-'+demo.fixture.run.replaceAll('-','');
  const sql=JSON.parse(docker(['inspect',sqlName]))[0];assert.equal(sql.State.Running,true);
  assert.equal(sql.Config.Labels['academydesk.qa.run'],demo.fixture.run.replaceAll('-',''));
  assert.ok(existing.includes(sql.Id));
  const ports=command('powershell',['-NoProfile','-Command',"@(Get-NetTCPConnection -State Listen -LocalPort 49151 -ErrorAction SilentlyContinue).Count"]);
  assert.equal(ports,'0','Prepared Core port is occupied');
  const code=Object.fromEntries(['academy_query.py','academy_workspace_api.py','academy_tasks_api.py','task_inference.py','task_reply.py','api.py','inference.py','inference_gate.py'].map(file=>[file,digest(fs.readFileSync(path.join(mini,'src/penta_mini',file)))]));
  const querySource=digest(Buffer.concat(['academy_query.py','academy_workspace_api.py','task_inference.py','task_reply.py'].map(file=>fs.readFileSync(path.join(mini,'src/penta_mini',file)))));
  return {active,demo,leaseBytes,lease,existing,weight,code,querySource,approval:bindings(),freeBytes:os.freemem(),ramGuardPassed:os.freemem()>=packet.limits.minimum_free_bytes_before_loading};
}
function validateApproval(file,expected){const approval=read(file);assert.equal(approval.approved,true,'Explicit approval missing');for(const[key,value]of Object.entries(expected))assert.equal(approval[key],value,'Approval binding changed: '+key);}
async function run(context,approvalFile){
  validateApproval(approvalFile,context.approval);
  assert.equal(context.ramGuardPassed,true,'5 GiB free RAM guard: stop before model loading');
  const run=crypto.randomUUID().replaceAll('-',''),folder=path.join(root,'.build-check/d73-evaluation',run);
  fs.mkdirSync(folder,{recursive:true});protect(folder);
  fs.copyFileSync(packetFile,path.join(folder,'packet.json'));fs.copyFileSync(__filename,path.join(folder,'evaluator.cjs'));
  fs.writeFileSync(path.join(folder,'runtime-packet.json'),JSON.stringify({...packet,expected_query_source:context.querySource},null,2));
  fs.copyFileSync(probeFile,path.join(folder,'probe.py'));fs.copyFileSync(approvalFile,path.join(folder,'owner-approval.json'));
  const label='academydesk.query.eval='+run,network='academydesk-query-eval-'+run;
  const names={model:network+'-model',core:network+'-core',probe:network+'-probe'};
  const result={run,approval:context.approval,source:context.code,academy:git(root,['rev-parse','HEAD']),
    demoBackend:context.demo.source,startedAt:new Date().toISOString(),downloads:0,azure:false,modelLoaded:false,accepted:false,cleanupErrors:[]};
  const save=()=>{result.ownedIds={...ids};result.networkId=networkId;fs.writeFileSync(path.join(folder,'result.json'),JSON.stringify(result,null,2));};
  let networkId,monitor,deadline,aborted=false,automatedDone=false;
  const inspect=id=>{const c=JSON.parse(docker(['inspect',id]))[0];assert.equal(c.Config.Labels['academydesk.query.eval'],run);assert.ok(Object.values(names).includes(c.Name.slice(1)));return c;};
  const ids={};
  function stopOwned(key){const id=ids[key]||docker(['ps','-aq','--no-trunc','--filter','name=^/'+names[key]+'$','--filter','label='+label]);if(!id)return;const c=inspect(id);if(c.State.Running)docker(['stop','--timeout','3',c.Id]);docker(['rm',c.Id]);delete ids[key];}
  function abort(reason){if(aborted)return;aborted=true;result.abort=reason;save();for(const key of ['probe','core','model'])try{stopOwned(key);}catch{result.cleanupErrors.push(key);}}
  const live=()=>{assert.deepEqual(fs.readFileSync(leaseFile),context.leaseBytes);process.kill(context.lease.pid,0);
    assert.equal(fs.existsSync(context.active.stop),false);assert.ok(Date.now()<Date.parse(context.demo.expiresAtUtc));
    assert.deepEqual(running().filter(id=>!Object.values(ids).includes(id)),context.existing,'Existing running services changed');};
  const flags=(key,memory)=>['create','--pull=never','--name',names[key],'--network',network,'--network-alias',key,
    '--label',label,'--read-only','--tmpfs','/tmp:rw,size=64m','--memory',String(memory),'--memory-swap',String(memory),
    '--cpus',key==='model'?'4':'1','--pids-limit','128','--cap-drop','ALL','--security-opt','no-new-privileges',
    '--log-driver','json-file','--log-opt','max-size=1m','--log-opt','max-file=1'];
  function core(publish){
    const args=flags('core',packet.limits.core_memory_bytes);
    if(publish)args.push('-p','127.0.0.1:49151:8000');
    args.push('--mount','type=bind,source='+path.join(mini,'src')+',target=/source,readonly','-e','PYTHONPATH=/source','-e','PYTHONDONTWRITEBYTECODE=1',
      '-e','PENTA_LLAMA_URL=http://model:8080','-e','PENTA_ACADEMY_QUERIES_ENABLED=1','-e','PENTA_ACADEMY_TASKS_ENABLED=0',
      packet.model.core_image,'python','-B','-m','uvicorn','penta_mini.academy_workspace_api:app','--host','0.0.0.0','--port','8000','--no-access-log');
    ids.core=docker(args);save();assert.equal(inspect(ids.core).Image,packet.model.core_image);docker(['start',ids.core]);
  }
  async function ready(){const until=Date.now()+120000;do{live();if(aborted)fail('Evaluation aborted');try{
    docker(['exec',ids.core,'python','-c',"import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/v1/academy-query/readiness', timeout=3).read()"]);return;
  }catch{await sleep(1000);}}while(Date.now()<until);fail('Candidate startup/readiness deadline');}
  process.once('SIGINT',()=>abort('Owner interrupted'));process.once('SIGTERM',()=>abort('Owner terminated'));
  try{
    live();result.supervisorPid=process.pid;result.absoluteDeadline=Math.min(Date.now()+packet.limits.max_total_seconds*1000,Date.parse(context.demo.expiresAtUtc));save();
    const watcher=cp.spawn(process.execPath,[__filename,'--watchdog',folder],{detached:true,windowsHide:true,stdio:'ignore'});watcher.unref();
    deadline=setTimeout(()=>abort('One-hour absolute deadline'),packet.limits.max_total_seconds*1000);
    monitor=setInterval(()=>{try{live();if(os.freemem()<packet.limits.minimum_free_safety_bytes)abort('768 MiB RAM safety guard');
      if(!automatedDone&&Date.now()-Date.parse(result.startedAt)>packet.limits.max_automated_seconds*1000)abort('15-minute automated deadline');
      const bytes=fs.readdirSync(folder,{withFileTypes:true}).filter(x=>x.isFile()).reduce((sum,x)=>sum+fs.statSync(path.join(folder,x.name)).size,0);
      // Reserve three bounded container log files inside the same total cap.
      if(bytes+3*1024*1024>packet.limits.owned_disk_bytes)abort('Owned evidence disk bound');
    }catch{abort('Owner demo lease/expiry/service guard');}},1000);
    networkId=docker(['network','create','--internal','--label',label,network]);save();
    ids.model=docker([...flags('model',packet.limits.model_memory_bytes),'--mount','type=bind,source='+context.weight+',target=/model/model.gguf,readonly',
      packet.model.image,'-m','/model/model.gguf','-c',String(packet.model.context_tokens),'-t','4','-ngl','0','--parallel','1','--reasoning','off',
      '--host','0.0.0.0','--port','8080','--no-webui','--no-warmup']);
    const model=inspect(ids.model);assert.equal(model.Image,packet.model.image);assert.deepEqual(model.HostConfig.PortBindings,{});
    save();
    assert.equal(model.Mounts.length,1);assert.equal(model.Mounts[0].RW,false);assert.ok(os.freemem()>=packet.limits.minimum_free_bytes_before_loading,'RAM changed before load');
    docker(['start',ids.model]);result.modelLoaded=true;save();core(false);await ready();
    const props=JSON.parse(docker(['exec',ids.core,'python','-c',"import urllib.request; print(urllib.request.urlopen('http://model:8080/props', timeout=3).read().decode())"]));
    assert.equal(props.default_generation_settings?.n_ctx,packet.model.context_tokens,'Actual runtime context differs from approved profile');
    result.runtimeContext=props.default_generation_settings.n_ctx;save();
    ids.probe=docker([...flags('probe',packet.limits.core_memory_bytes),'--mount','type=bind,source='+folder+',target=/qa,readonly',
      '-e','PYTHONDONTWRITEBYTECODE=1',packet.model.core_image,'python','-B','/qa/probe.py','--packet','/qa/runtime-packet.json','--base-url','http://core:8000']);
    save();
    const probeOutput=cp.spawn('docker',['start','--attach',ids.probe],{windowsHide:true,stdio:['ignore','pipe','pipe']});
    const stdout=fs.createWriteStream(path.join(folder,'semantic.jsonl')),stderr=fs.createWriteStream(path.join(folder,'probe-errors.log'));
    probeOutput.stdout.pipe(stdout);probeOutput.stderr.pipe(stderr);
    const exit=await new Promise((resolve,reject)=>{probeOutput.once('error',reject);probeOutput.once('close',resolve);});
    await Promise.all([new Promise(resolve=>stdout.end(resolve)),new Promise(resolve=>stderr.end(resolve))]);
    assert.equal(exit,0,'Semantic packet failed; retain every failure and unrun case');
    assert.equal(inspect(ids.probe).State.ExitCode,0,'Probe container failed');
    const rows=fs.readFileSync(path.join(folder,'semantic.jsonl'),'utf8').trim().split(/\r?\n/).map(x=>JSON.parse(x));
    const summary=rows.find(x=>x.summary)?.summary;assert.equal(summary?.accepted,true);assert.equal(summary.executed,packet.cases.length);
    result.semantics=summary;stopOwned('probe');stopOwned('core');if(aborted)fail('Evaluation aborted');
    core(true);await ready();result.host=await hostChecks(context,folder);automatedDone=true;result.accepted=true;save();
    console.log(JSON.stringify({run,phase:'owner-preview-ready',url:'http://127.0.0.1:3001/login',expiresAt:new Date(Math.min(Date.parse(result.startedAt)+packet.limits.max_total_seconds*1000,Date.parse(context.demo.expiresAtUtc))).toISOString(),semanticCases:summary.executed,hostChecks:result.host}));
    const until=Math.min(Date.parse(result.startedAt)+packet.limits.max_total_seconds*1000,Date.parse(context.demo.expiresAtUtc));
    while(!aborted&&Date.now()<until){if(fs.existsSync(path.join(folder,'STOP')))break;await sleep(1000);}
  }catch(error){result.failure=error.message;console.log(JSON.stringify({run,phase:'failed',reason:error.message}));}
  finally{
    clearInterval(monitor);clearTimeout(deadline);
    try{result.cases=fs.readFileSync(path.join(folder,'semantic.jsonl'),'utf8').split(/\r?\n/).filter(Boolean).flatMap(line=>{try{const row=JSON.parse(line);return row.id?[{id:row.id,passed:row.passed,status:row.status,seconds:row.seconds}]:[];}catch{return[];}});}catch{result.cases=[];}
    result.unrun=packet.cases.filter(row=>!result.cases.some(done=>done.id===row.id)).map(row=>row.id);
    for(const key of ['probe','core','model'])try{stopOwned(key);}catch{result.cleanupErrors.push(key);}
    if(networkId)try{const n=JSON.parse(docker(['network','inspect',networkId]))[0];assert.equal(n.Labels['academydesk.query.eval'],run);docker(['network','rm',networkId]);}catch{result.cleanupErrors.push('network');}
    try{live();result.ownerDemoPreserved=true;}catch{result.ownerDemoPreserved=false;}
    result.finishedAt=new Date().toISOString();save();console.log(JSON.stringify({run,accepted:result.accepted,cleanupErrors:result.cleanupErrors,ownerDemoPreserved:result.ownerDemoPreserved,folder}));
    if(!result.accepted||result.cleanupErrors.length||!result.ownerDemoPreserved)process.exitCode=1;
  }
}
async function hostChecks(context,folder){
  const{demoPassword}=require('./interactive-demo-credentials.cjs');
  const api=context.demo.fixture.api,academy=context.demo.fixture.academyId;
  async function login(role,email){const response=await fetch(api+'/api/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email,password:demoPassword(context.active,role)}),signal:AbortSignal.timeout(20000)});assert.equal(response.status,200);return(await response.json()).accessToken;}
  const token=await login('owner','owner@demo.example.invalid');
  const call=async(suffix,options={})=>fetch(api+suffix,{...options,headers:{'Content-Type':'application/json',Authorization:'Bearer '+token,...options.headers},signal:AbortSignal.timeout(130000)});
  const root='/api/academies/'+academy,queries=root+'/penta/queries';
  const health=await call(queries+'/health');assert.equal(health.status,200);const catalog=await health.json();assert.equal(catalog.tools.length,10);
  const teachers=await call(root+'/teachers');assert.equal(teachers.status,200);const authoritative=await teachers.json();
  const created=await call(queries+'/conversations',{method:'POST'});assert.equal(created.status,201);const session=await created.json();
  let version=0,last;
  for(const[text,entity]of [['Show the students in my academy.','student'],['I said teachers','teacher'],['Open the first result','teacher']]){
    const requestId=crypto.randomUUID();const response=await call(queries+'/conversations/'+session.conversationId+'/turns',{method:'POST',body:JSON.stringify({requestId,expectedVersion:version,text})});
    assert.equal(response.status,201);last=await response.json();assert.equal(last.kind,'RESULT');assert.equal(last.result.entity,entity);assert.equal(last.version,++version);
    if(entity==='teacher'){assert.ok(last.result.records.length>0);assert.ok(last.result.records.every(row=>authoritative.some(t=>t.id===row.id&&row.label===`${t.firstName} ${t.lastName}`.trim())));}
    const recovery=await call(queries+'/conversations/'+session.conversationId+'/turns/'+requestId);assert.equal(recovery.status,200);assert.deepEqual(await recovery.json(),last);
  }
  const teacher=await login('teacher','teacher@demo.example.invalid');
  const denied=await call(queries+'/conversations',{method:'POST',headers:{Authorization:'Bearer '+teacher}});assert.ok([403,404].includes(denied.status));
  fs.writeFileSync(path.join(folder,'host-receipt.json'),JSON.stringify(last,null,2));
  return {turns:3,teacherIdsMatchAuthoritativeApi:true,getRecoveries:3,teacherDenied:denied.status,backendSource:context.demo.source};
}
async function watchdog(directory){
  const parent=path.join(root,'.build-check/d73-evaluation');assert.equal(path.dirname(path.resolve(directory)),path.resolve(parent));
  for(;;){await sleep(2000);let state;try{state=read(path.join(directory,'result.json'));}catch{continue;}if(state.finishedAt)return;
    let alive=true;try{process.kill(state.supervisorPid,0);}catch{alive=false;}
    if(alive&&Date.now()<state.absoluteDeadline)continue;
    const failures=[];const owned=docker(['ps','-aq','--no-trunc','--filter','label=academydesk.query.eval='+state.run]).split(/\r?\n/).filter(Boolean);
    for(const id of owned)try{const c=JSON.parse(docker(['inspect',id]))[0];assert.equal(c.Config.Labels['academydesk.query.eval'],state.run);assert.ok(c.Name.startsWith('/academydesk-query-eval-'+state.run+'-'));if(c.State.Running)docker(['stop','--timeout','3',id]);docker(['rm',id]);}catch{failures.push(id);}
    if(state.networkId)try{const n=JSON.parse(docker(['network','inspect',state.networkId]))[0];assert.equal(n.Labels['academydesk.query.eval'],state.run);docker(['network','rm',state.networkId]);}catch{failures.push('network');}
    fs.writeFileSync(path.join(directory,'watchdog.json'),JSON.stringify({at:new Date().toISOString(),reason:alive?'deadline':'supervisor-exited',failures}));return;
  }
}
if(require.main===module){(async()=>{const watchdogAt=process.argv.indexOf('--watchdog');if(watchdogAt>=0){await watchdog(process.argv[watchdogAt+1]);return;}
  const context=await preflight();if(!process.argv.includes('--execute')){console.log(JSON.stringify({preflight:true,approvalRequired:true,...context.approval,freeBytes:context.freeBytes,ramGuardPassed:context.ramGuardPassed,modelLoaded:false,source:context.code}));return;}
  const at=process.argv.indexOf('--approval');assert.ok(at>=0&&process.argv[at+1],'Bound NEW explicit approval file is required');await run(context,process.argv[at+1]);
})().catch(error=>{console.error(error.message);process.exitCode=1;});}
module.exports={bindings,validateApproval};
