// Exported Communication Settings UI/synthetic transport; not live Identity/SQL/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','communication-settings-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port;let browser;const checks=[];
 try{
  browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of[320,1440])for(const theme of['light','dark']){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-settings-token');},theme);
   let rows=structuredClone(require('./communication-page-harness.cjs').base),release,gate=null,reject=false;
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     calls.push({path:url.pathname,method,body:req.postDataJSON()});const headers={'Access-Control-Allow-Origin':origin};
     if(method==='PUT'){
      assert.match(url.pathname,/^\/api\/academies\/owned\/communication-settings\/(Email|WhatsApp|Meeting)$/);
      if(gate)await gate;
      if(reject)return route.fulfill({status:400,headers,contentType:'application/json',body:JSON.stringify({message:'Synthetic rejection'})});
      const channel=url.pathname.split('/').at(-1),body=req.postDataJSON(),old=rows.find(x=>x.channel===channel);
      const saved={...old,...Object.fromEntries(Object.entries(body).map(([k,v])=>[k,typeof v==='string'?(v.trim()||null):v]))};
      rows=rows.filter(x=>x.channel!==channel).concat(saved);
      return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(saved)});
     }
     assert.equal(method,'GET');
     const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Engagement"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[],'/api/academies/owned/communication-settings':rows};
     assert.ok(Object.hasOwn(data,url.pathname));return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/communication-settings');
   const panels={Email:page.locator('form').filter({has:page.getByRole('heading',{name:'Email sender',exact:true})}),WhatsApp:page.locator('form').filter({has:page.getByRole('heading',{name:'WhatsApp sender',exact:true})}),Meeting:page.locator('section').filter({has:page.getByRole('heading',{name:'Meeting provider',exact:true})})};
   const fields={Email:panels.Email.getByPlaceholder('Display name'),WhatsApp:panels.WhatsApp.getByPlaceholder('Display name'),Meeting:panels.Meeting.getByLabel('Organizer name')};
   await panels.Email.getByRole('button',{name:'Save email settings',exact:true}).waitFor({state:'visible'});
   for(const channel of['Email','WhatsApp','Meeting'])await fields[channel].fill('Unsaved '+channel);
   for(const channel of['Email','WhatsApp','Meeting']){
    gate=new Promise(r=>release=r);
    await fields[channel].fill('  Submitted '+channel+'  ');
    const button=panels[channel].getByRole('button',{name:channel==='Meeting'?'Save meeting provider':'Save '+channel.toLowerCase()+' settings',exact:true});
    await button.click();await page.waitForFunction(c=>document.querySelector('[data-channel-notice="'+c+'"]')?.textContent.includes('Saving'),channel);
    assert.equal(await (channel==='Meeting'?button:panels[channel].getByRole('button',{name:'Saving…',exact:true})).isDisabled(),true);
    await fields[channel].fill('Newer '+channel);release();gate=null;
    const notice=page.locator('[data-channel-notice="'+channel+'"]');
    await notice.filter({hasText:channel+' settings saved.'}).waitFor();assert.match(await notice.textContent(),/Newer edits.*not yet saved/);
    assert.equal(await button.isDisabled(),false);
    assert.equal(await fields[channel].inputValue(),'Newer '+channel);
    for(const other of['Email','WhatsApp','Meeting'].filter(x=>x!==channel))assert.equal(await fields[other].inputValue(),calls.some(x=>x.method==='PUT'&&x.path.endsWith('/'+other))?'Newer '+other:'Unsaved '+other);
    const write=calls.filter(x=>x.method==='PUT').at(-1),original=require('./communication-page-harness.cjs').base.find(x=>x.channel===channel);
    assert.equal(write.body.replyToAddress,original.replyToAddress);assert.equal(write.body.phoneNumber,original.phoneNumber);
    assert.equal(await notice.getAttribute('role'),'status');assert.equal(await notice.getAttribute('aria-live'),'polite');
    checks.push({width,theme,channel,scenario:'in-flight edit',notice:await notice.textContent()});
   }
   reject=true;
   for(const channel of['Email','WhatsApp','Meeting']){
    await panels[channel].getByRole('button',{name:channel==='Meeting'?'Save meeting provider':'Save '+channel.toLowerCase()+' settings',exact:true}).click();
    const notice=page.locator('[data-channel-notice="'+channel+'"]');await notice.filter({hasText:'Synthetic rejection'}).waitFor();
    assert.equal(await fields[channel].inputValue(),'Newer '+channel);assert.match(await notice.textContent(),/entries are kept/);
    checks.push({width,theme,channel,scenario:'rejected',notice:await notice.textContent()});
   }
   assert.equal(calls.filter(x=>x.method==='GET'&&x.path.endsWith('/communication-settings')).length,1);assert.equal(calls.filter(x=>x.method==='PUT').length,6);
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));
   const unexpected=errors.filter(x=>!x.startsWith('Failed to load resource:')||!x.includes('400'));assert.deepEqual(unexpected,[]);
   await page.screenshot({path:path.join(evidence,width+'-'+theme+'.png'),fullPage:true});console.log('PASS SETTINGS '+width+' '+theme+' 6 channel cases');await context.close();
  }
  assert.equal(checks.length,24);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API, not live Identity/SQL',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:24,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
