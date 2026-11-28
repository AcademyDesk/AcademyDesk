'use strict';
// Inspection is the default. A separate, explicit, source-bound window approval
// permits at most three distinct validated sources. No host/domain dispatch.
const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process');
const crypto=require('node:crypto'),os=require('node:os'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),mini='D:/PENTA-Models/operations/mini-academy-conversation-20261123';
function selectProposal(file){const target=path.resolve(root,file);a.equal(path.dirname(target),path.join(root,'QA/HANDOFF'),'Unreviewed proposal path');
  const names={'D76_CORE_4B_REPAIR_WINDOW_2026-11-28.json':'d76-core-window','D77_REFERENCE_4B_REPAIR_WINDOW_2026-11-28.json':'d77-core-window'};
  a.ok(Object.hasOwn(names,path.basename(target)),'Unreviewed proposal file');return{file:target,directory:path.join(root,'.build-check',names[path.basename(target)])};}
const proposalAt=process.argv.indexOf('--proposal'),selected=selectProposal(proposalAt>=0?process.argv[proposalAt+1]:'QA/HANDOFF/D76_CORE_4B_REPAIR_WINDOW_2026-11-28.json');
const proposalFile=selected.file;
const proposal=JSON.parse(fs.readFileSync(proposalFile));
const probeFile=path.join(__dirname,'penta-connected-query-probe.py'),helperFile=path.join(__dirname,'penta-connected-query-evaluation.cjs');
const entryFile=path.join(__dirname,'penta-core-diagnostic-entry.py');
const{preserveContainerLogs}=require('./penta-connected-query-evaluation.cjs');
const coordination='D:/AcademyDesk-codex-p0/.build-check';
const windowDir=selected.directory;
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const read=file=>JSON.parse(fs.readFileSync(file));
const command=(file,args,options={})=>cp.execFileSync(file,args,{encoding:'utf8',windowsHide:true,timeout:30000,stdio:['ignore','pipe','pipe'],...options}).trim();
const docker=args=>command('docker',args),git=(cwd,args)=>command('git',args,{cwd});
const running=()=>docker(['ps','--no-trunc','--format','{{.ID}}']).split(/\r?\n/).filter(Boolean).sort();
const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
function footprint(directory){let bytes=0;for(const entry of fs.readdirSync(directory,{withFileTypes:true})){a.equal(entry.isSymbolicLink(),false);const file=path.join(directory,entry.name);bytes+=entry.isDirectory()?footprint(file):fs.statSync(file).size;}return bytes;}
function bindings(){return{scope:proposal.scope,proposalSha256:hash(fs.readFileSync(proposalFile)),runnerSha256:hash(fs.readFileSync(__filename)),probeSha256:hash(fs.readFileSync(probeFile)),helperSha256:hash(fs.readFileSync(helperFile)),entrySha256:hash(fs.readFileSync(entryFile)),miniBase:proposal.mini_base};}
function validateApproval(value){a.equal(value.approved,true,'Explicit repair-window approval missing');for(const[key,wanted]of Object.entries(bindings()))a.equal(value[key],wanted,'Repair-window binding changed: '+key);}
function validateAttempt(state,source,now,codeHash=source){
  a.ok(Array.isArray(state.attempts));a.ok(state.attempts.length<proposal.limits.max_attempts,'Three-attempt limit reached');
  a.ok(!state.attempts.some(row=>row.accepted===true),'Successful window is complete');
  a.ok(now<state.deadline,'Repair window expired');a.ok(!state.attempts.some(row=>row.mini===source),'Same-source retry prohibited');
  a.ok(!state.attempts.some(row=>row.sourceHash===codeHash),'Unchanged model-code retry prohibited');
}
function validationResult(manifest,source){
  a.equal(manifest.mini,source,'Validation is for another Mini commit');
  const report=fs.readFileSync(manifest.xml);a.equal(hash(report),manifest.xmlSha256);
  const suites=[...report.toString('utf8').matchAll(/<testsuite\b([^>]*)>/g)];a.equal(suites.length,1);
  const attrs=Object.fromEntries([...suites[0][1].matchAll(/([a-zA-Z]+)="([^"]*)"/g)].map(m=>[m[1],m[2]]));
  for(const key of ['failures','errors','skipped'])a.equal(Number(attrs[key]),0,'Model-free validation did not fully pass');
  a.ok(Number(attrs.tests)>=361,'Required selected model-free coverage missing');return Number(attrs.tests);
}
async function hashFile(file){const h=crypto.createHash('sha256');for await(const bytes of fs.createReadStream(file))h.update(bytes);return h.digest('hex');}
async function preflight(){
  a.equal(proposal.may_execute,false);a.equal(proposal.contract,'penta-core-repair-window.v1');
  a.equal(git(root,['branch','--show-current']),'codex/penta-host-integration-20261030');
  a.equal(git(mini,['branch','--show-current']),'codex/academy-conversation-core-20261123');
  git(mini,['merge-base','--is-ancestor',proposal.mini_base,'HEAD']);
  a.equal(git(mini,['status','--porcelain','--untracked-files=no']),'','Commit validated Mini changes before an attempt');
  const changed=git(mini,['diff','--name-only',proposal.mini_base,'HEAD']).split(/\r?\n/).filter(Boolean);
  a.ok(changed.every(file=>proposal.allowed_source_files.includes(file)||file.startsWith('tests/')||/^operations\/[^/]+\.md$/.test(file)||file==='HANDOFF_TO_ACADEMY_DESK.md'),'Source changes exceed this repair window');
  const source=git(mini,['rev-parse','HEAD']),weight=path.join('D:/PENTA-Models',proposal.model.relative_path);
  a.equal(fs.lstatSync(weight).isSymbolicLink(),false);a.equal(fs.statSync(weight).size,proposal.model.bytes);a.equal(await hashFile(weight),proposal.model.sha256);
  for(const image of [proposal.model.image,proposal.model.core_image])a.equal(JSON.parse(docker(['image','inspect',image]))[0].Id,image);
  const files=['academy_query.py','academy_workspace_api.py','task_inference.py','task_reply.py'];
  const sourceHash=hash(Buffer.concat(files.map(file=>fs.readFileSync(path.join(mini,'src/penta_mini',file)))));
  return{source,sourceHash,weight,existing:running(),freeBytes:os.freemem(),ramGuardPassed:os.freemem()>=proposal.limits.minimum_free_bytes_before_loading};
}
function secureDirectory(directory){fs.mkdirSync(directory,{recursive:true});command('icacls.exe',[directory,'/inheritance:r','/grant:r',command('whoami',[])+':(OI)(CI)(F)','*S-1-5-18:(OI)(CI)(F)']);}
async function execute(context,approvalFile,validationFile){
  const approval=read(approvalFile);validateApproval(approval);a.equal(context.ramGuardPassed,true,'5 GiB free RAM required before loading');
  const tests=validationResult(read(validationFile),context.source);secureDirectory(windowDir);
  const stateFile=path.join(windowDir,'state.json'),now=Date.now();
  const state=fs.existsSync(stateFile)?read(stateFile):{approval:bindings(),startedAt:now,deadline:now+proposal.limits.max_total_seconds*1000,attempts:[]};
  a.deepEqual(state.approval,bindings());validateAttempt(state,context.source,now,context.sourceHash);
  const run=crypto.randomUUID().replaceAll('-',''),folder=path.join(windowDir,run);secureDirectory(folder);
  fs.copyFileSync(proposalFile,path.join(folder,'proposal.json'));fs.copyFileSync(approvalFile,path.join(folder,'owner-approval.json'));fs.copyFileSync(__filename,path.join(folder,'executed-runner.cjs'));fs.copyFileSync(probeFile,path.join(folder,'probe.py'));
  fs.copyFileSync(entryFile,path.join(folder,'diagnostic-entry.py'));
  fs.writeFileSync(path.join(folder,'packet.json'),JSON.stringify({...proposal,expected_query_source:context.sourceHash},null,2));
  fs.copyFileSync(validationFile,path.join(folder,'validation.json'));
  const result={run,mini:context.source,querySource:context.sourceHash,academy:git(root,['rev-parse','HEAD']),startedAt:Date.now(),supervisorPid:process.pid,
    deadline:Math.min(state.deadline,now+proposal.limits.max_attempt_seconds*1000),tests,accepted:false,modelStarted:false,downloads:0,sql:false,azure:false,cleanupErrors:[]};
  state.attempts.push({run,mini:context.source,sourceHash:context.sourceHash,startedAt:now});fs.writeFileSync(stateFile,JSON.stringify(state,null,2));
  const resultFile=path.join(folder,'result.json'),ids={},network='penta-core-repair-'+run,label='penta.core.repair='+run;
  let networkId,monitor,aborted=false;
  const save=()=>fs.writeFileSync(resultFile,JSON.stringify({...result,ownedIds:ids,networkId},null,2));
  const owned=id=>{const c=JSON.parse(docker(['inspect',id]))[0];a.equal(c.Config.Labels['penta.core.repair'],run);a.ok(c.Name.startsWith('/'+network+'-'));return c;};
  const capture=()=>{result.logs={...result.logs,...preserveContainerLogs(ids,owned,folder)};};
  const stop=key=>{const id=ids[key]||docker(['ps','-aq','--no-trunc','--filter','label='+label,'--filter','name=^/'+network+'-'+key+'$']);if(!id)return;const c=owned(id);if(c.State.Running)docker(['stop','--timeout','3',id]);};
  const remove=key=>{stop(key);const id=ids[key]||docker(['ps','-aq','--no-trunc','--filter','label='+label,'--filter','name=^/'+network+'-'+key+'$']);if(id){owned(id);docker(['rm',id]);delete ids[key];}};
  const abort=reason=>{if(aborted)return;aborted=true;result.abort=reason;for(const key of ['probe','core','model'])try{stop(key);}catch{result.cleanupErrors.push(key);}capture();save();};
  const leaseFile=path.join(coordination,'qa-validation-window.lock'),leaseBytes=fs.readFileSync(leaseFile);
  const live=()=>{a.deepEqual(fs.readFileSync(leaseFile),leaseBytes);a.deepEqual(running().filter(id=>!Object.values(ids).includes(id)),context.existing,'Existing service inventory changed');};
  const flags=(key,memory)=>['create','--pull=never','--name',network+'-'+key,'--network',network,'--network-alias',key,'--label',label,'--read-only','--tmpfs','/tmp:rw,size=64m',
    '--memory',String(memory),'--memory-swap',String(memory),'--cpus',key==='model'?'4':'1','--pids-limit','128','--cap-drop','ALL','--security-opt','no-new-privileges',
    '--log-driver','json-file','--log-opt','max-size=1m','--log-opt','max-file=1'];
  try{
    live();save();const watcher=cp.spawn(process.execPath,[__filename,'--proposal',proposalFile,'--watchdog',resultFile],{detached:true,windowsHide:true,stdio:'ignore'});watcher.unref();
    monitor=setInterval(()=>{try{live();if(Date.now()>result.deadline)abort('Attempt/window deadline');if(os.freemem()<proposal.limits.minimum_free_safety_bytes)abort('768 MiB safety guard');
      if(footprint(windowDir)+9*1024*1024>proposal.limits.owned_disk_bytes)abort('Evidence disk cap');
    }catch{abort('Validation lease or existing services changed');}},1000);
    networkId=docker(['network','create','--internal','--label',label,network]);save();
    ids.model=docker([...flags('model',proposal.limits.model_memory_bytes),'--mount','type=bind,source='+context.weight+',target=/model/model.gguf,readonly',proposal.model.image,
      '-m','/model/model.gguf','-c','8192','-t','4','-ngl','0','--parallel','1','--reasoning','off','--host','0.0.0.0','--port','8080','--no-webui','--no-warmup']);save();
    const model=owned(ids.model);a.equal(model.Image,proposal.model.image);a.equal(model.Mounts.length,1);a.equal(model.Mounts[0].RW,false);a.deepEqual(model.HostConfig.PortBindings,{});
    a.ok(os.freemem()>=proposal.limits.minimum_free_bytes_before_loading);docker(['start',ids.model]);result.modelStarted=true;save();
    ids.core=docker([...flags('core',proposal.limits.core_memory_bytes),'--mount','type=bind,source='+path.join(mini,'src')+',target=/source,readonly',
      '--mount','type=bind,source='+folder+',target=/qa',
      '-e','PYTHONDONTWRITEBYTECODE=1','-e','PYTHONPATH=/source','-e','PENTA_LLAMA_URL=http://model:8080','-e','PENTA_ACADEMY_QUERIES_ENABLED=1','-e','PENTA_ACADEMY_TASKS_ENABLED=0',
      proposal.model.core_image,'python','-B','/qa/diagnostic-entry.py']);save();owned(ids.core);docker(['start',ids.core]);
    const until=Math.min(result.deadline,Date.now()+120000);let ready=false;
    while(Date.now()<until&&!aborted){try{docker(['exec',ids.core,'python','-c',"import urllib.request;urllib.request.urlopen('http://127.0.0.1:8000/v1/academy-query/readiness',timeout=3).read()"]);ready=true;break;}catch{await sleep(1000);}}
    a.equal(ready,true,'Core/model readiness failed');
    const props=JSON.parse(docker(['exec',ids.core,'python','-c',"import urllib.request;print(urllib.request.urlopen('http://model:8080/props',timeout=3).read().decode())"]));a.equal(props.default_generation_settings?.n_ctx,8192);
    result.actualContext=8192;save();
    ids.probe=docker([...flags('probe',proposal.limits.core_memory_bytes),'--mount','type=bind,source='+folder+',target=/qa,readonly','-e','PYTHONDONTWRITEBYTECODE=1',proposal.model.core_image,
      'python','-B','/qa/probe.py','--packet','/qa/packet.json','--base-url','http://core:8000']);save();owned(ids.probe);
    console.log(JSON.stringify({run,phase:'running',mini:context.source,folder,deadline:new Date(result.deadline).toISOString()}));
    const child=cp.spawn('docker',['start','--attach',ids.probe],{windowsHide:true,stdio:['ignore','pipe','pipe']});
    const stdout=fs.createWriteStream(path.join(folder,'semantic.jsonl')),stderr=fs.createWriteStream(path.join(folder,'probe-errors.log'));child.stdout.pipe(stdout);child.stderr.pipe(stderr);
    const exit=await new Promise((resolve,reject)=>{child.once('error',reject);child.once('close',resolve);});await Promise.all([new Promise(r=>stdout.end(r)),new Promise(r=>stderr.end(r))]);
    const rows=fs.readFileSync(path.join(folder,'semantic.jsonl'),'utf8').split(/\r?\n/).filter(Boolean).map(line=>JSON.parse(line));result.summary=rows.find(x=>x.summary)?.summary;
    result.accepted=exit===0&&result.summary?.accepted===true&&result.summary.executed===proposal.cases.length&&!aborted;
  }catch(error){result.failure=error.message;}
  finally{
    clearInterval(monitor);capture();
    for(const key of ['probe','core','model'])try{remove(key);}catch{result.cleanupErrors.push(key);}
    if(networkId)try{const n=JSON.parse(docker(['network','inspect',networkId]))[0];a.equal(n.Labels['penta.core.repair'],run);docker(['network','rm',networkId]);}catch{result.cleanupErrors.push('network');}
    try{result.servicesPreserved=JSON.stringify(running())===JSON.stringify(context.existing);}catch{result.servicesPreserved=false;}
    result.finishedAt=Date.now();save();state.attempts[state.attempts.length-1]={...state.attempts.at(-1),accepted:result.accepted,finishedAt:result.finishedAt};fs.writeFileSync(stateFile,JSON.stringify(state,null,2));
    console.log(JSON.stringify({run,accepted:result.accepted,summary:result.summary,failure:result.failure,abort:result.abort,logs:result.logs,cleanupErrors:result.cleanupErrors,servicesPreserved:result.servicesPreserved,folder}));
    if(!result.accepted||result.cleanupErrors.length||!result.servicesPreserved)process.exitCode=1;
  }
}
async function watchdog(file){
  const resolved=path.resolve(file);a.equal(path.dirname(path.dirname(resolved)),windowDir);a.equal(path.basename(resolved),'result.json');
  for(;;){await sleep(2000);let s;try{s=read(resolved);}catch{continue;}if(s.finishedAt)return;let alive=true;try{process.kill(s.supervisorPid,0);}catch{alive=false;}if(alive&&Date.now()<s.deadline)continue;
    const owned=id=>{const c=JSON.parse(docker(['inspect',id]))[0];a.equal(c.Config.Labels['penta.core.repair'],s.run);a.ok(c.Name.startsWith('/penta-core-repair-'+s.run+'-'));return c;};
    for(const id of Object.values(s.ownedIds||{}))try{const c=owned(id);if(c.State.Running)docker(['stop','--timeout','3',id]);}catch{}
    const logs=preserveContainerLogs(s.ownedIds||{},owned,path.dirname(resolved));
    for(const id of Object.values(s.ownedIds||{}))try{owned(id);docker(['rm',id]);}catch{}
    if(s.networkId)try{const n=JSON.parse(docker(['network','inspect',s.networkId]))[0];a.equal(n.Labels['penta.core.repair'],s.run);docker(['network','rm',s.networkId]);}catch{}
    fs.writeFileSync(path.join(path.dirname(resolved),'watchdog.json'),JSON.stringify({reason:alive?'deadline':'supervisor-exited',logs}));return;
  }
}
if(require.main===module)(async()=>{
  const watchdogAt=process.argv.indexOf('--watchdog');if(watchdogAt>=0)return watchdog(process.argv[watchdogAt+1]);
  const context=await preflight();if(!process.argv.includes('--execute')){console.log(JSON.stringify({prepared:true,...bindings(),mini:context.source,freeBytes:context.freeBytes,ramGuardPassed:context.ramGuardPassed,modelLoaded:false}));return;}
  const approvalAt=process.argv.indexOf('--approval'),validationAt=process.argv.indexOf('--validation');a.ok(approvalAt>=0&&validationAt>=0,'Approval and validation records required');
  await require('D:/AcademyDesk-codex-p0/QA/tools/local-validation-lease.cjs').withLease(coordination,'penta-core-repair',()=>execute(context,process.argv[approvalAt+1],process.argv[validationAt+1]));
})().catch(error=>{console.error(error.message);process.exitCode=1;});
module.exports={bindings,validateApproval,validateAttempt,validationResult,selectProposal};
