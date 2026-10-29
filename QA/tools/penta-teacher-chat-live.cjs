'use strict';
// Actual exported UI + host/Identity/SQL via a run-owned loopback bridge.
const a=require('node:assert/strict'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const{chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const[api,run]=process.argv.slice(2),build=process.env.QA_PENTA_TEACHER_CHAT_EXPORT;a.match(api||'',/^http:\/\/127\.0\.0\.1:\d+$/);a.match(run||'',/^[a-f0-9]{32}$/);a.match(build||'',/^[a-f0-9]{32}$/);
const root=path.resolve(__dirname,'../..'),proof=JSON.parse(fs.readFileSync(path.join(root,'QA/EVIDENCE/default-web-build-'+build+'/result.json')));a.equal(proof.passed,true);a.equal(proof.downloaded,false);
a.deepEqual(require('./default-web-build.cjs').sourcePins(path.join(root,'apps/web')),proof.sourcePins);
const web=path.join(root,'.build-check/default-web-build',build,'app/out'),evidence=path.join(root,'QA/EVIDENCE/penta-teacher-chat-live-'+run);fs.mkdirSync(evidence,{recursive:true});a.equal(fs.realpathSync(web).toLowerCase(),web.toLowerCase());
const mime={'.html':'text/html','.js':'application/javascript','.css':'text/css','.txt':'text/plain','.json':'application/json','.woff2':'font/woff2','.ico':'image/x-icon'};
const server=http.createServer((req,res)=>{try{a.ok(['GET','HEAD'].includes(req.method));let file=path.resolve(web,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));a.ok(file.startsWith(web+path.sep));if(!path.extname(file))file+='.html';
 const segment=/^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));if(!fs.existsSync(file)&&segment)file=path.join(path.dirname(file),'__next.'+segment[1],'__PAGE__.txt');file=fs.realpathSync(file);a.ok(file.startsWith(web+path.sep));
 res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);}catch{res.writeHead(404).end();}});
