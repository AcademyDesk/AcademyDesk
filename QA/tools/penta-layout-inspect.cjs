'use strict';
// Synthetic, read-only geometry diagnostic. No model/SQL/authentication proof.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),a=require('node:assert/strict');
const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),run=process.argv[2];a.match(run,/^[0-9a-f]{32}$/);
const web=fs.realpathSync(path.join(root,'.build-check/default-web-build',run,'app/out'));
const evidence=path.join(root,'QA/EVIDENCE','penta-layout-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const server=http.createServer((req,res)=>{try{
 let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));a.ok(file.startsWith(web+path.sep));
 if(!path.extname(file))file+='.html';file=fs.realpathSync(file);a.ok(file.startsWith(web+path.sep));
 res.writeHead(200,{'Content-Type':({'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml'})[path.extname(file)]||'application/octet-stream'});fs.createReadStream(file).pipe(res);
 }catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 try{
 const context=await browser.newContext({viewport:{width:1440,height:1000}}),page=await context.newPage();
 const academy='11111111-1111-4111-8111-111111111111';
 await context.addInitScript(()=>{localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-test-token');localStorage.setItem('academydesk.theme','light');});
 await context.route('**/*',route=>{const url=new URL(route.request().url());
  if(url.pathname.startsWith('/api/')){
   let value=[];
   if(url.pathname==='/api/auth/session')value={academyId:academy,displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false};
   else if(url.pathname==='/api/academies')value=[{name:'Synthetic academy',enabledModulesJson:'["Core","Finance"]'}];
   else if(url.pathname.endsWith('/academy-context'))value={academyId:academy,name:'Synthetic academy',timeZone:'Asia/Kolkata'};
   else if(url.pathname.endsWith('/chat/health'))value={status:'Available',provider:'PENTA Mini',protocol:'0.1',readOnly:true};
   return route.fulfill({contentType:'application/json',headers:{'Access-Control-Allow-Origin':origin},body:JSON.stringify(value)});
  }
  if(url.origin===origin)return route.continue();return route.abort();
 });
 await page.goto(origin+'/penta');await page.getByText('● Mini connected').waitFor();
 if(process.argv[3]==='--candidate'){
  const shell=await page.locator('.enterprise-app-shell').getAttribute('class');
  const local=shell.split(' ').find(name=>name.includes('__aiShell'));
  a.ok(local);await page.addStyleTag({content:`.${local}.enterprise-app-shell{grid-template-columns:minmax(0,1fr)}`});
 }
 await page.getByLabel('Message PENTA AI',{exact:true}).fill('Synthetic layout diagnostic');
 const data=await page.evaluate(()=>{
  const button=document.querySelector('section[aria-label="PENTA conversation"] button[type="submit"]');
  const targets=[];for(let el=button;el;el=el.parentElement){const css=getComputedStyle(el),box=el.getBoundingClientRect();targets.push({tag:el.tagName,className:el.className,hidden:el.hidden,rect:{x:box.x,y:box.y,width:box.width,height:box.height},display:css.display,position:css.position,gridTemplateColumns:css.gridTemplateColumns,gridColumn:css.gridColumn,overflow:css.overflow,transform:css.transform,visibility:css.visibility});}
  const selectors=[];for(const sheet of document.styleSheets){try{for(const rule of sheet.cssRules){if(rule.selectorText&&[button,...button.parentElement.parentElement.parentElement.parentElement.children].some(el=>el.matches(rule.selectorText)))selectors.push({file:new URL(sheet.href||location.href).pathname,selector:rule.selectorText,css:rule.style.cssText});}}catch{}}
  return{targets,selectors,viewport:{width:innerWidth,height:innerHeight},scroll:{x:scrollX,y:scrollY}};
 });
 await page.screenshot({path:path.join(evidence,'desktop.png'),fullPage:true});fs.writeFileSync(path.join(evidence,'geometry.json'),JSON.stringify(data,null,2));console.log(JSON.stringify({evidence,candidate:process.argv[3]==='--candidate',targets:data.targets}));
 if(process.argv[3]==='--candidate'){a.ok(data.targets.find(item=>item.tag==='SECTION').rect.width>=700);a.ok(data.targets.find(item=>item.className.includes('enterprise-workspace')).rect.width>=1300);}
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);server.close();process.exitCode=1;});
