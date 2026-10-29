'use strict';
// Never installs through the worktree's node_modules junction into main.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),a=require('node:assert/strict');
const{spawn,execFileSync}=require('node:child_process');
const{sourceFiles,sourcePins,copyFiles,buildEnvironment,memoryGuard}=require('./default-web-build.cjs');
const root=path.resolve(__dirname,'../..'),base=path.join(root,'.build-check/web-dependency-candidate'),web=path.join(root,'apps/web');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
function candidateManifest(original){const proposed=structuredClone(original);proposed.dependencies.next='16.4.1';proposed.devDependencies['eslint-config-next']='16.4.1';a.equal(proposed.overrides,undefined);proposed.overrides={'source-map-js':'1.2.2'};return proposed;}
function validateManifest(original,candidate){a.deepEqual(candidate,candidateManifest(original));}
function environment(stage,offline){
 const env=buildEnvironment(process.env);return{...env,NODE_ENV:'development',npm_config_offline:offline?'true':'false',npm_config_registry:'https://registry.npmjs.org/',npm_config_cache:path.join(base,'npm-cache'),npm_config_userconfig:path.join(stage,'empty-user.npmrc'),npm_config_globalconfig:path.join(stage,'empty-global.npmrc'),npm_config_ignore_scripts:'true',npm_config_fund:'false',npm_config_audit:'false',TEMP:path.join(stage,'tmp'),TMP:path.join(stage,'tmp')};
}
function worktreeState(current,result){
 if(JSON.stringify(current)===JSON.stringify(result.originalSourcePins))return'original';
 a.equal(result.validated,true,'Only a successfully built exact candidate can match an applied worktree.');
 a.deepEqual(current,result.candidateSourcePins,'Worktree changed; do not overwrite unrelated edits.');return'exact-candidate';
}
function open(run){a.match(run,/^[a-f0-9]{32}$/);const stage=path.join(base,run),app=path.join(stage,'app'),evidence=path.join(root,'QA/EVIDENCE/web-dependency-candidate-'+run);for(const directory of[stage,app])a.equal(fs.realpathSync(directory).toLowerCase(),directory.toLowerCase());const file=path.join(evidence,'result.json'),result=JSON.parse(fs.readFileSync(file));a.equal(result.run,run);const state=worktreeState(sourcePins(web),result);return{stage,app,evidence,file,result,worktreeState:state};}
async function command(context,name,args,{offline=false,timeout=240000}={}){
 memoryGuard();const{app,evidence,stage,result,file}=context,npm=path.join(path.dirname(process.execPath),'node_modules/npm/bin/npm-cli.js');a.ok(fs.existsSync(npm));
 const env=environment(stage,offline);if(!['install','resolve'].includes(name)){env.NODE_ENV='production';env.npm_config_ignore_scripts='false';}
 const attempt=result.steps.filter(step=>step.name===name).length+1,logName=name+(attempt===1?'':'-'+attempt)+'.log';
 const log=fs.createWriteStream(path.join(evidence,logName),{flags:'wx'}),child=spawn(process.execPath,[npm,...args],{cwd:app,env,windowsHide:true,stdio:['ignore','pipe','pipe']});child.stdout.pipe(log,{end:false});child.stderr.pipe(log,{end:false});let timedOut=false;
 const timer=setTimeout(()=>{if(child.exitCode===null){timedOut=true;execFileSync('taskkill.exe',['/PID',String(child.pid),'/T','/F'],{windowsHide:true,stdio:'ignore'});}},timeout);
 const exitCode=await new Promise((resolve,reject)=>{child.on('error',reject);child.on('close',resolve);}).finally(()=>clearTimeout(timer));await new Promise(resolve=>log.end(resolve));
 result.steps.push({name,attempt,log:logName,exitCode,timedOut,offline,passed:exitCode===0&&!timedOut});a.deepEqual(sourcePins(web),result.originalSourcePins);fs.writeFileSync(file,JSON.stringify(result,null,2));a.equal(exitCode,0,'Candidate step failed; retained bounded log.');a.equal(timedOut,false);
}
async function run(){
 a.equal(root.toLowerCase(),'d:\\academydesk-codex-p0');const[mode,id]=process.argv.slice(2);
 if(mode==='--prepare'){
  a.equal(id,undefined);memoryGuard();const disk=fs.statfsSync('D:/');a.ok(Number(disk.bavail)*Number(disk.bsize)>5*1024**3);
  require('./api-image-payment-browser.cjs').verifyExport();const run=crypto.randomUUID().replaceAll('-',''),stage=path.join(base,run),app=path.join(stage,'app'),evidence=path.join(root,'QA/EVIDENCE/web-dependency-candidate-'+run);
  fs.mkdirSync(app,{recursive:true});fs.mkdirSync(path.join(stage,'tmp'));fs.mkdirSync(evidence,{recursive:true});copyFiles(sourceFiles(web),web,app);
  // These are newly created isolated config placeholders, never user config.
  fs.writeFileSync(path.join(stage,'empty-user.npmrc'),'');fs.writeFileSync(path.join(stage,'empty-global.npmrc'),'');
  const result={run,stage,originalSourcePins:sourcePins(web),originalManifest:JSON.parse(fs.readFileSync(path.join(web,'package.json'))),steps:[],productionApproved:false,mainDependencyMutation:false,applicationLogicChanged:false};fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify(result,null,2));console.log(JSON.stringify({run,stage,evidence,phase:'prepared-current-source-copy',manifestRequiresReviewedPatch:true}));return;
 }
 a.ok(['--resolve','--install','--validate'].includes(mode));const context=open(id),{app,result,file}=context;validateManifest(result.originalManifest,JSON.parse(fs.readFileSync(path.join(app,'package.json'))));
 if(mode==='--resolve')await command(context,'resolve',['install','--package-lock-only','--ignore-scripts','--no-audit','--no-fund','--registry=https://registry.npmjs.org/']);
 if(mode==='--install'){
  a.ok(result.steps.some(step=>step.name==='resolve'&&step.passed));await command(context,'install',['ci','--ignore-scripts','--no-audit','--no-fund','--registry=https://registry.npmjs.org/'],{timeout:360000});
 }
 if(mode==='--validate'){
  a.ok(result.steps.some(step=>step.name==='install'&&step.passed));
  for(const[name,version]of[['next','16.4.1'],['eslint-config-next','16.4.1'],['source-map-js','1.2.2']])a.equal(JSON.parse(fs.readFileSync(path.join(app,'node_modules',name,'package.json'))).version,version);
  a.ok(require(path.join(app,'node_modules/semver')).gte(JSON.parse(fs.readFileSync(path.join(app,'node_modules/sharp/package.json'))).version,'0.35.5'));
  await command(context,'typegen',['exec','--offline','--','next','typegen'],{offline:true});
  await command(context,'typecheck',['exec','--offline','--','tsc','--noEmit','--incremental','false'],{offline:true});
  await command(context,'lint',['run','lint'],{offline:true});
  await command(context,'build',['run','build'],{offline:true,timeout:600000});
  const log=fs.readFileSync(path.join(context.evidence,'build.log'),'utf8');a.ok(log.includes('Static segment aliases:'),'Real postbuild segment preparation must complete.');a.ok(fs.existsSync(path.join(app,'out/login.html')));a.ok(log.includes('16.4.1'));
  result.candidateSourcePins=sourcePins(app);result.candidateLockSha256=hash(fs.readFileSync(path.join(app,'package-lock.json')));result.staticPages=log.match(/\((\d+)\/(\d+)\) in/)?.slice(1).map(Number)||[];
  result.exportManifest=require('./default-web-build.cjs').inventory(path.join(app,'out')).map(({file,bytes})=>({path:path.relative(path.join(app,'out'),file).replaceAll('\\','/'),bytes,sha256:hash(fs.readFileSync(file))}));
  result.validated=true;fs.writeFileSync(file,JSON.stringify(result,null,2));
 }
 console.log(JSON.stringify({run:id,phase:mode,steps:result.steps,validated:result.validated||false,evidence:context.evidence}));
}
module.exports={candidateManifest,validateManifest,environment,worktreeState,open};if(require.main===module)run().catch(error=>{console.error(error.message.startsWith('3GiB')?error.message:'Candidate step rejected/failed; retained evidence, no main/worktree install.');process.exitCode=1;});
