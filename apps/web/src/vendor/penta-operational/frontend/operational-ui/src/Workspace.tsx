"use client";
import { useEffect, useRef, useState, type FormEvent } from "react";
import { Foundation, Button, Icon, Modal, Notice, Field, TextArea, Select, Popover, DataTable } from "../../shared-foundation/src";
import type { Theme } from "../../shared-foundation/src/tokens";
import { validateProjection } from "./validate";
import type { Projection, HostAdapter, Intent, FieldSpec, Value, Scalar, Artifact } from "./types";

export function OperationalWorkspace({projection,adapter,theme="orbit"}:{projection:unknown;adapter:HostAdapter;theme?:Theme}) {
  let p:Projection;
  try { p=validateProjection(projection); }
  catch { return <Foundation theme={theme} className="penta-operational"><div className="op-invalid"><Notice tone="danger" title="Workspace unavailable">The host context could not be verified. Return to your application and refresh securely.</Notice></div></Foundation>; }
  return <Session key={`${p.context_key}:${p.projection_revision}:${p.host.scope.authority_version}:${p.host.scope.context_version}:${p.host.draft?.revision??0}`} p={p} adapter={adapter} theme={theme}/>;
}

function Session({p,adapter,theme}:{p:Projection;adapter:HostAdapter;theme:Theme}) {
  const [text,setText]=useState(""),[panel,setPanel]=useState<"history"|"help"|"canvas"|"feedback"|null>(null);
  const [feedback,setFeedback]=useState(""),[messageRef,setMessageRef]=useState<string|null>(null);
  const [busy,setBusy]=useState(false),[submitted,setSubmitted]=useState(false),[notice,setNotice]=useState("");
  const controller=useRef<AbortController|null>(null),lock=useRef(false),alive=useRef(true),sequence=useRef(0);
  const composer=useRef<HTMLTextAreaElement>(null);
  useEffect(()=>{alive.current=true;return()=>{alive.current=false;controller.current?.abort();};},[]);
  const draft=p.host.draft,op=p.operation;
  const chosen=p.capabilities.find(c=>c.key===p.selected_capability);
  const canSend=p.controls.send&&chosen?.available&&p.host.availability==="ready"&&!busy&&!submitted;
  const sendIntent=async(intent:Intent)=>{
    // Exactly one in-flight callback per mounted host projection. The host must
    // still reserve/recheck each operation atomically; this is only UI protection.
    if(lock.current)return;
    lock.current=true;const current=++sequence.current,abort=new AbortController();controller.current=abort;setBusy(true);setNotice("");
    try {
      await adapter.onIntent(intent,abort.signal);
      if(alive.current&&sequence.current===current){setSubmitted(true);setNotice("Request submitted. Waiting for the current host result.");}
    } catch {
      if(alive.current&&sequence.current===current){setSubmitted(true);setNotice("Outcome unconfirmed. Check status or refresh in your application before another action.");}
    } finally {if(alive.current&&sequence.current===current){setBusy(false);lock.current=false;}}
  };
  const stopWaiting=()=>{++sequence.current;controller.current?.abort();lock.current=false;setBusy(false);setSubmitted(true);setNotice("Stopped waiting. This does not cancel a server operation. Check its status.");};
  const submit=(e?:FormEvent)=>{e?.preventDefault();if(canSend&&text.trim()){void sendIntent({kind:"send",text:text.trim(),capability:p.selected_capability,base_revision:draft?.revision??0});}};
  const status=()=>{if(op)void sendIntent({kind:"status",operation_ref:op.ref,revision:op.draft_revision,payload_hash:op.payload_hash});};
  const showSources=(source:string)=>{void sendIntent({kind:"source",source_ref:source});};
  const disabled=busy||submitted||p.host.availability!=="ready";
  const latestStatus=p.receipt?.status??op?.status??draft?.status;
  return <Foundation theme={theme} className="penta-operational">
    <header className="op-header">
      <div className="op-brand"><span className="op-mark"><Icon name="spark"/></span><div><strong>PENTA</strong><small>{p.workspace_label}</small></div></div>
      <div className="op-identity"><span>{p.actor_label}</span><small>{p.role_label}</small></div>
      <div className="op-header-actions">
        <Popover label="Appearance"><p>Choose your workspace theme.</p>{(["orbit","aura","prism"] as Theme[]).map(t=><Button key={t} data-pui-close aria-pressed={theme===t} disabled={!adapter.onTheme} onClick={()=>adapter.onTheme?.(t)}>{t==="prism"?"Prism · Light":t==="aura"?"Aura · Dark":"Orbit · Dark"}</Button>)}</Popover>
      </div>
    </header>
    <div className="op-toolbar">
      {p.surface==="academy"&&adapter.onManual&&<Button onClick={adapter.onManual}>Manual workspace</Button>}
      <Button disabled={busy} onClick={()=>void sendIntent({kind:"new_conversation"})}>New chat</Button>
      {p.controls.history&&<Button onClick={()=>setPanel("history")}>History</Button>}
      <Popover label={p.selected_capability}><div className="op-options">{p.capabilities.map(c=><Button key={c.key} data-pui-close disabled={!c.available||disabled} aria-pressed={p.selected_capability===c.key} title={c.reason} onClick={()=>void sendIntent({kind:"select_capability",capability:c.key})}><strong>{c.key}</strong><small>{c.reason}</small></Button>)}</div></Popover>
      {p.models.length>0&&<Popover label="Model"><div className="op-options">{p.models.map(m=><Button key={m.ref} data-pui-close disabled={!m.qualified||disabled} aria-pressed={p.selected_model_ref===m.ref} onClick={()=>void sendIntent({kind:"select_model",model_ref:m.ref})}>{m.label}<small>{m.qualified?"Qualified for this host context":"Unavailable · not qualified"}</small></Button>)}</div></Popover>}
      <Button onClick={()=>setPanel("help")}>Capability help</Button>
      {(draft||op||p.observation)&&<Button className="op-canvas-button" onClick={()=>setPanel("canvas")}>Work canvas {latestStatus&&<span className="op-status">{latestStatus.replaceAll("_"," ")}</span>}</Button>}
    </div>
    <main className="op-main">
      <section className="op-transcript" aria-label="Conversation" tabIndex={0}>
        {p.messages.length===0?<div className="op-welcome"><span className="op-orb"><Icon name="spark"/></span><p className="op-eyebrow">YOUR INTELLIGENT WORKSPACE</p><h1>What shall we work on?</h1><p>Start a conversation. Bring the next step into focus.</p><div className="op-hints">{p.capabilities.map(c=><button type="button" disabled={!c.available||disabled} key={c.key} onClick={()=>{setText(`Help me with ${c.key.toLowerCase()}.`);composer.current?.focus();}}><strong>{c.key}</strong><span>{c.reason}</span></button>)}</div></div>:
        <div className="op-messages">{p.messages.map(m=><article key={m.ref} className={`op-message op-${m.kind}`}><header><strong>{m.kind==="user"?"You":"PENTA"}</strong>{m.kind!=="user"&&<span>{m.kind==="observed"?"Source observation":m.kind}</span>}</header><p>{m.text}</p>{m.kind==="observed"&&p.host.evidence.filter(e=>m.source_refs.includes(e.source_ref)).map(e=><p className="op-muted" key={e.ref}>As of {e.as_of} · {e.coverage} coverage</p>)}{m.source_refs.length>0&&<div className="op-sources">{m.source_refs.map(s=><Button key={s} onClick={()=>showSources(s)}>Source · {s}</Button>)}</div>}{m.kind!=="user"&&p.controls.feedback&&<Button variant="quiet" onClick={()=>{setMessageRef(m.ref);setPanel("feedback");}}>Give feedback</Button>}</article>)}</div>}
        {p.observation&&<div className="op-result-grid">{p.observation.artifacts.map(a=><Button key={a.ref} className="op-result" onClick={()=>setPanel("canvas")}><span className="op-eyebrow">{a.kind} · {a.provenance.coverage} coverage</span><strong>{a.title}</strong><span>Open work canvas <Icon name="arrow"/></span></Button>)}</div>}
      </section>
      <div className="op-composer-area">
        {p.host.availability!=="ready"&&<Notice tone="warning" title={`Connection ${p.host.availability}`}>Current authorized context is required. Operations are unavailable.</Notice>}
        {!chosen?.available&&<p className="op-muted" role="status">{chosen?.reason}</p>}
        {notice&&<div className="op-recovery" role="status"><span>{notice}</span>{p.controls.reconcile&&op&&<Button disabled={busy} onClick={status}>Check status</Button>}</div>}
        <form className="op-composer" onSubmit={submit}>
          <label htmlFor="penta-operational-composer">Ask PENTA</label>
          <textarea id="penta-operational-composer" ref={composer} placeholder="Ask, explore, or prepare your next step…" value={text} maxLength={2000} disabled={!canSend} rows={2} onChange={e=>setText(e.target.value)} onKeyDown={e=>{if(e.key==="Enter"&&!e.shiftKey&&!e.nativeEvent.isComposing){e.preventDefault();submit();}}}/>
          <div className="op-composer-actions"><span>{p.selected_capability} <span aria-hidden="true">·</span> {text.length}/2000</span><div>{p.controls.secure_media&&<Button disabled={disabled} onClick={()=>void sendIntent({kind:"open_secure_media"})}>Secure media</Button>}{busy?<Button onClick={stopWaiting}>Stop waiting</Button>:<Button variant="primary" type="submit" disabled={!canSend||!text.trim()}><Icon name="arrow"/><span>Send</span></Button>}</div></div>
        </form>
        <small className="op-footer">Review sources and the exact preview before confirming an action.</small>
      </div>
    </main>
    <Modal open={panel==="history"} title="Conversation history" onClose={()=>setPanel(null)} drawer><div className="op-options">{p.history.length===0?<p>No authorized history is available.</p>:p.history.map(h=><Button disabled={busy} key={h.ref} onClick={()=>void sendIntent({kind:"history",conversation_ref:h.ref})}>{h.title}</Button>)}</div></Modal>
    <Modal open={panel==="help"} title="Your capabilities" onClose={()=>setPanel(null)}>{p.capabilities.map(c=><section key={c.key}><h3>{c.key} {!c.available&&"· Unavailable"}</h3><p>{c.reason}</p></section>)}</Modal>
    <Modal open={panel==="feedback"} title="Review feedback" onClose={()=>setPanel(null)}><p>Your host reviews feedback under its privacy and retention policy. Feedback does not change model weights.</p><TextArea label="Correction or feedback" maxLength={500} value={feedback} onChange={e=>setFeedback(e.target.value)}/><Button variant="primary" disabled={!p.controls.feedback||busy||submitted||!feedback.trim()||!messageRef} onClick={()=>{if(messageRef)void sendIntent({kind:"feedback",message_ref:messageRef,text:feedback.trim()});}}>Submit for review</Button></Modal>
    <Modal open={panel==="canvas"} title="Work canvas" onClose={()=>setPanel(null)} drawer>
      <div className="op-canvas">
        {p.receipt&&<Notice title={`Host outcome · ${p.receipt.status.replaceAll("_"," ")}`} tone={p.receipt.status==="succeeded"?"success":p.receipt.status==="unknown"?"warning":"info"}><p>{p.receipt.status==="succeeded"?`Receipt ${p.receipt.receipt_ref} · Audit ${p.receipt.audit_ref}`:"Only a committed host receipt confirms success."}</p>{p.controls.reconcile&&<Button disabled={busy} onClick={status}>Check status</Button>}</Notice>}
        {p.validation&&<section><h3>Host validation · {p.validation.status.replaceAll("_"," ")}</h3>{p.validation.issues.map((i,n)=><div key={n} className="op-issue"><strong>{i.field??"Operation"} · {i.code.replaceAll("_"," ")}</strong>{i.choices.map(c=><p key={c.id}>{c.label} · {c.id}</p>)}{i.code==="duplicate_candidates"&&<p>Potential matches require host review. No record has been selected or merged.</p>}{i.code==="secure_host_step"&&<p>Complete this step in your application’s secure interface.</p>}</div>)}</section>}
        {draft&&<section><h3>Draft · revision {draft.revision}</h3><p>{draft.status.replaceAll("_"," ")}</p><DraftEditor p={p} disabled={disabled||!p.controls.prepare} onSave={sendIntent}/><div className="op-lifecycle">{(["cancel","pause","resume"] as const).filter(k=>p.controls[k]).map(k=><Button key={k} disabled={disabled} variant={k==="cancel"?"danger":"secondary"} onClick={()=>void sendIntent({kind:k,draft_ref:draft.ref,revision:draft.revision})}>{k[0].toUpperCase()+k.slice(1)} draft</Button>)}</div></section>}
        {op&&<section className="op-exact-preview"><p className="op-eyebrow">HOST CANONICAL PREVIEW</p><h3>Review the exact operation</h3><p className="op-pre-wrap">{op.preview}</p><dl>{Object.entries(op.canonical_fields).map(([key,value])=><div key={key}><dt>{p.host.tools.find(t=>t.key===op.tool_key)?.fields.find(f=>f.key===key)?.label??key}</dt><dd>{value.kind==="rows"?<pre className="op-pre-wrap">{JSON.stringify(value.value,null,2)}</pre>:String(value.value)}</dd></div>)}<dt>Operation</dt><dd>{op.ref}</dd><dt>Revision</dt><dd>{op.draft_revision}</dd><dt>Payload digest</dt><dd>{op.payload_hash}</dd><dt>Approval expires</dt><dd>{op.approval_expires_at??"No approval issued"}</dd></dl>{p.controls.confirm&&<Button variant="primary" disabled={disabled} onClick={()=>{if(op.approval_ref)void sendIntent({kind:"confirm",operation_ref:op.ref,approval_ref:op.approval_ref,revision:op.draft_revision,payload_hash:op.payload_hash});}}>Confirm exact operation</Button>}<p className="op-muted">Any edit requires a new host preview and approval. A chat reply alone cannot authorize execution.</p></section>}
        {p.observation?.artifacts.map(a=><ArtifactView key={a.ref} artifact={a} onSource={showSources}/>)}
      </div>
    </Modal>
  </Foundation>;
}

