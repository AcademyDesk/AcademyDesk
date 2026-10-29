'use strict';
const a=require('node:assert/strict');
const cases=[
 {name:'malformed-enabled',options:{enabled:'synthetic-invalid-flag'},field:'Enabled'},
 {name:'unprovisioned-directory',options:{directory:'/qa-private/unprovisioned'},field:'KeyRingDirectory'},
 {name:'application-directory',options:{directory:'/app/keys'},field:'KeyRingDirectory'},
 {name:'missing-certificate',options:{certificate:'/qa-private/certs/missing.pfx'},field:'Certificate'},
 {name:'wrong-certificate-password',options:{wrongPassword:true},field:'Certificate'}
];
function verifyFailure(state,log,field,privateValues){
 a.equal(state.Running,false);a.equal(state.OOMKilled,false);a.ok(Number.isInteger(state.ExitCode)&&state.ExitCode!==0,'Misconfigured API must exit, not serve with fallback.');
 a.ok(log.includes('Security:DataProtection:'+field)&&log.includes('No fallback is permitted.'));
 for(const value of privateValues.filter(Boolean))a.equal(log.includes(value),false,'Startup failure must not echo configured secret/path values.');
 return{exited:true,exitCode:state.ExitCode,notOom:true,safeConfigurationFailure:true};
}
async function verifyConfiguration({result,probe,restore}){
 const facts=result.keyConfiguration={passed:false,cases:[],syntheticOnly:true};
 for(const row of cases){const outcome=await probe(row);a.equal(outcome.safeConfigurationFailure,true);facts.cases.push({name:row.name,...outcome});result.checks.push({label:'encrypted-key fail-closed '+row.name,kind:'startup',passed:true});}
 await restore();facts.passed=true;
}
module.exports={cases,verifyFailure,verifyConfiguration};
