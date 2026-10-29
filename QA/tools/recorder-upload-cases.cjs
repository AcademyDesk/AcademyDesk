const assert=require('node:assert/strict'),path=require('node:path'),crypto=require('node:crypto');
exports.verify=async({page,form,calls,api,token,evidence,width,theme})=>{
 const title=`QA recorded ${width} ${theme}`,comment='Real synthetic-device audio; explicit confirmation only';
 await form.locator('[name=title]').fill(title);await form.locator('[name=description]').fill(comment);
 await form.getByRole('button',{name:'Record audio',exact:true}).click();await form.getByRole('button',{name:'Pause recording',exact:true}).waitFor();
 await page.waitForTimeout(1250);await form.getByRole('button',{name:'Pause recording',exact:true}).click();await form.getByRole('button',{name:'Resume recording',exact:true}).click();
 await page.waitForTimeout(1250);await form.getByRole('button',{name:'Stop recording',exact:true}).click();await form.getByRole('status').filter({hasText:'Recording ready. Review it, then confirm upload.'}).waitFor();
 assert.equal(calls.filter(x=>x.method!=='GET').length,0,'Recording published before explicit confirmation');
 assert.equal(await page.evaluate(()=>window.__qaStreams.length===1&&window.__qaStreams.every(s=>s.getTracks().every(t=>t.readyState==='ended'))),true);
 const media=form.locator('audio,video');await media.waitFor();
 const recorded=await media.evaluate(async e=>{const b=await (await fetch(e.src)).arrayBuffer();const hash=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',b))).map(x=>x.toString(16).padStart(2,'0')).join('');return {size:b.byteLength,hash,head:Array.from(new Uint8Array(b).slice(0,4))};});assert.ok(recorded.size>0&&recorded.size<1024*1024);assert.deepEqual(recorded.head,[26,69,223,163]);
 await form.getByRole('button',{name:'Confirm upload',exact:true}).click();await form.getByRole('status').filter({hasText:'Class material uploaded successfully.'}).waitFor();
 const article=page.locator('.teacher-activity-list article').filter({has:page.getByText(title,{exact:true})});await article.waitFor();assert.equal(await article.count(),1);assert.equal(await article.getByText(comment,{exact:true}).count(),1);
 assert.equal(await form.locator('[name=title]').inputValue(),'');assert.equal(await form.locator('[name=description]').inputValue(),'');assert.equal(await form.getByRole('button',{name:'Confirm upload',exact:true}).isDisabled(),true);
 const writes=calls.filter(x=>x.method==='POST'||x.method==='PUT');assert.equal(writes.length,3);assert.equal(writes[0].status,201);assert.equal(writes[1].status,200);assert.equal(writes[2].status,200);
 assert.match(writes[1].path,/\/chunks\/0$/);const id=writes[1].path.split('/')[4];assert.match(id,/^[a-f0-9-]{36}$/);assert.equal(writes[2].path,`/api/teacher/media-uploads/${id}/complete`);
 // Real exported PrivateMaterialActions: HEAD + protected GET, no native-download shortcut.
 await article.getByRole('button',{name:'Preview',exact:true}).click();const saved=article.locator('audio,video');await saved.waitFor();await saved.scrollIntoViewIfNeeded();
 const played=await saved.evaluate(async e=>{await e.play();for(let i=0;i<20&&e.currentTime<=0;i++)await new Promise(r=>setTimeout(r,250));const result={time:e.currentTime,ready:e.readyState,error:e.error?.code??null};e.pause();return result;});assert.ok(played.time>0);assert.ok(played.ready>=2);assert.equal(played.error,null);
 const savedHash=await saved.evaluate(async e=>{const b=await (await fetch(e.src)).arrayBuffer();return {size:b.byteLength,hash:Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',b))).map(x=>x.toString(16).padStart(2,'0')).join('')};});assert.equal(savedHash.size,recorded.size);assert.equal(savedHash.hash,recorded.hash);
 const login=async email=>{const r=await page.request.post(api+'/api/auth/login',{data:{email,password:'Synthetic!39Ab'}});assert.equal(r.status(),200);return (await r.json()).accessToken;};
 const family=await login('qa-blob-student@example.invalid'),foreign=await login('qa-admin-b@example.invalid'),otherTeacher=await login('qa-browser-teacher-b@example.invalid');
 const content=`/api/class-media/${id}/content`;
 const read=async(bearer,expected)=>{const r=await page.request.get(api+content,{headers:bearer?{Authorization:'Bearer '+bearer}:{}});assert.equal(r.status(),expected);if(expected===200){const bytes=await r.body();assert.equal(bytes.length,recorded.size);assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),recorded.hash);}return r.status();};
 await read(family,200);await read(null,401);await read(foreign,404);await read(otherTeacher,404);
 const retry=await page.request.post(api+`/api/teacher/media-uploads/${id}/complete`,{headers:{Authorization:'Bearer '+token}});assert.equal(retry.status(),200);assert.equal((await retry.json()).id,id);
 await page.screenshot({path:path.join(evidence,`${width}-${theme}-saved.png`),fullPage:true});
 return {id,title,recordedBytes:recorded.size,recordedSha256:recorded.hash,savedSha256:savedHash.hash,playback:played,writes:3,completionRetry:200,studentRead:200,anonymousRead:401,foreignTenantRead:404,unassignedTeacherRead:404,nativeDownload:'NOT RUN',physicalDevices:'NOT RUN'};
};
