'use strict';
// Existing bounded frontend-only validation authority. Never reruns SQL/model
// work or installs packages; restores the exact two original services in finally.
const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process'),crypto=require('node:crypto'),os=require('node:os'),a=require('node:assert/strict');
const root=path.resolve(__dirname,'../..'),sleep=ms=>new Promise(r=>setTimeout(r,ms));
const originals=[['pentaaimodels-api-1','978357d1b07fc4e52fddd2530f4c485a1bbe34858a199de4e752bda621131278','sha256:c7eb72ae4cc71208006b57e32122165f4f45b4e9c88d709b8ce05d28ceed6bdf'],['pentaaimodels-inference-1','c06a0392290ce1b9198ff49e2bf6d1e9d4c2891b88e115b40e0329dda580aca2','sha256:559ac229adefe0f7e2d4e32f5222f26927b6bb8ba44b9db08e2544afebf41984']];
const docker=args=>cp.execFileSync('docker',args,{windowsHide:true,encoding:'utf8',timeout:30000,stdio:['ignore','pipe','pipe']}).trim();
const inventory=()=>Object.fromEntries([['containers',['ps','-aq','--no-trunc']],['volumes',['volume','ls','-q']],['networks',['network','ls','-q','--no-trunc']],['images',['image','ls','-aq','--no-trunc']]].map(([k,args])=>[k,[...new Set(docker(args).split(/\r?\n/).filter(Boolean))].sort()]));
const files=['QA/tools/teacher-frontend-window.cjs','apps/web/src/lib/penta-teacher-operation.ts','apps/web/src/lib/api.ts','apps/web/src/lib/penta-chat-contract.ts','apps/web/src/lib/penta-onboarding-channel.ts','QA/tools/penta-teacher-operation.test.cjs','apps/web/tsconfig.json','apps/web/package-lock.json',
 'apps/web/src/app/penta/teachers/page.tsx','apps/web/src/components/penta/teacher-review-workspace.tsx','apps/web/src/components/penta/teacher-review.module.css','apps/web/src/components/workspace-frame.tsx',
 ...(process.argv.includes('--teacher-start')?['apps/web/src/lib/penta-teacher-start.ts','QA/tools/penta-teacher-start.test.cjs']:[]),
 ...(process.argv.includes('--teacher-chat')?['apps/web/src/lib/penta-teacher-dialogue.ts','QA/tools/penta-teacher-dialogue.test.cjs','apps/web/src/components/penta/teacher-chat-workspace.tsx','apps/web/src/components/penta/teacher-chat.module.css','apps/web/src/components/penta/teacher-start-workspace.tsx','apps/web/src/app/penta/teachers/chat/page.tsx','apps/web/src/components/penta/penta-chat-workspace.tsx']:[])];
