"use client";
import {FormEvent,useEffect,useRef,useState} from "react";import {WorkspaceNav} from "@/components/workspace-nav";import {academyApi,apiHeaders} from "@/lib/api";
import { StandardDateField, StandardSelectField } from "@/components/design-system/controls";
import { RequiredControlValue } from "@/components/design-system/required-control-value";
import { OnboardingField } from "@/components/design-system/onboarding-field";
import styles from "@/components/design-system/operations-workspace.module.css";
type A={id:string};type Person={id:string;firstName:string;lastName:string};type Leave={id:string;requesterType:string;studentId?:string;teacherId?:string;startDate:string;endDate:string;reason:string;status:string};
async function fetchWorkspace(z:string):Promise<{students:Person[];teachers:Person[];leaves:Leave[]}>{const[r1,r2,r3]=await Promise.all([academyApi(`/api/academies/${z}/students`),academyApi(`/api/academies/${z}/teachers`),academyApi(`/api/academies/${z}/leave-requests`)]);if(!r1.ok||!r2.ok||!r3.ok)throw new Error();return{students:await r1.json(),teachers:await r2.json(),leaves:await r3.json()}}
export default function LeavePage(){const[a,setA]=useState<A>();const[s,setS]=useState<Person[]>([]);const[t,setT]=useState<Person[]>([]);const[l,setL]=useState<Leave[]>([]);const[type,setType]=useState("Student");const[id,setId]=useState("");const[start,setStart]=useState("");const[end,setEnd]=useState("");const[reason,setReason]=useState("");const[m,setM]=useState("Loading leave requests…");const[saving,setSaving]=useState(false);const pendingWrite=useRef(false);async function load(){if(!a)return;const data=await fetchWorkspace(a.id);setS(data.students);setT(data.teachers);setL(data.leaves);}useEffect(()=>{void(async()=>{try{const r=await academyApi("/api/academies");if(!r.ok)throw new Error();const x:A[]=await r.json();if(!x[0])throw new Error();setA(x[0]);const data=await fetchWorkspace(x[0].id);setS(data.students);setT(data.teachers);setL(data.leaves);setM("")}catch{setM("Leave requests could not be loaded. Please refresh or check your access.")}})()},[]);
async function refreshAfterSave(success:string){setM(success);try{await load()}catch{setM(success+" The list could not be refreshed; refresh to see the saved record and do not submit it again.")}}
async function failure(response:Response,fallback:string){const result=await response.json().catch(()=>null);const detail=typeof result?.message==="string"?result.message.trim():"";setM(response.status>=500?(detail?detail+" ":"")+"The leave change could not be confirmed; check the saved record before retrying.":detail||fallback)}
async function create(e:FormEvent){e.preventDefault();if(!a||pendingWrite.current)return;pendingWrite.current=true;setSaving(true);setM("");try{const r=await academyApi(`/api/academies/${a.id}/leave-requests`,{method:"POST",headers:apiHeaders(true),body:JSON.stringify({requesterType:type,studentId:type==="Student"?id:null,teacherId:type==="Teacher"?id:null,startDate:start,endDate:end,reason})});if(!r.ok)return await failure(r,"Check the person, reason, and dates.");setId("");setStart("");setEnd("");setReason("");await refreshAfterSave("Leave request submitted.")}catch{setM("The leave change could not be confirmed; check the saved record before retrying.")}finally{pendingWrite.current=false;setSaving(false)}}
async function decide(x:Leave,status:string){if(!a||pendingWrite.current)return;pendingWrite.current=true;setSaving(true);setM("");try{const r=await academyApi(`/api/academies/${a.id}/leave-requests/${x.id}`,{method:"PATCH",headers:apiHeaders(true),body:JSON.stringify({status,notes:null})});if(!r.ok)return await failure(r,"The leave decision could not be saved.");await refreshAfterSave(`Leave request ${status.toLowerCase()}.`)}catch{setM("The leave change could not be confirmed; check the saved record before retrying.")}finally{pendingWrite.current=false;setSaving(false)}}
const people=type==="Student"?s:t;const name=(x:Leave)=>{const p=(x.requesterType==="Student"?s:t).find(v=>v.id===(x.requesterType==="Student"?x.studentId:x.teacherId));return p?`${p.firstName} ${p.lastName}`:"Unknown"};
return <main className={`${styles.page} enterprise-settings enterprise-legacy-standard min-h-screen bg-slate-950 text-slate-100`}><WorkspaceNav/><div className={styles.body}>
  <header className={styles.heading}><span aria-hidden="true">◷</span><div><p>Operations</p><h1>Leave requests</h1></div></header>
  {m&&<p role="status" aria-live="polite" className="mt-6 rounded-lg border border-amber-700/50 bg-amber-950/40 p-4 text-amber-100">{m}</p>}
  <section className={styles.layout}>
    <form onSubmit={create} className={styles.panel} aria-label="Create leave request" aria-busy={saving}><h2>Create request</h2><div className={styles.fields}>
      <StandardSelectField name="requester-type" label="Requester type" value={type} onChange={value=>{if(value){setType(value);setId("")}}} disabled={saving||!a} placeholder="Select requester type" options={["Student","Teacher"].map(value=>({value,label:value}))}/>
      <StandardSelectField name="requester" label={`${type} (required)`} value={id} onChange={setId} disabled={saving||!a} required placeholder={`Select ${type.toLowerCase()}`} options={people.map(x=>({value:x.id,label:`${x.firstName} ${x.lastName}`}))}/>
      <div className={styles.dates}>
        <div className={styles.dateField}><RequiredControlValue value={start} label="Start date (required)" disabled={saving||!a}/><StandardDateField name="leave-start" label="Start date (required)" value={start} onChange={setStart} required disabled={saving||!a}/></div>
        <div className={styles.dateField}><RequiredControlValue value={end} label="End date (required)" disabled={saving||!a}/><StandardDateField name="leave-end" label="End date (required)" value={end} onChange={setEnd} required disabled={saving||!a}/></div>
      </div>
      <OnboardingField label="Reason (required)"><textarea disabled={saving||!a} value={reason} onChange={e=>setReason(e.target.value)} placeholder="Reason" required/></OnboardingField>
      <button disabled={saving||!a} className={`${styles.action} ${styles.primary}`}>{saving?"Saving…":"Submit request"}</button>
    </div></form>
    <section className={styles.panel}><h2>Requests</h2>{l.length===0?<p className="mt-5 text-slate-400">No leave requests.</p>:<ul className={styles.register}>{l.map(x=><li key={x.id} className={styles.record}>
      <b>{name(x)} · {x.requesterType}</b><small>{x.startDate} to {x.endDate}</small><p className="mt-3">{x.reason}</p>
      <div className={styles.actions}><span className="text-sm text-cyan-200">{x.status}</span>{x.status==="Requested"&&<>
        <button disabled={saving||!a} onClick={()=>decide(x,"Approved")} className={`${styles.action} ${styles.primary}`}>Approve</button>
        <button disabled={saving||!a} onClick={()=>decide(x,"Rejected")} className={styles.action}>Reject</button>
      </>}</div>
    </li>)}</ul>}</section>
  </section>
</div></main>}