async function main(){let raw='';for await(const b of process.stdin){raw+=b;a.ok(raw.length<65536);}const f=JSON.parse(raw);raw='';
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const origin=`http://127.0.0.1:${server.address().port}`;
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'}),checks=[];let known=null;
 try{for(const name of['chat-light','chat-dark','chat-lost','chat-denied','chat-unavailable']){
  const width=name==='chat-light'?320:name==='chat-dark'?1440:name==='chat-unavailable'?768:390;
  const context=await browser.newContext({viewport:{width,height:900}}),page=await context.newPage(),calls=[],errors=[];let promptPosts=0,lost=false;
  try{
   await context.addInitScript(({token,theme})=>{localStorage.setItem('academydesk.accessToken.AcademyAdmin',token);localStorage.setItem('academydesk.theme',theme);},{token:name==='chat-denied'?f.teacher:f.token,theme:name==='chat-light'?'light':'dark'});
   page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error'&&!/Failed to load resource.*(?:403|404)|net::ERR_FAILED/.test(m.text()))errors.push(m.text());});
   await context.route('**/*',async route=>{const request=route.request(),url=new URL(request.url());if(url.pathname.startsWith('/api/')){
    a.ok(['localhost','127.0.0.1'].includes(url.hostname));const response=await route.fetch({url:api+(name==='chat-unavailable'?'/offline':'')+url.pathname,maxRedirects:0,maxRetries:0});a.notEqual(response.status(),429);
    calls.push({path:url.pathname,method:request.method(),status:response.status()});
    if(request.method()==='POST'&&url.pathname.endsWith('/conversation/turns')){
     promptPosts++;if(name==='chat-lost'&&promptPosts===2){a.equal(lost,false);lost=true;a.equal(response.status(),200);return route.abort('failed');}
    }
    return route.fulfill({response,headers:{...response.headers(),'access-control-allow-origin':origin,'access-control-allow-headers':'Authorization,Content-Type'}});
   }if(url.origin===origin||url.protocol==='data:')return route.continue();throw Error('Unexpected external browser request');});
   if(name==='chat-denied'){
    a.ok(known);await page.goto(`${origin}/penta/teachers/chat?conversationId=${known.session}&draftId=${known.draft}`);
    await page.getByText('Current academy access could not be verified',{exact:true}).waitFor();a.equal(await page.getByText('TeacherChatSynthetic',{exact:true}).count(),0);a.ok(calls.every(x=>x.method==='GET'));
   }else{
    if(name==='chat-light'){
     await page.goto(origin+'/penta');await page.getByText('PENTA is not ready for this conversation.',{exact:true}).waitFor();await page.getByRole('link',{name:'Teacher onboarding conversation ↗',exact:true}).click();
    }else await page.goto(origin+'/penta/teachers/chat');
    if(name==='chat-unavailable'){
     await page.getByText('Protected start is unavailable',{exact:true}).waitFor();a.equal(await page.getByRole('button',{name:'Start teacher conversation',exact:true}).count(),0);a.ok(calls.every(x=>x.method==='GET'));
    }else{
     await page.getByRole('button',{name:'Start teacher conversation',exact:true}).waitFor();a.ok(calls.every(x=>x.method==='GET'));
     if(name==='chat-light')await page.screenshot({path:path.join(evidence,'chat-light-start.png')});
     await page.getByRole('button',{name:'Start teacher conversation',exact:true}).click();await page.waitForURL(u=>u.searchParams.has('conversationId')&&u.searchParams.has('draftId'));
     const parsed=new URL(page.url()),binding={session:parsed.searchParams.get('conversationId'),draft:parsed.searchParams.get('draftId')};if(!known)known=binding;
     const prompt=page.getByRole('textbox',{name:'Message PENTA AI about this teacher',exact:true});await prompt.waitFor();await prompt.fill('QA first name');await page.getByRole('button',{name:'Send message',exact:true}).click();
     await page.getByRole('log',{name:'Conversation messages'}).getByText(/Please provide the teacher.*last name/).waitFor();a.equal(promptPosts,1);
     await prompt.fill('QA last name '+name);await page.getByRole('button',{name:'Send message',exact:true}).click();
     if(name==='chat-lost'){
      await page.getByText('Teacher request is unconfirmed',{exact:true}).waitFor();a.equal(await page.getByText('TeacherChatSynthetic',{exact:true}).count(),0);
      a.ok(new URL(page.url()).searchParams.get('teacherRequestId'));await page.screenshot({path:path.join(evidence,'chat-lost-unconfirmed.png')});
      await page.reload();await page.getByText('Teacher request is unconfirmed',{exact:true}).waitFor();await page.getByRole('button',{name:'Check saved teacher request',exact:true}).click();
     }
     await page.getByRole('log',{name:'Conversation messages'}).getByText('Your draft is ready. Review the exact details; approval and creation are separate steps.',{exact:true}).waitFor();
     await page.waitForFunction(()=>{const pane=document.querySelector('[role="log"]');if(!pane)return false;const replies=pane.querySelectorAll('article p'),el=replies[replies.length-1];if(!el)return false;const r=el.getBoundingClientRect(),p=pane.getBoundingClientRect();return r.height>0&&r.top>=p.top-1&&r.bottom<=p.bottom+1;});
     const visibleReply=await page.getByRole('log',{name:'Conversation messages'}).getByText('Your draft is ready. Review the exact details; approval and creation are separate steps.',{exact:true}).evaluate(el=>{const r=el.getBoundingClientRect(),p=el.closest('[role="log"]').getBoundingClientRect();return r.height>0&&r.top>=p.top-1&&r.bottom<=p.bottom+1;});a.equal(visibleReply,true,'Latest reply must fit its message pane.');
     a.equal(promptPosts,2);a.equal(calls.filter(x=>x.path.endsWith('/approve')||x.path.endsWith('/execute')).length,0);
     a.doesNotMatch(page.url(),/QA|TeacherChatSynthetic/);a.equal(await page.evaluate(()=>Object.values(localStorage).some(v=>v.includes('QA last name')||v.includes('TeacherChatSynthetic'))),false);
     if(name==='chat-dark'){
      await page.screenshot({path:path.join(evidence,'chat-dark-prepared.png')});const chatUrl=page.url();
      await page.getByRole('link',{name:'Open protected teacher review',exact:true}).click();await page.getByRole('button',{name:'Cancel this draft',exact:true}).click();await page.getByText('Teacher draft cancelled',{exact:true}).waitFor();
      await page.goto(chatUrl);await page.getByRole('button',{name:'Check saved teacher request',exact:true}).click();
      await page.getByRole('log',{name:'Conversation messages'}).getByText('This teacher draft was cancelled. No teacher was created by this draft.',{exact:true}).waitFor();a.equal(await prompt.isDisabled(),true);
     }
     if(name==='chat-light'){
      await page.screenshot({path:path.join(evidence,'chat-light-prepared.png')});
      await page.getByRole('link',{name:'Open protected teacher review',exact:true}).click();await page.getByRole('button',{name:'Approve exact details',exact:true}).click();
      const dialog=page.getByRole('dialog',{name:'Approve this exact teacher draft'});await dialog.waitFor();a.equal(calls.filter(x=>x.path.endsWith('/approve')).length,0);
      await dialog.getByRole('button',{name:'Approve this teacher draft',exact:true}).click();await page.getByRole('button',{name:'Create approved teacher',exact:true}).waitFor();
      a.equal(calls.filter(x=>x.path.endsWith('/execute')).length,0);await page.getByRole('button',{name:'Create approved teacher',exact:true}).click();await page.getByText('Teacher created',{exact:true}).waitFor();
      a.equal(calls.filter(x=>x.path.endsWith('/execute')).length,1);await page.reload();await page.getByText('Teacher created',{exact:true}).waitFor();a.equal(calls.filter(x=>x.path.endsWith('/execute')).length,1);
     }
    }
   }
   a.equal(await page.getByRole('link',{name:'Manual workspace',exact:true}).getAttribute('href'),'/teacher-onboarding');
   a.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);a.deepEqual(errors,[]);
   await page.screenshot({path:path.join(evidence,name+'.png')});checks.push({name,width,passed:true,calls,errors,promptPosts,lostResponseAfterCommit:lost});console.log(`PASS: PENTATEACHERCHATBROWSER ${name};${calls.length} actual HTTP requests`);
  }catch(error){await page.screenshot({path:path.join(evidence,'failed-'+name+'.png')}).catch(()=>{});fs.writeFileSync(path.join(evidence,'failed.json'),JSON.stringify({name,calls,errors,error:error.message},null,2));throw error;}finally{await context.close();}
 }
 fs.writeFileSync(path.join(evidence,'result.json'),JSON.stringify({passed:true,run,webBuild:build,checks,sqlBacked:true,syntheticApiResponses:false,syntheticInterpreter:true,modelInference:false,physicalDevices:'NOT RUN',productionApproved:false},null,2));
 }finally{await browser.close();await new Promise(r=>server.close(r));}
}
main().catch(e=>{console.error('Teacher chat native browser failed: '+e.name+': '+e.message);process.exitCode=1;server.close();});
