/* Read-only infrastructure probes. Does not boot the app or create/reset a DB. */
const fs=require('node:fs'),path=require('node:path'),cp=require('node:child_process'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');
function probe(program,args){
 const r=cp.spawnSync(program,args,{cwd:root,encoding:'utf8',timeout:15000,windowsHide:true});
 return {program,args,exitCode:r.status,stdout:(r.stdout||'').trim(),stderr:(r.stderr||'').trim(),error:r.error?.message};
}
const result={
 checkedAt:new Date().toISOString(),scope:'Infrastructure availability only; no application test PASS',
 git:probe('git',['rev-parse','HEAD']),
 sql:probe('sqlcmd',['-S','localhost','-E','-C','-l','5','-t','5','-W','-Q',"SET NOCOUNT ON; SELECT CONVERT(varchar(32),SERVERPROPERTY('ProductVersion')) AS ProductVersion, CONVERT(varchar(80),SERVERPROPERTY('Edition')) AS Edition, HAS_PERMS_BY_NAME(NULL, NULL, 'CREATE ANY DATABASE') AS CanCreateDatabase;"]),
 dockerContext:probe('docker',['context','show']),
 dockerServer:probe('docker',['version','--format','{{json .Server}}']),
 limitations:['No user tables read; only localhost server metadata queried.','CREATE ANY DATABASE permission does not prove isolation or authorize resetting any existing database.','No test DB, test credentials, browser harness or host created.']
};
const evidence=path.join(root,'QA/EVIDENCE/logs');
fs.writeFileSync(path.join(evidence,'phase1-readiness.json'),JSON.stringify(result,null,2)+'\n');
const diff=cp.execFileSync('git',['diff','--no-ext-diff','--','apps/web/src/app/student-onboarding/page.tsx','apps/web/src/app/student-profile/page.tsx'],{cwd:root,encoding:'utf8'});
fs.writeFileSync(path.join(evidence,'phase1-preexisting-ui.patch'),diff);
const hashes=['phase1-existing.trx','eslint.json','observed-checks.json','phase1-readiness.json','phase1-preexisting-ui.patch'].map(file=>({file,sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(evidence,file))).digest('hex')}));
fs.writeFileSync(path.join(evidence,'phase1-evidence-hashes.json'),JSON.stringify({timestamp:new Date().toISOString(),hashes},null,2)+'\n');
console.log(JSON.stringify(result,null,2));
