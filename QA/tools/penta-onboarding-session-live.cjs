'use strict';
// Exported React workspace + actual api helper/native StorageEvents + real
// Identity/HTTP/disposable SQL reads. No synthetic API responses or model calls.
const a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http'),crypto=require('node:crypto');
const ts=require('../../apps/web/node_modules/typescript');
const{chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const[api,run]=process.argv.slice(2);a.match(api||'',/^http:\/\/127\.0\.0\.1:\d+$/);a.match(run||'',/^[0-9a-f]{32}$/);
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const sources=['apps/web/src/lib/api.ts','apps/web/src/lib/penta-onboarding-review.ts','apps/web/src/lib/penta-onboarding-channel.ts','apps/web/src/lib/penta-onboarding-operation.ts','apps/web/src/lib/penta-onboarding-dialogue.ts','apps/web/src/lib/penta-onboarding-start.ts','apps/web/src/components/penta/onboarding-review-workspace.tsx','apps/web/src/components/penta/onboarding-start-workspace.tsx'];
const helper='const process={env:{}};\n'+ts.transpileModule(fs.readFileSync(path.join(root,sources[0]),'utf8'),{compilerOptions:{module:ts.ModuleKind.ES2022,target:ts.ScriptTarget.ES2022}}).outputText;
const evidence=path.join(root,'QA/EVIDENCE','penta-onboarding-session-live-'+run);fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.woff2':'font/woff2','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 a.ok(['GET','HEAD'].includes(req.method));const pathname=new URL(req.url,'http://localhost').pathname;
 if(pathname==='/qa/api.js')return res.writeHead(200,{'Content-Type':'application/javascript','Cache-Control':'no-store'}).end(helper);
 // Peer control is a QA page, not an application route or native login acceptance.
 if(pathname==='/qa/peer')return res.writeHead(200,{'Content-Type':'text/html'}).end('<!doctype html><html><head><title>Private QA peer</title></head><body>Disposable session peer</body></html>');
 let file=path.resolve(web,'.'+decodeURIComponent(pathname));a.ok(file.startsWith(web+path.sep));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);a.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
const deferred=()=>{let resolve;const promise=new Promise(r=>resolve=r);return{promise,resolve};};
async function main(){let input='';for await(const chunk of process.stdin){input+=chunk;a.ok(input.length<65536);}const fixture=JSON.parse(input);input='';
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 const checks=[],browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 try{for(const state of['selected','pending'])for(const scenario of['peer','local','late']){
  const width=state==='selected'?320:1440,theme=state==='selected'?'light':'dark',target=fixture[state];
  const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[],hold=deferred(),observed=deferred();let replaced=false;
  try{
   await page.addInitScript(({token,theme})=>{localStorage.setItem('academydesk.accessToken.AcademyAdmin',token);localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','synthetic-original-login');localStorage.setItem('academydesk.theme',theme);},{token:fixture.tokens.admin,theme});
   page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{if(message.type()==='error')errors.push(message.text());});
   await context.route('**/*',async route=>{const request=route.request(),url=new URL(request.url());
    if(url.pathname.startsWith('/api/')){
     a.ok(['localhost','127.0.0.1'].includes(url.hostname));a.equal(request.method(),'GET');a.equal(replaced,false,'No new operation/read after replacement');
     const response=await route.fetch({url:api+url.pathname+url.search,maxRedirects:0,maxRetries:0});a.equal(response.status(),200);calls.push({path:url.pathname,method:request.method(),status:response.status()});
     if(scenario==='late'&&(state==='selected'?url.pathname.endsWith('/drafts/'+target.draft):url.pathname.endsWith('/current'))){observed.resolve();await hold.promise;}
     try{return await route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Accept'}});}catch(error){if(!replaced)throw error;return;}
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();throw Error('External request denied');
   });
   const goto=page.goto(`${origin}/penta/onboarding?conversationId=${target.conversation}&draftId=${target.draft}`);
   if(scenario==='late'){await observed.promise;await goto;}else{
    await goto;if(state==='selected')await page.getByRole('button',{name:/Work canvas/}).waitFor();else await page.getByRole('button',{name:'Check pending request',exact:true}).waitFor();
    await page.waitForLoadState('networkidle');
   }
   const count=calls.length;a.equal(count,state==='selected'?(scenario==='late'?3:4):2);
   const replace=async control=>control.evaluate(async()=>{const helper=await import('/qa/api.js');const token=helper.portalAccessToken('AcademyAdmin');helper.beginPortalSession('AcademyAdmin',token);});
   if(scenario==='local')await replace(page);else{const peer=await context.newPage();await peer.goto(origin+'/qa/peer');await replace(peer);}
   replaced=true;hold.resolve();
   await page.getByRole('heading',{name:'Session ended',exact:true}).waitFor();
   a.equal(await page.getByRole('button',{name:/Work canvas/}).count(),0);a.equal(await page.getByRole('button',{name:'Check pending request',exact:true}).count(),0);
   a.equal(await page.getByRole('button',{name:'Create approved student',exact:true}).count(),0);a.equal(await page.locator('input').count(),0);
   a.ok(!(await page.locator('body').innerText()).includes('SQL-REVIEW-ONLY'));a.equal(await page.getByRole('link',{name:'Sign in again',exact:true}).getAttribute('href'),'/login');
   await page.evaluate(()=>window.dispatchEvent(new Event('focus')));await page.waitForLoadState('networkidle');a.equal(calls.length,count);
   a.deepEqual(errors,[]);a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
   await page.screenshot({path:path.join(evidence,state+'-'+scenario+'.png')});
   checks.push({state,scenario,width,theme,passed:true,calls,errors,domainWrites:0,credentialsRecorded:false});
   console.log(`PASS: PENTAONBOARDINGSESSION ${state}/${scenario};${count}native GETs;no late adoption/read/replay`);
  }catch(error){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({state,scenario,calls,errors,error:error.message},null,2));throw error;}
  finally{hold.resolve();await context.close();}
 }
 a.equal(checks.length,6);a.equal(checks.reduce((sum,x)=>sum+x.calls.length,0),17);
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({run,passed:6,checks,sourcePins:Object.fromEntries(sources.map(file=>[file,crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex')])),sqlBacked:true,syntheticApiResponses:false,modelInference:false,browserDomainWrites:0,nativeLoginUiTested:false,productionApproved:false},null,2));
 }finally{await browser.close();await new Promise(resolve=>server.close(resolve));}
}
main().catch(error=>{console.error('PENTA session browser failed: '+error.name+': '+error.message);process.exitCode=1;server.close();});
