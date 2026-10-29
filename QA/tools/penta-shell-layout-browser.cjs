'use strict';
// Actual shell CSS in both source orders; synthetic markup, not portal E2E.
const fs=require('node:fs'),path=require('node:path'),a=require('node:assert/strict');
const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),globalCss=fs.readFileSync(path.join(root,'apps/web/src/app/globals.css'),'utf8');
const manual=/^\.enterprise-app-shell\s*\{[^}]+\}/m.exec(globalCss)?.[0];a.ok(manual);
const shell=fs.readFileSync(path.join(root,'apps/web/src/components/penta/penta-shell.module.css'),'utf8').replace(/:global\(([^)]+)\)/g,'$1');
const evidence=path.join(root,'QA/EVIDENCE','penta-shell-order-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
(async()=>{const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});const checks=[];
 try{for(const width of[320,768,1440])for(const theme of['light','dark'])for(const order of['module-first','global-first']){
  const page=await browser.newPage({viewport:{width,height:1000}});
  await page.route('**/*',route=>route.abort());
  await page.setContent(`<html data-theme="${theme}"><style>*{box-sizing:border-box}body{margin:0}.enterprise-workspace{min-width:0}button{min-height:44px}.enterprise-sidebar{width:100%}</style><style>${order==='module-first'?shell:manual}</style><style>${order==='module-first'?manual:shell}</style><div class="enterprise-app-shell aiShell"><aside class="enterprise-sidebar">Manual navigation</aside><section class="enterprise-workspace"><button>Send synthetic message</button></section></div></html>`);
  const data=await page.locator('.enterprise-app-shell').evaluate(element=>({shell:element.getBoundingClientRect().width,workspace:element.querySelector('.enterprise-workspace').getBoundingClientRect().width,sidebar:getComputedStyle(element.querySelector('.enterprise-sidebar')).display,columns:getComputedStyle(element).gridTemplateColumns}));
  a.equal(data.sidebar,'none');a.ok(data.workspace>=data.shell-3,'AI uses full shell width regardless of stylesheet order');
  await page.getByRole('button',{name:'Send synthetic message'}).click();
  await page.locator('.enterprise-app-shell').evaluate(element=>element.classList.remove('aiShell'));
  const fallback=await page.locator('.enterprise-app-shell').evaluate(element=>({sidebar:getComputedStyle(element.querySelector('.enterprise-sidebar')).display,columns:getComputedStyle(element).gridTemplateColumns}));
  a.notEqual(fallback.sidebar,'none','Manual navigation remains visible outside AI mode');a.equal(fallback.columns.split(' ').length,2,'Manual two-column layout retained');
  checks.push({width,theme,order,ai:data,manual:fallback});await page.close();
 }
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,productionApproved:false,scope:'real CSS/source-order synthetic markup; compiled portal checked separately'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