function DraftEditor({p,disabled,onSave}:{p:Projection;disabled:boolean;onSave:(intent:Intent)=>Promise<void>}) {
  const draft=p.host.draft!,tool=p.host.tools.find(t=>t.key===draft.tool_key);
  const [changes,setChanges]=useState<Record<string,Value>>({}),[remove,setRemove]=useState<string[]>([]);
  if(!tool)return <p>Current host field definitions are unavailable. Request a refreshed draft.</p>;
  const set=(key:string,value:Value|undefined)=>{setChanges(prev=>{const next={...prev};if(value)next[key]=value;else delete next[key];return next;});setRemove(prev=>prev.filter(k=>k!==key));};
  const clear=(key:string)=>{setChanges(prev=>{const next={...prev};delete next[key];return next;});setRemove(prev=>[...prev.filter(k=>k!==key),key]);};
  return <form className="op-draft-form" onSubmit={e=>{e.preventDefault();void onSave({kind:"edit",draft_ref:draft.ref,revision:draft.revision,edit:{mode:"refine",set:changes,remove}});}}>
    {tool.fields.map(f=><div key={f.key} className="op-draft-field"><ValueControl spec={f} value={remove.includes(f.key)?undefined:changes[f.key]??draft.fields[f.key]} disabled={disabled||remove.includes(f.key)} maxItems={tool.max_items} onChange={v=>set(f.key,v)}/>{f.clearable&&<div className="op-field-actions"><Button disabled={disabled} onClick={()=>remove.includes(f.key)?set(f.key,draft.fields[f.key]):clear(f.key)}>{remove.includes(f.key)?"Undo clear":"Clear"} {f.label}</Button>{remove.includes(f.key)&&<small>Will be removed on submission</small>}</div>}</div>)}
    <Button type="submit" variant="primary" disabled={disabled||(!Object.keys(changes).length&&!remove.length)}>Request revised preview</Button>
  </form>;
}