const pins=()=>files.map(p=>({path:p,sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex')}));
async function work(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');const start=process.argv[3]==='--teacher-start',chat=start&&process.argv[4]==='--teacher-chat',build=process.argv[chat?5:start?4:3]==='--build';a.deepEqual(process.argv.slice(2),['--validate',...(start?['--teacher-start']:[]),...(chat?['--teacher-chat']:[]),...(build?['--build']:[])]);
 a.deepEqual(docker(['ps','--format','{{.Names}}']).split(/\r?\n/).sort(),originals.map(x=>x[0]).sort());
 for(const[name,id,image]of originals){const s=JSON.parse(docker(['inspect',id]))[0];a.equal(s.Name,'/'+name);a.equal(s.Image,image);a.equal(s.State.Running,true);}
 const run=crypto.randomUUID().replaceAll('-',''),dir=path.join(root,'QA/EVIDENCE/teacher-frontend-'+run);fs.mkdirSync(dir);
 const result={run,passed:false,steps:[],sourcePins:pins(),apiSourcePins:require('./api-image-build.cjs').inputs(),before:inventory(),productionApproved:false,modelLoaded:false,modelInference:false,downloaded:false};
 const save=()=>fs.writeFileSync(path.join(dir,'result.json'),JSON.stringify(result,null,2));save();let stopped=false;
 async function guard(){if(global.gc)global.gc();const end=Date.now()+45000;while(os.freemem()<3*1024**3&&Date.now()<end)await sleep(2000);a.ok(os.freemem()>=3*1024**3,'Unchanged3GiB guard refused');}
 async function child(name,args,cwd,limit){await guard();console.log(JSON.stringify({run,phase:name}));const log=fs.createWriteStream(path.join(dir,name+'.log'));let tail='';
  const p=cp.spawn(process.execPath,args,{cwd,windowsHide:true,env:{...process.env,...(name==='frontend-start-checks'?{QA_PENTA_TEACHER_START_OUTPUT:path.join(dir,'start-transport.json')}:{})},stdio:['ignore','pipe','pipe']});p.stdout.on('data',b=>tail=(tail+b.toString()).slice(-1048576));p.stdout.pipe(log,{end:false});p.stderr.pipe(log,{end:false});let timeout=false;
  const timer=setTimeout(()=>{if(p.exitCode===null){timeout=true;cp.execFileSync('taskkill.exe',['/PID',String(p.pid),'/T','/F'],{windowsHide:true,stdio:'ignore'});}},limit);
  const status=await new Promise((resolve,reject)=>{p.on('error',reject);p.on('close',resolve);}).finally(()=>clearTimeout(timer));await new Promise(r=>log.end(r));result.steps.push({name,exitCode:status,timedOut:timeout});save();
  a.equal(status,0,name+' failed; log retained');a.equal(timeout,false);a.deepEqual(pins(),result.sourcePins);return tail;
 }
 try{stopped=true;for(const[,id]of originals)docker(['stop','--time','15',id]);await guard();result.admissionFreeBytes=os.freemem();
  const web=path.join(root,'apps/web'),checks=await child('frontend-checks',['--max-old-space-size=768','--test','--test-reporter=tap','QA/tools/penta-teacher-operation.test.cjs'],root,60000);
  a.match(checks,/# tests 52/);a.match(checks,/# fail 0/);result.clientChecks=52;
  if(start){const output=await child('frontend-start-checks',['--max-old-space-size=768','QA/tools/penta-teacher-start.test.cjs'],root,60000);a.deepEqual(JSON.parse(output.trim().split(/\r?\n/).at(-1)),{passed:61,failed:0});result.startClientChecks=61;}
  if(chat){const output=await child('frontend-dialogue-checks',['--max-old-space-size=768','--test','--test-reporter=tap','QA/tools/penta-teacher-dialogue.test.cjs'],root,60000);a.match(output,/# tests 54/);a.match(output,/# fail 0/);result.dialogueClientChecks=54;}
  await child('frontend-types',['--max-old-space-size=1536',path.join(web,'node_modules/typescript/bin/tsc'),'--noEmit','--incremental','false'],web,180000);
  await child('frontend-lint',['--max-old-space-size=768',path.join(web,'node_modules/eslint/bin/eslint.js'),'src/lib/penta-teacher-operation.ts','src/app/penta/teachers/page.tsx','src/components/penta/teacher-review-workspace.tsx','src/components/workspace-frame.tsx',...(start?['src/lib/penta-teacher-start.ts']:[]),...(chat?['src/lib/penta-teacher-dialogue.ts','src/app/penta/teachers/chat/page.tsx','src/components/penta/teacher-chat-workspace.tsx','src/components/penta/teacher-start-workspace.tsx','src/components/penta/penta-chat-workspace.tsx']:[]),'--max-warnings=0'],web,180000);
  if(build){const output=await child('frontend-build',['--max-old-space-size=1536',path.join(root,'QA/tools/default-web-build.cjs')],root,900000);const proof=JSON.parse(output.trim().split(/\r?\n/).at(-1));a.equal(proof.passed,true);result.webBuild=proof.run;}
  result.validationComplete=true;
 }catch(error){result.failure=String(error.message).slice(0,180);}
 finally{
  if(stopped)for(const[,id,image]of [...originals].reverse()){const s=JSON.parse(docker(['inspect',id]))[0];a.equal(s.Image,image);if(!s.State.Running)docker(['start',id]);}
  const end=Date.now()+120000;do{result.restored=originals.map(([name,id,image])=>{const s=JSON.parse(docker(['inspect',id]))[0];return{name,id:s.Id,image:s.Image,running:s.State.Running,health:s.State.Health?.Status||null,expectedImage:image};});if(result.restored.every(s=>s.running&&s.image===s.expectedImage&&(!s.health||s.health==='healthy')))break;await sleep(2000);}while(Date.now()<end);
  result.restorationPassed=result.restored.every(s=>s.running&&s.image===s.expectedImage&&(!s.health||s.health==='healthy'));result.after=inventory();result.inventoryPreserved=JSON.stringify(result.before)===JSON.stringify(result.after);
  result.sourceUnchanged=JSON.stringify(pins())===JSON.stringify(result.sourcePins)&&JSON.stringify(require('./api-image-build.cjs').inputs())===JSON.stringify(result.apiSourcePins);
  result.passed=!!(result.validationComplete&&result.restorationPassed&&result.inventoryPreserved&&result.sourceUnchanged);save();console.log(JSON.stringify({run,passed:result.passed,steps:result.steps,restorationPassed:result.restorationPassed,inventoryPreserved:result.inventoryPreserved,failure:result.failure,evidence:dir}));if(!result.passed)process.exitCode=1;
 }
}
require('./local-validation-lease.cjs').withLease(path.join(root,'.build-check'),'teacher-frontend',work).catch(error=>{console.error('Teacher frontend preflight refused: '+String(error.message).slice(0,180));process.exitCode=1;});
