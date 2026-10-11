// Production-export browser proof with synthetic transport, not linked SQL/device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','teacher-compensation-browser-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{
 assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}
 file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}catch{res.writeHead(404).end();}});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin='http://127.0.0.1:'+server.address().port,checks=[];let browser;
 const fields=['monthlySalary','standardHourlyRate','beginnerHourlyRate','intermediateHourlyRate','advancedHourlyRate'];
 const labels={'monthlySalary':'Monthly salary','standardHourlyRate':'Standard hourly rate','beginnerHourlyRate':'Beginner hourly rate (optional)','intermediateHourlyRate':'Intermediate hourly rate (optional)','advancedHourlyRate':'Advanced hourly rate (optional)'};
 const cases=[
  {name:'monthly-zero',model:'Monthly',monthlySalary:0},
  {name:'hourly-zero',model:'Hourly',standardHourlyRate:0},
  {name:'hourly-bands-zero',model:'Hourly',standardHourlyRate:10,beginnerHourlyRate:0,intermediateHourlyRate:0,advancedHourlyRate:0},
  {name:'positive',model:'Hourly',standardHourlyRate:125.5,beginnerHourlyRate:10.25,intermediateHourlyRate:20.5,advancedHourlyRate:30.75,effectiveFrom:'2024-02-29'},
  {name:'typed-zero',model:'Monthly',monthlySalary:22,edit:'0'},
  {name:'optional-clear',model:'Hourly',standardHourlyRate:10,beginnerHourlyRate:0,clear:true},
  {name:'monthly-negative',model:'Monthly',monthlySalary:10,edit:'-1',invalid:true},
  {name:'hourly-negative',model:'Hourly',standardHourlyRate:10,edit:'-1',invalid:true},
  {name:'monthly-required',model:'Monthly',monthlySalary:10,edit:'',invalid:true},
  {name:'hourly-required',model:'Hourly',standardHourlyRate:10,edit:'',invalid:true}
 ];
 try{
  browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const width of[320,1440])for(const theme of['light','dark'])for(const item of cases){
   const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
   await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-compensation-token');},theme);
   let stored={model:item.model,effectiveFrom:item.effectiveFrom??null};for(const f of fields)stored[f]=item[f]??null;
   const expected={...stored};const primary=item.model==='Monthly'?'monthlySalary':'standardHourlyRate';if(item.edit!==undefined&&!item.invalid)expected[primary]=Number(item.edit);if(item.clear)expected.beginnerHourlyRate=null;
   await context.route('**/*',async route=>{
    const req=route.request(),url=new URL(req.url()),method=req.method();
    if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const body=req.postDataJSON();calls.push({path:url.pathname,method,body});const headers={'Access-Control-Allow-Origin':origin};
     if(method==='PUT'){
      assert.equal(url.pathname,'/api/academies/owned/teachers/teacher/compensation');assert.ok(!item.invalid);
      const bad=body[primary]===null||fields.some(f=>body[f]!==null&&body[f]<0);
      if(!bad)stored={...body};
      return route.fulfill({status:bad?400:200,headers,contentType:'application/json',body:JSON.stringify(bad?{message:'Synthetic required/negative-rate rejection.'}:stored)});
     }
     assert.equal(method,'GET');const data={'/api/academies':[{id:'owned',name:'Synthetic academy',enabledModulesJson:'["Core","Teachers","Operations"]'}],'/api/auth/session':{academyId:'owned',displayName:'Synthetic admin',roles:['AcademyAdmin'],isPlatformOwner:false},'/api/portal/announcements':[], '/api/academies/owned/teachers':[{id:'teacher',firstName:'Synthetic',lastName:'Teacher',isActive:true}],'/api/academies/owned/teachers/teacher/compensation':stored};
     assert.ok(Object.hasOwn(data,url.pathname),url.pathname);return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(data[url.pathname])});
    }
    if(url.origin===origin||url.protocol==='data:')return route.continue();return route.abort('blockedbyclient');
   });
   await page.goto(origin+'/teacher-payments');const input=page.getByLabel(labels[primary],{exact:true}),save=page.getByRole('button',{name:'Save payment details',exact:true}),notice=page.locator('.teacher-payments-message');
   await input.waitFor();await page.waitForFunction(({primary,value})=>{const input=[...document.querySelectorAll('label')].find(x=>x.textContent.trim()===(primary==='monthlySalary'?'Monthly salary':'Standard hourly rate'))?.querySelector('input');return input?.value===String(value);},{primary,value:item[primary]});
   if(item.edit!==undefined)await input.fill(item.edit);if(item.clear)await page.getByLabel(labels.beginnerHourlyRate,{exact:true}).fill('');
   await save.click();
   if(item.invalid){
    assert.equal(await input.evaluate(e=>e.checkValidity()),false);assert.equal(calls.filter(c=>c.method==='PUT').length,0);
   }else{
    await notice.filter({hasText:'Teacher payment details saved.'}).waitFor();assert.deepEqual(stored,expected);
    await page.reload();await input.waitFor();await page.waitForFunction(({label,value})=>{const e=[...document.querySelectorAll('label')].find(x=>x.textContent.trim()===label)?.querySelector('input');return e?.value===String(value);},{label:labels[primary],value:expected[primary]});
    await save.click();await notice.filter({hasText:'Teacher payment details saved.'}).waitFor();
    const writes=calls.filter(c=>c.method==='PUT');assert.equal(writes.length,2);for(const w of writes)assert.deepEqual(w.body,expected);assert.deepEqual(stored,expected);
    for(const f of item.model==='Monthly'?['monthlySalary']:fields.slice(1))assert.equal(await page.getByLabel(labels[f],{exact:true}).inputValue(),expected[f]===null?'':String(expected[f]));
   }
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1));assert.deepEqual(errors,[]);
   await save.scrollIntoViewIfNeeded();await page.screenshot({path:path.join(evidence,`${width}-${theme}-${item.name}.png`),fullPage:true});checks.push({width,theme,case:item.name,calls,stored});console.log(`PASS COMPENSATION UI ${width} ${theme} ${item.name}`);await context.close();
  }
  assert.equal(checks.length,40);fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,transport:'synthetic API; not linked SQL/Identity',physicalDevices:'NOT RUN'},null,2));console.log(JSON.stringify({passed:checks.length,evidence}));
 }catch(e){fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({checks,failure:e.message},null,2));throw e;}
 finally{if(browser)await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1;server.close();});