function ValueControl({spec:f,value,disabled,maxItems,onChange}:{spec:FieldSpec;value:Value|undefined;disabled:boolean;maxItems:number;onChange:(v:Value|undefined)=>void}) {
  const label=`${f.label}${f.required?" (required)":""}`;
  if(f.kind==="rows"){
    const rows=value?.kind==="rows"?value.value:[];
    return <fieldset disabled={disabled}><legend>{label}</legend>{rows.map((row,i)=><fieldset key={i}><legend>Item {i+1}</legend>{f.columns.map(c=><ValueControl key={c.key} spec={c} value={row[c.key]} maxItems={1} disabled={disabled} onChange={v=>{if(v?.kind==="rows")return;const next=rows.map(r=>({...r}));if(v)next[i][c.key]=v;else delete next[i][c.key];onChange({kind:"rows",value:next});}}/>)}<Button onClick={()=>onChange({kind:"rows",value:rows.filter((_,n)=>n!==i)})}>Remove item {i+1}</Button></fieldset>)}<Button disabled={disabled||rows.length>=maxItems} onClick={()=>onChange({kind:"rows",value:[...rows,{}]})}>Add item</Button></fieldset>;
  }
  const scalar=value?.kind!=="rows"?value:undefined;
  if(f.kind==="reference")return <Select label={label} disabled={disabled} value={String(scalar?.value??"")} options={[{value:"",label:"Choose an authorized reference"},...f.choices.map(c=>({value:c.id,label:`${c.label} · ${c.id}`}))]} onChange={v=>onChange(v?{kind:"reference",value:v}:undefined)}/>;
  if(f.kind==="boolean")return <Select label={label} disabled={disabled} value={scalar?String(scalar.value):""} options={[{value:"",label:"Not supplied"},{value:"true",label:"Yes"},{value:"false",label:"No"}]} onChange={v=>onChange(v?{kind:"boolean",value:v==="true"}:undefined)}/>;
  return <Field label={label} disabled={disabled} maxLength={f.max_length} type={f.kind==="date"?"date":f.kind==="integer"?"number":"text"} step={f.kind==="integer"?1:undefined} inputMode={f.kind==="decimal"?"decimal":undefined} value={String(scalar?.value??"")} onChange={e=>{const v=e.target.value;if(f.kind==="integer")onChange(v&&Number.isSafeInteger(Number(v))?{kind:"integer",value:Number(v)}:undefined);else if(f.kind!=="rows"&&f.kind!=="reference"&&f.kind!=="boolean")onChange({kind:f.kind,value:v} as Scalar);}}/>;
}

