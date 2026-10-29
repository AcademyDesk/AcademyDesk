// Production export + synthetic transport. Not SQL, real authentication or device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const {verifyVendor,assertManual}=require('./penta-foundation-contract.cjs'),pick=require('./shared-picker-actions.cjs');
verifyVendor();
// All records and action responses below are synthetic, not real delivery or permissions.
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out')),evidence=path.join(root,'QA/EVIDENCE/meeting-integration-options-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{assert.ok(['GET','HEAD'].includes(req.method));let f=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(f))f+='.html';const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(f));if(!fs.existsSync(f)&&segment){const p=segment[1].split('.');f=path.join(path.dirname(f),'__next.'+p[0],...p.slice(1),'__PAGE__.txt');}f=fs.realpathSync(f);assert.ok(f.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(f)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(f).pipe(res);}catch{res.writeHead(404).end();}});

(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser;
 try{
  browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  async function run(width,theme,state){
   const context=await browser.newContext({viewport:{width,height:950}}),page=await context.newPage(),calls=[],errors=[];let release;
   const gate=new Promise(r=>release=r);
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-meeting-options');},theme);
   page.on('pageerror',e=>errors.push(e.message));
   await context.route('**/*',async route=>{
    const req=route.request(),u=new URL(req.url());
    if(u.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(u.hostname)){
     const headers={'Access-Control-Allow-Origin':origin,'Access-Control-Allow-Headers':'*','Access-Control-Allow-Methods':'GET,OPTIONS'};
     if(req.method()==='OPTIONS')return route.fulfill({status:204,headers});
     calls.push({path:u.pathname,method:req.method()});assert.equal(req.method(),'GET','integration options cannot write/connect');
     if(u.pathname.endsWith('/meetings/capabilities')){
      if(state==='loading')await gate;
      if(state==='error'||state==='denied')return route.fulfill({status:state==='error'?503:403,headers,body:'{}'});
      const data=require('./meeting-integration-fixture.cjs')();if(state==='malformed')data.providers[1]=data.providers[0];if(state==='claimed-ready')data.providers[0].available=true;
      return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data)});
     }
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Engagement"]'}],'/api/auth/session':{workspace:'AcademyAdmin',academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],'/api/academies/owned/communication-settings':require('./communication-page-harness.cjs').base};
     assert.ok(Object.hasOwn(data,u.pathname),u.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[u.pathname])});
    }
    if(u.origin===origin||u.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   try{
    await page.goto(origin+'/communication-settings');await assertManual(page,theme);
    const hub=page.getByRole('region',{name:'Three providers. One academy workspace.'});
    const expected=state==='loading'?'Checking availability':state==='denied'?'Access unavailable':state==='normal'?'Secure setup not enabled':'Status could not be verified';
    await hub.getByRole('status').filter({hasText:expected}).waitFor();
    assert.equal(await hub.locator('[data-meeting-integration]').count(),3);
    for(const provider of['Google Meet','Microsoft Teams','Zoom']){
     const card=hub.locator('article').filter({has:page.getByRole('heading',{name:provider,exact:true})});
     assert.ok(await card.getByRole('button',{name:'Connect '+provider,exact:true}).isDisabled());
     assert.ok(await card.getByRole('button',{name:'Reconnect '+provider,exact:true}).isDisabled());
     assert.ok(await card.getByRole('button',{name:'Disconnect '+provider,exact:true}).isDisabled());
     const summary=card.locator('summary');await summary.focus();await summary.press('Enter');
     assert.ok(await card.locator('details').evaluate(e=>e.open));assert.equal(await card.locator('ol li').count(),3);
     assert.match(await card.getByRole('link').getAttribute('href'),/^https:\/\/(developers\.google\.com|learn\.microsoft\.com|developers\.zoom\.us)\//);
     await summary.press('Enter');assert.equal(await card.locator('details').evaluate(e=>e.open),false);
    }
    const refresh=hub.getByRole('button',{name:'Refresh availability'});
    if(state==='loading')assert.ok(await refresh.isDisabled());
    else{
     await refresh.click();await page.waitForFunction(()=>!document.querySelector('[aria-labelledby="meeting-integrations-title"]')?.getAttribute('aria-busy')?.includes('true'));
     await hub.getByRole('status').filter({hasText:expected}).waitFor();
     assert.equal(calls.filter(x=>x.path.endsWith('/meetings/capabilities')).length,2);
    }
    assert.deepEqual(errors,[]);assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
    assert.deepEqual(await hub.locator('button,summary').evaluateAll(es=>es.filter(e=>e.getBoundingClientRect().height<44).map(e=>e.textContent)),[]);
    if(width===320)await page.screenshot({path:path.join(evidence,state+'-'+theme+'.png'),fullPage:true});
    if(state==='normal')await hub.screenshot({path:path.join(evidence,'hub-'+width+'-'+theme+'.png')});
    checks.push({width,theme,state,calls});console.log('PASS MEETING OPTIONS '+[width,theme,state].join(' '));
   }catch(error){await page.screenshot({path:path.join(evidence,[width,theme,state,'failure.png'].join('-')),fullPage:true}).catch(()=>{});throw error;}
   finally{release();await context.close();}
  }
  const cases=[];for(const width of[320,768,1440])for(const theme of['light','dark'])for(const state of['normal','loading','error','denied','malformed','claimed-ready'])cases.push([width,theme,state]);
  let next=0;await Promise.all(Array.from({length:2},async()=>{while(next<cases.length)await run(...cases[next++]);}));assert.equal(checks.length,36);
  const sources=Object.fromEntries(['apps/web/src/app/communication-settings/page.tsx','apps/web/src/components/meeting-integration-options.tsx','apps/web/src/components/meeting-integration-options.module.css','apps/web/src/lib/meeting-integration-options.ts'].map(f=>[f,crypto.createHash('sha256').update(fs.readFileSync(path.join(root,f))).digest('hex')]));
  fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,sources,scope:'Synthetic API UI; no real OAuth, account connection, permissions, provider execution or device acceptance'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(error){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:error.message},null,2));throw error;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(error=>{console.error(error);process.exitCode=1;server.close();});
