// Real Chromium/React/DOM with intercepted synthetic auth/protected host only. No live Identity/SQL/model acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/penta-entry-browser');
const executable = process.env.QA_BROWSER_EXECUTABLE;
if (!executable || !fs.existsSync(path.join(out, 'penta.html'))) throw Error('Select Chromium and build the local static app first.');
const apiOrigin = process.env.QA_PENTA_BROWSER_API_ORIGIN || 'http://127.0.0.1:49142';
const apiAddress = new URL(apiOrigin);
a.equal(apiAddress.protocol, 'http:'); a.equal(apiAddress.hostname, '127.0.0.1');
a.ok(Number(apiAddress.port) >= 1024); a.equal(apiAddress.origin, apiOrigin);
fs.mkdirSync(evidence, { recursive: true });
const resultFile=path.join(evidence,'result.json');
if(fs.existsSync(resultFile)) fs.renameSync(resultFile,path.join(evidence,'result-previous-'+Date.now()+'.json'));
fs.writeFileSync(resultFile,JSON.stringify({status:'RUNNING'}));
const types = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.txt': 'text/plain', '.json': 'application/json', '.woff2': 'font/woff2', '.ico': 'image/x-icon', '.svg': 'image/svg+xml' };
const ts = require('../../apps/web/node_modules/typescript');
const sessionModule = 'const process={env:' + JSON.stringify({NEXT_PUBLIC_API_URL:apiOrigin}) + '};\n' + ts.transpileModule(fs.readFileSync(path.join(root,'apps/web/src/lib/api.ts'),'utf8'),{compilerOptions:{module:ts.ModuleKind.ES2022,target:ts.ScriptTarget.ES2022}}).outputText;
const server = http.createServer((req, res) => {
  try {
    if (req.method !== 'GET') { res.writeHead(405); return res.end(); }
    const relative = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname);
    if(relative==='/__qa_session_api.js'){res.writeHead(200,{'Content-Type':'application/javascript'});return res.end(sessionModule);}
    let file = path.resolve(out, '.' + relative);
    if (!file.startsWith(out + path.sep) && file !== out) throw Error('path');
    if (file === out) file = path.join(out, 'index.html'); else if (!path.extname(file)) file += '.html';
    if (!fs.existsSync(file)) { res.writeHead(404); return res.end(); }
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream' }); fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404); res.end(); }
});
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
class CDP {
  constructor(socket) { this.socket = socket; this.id = 0; this.pending = new Map(); this.handlers = new Map(); socket.addEventListener('message', event => { const msg = JSON.parse(event.data); if (msg.id) { const item = this.pending.get(msg.id); if (item) { clearTimeout(item.timer); this.pending.delete(msg.id); msg.error ? item.reject(Error(msg.error.message)) : item.resolve(msg.result); } } else this.handlers.get(msg.sessionId + ':' + msg.method)?.(msg.params); }); }
  send(method, params = {}, sessionId) { const id = ++this.id; return new Promise((resolve, reject) => { const timer = setTimeout(() => { this.pending.delete(id); reject(Error('CDP timeout ' + method)); }, 15000); this.pending.set(id, { resolve, reject, timer }); this.socket.send(JSON.stringify({ id, method, params, ...(sessionId ? { sessionId } : {}) })); }); }
  on(session, name, fn) { this.handlers.set(session + ':' + name, fn); }
}
async function main() {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const origin = 'http://127.0.0.1:' + server.address().port, profile = fs.mkdtempSync(path.join(os.tmpdir(), 'c3-chromium-'));
  const child = spawn(executable, ['--headless', '--disable-background-networking', '--no-first-run', '--remote-debugging-port=0', '--user-data-dir=' + profile, 'about:blank'], { windowsHide:true, stdio: ['ignore', 'ignore', 'pipe'] });
  let log = '', socket, cdp, runtimeErrors = [], results = [], writes = [], requests = [], success=false, failure=null;
  try {
    const endpoint = await new Promise((resolve, reject) => { const timer = setTimeout(() => reject(Error('Chromium startup timeout')), 15000); child.once('error', reject); child.stderr.on('data', data => { log += data; const match = log.match(/DevTools listening on (ws:\/\/[^\s]+)/); if (match) { clearTimeout(timer); resolve(match[1]); } }); });
    socket = new WebSocket(endpoint); await new Promise((resolve, reject) => { socket.addEventListener('open', resolve, { once: true }); socket.addEventListener('error', reject, { once: true }); }); cdp = new CDP(socket);
    const cases = [
      { workspace: 'AcademyAdmin', route: '/penta', roles: ['Owner'], manual: '/dashboard', label: 'Academy administration' },
      { workspace: 'Teacher', route: '/teacher/ai', roles: ['Teacher'], manual: '/teacher', label: 'Teacher' },
      { workspace: 'Portal', route: '/portal/ai', roles: ['Guardian'], manual: '/portal', label: 'Student / Guardian' },
      { workspace: 'Platform', route: '/platform/ai', roles: ['PlatformOwner'], manual: '/platform', label: 'Platform Owner' },
    ];
    async function page(config) {
      const { targetId } = await cdp.send('Target.createTarget', { url: 'about:blank' }); const { sessionId: sid } = await cdp.send('Target.attachToTarget', { targetId, flatten: true });
      await cdp.send('Page.enable', {}, sid); await cdp.send('Runtime.enable', {}, sid);
      await cdp.send('Emulation.setDeviceMetricsOverride', { width: config.width || 1280, height: 900, deviceScaleFactor: 1, mobile: false }, sid);
      cdp.on(sid, 'Runtime.exceptionThrown', event => runtimeErrors.push(event.exceptionDetails.text + ':' + (event.exceptionDetails.exception?.description || '')));
      cdp.on(sid, 'Runtime.consoleAPICalled', event => { if(event.type==='error') runtimeErrors.push('Console: '+event.args.map(arg=>arg.value??arg.description??'').join(' ')); });
      cdp.on(sid, 'Fetch.requestPaused', async ({ requestId, request }) => {
        try {
          const url = new URL(request.url);
          if (url.origin === origin) return await cdp.send('Fetch.continueRequest', { requestId }, sid);
          if (url.origin !== apiOrigin) return await cdp.send('Fetch.failRequest', { requestId, errorReason: 'BlockedByClient' }, sid);
          requests.push({ method: request.method, path: url.pathname });
          if (request.method === 'OPTIONS') return await fulfill(204, {});
          const authPath = url.pathname === '/api/auth/login' || url.pathname === '/api/auth/refresh';
          if (request.method !== 'GET' && !authPath) writes.push(url.pathname);
          if (config.api) { const reply = await config.api(request, url); if (reply) return await fulfill(reply.status ?? 200, reply.body); }
          if (url.pathname === '/api/auth/login') return await fulfill(200, { accessToken: 'synthetic-access', refreshToken: 'synthetic-refresh' });
          if (config.route==='/teacher' && url.pathname==='/api/teacher/me') return await fulfill(200,{userId:'00000000-0000-0000-0000-000000000001',firstName:'Synthetic',lastName:'Teacher',batches:[],sessions:[]});
          if (config.route==='/teacher' && ['/api/portal/announcements','/api/portal/notifications'].includes(url.pathname)) return await fulfill(200,[]);
          if (url.pathname !== '/api/auth/session') return await fulfill(401, {});
          if (config.holdProfile) await config.holdProfile;
          if (config.status) return await fulfill(config.status, {});
          return await fulfill(200, config.badProfile ? { workspace: config.workspace, roles: ['WrongRole'], isPlatformOwner: false } : { workspace: config.workspace, roles: config.roles, isPlatformOwner: config.workspace === 'Platform' });
          async function fulfill(code, body) { await cdp.send('Fetch.fulfillRequest', { requestId, responseCode: code, responseHeaders: [{ name: 'Content-Type', value: 'application/json' }, { name: 'Access-Control-Allow-Origin', value: origin }, { name: 'Access-Control-Allow-Headers', value: 'Authorization, Content-Type' }, { name: 'Access-Control-Allow-Methods', value: 'GET, POST, OPTIONS' }], body: code===204?'':Buffer.from(JSON.stringify(body)).toString('base64') }, sid); }
        } catch (error) { runtimeErrors.push('Intercept: ' + error.message); }
      });
      await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*', requestStage: 'Request' }] }, sid);
      await cdp.send('Page.addScriptToEvaluateOnNewDocument', { source: `if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(config.theme || 'dark')});localStorage.setItem(${JSON.stringify('academydesk.accessToken.' + config.workspace)},'synthetic-access');localStorage.setItem(${JSON.stringify('academydesk.refreshToken.' + config.workspace)},'synthetic-refresh');${config.epoch ? "localStorage.setItem("+JSON.stringify('academydesk.sessionEpoch.' + config.workspace)+",'synthetic-login');" : ''}}` }, sid);
      const evaluate = async expression => { const value = await cdp.send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true }, sid); if (value.exceptionDetails) throw Error(value.exceptionDetails.exception?.description || value.exceptionDetails.text); return value.result.value; };
      const wait = async expression => { for (let i = 0; i < 100; i++) { if (await evaluate(expression)) return; await sleep(50); } throw Error('DOM wait failed: ' + expression); };
      await cdp.send('Page.navigate', { url: origin + config.route }, sid);
      return { sid, targetId, evaluate, wait, close: () => cdp.send('Target.closeTarget', { targetId }) };
    }
    for (const item of cases) for (const width of [1280, 390]) {
      const p = await page({ ...item, width, theme: width === 390 ? 'light' : 'dark' });
      try {
        await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes('conversation is not available')`);
        a.ok(await p.evaluate(`document.body?.innerText.includes('Conversations not available')`));
        a.equal(await p.evaluate(`document.querySelector('a[href="${item.manual}"]')!==null`), true);
        a.equal(await p.evaluate(`document.documentElement.scrollWidth<=innerWidth+1`), true);
        a.equal(await p.evaluate(`document.querySelector('[data-penta-theme]')?.getAttribute('data-penta-theme')`), width === 390 ? 'prism' : 'orbit');
        a.equal(await p.evaluate(`document.querySelector('textarea')===null`), true);
        if (item.workspace === 'Teacher' && width === 390) { const shot = await cdp.send('Page.captureScreenshot', { format: 'png' }, p.sid); fs.writeFileSync(path.join(evidence, 'teacher-prism-mobile.png'), Buffer.from(shot.data, 'base64')); }
        results.push({ case: item.workspace + '/' + width, pass: true });
      } finally { await p.close(); }
    }
    for (const config of [{ ...cases[1], status: 401, text: 'Sign in to continue' }, { ...cases[1], badProfile: true, text: 'could not be verified' }, { ...cases[1], status: 503, text: 'verification is unavailable' }]) {
      const p = await page(config); try { await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes(${JSON.stringify(config.text)})`); results.push({ case: config.text, pass: true }); } finally { await p.close(); }
    }
    {
      let release; const holdProfile = new Promise(resolve => { release = resolve; }); const start = requests.length;
      const p = await page({ ...cases[1], holdProfile }); try {
        await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes('Checking')`);
        for (let i=0;i<100&&!requests.slice(start).some(r=>r.path==='/api/auth/session');i++) await sleep(25);
        a.ok(requests.slice(start).some(r=>r.path==='/api/auth/session'));
        await p.evaluate(`localStorage.setItem('academydesk.accessToken.Teacher','replacement')`); release();
        await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes('verification is unavailable')`);
        await p.evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Check workspace again').click()`);
        await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes('conversation is not available')`);
        results.push({ case: 'late profile cannot verify a replaced token; explicit recheck recovers', pass: true });
      } finally { release(); await p.close(); }
    }
    {
      const p = await page({ ...cases[1], route: '/teacher' }); try {
        await p.wait(`document.querySelector('nav[aria-label="Work view"] a[href="/teacher/ai"]')!==null && document.body?.innerText.includes('Welcome, Synthetic')`);
        await p.evaluate(`new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))`);
        await p.evaluate(`{const f=document.createElement('form'),i=document.createElement('input');f.id='c3-guard-probe';f.append(i);document.body.append(f);i.value='unsaved synthetic';i.dispatchEvent(new Event('input',{bubbles:true}));document.querySelector('nav[aria-label="Work view"] a').click();}`);
        await p.wait(`Array.from(document.querySelectorAll('[role="dialog"],dialog')).some(d=>d.textContent.includes('Leave Manual'))`);
        a.equal(await p.evaluate(`location.pathname`), '/teacher');
        a.equal(await p.evaluate(`document.querySelector('dialog')?.open`), true);
        await p.evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Stay in Manual').click()`);
        a.equal(await p.evaluate(`document.querySelector('#c3-guard-probe input')?.value==='unsaved synthetic'`), true);
        await p.evaluate(`document.querySelector('nav[aria-label="Work view"] a').click()`);
        await p.wait(`document.querySelector('dialog')?.open===true`);
        await cdp.send('Input.dispatchKeyEvent',{type:'keyDown',key:'Escape',code:'Escape',windowsVirtualKeyCode:27},p.sid);
        await cdp.send('Input.dispatchKeyEvent',{type:'keyUp',key:'Escape',code:'Escape',windowsVirtualKeyCode:27},p.sid);
        await p.wait(`document.querySelector('dialog')===null`);
        a.equal(await p.evaluate(`document.activeElement?.getAttribute('href')`),'/teacher/ai');
        results.push({ case: 'Manual guard retains synthetic edits; native Escape restores mode-link focus', pass: true });
      } finally { await p.close(); }
    }

    // Session marker distinguishes explicit replacement from token renewal.
    for (const mode of ['renewal', 'replacement', 'other-workspace']) {
      const p = await page({ ...cases[1], epoch: true });
      try {
        await p.wait(`document.body?.innerText.includes('Teacher conversation is not available yet')`);
        await p.evaluate(mode === 'renewal' ? `localStorage.setItem('academydesk.accessToken.Teacher','synthetic-renewed');dispatchEvent(new PageTransitionEvent('pageshow'))` : mode === 'replacement' ? `localStorage.setItem('academydesk.sessionEpoch.Teacher','synthetic-other-login');dispatchEvent(new PageTransitionEvent('pageshow'))` : `localStorage.setItem('academydesk.sessionEpoch.Portal','synthetic-other-login');dispatchEvent(new CustomEvent('academydesk:portal-session-changed',{detail:'Portal'}))`);
        if (mode === 'replacement') await p.wait(`document.body?.innerText.includes('Session ended')`);
        else a.ok(await p.evaluate(`document.body?.innerText.includes('Teacher conversation is not available yet')`));
        results.push({case:'explicit epoch: '+mode,pass:true});
      } finally { await p.close(); }
    }

    // Execute the actual api.ts in separate real browser module realms and
    // native Web Locks. This QA module is served from memory by this runner only.
    {
      let release;const gate=new Promise(resolve=>release=resolve);let refreshes=0,probes=0;
      const config={...cases[1],epoch:true,api:async(request,url)=>{
        if(url.pathname==='/api/qa-session-probe'){probes++;return{status:new Headers(request.headers).get('Authorization')==='Bearer synthetic-renewed'?200:401,body:{}};}
        if(url.pathname==='/api/auth/refresh'){refreshes++;await gate;return{body:{accessToken:'synthetic-renewed',refreshToken:'synthetic-renewed-refresh'}};}
      }};
      const first=await page(config),second=await page(config);
      try{
        await second.wait(`document.body?.innerText.includes('Teacher conversation is not available yet')`);
        for(const p of [first,second])await p.evaluate(`import('/__qa_session_api.js').then(api=>{window.qaSessionApi=api})`);
        for(const p of [first,second])await p.evaluate(`window.qaSessionResult=null;void window.qaSessionApi.academyApi('/api/qa-session-probe').then(r=>{window.qaSessionResult=r.status})`);
        await second.wait(`navigator.locks.query().then(q=>q.pending.some(x=>x.name==='academydesk:session-refresh:Teacher'))`);
        a.equal(refreshes,1);release();
        for(const p of [first,second])await p.wait(`window.qaSessionResult===200`);
        a.equal(refreshes,1);a.equal(probes,4);
        a.equal(await first.evaluate(`localStorage.getItem('academydesk.sessionEpoch.Teacher')`),'synthetic-login');
        results.push({case:'two native browser realms share one refresh through Web Locks, retain explicit login epoch',pass:true});
      }finally{release();await first.close();await second.close();}
    }
    const synthetic = require('./penta-onboarding-review.test.cjs');
    const ids={conversation:synthetic.conversation,draft:synthetic.draft,academy:synthetic.academy};
    const account=synthetic.fixture().account;
    const reviewRoute='/penta/onboarding?conversationId='+ids.conversation+'&draftId='+ids.draft;
    const teacherRoute='/penta/teachers?conversationId='+ids.conversation+'&draftId='+ids.draft;
    const base='/api/academies/'+ids.academy+'/penta/teachers/conversations/'+ids.conversation+'/drafts/'+ids.draft;
    const click = async (p,text) => {
      await p.wait(`Array.from(document.querySelectorAll('button')).some(b=>b.textContent===${JSON.stringify(text)}&&!b.disabled)`);
      await p.evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent===${JSON.stringify(text)}&&!b.disabled).click()`);
    };
    // Host disabled: every protected landing remains usable through Manual.
    for (const route of ['/penta/onboarding','/penta/teachers/chat',reviewRoute,teacherRoute]) {
      const start=requests.length;
      const p=await page({...cases[0],route,epoch:true,api:(request,url)=>({status:url.pathname==='/api/auth/session'?200:404,body:url.pathname==='/api/auth/session'?account:{}})});
      try {
        await p.wait(`document.body?.innerText.includes('unavailable')`);
        a.ok(await p.evaluate(`Array.from(document.querySelectorAll('a')).some(a=>/Manual/.test(a.textContent))`));
        a.equal(requests.slice(start).filter(r=>r.method==='POST').length,0);
        a.equal(await p.evaluate(`Array.from(document.querySelectorAll('textarea')).every(t=>t.disabled)`),true);
        results.push({case:'disabled host '+route.split('?')[0],pass:true});
      }finally{await p.close();}
    }
    // Student review renders only the exact host-bound projection; no action on mount.
    {
      const f=synthetic.fixture(), start=requests.length;
      const p=await page({...cases[0],route:reviewRoute,epoch:true,api:(request,url)=>({body:url.pathname==='/api/auth/session'?f.account:url.pathname.endsWith('/current')?f.current:url.pathname.endsWith('/drafts/'+ids.draft)?f.view:{}})});
      try{
        await p.wait(`document.querySelector('.penta-operational')!==null`);
        a.equal(requests.slice(start).filter(r=>r.method==='POST').length,0);
        await p.evaluate(`dispatchEvent(new CustomEvent('academydesk:portal-session-changed',{detail:'AcademyAdmin'}))`);
        await p.wait(`document.querySelector('.penta-operational')===null`);
        results.push({case:'authenticated student projection; local replacement removes private UI',pass:true});
      }finally{await p.close();}
    }
    a.equal(writes.length,0,'All entry, invalidation and disabled-host tests must remain read-only');
    // A simulated host advances real UI controls; no network request leaves CDP.
    const crypto=require('node:crypto');
    const canonicalPayload=JSON.stringify({tool_key:'AcademyDesk.Teacher.Create',tool_revision:1,fields:{FirstName:{kind:'text',value:'Synthetic'},LastName:{kind:'text',value:'Teacher'}}});
    const approval='44444444-4444-4444-8444-444444444444',teacher='55555555-5555-4555-8555-555555555555';
    const receipt='66666666-6666-4666-8666-666666666666',audit='77777777-7777-4777-8777-777777777777';
    for(const lost of [false,true]){
      let state={id:ids.draft,sessionId:ids.conversation,contextVersion:1,revision:1,status:'AwaitingReview',expiresAtUtc:'2099-01-01T00:00:00Z',canonicalPayload,payloadHash:crypto.createHash('sha256').update(canonicalPayload).digest('hex'),approvalId:null,approvalExpiresAtUtc:null,receipt:null};
      const actions=[],start=requests.length;
      const p=await page({...cases[0],route:teacherRoute,width:390,theme:'light',epoch:true,api:(request,url)=>{
        if(url.pathname==='/api/auth/session')return{body:account};
        if(!url.pathname.startsWith(base))throw Error('Unexpected protected teacher request');
        if(request.method==='POST'){
          const body=JSON.parse(request.postData);a.equal(body.revision,1);a.equal(body.payloadHash,state.payloadHash);
          const action=url.pathname.slice(base.length);actions.push(action);
          if(action==='/review')Object.assign(state,{status:'Reviewed',approvalId:approval,approvalExpiresAtUtc:'2098-01-01T00:00:00Z'});
          else if(action==='/approve'){a.equal(body.approvalId,approval);a.equal(body.intent,'CreateTeacherWithoutAccount');state.status='Authorized';}
          else if(action==='/execute'){a.equal(body.approvalId,approval);Object.assign(state,{status:'Succeeded',receipt:{receiptId:receipt,teacherId:teacher,auditId:audit,completedAtUtc:'2026-11-06T12:00:00Z'}});if(lost)return{status:503,body:{}};}
          else throw Error('Unexpected mutation '+action);
        }
        return{body:structuredClone(state)};
      }});
      try{
        await p.wait(`document.querySelector('[aria-label="Canonical teacher details"]')?.textContent.includes('Synthetic')`);
        a.equal(requests.slice(start).filter(r=>r.method==='POST').length,0);
        a.equal(await p.evaluate(`document.documentElement.scrollWidth<=innerWidth+1`),true);
        await click(p,'Review teacher draft');
        await click(p,'Approve exact details');
        await p.wait(`document.querySelector('dialog')?.open===true`);
        a.deepEqual(actions,['/review']);
        await click(p,'Approve this teacher draft');
        await p.wait(`document.body?.innerText.includes('Creation is a separate explicit step')`);
        a.deepEqual(actions,['/review','/approve']);
        await click(p,'Create approved teacher');
        if(lost){await p.wait(`document.body?.innerText.includes('Teacher outcome is unconfirmed')`);await click(p,'Check saved teacher result');}
        await p.wait(`document.body?.innerText.includes('The academy verified the saved teacher, receipt and audit')`);
        a.equal(await p.evaluate(`document.querySelector('a[href="/teacher-profile?teacherId=${teacher}"]')!==null`),true);
        a.deepEqual(actions,['/review','/approve','/execute']);
        a.equal(await p.evaluate(`Array.from(document.querySelectorAll('button')).some(b=>b.textContent==='Create approved teacher')`),false);
        await p.evaluate(`localStorage.setItem('academydesk.sessionEpoch.AcademyAdmin','replaced');dispatchEvent(new PageTransitionEvent('pageshow'))`);
        await p.wait(`document.body?.innerText.includes('Session ended')`);
        a.equal(await p.evaluate(`document.querySelector('[aria-label="Canonical teacher details"]')===null`),true);
        results.push({case:lost?'teacher lost execute response: GET-only recovery, one execution':'teacher separate review, explicit approval, create and verified receipt',pass:true});
      }finally{await p.close();}
    }
    a.equal(writes.length,6,'Exactly three simulated mutations per teacher flow');
    a.equal(writes.filter(p=>p.endsWith('/execute')).length,2,'One execution per isolated synthetic draft');

    a.equal(requests.filter(r => /inference|model/.test(r.path)).length, 0);
    a.deepEqual(runtimeErrors, []);
    success=true;
    console.log(results.length + ' actual Chromium/React groups PASS; zero model calls; six intercepted synthetic teacher mutations.');
  } catch(error) { failure=error.message; throw error; } finally {
    if (cdp) await cdp.send('Browser.close').catch(() => {}); socket?.close(); child.kill('SIGTERM');
    if(child.exitCode===null) await Promise.race([new Promise(resolve=>child.once('exit',resolve)),sleep(2000)]);
    server.closeAllConnections(); await new Promise(resolve => server.close(resolve));
    const ownedProfile=path.resolve(profile);
    a.equal(path.dirname(ownedProfile),path.resolve(os.tmpdir()));
    a.ok(path.basename(ownedProfile).startsWith('c3-chromium-'));
    fs.rmSync(ownedProfile, { recursive: true, force: true });
    fs.writeFileSync(resultFile,JSON.stringify({status:success?'PASS':'FAIL',failure,results,requests,interceptedSyntheticWrites:writes,runtimeErrors,limitation:'All API responses intercepted synthetic fixtures, including six teacher workflow mutations; no live Identity/SQL/model/provider/physical-device qualification.'},null,2));
  }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