function ArtifactView({artifact:a,onSource}:{artifact:Artifact;onSource:(ref:string)=>void}) {
  return <section className="op-artifact"><h3>{a.title}</h3>{a.kind==="text"&&<p className="op-pre-wrap">{a.text}</p>}{a.kind==="table"&&<DataTable caption={a.title} columns={a.columns.map(c=>c.label)} rows={a.rows.map((r,i)=>({key:String(i),cells:a.columns.map(c=>r[c.key]?String(r[c.key].value):"Not supplied")}))}/>}{a.kind==="chart"&&<><p>{a.chart==="line"?"Trend":"Comparison"} · {a.unit}</p><svg role="img" aria-label={`${a.title} · ${a.unit}. Exact values in the table below.`} viewBox="0 0 600 180" className="op-chart"><title>{a.title}</title>{a.chart==="bar"?a.points.map((point,i)=>{const max=Math.max(...a.points.map(p=>Math.abs(Number(p.value))),1),width=500*Math.abs(Number(point.value))/max;return <rect key={i} x="50" y={i*160/a.points.length+5} width={width} height={Math.max(1,140/a.points.length)}><title>{point.label}: {point.value} {a.unit}</title></rect>;}):<polyline fill="none" stroke="currentColor" strokeWidth="3" points={a.points.map((point,i)=>{const vals=a.points.map(p=>Number(p.value)),min=Math.min(...vals),max=Math.max(...vals);return `${20+i*560/Math.max(1,a.points.length-1)},${160-140*(Number(point.value)-min)/Math.max(1,max-min)}`;}).join(" ")}/>}</svg><DataTable caption={`${a.title} exact values`} columns={["Item",a.unit]} rows={a.points.map((point,i)=>({key:String(i),cells:[point.label,point.value]}))}/></>}
    <p className="op-muted">As of {a.provenance.as_of} · {a.provenance.coverage} coverage</p><div className="op-sources">{a.provenance.source_refs.map(s=><Button key={s} onClick={()=>onSource(s)}>View source · {s}</Button>)}</div>
  </section>;
}
