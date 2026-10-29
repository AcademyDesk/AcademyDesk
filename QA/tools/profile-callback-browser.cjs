// Actual exported profile pages and shared pickers. Synthetic reads only;
// not SQL, server authorization or physical-device acceptance.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http'),assert=require('node:assert/strict');
const pick=require('./shared-picker-actions.cjs');
const {chromium}=require(process.env.QA_PLAYWRIGHT_MODULE||'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root=path.resolve(__dirname,'../..'),web=fs.realpathSync(path.join(root,'apps/web/out'));
const evidence=path.join(root,'QA/EVIDENCE','profile-callback-'+Date.now());fs.mkdirSync(evidence,{recursive:true});
const mime={'.html':'text/html','.css':'text/css','.js':'application/javascript','.txt':'text/plain','.woff2':'font/woff2','.svg':'image/svg+xml','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{assert.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!path.extname(file))file+='.html';const segment=/^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment){const parts=segment[1].split('.');file=path.join(path.dirname(file),'__next.'+parts[0],...parts.slice(1),'__PAGE__.txt');}file=fs.realpathSync(file);assert.ok(file.startsWith(web+path.sep));res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
const people=['one','two'].map(id=>({id,firstName:'Synthetic',lastName:id,isActive:true}));
function data(p){
 if(p==='/api/auth/session')return{workspace:'AcademyAdmin',roles:['AcademyAdmin'],isPlatformOwner:false,academyId:'owned',displayName:'Synthetic Admin'};
 if(p==='/api/academies')return[{id:'owned',name:'Synthetic academy',enabledModulesJson:JSON.stringify(['Core','AcademicGovernance','Finance','FinanceControls'])}];
 if(p==='/api/portal/announcements')return[];
 if(/^\/api\/academies\/owned\/(teachers|guardians)$/.test(p))return people;
 if(/^\/api\/academies\/owned\/(teachers|guardians)\/(one|two)\/profile$/.test(p))return{preferredName:p.includes('/two/')?'Second':'First',availability:[],students:[],batches:[],classes:[],invoices:[],communications:[]};
 if(/^\/api\/academies\/owned\/teachers\/(one|two)\/compensation$/.test(p))return{model:'Monthly',monthlySalary:p.includes('/two/')?200:100};
 throw Error('Unexpected API '+p);
}
(async()=>{let browser;const checks=[],failures=[];await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));const origin='http://127.0.0.1:'+server.address().port;
 try{browser=await chromium.launch({headless:true,executablePath:process.env.QA_BROWSER_EXECUTABLE||'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  for(const routeName of ['guardian-profile','teacher-profile','teacher-payments'])for(const width of [320,1440])for(const theme of ['light','dark']){
   const context=await browser.newContext({viewport:{width,height:1000}}),page=await context.newPage(),errors=[],reads=[],writes=[];
   try{
    page.on('pageerror',error=>errors.push(error.message));page.on('console',message=>{if(message.type()==='error'&&/hydrat/i.test(message.text()))errors.push(message.text());});
    await context.addInitScript(theme=>{localStorage.setItem('academydesk.theme',theme);localStorage.setItem('academydesk.accessToken.AcademyAdmin','synthetic-profile');},theme);
    await context.route('**/*',async route=>{const req=route.request(),url=new URL(req.url());if(url.pathname.startsWith('/api/')&&['localhost','127.0.0.1'].includes(url.hostname)){
     const headers={'Access-Control-Allow-Origin':origin};if(req.method()==='OPTIONS')return route.fulfill({status:204,headers:{...headers,'Access-Control-Allow-Headers':'authorization,content-type','Access-Control-Allow-Methods':'GET'}});
     if(req.method()!=='GET'){writes.push(url.pathname);return route.abort('blockedbyclient');}
     reads.push(url.pathname);let body;try{body=data(url.pathname);}catch(error){errors.push(error.message);return route.fulfill({status:500,headers,contentType:'application/json',body:'{}'});}
     return route.fulfill({status:200,headers,contentType:'application/json',body:JSON.stringify(body)});
    }return url.origin===origin||url.protocol==='data:'?route.continue():route.abort('blockedbyclient');});
    await page.goto(origin+'/'+routeName);
    const input=routeName==='teacher-payments'?page.getByRole('spinbutton',{name:'Monthly salary',exact:true}):page.getByPlaceholder('Preferred name',{exact:true});
    await input.waitFor();
    await page.waitForFunction(({pay})=>{const el=pay?document.querySelector('input[type="number"]'):document.querySelector('input[placeholder="Preferred name"]');return el?.value===(pay?'100':'First');},{pay:routeName==='teacher-payments'});
    const readsBefore=reads.length;await input.fill(routeName==='teacher-payments'?'321':'Preserved draft');
    await page.evaluate(()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve))));
    assert.equal(await input.inputValue(),routeName==='teacher-payments'?'321':'Preserved draft');assert.equal(reads.length,readsBefore,'typing cannot restart discovery');
    if(routeName==='guardian-profile')await page.locator('.enterprise-settings select').selectOption('two');else await pick.select(page,routeName==='teacher-profile'?'teacher-record':'payment-teacher','Synthetic two');
    await page.waitForFunction(({pay})=>{const el=pay?document.querySelector('input[type="number"]'):document.querySelector('input[placeholder="Preferred name"]');return el?.value===(pay?'200':'Second');},{pay:routeName==='teacher-payments'});
    assert.ok(reads.some(p=>p.includes('/owned/')&&p.includes('/two/')));assert.deepEqual(errors,[]);assert.deepEqual(writes,[]);
    // The shell and page each legitimately discover academy scope. Selection
    // and typing must not issue either discovery again.
    assert.equal(reads.filter(p=>p==='/api/academies').length,reads.slice(0,readsBefore).filter(p=>p==='/api/academies').length);
    checks.push({route:routeName,width,theme,selected:'two',draft_preserved:true,writes:0,page_errors:0,synthetic_API:true});
    if(width===320)await page.screenshot({path:path.join(evidence,routeName+'-'+theme+'.png'),fullPage:true});
   }catch(error){failures.push({route:routeName,width,theme,error:error.message});await page.screenshot({path:path.join(evidence,'incomplete.png'),fullPage:true}).catch(()=>{});throw error;}finally{await context.close();}
  }
 }finally{fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({checks,failures,scope:'synthetic API;not SQL/role/device acceptance',production_approved:false},null,2));console.log(JSON.stringify({evidence,passed:checks.length,failed:failures.length}));if(browser)await browser.close();await new Promise(resolve=>server.close(resolve));}
})().catch(error=>{console.error(error);process.exitCode=1;});
