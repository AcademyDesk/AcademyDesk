"use client";
import { useCallback, useEffect, useId, useRef, useState, type Ref, type ReactNode, type ButtonHTMLAttributes, type InputHTMLAttributes, type TextareaHTMLAttributes } from "react";
import type { Theme } from "./tokens";
import { FOUNDATION_VERSION } from "./tokens";

export function Foundation({ theme, children, className="" }: { theme:Theme; children:ReactNode; className?:string }) {
  return <div className={`penta-ui ${className}`} data-penta-foundation={FOUNDATION_VERSION} data-penta-theme={theme}>{children}</div>;
}
export function Icon({ name }: { name:"spark"|"arrow"|"close"|"menu"|"search"|"grid"|"check" }) {
  const paths = { spark:"M12 2l2.8 7.2L22 12l-7.2 2.8L12 22l-2.8-7.2L2 12l7.2-2.8z",arrow:"M4 12h16m-6-6 6 6-6 6",close:"m6 6 12 12M6 18 18 6",menu:"M4 6h16M4 12h16M4 18h16",search:"M15 15l6 6M17 10a7 7 0 1 1-14 0 7 7 0 0 1 14 0",grid:"M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z",check:"m4 12 5 5L20 6" };
  return <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name]}/></svg>;
}
export function Button({ variant="secondary", className="", ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { ref?:Ref<HTMLButtonElement>; variant?:"primary"|"secondary"|"quiet"|"danger" }) {
  return <button type="button" {...props} className={`pui-button pui-button-${variant} ${className}`}/>;
}
export function IconAction({ label, icon, ...props }: Omit<ButtonHTMLAttributes<HTMLButtonElement>,"aria-label"|"children"> & { label:string;icon:Parameters<typeof Icon>[0]["name"] }) {
  return <Button {...props} className="pui-icon-action" aria-label={label} title={label}><Icon name={icon}/></Button>;
}
export function Field({ label, help, error, ...props }: InputHTMLAttributes<HTMLInputElement> & { label:string;help?:string;error?:string }) {
  const uid=useId(), id=props.id ?? uid, detail=`${id}-detail`;
  return <div className="pui-field"><label htmlFor={id}>{label}</label><input {...props} id={id} aria-invalid={!!error} aria-describedby={help||error?detail:undefined}/>{(help||error)&&<span id={detail} className={error?"pui-error":"pui-help"}>{error||help}</span>}</div>;
}
export function TextArea({label,help,error,...props}:TextareaHTMLAttributes<HTMLTextAreaElement>&{label:string;help?:string;error?:string}) {
  const id=useId(); return <div className="pui-field"><label htmlFor={id}>{label}</label><textarea {...props} id={id} aria-invalid={!!error} aria-describedby={help||error?`${id}-help`:undefined}/>{(help||error)&&<span id={`${id}-help`} className={error?"pui-error":"pui-help"}>{error||help}</span>}</div>;
}
export function Select({label,value,onChange,options,disabled=false,help}: {label:string;value:string;onChange:(value:string)=>void;options:readonly {value:string;label:string}[];disabled?:boolean;help?:string}) {
  const id=useId();return <div className="pui-field"><label htmlFor={id}>{label}</label><select id={id} value={value} disabled={disabled} onChange={e=>onChange(e.target.value)} aria-describedby={help?`${id}-help`:undefined}>{options.map(o=><option key={o.value} value={o.value}>{o.label}</option>)}</select>{help&&<span className="pui-help" id={`${id}-help`}>{help}</span>}</div>;
}
export function Card({title,eyebrow,children,className=""}: {title?:string;eyebrow?:string;children:ReactNode;className?:string}) {
  return <section className={`pui-card ${className}`}>{eyebrow&&<p className="pui-eyebrow">{eyebrow}</p>}{title&&<h2>{title}</h2>}{children}</section>;
}
export function Kpi({label,value,note}: {label:string;value:string;note:string}) {return <Card className="pui-kpi"><span className="pui-muted">{label}</span><strong>{value}</strong><small>{note}</small></Card>;}
export function Notice({title,children,tone="info",onDismiss}: {title:string;children?:ReactNode;tone?:"info"|"success"|"warning"|"danger";onDismiss?:()=>void}) {
  return <div className="pui-notice" data-tone={tone} role={tone==="danger"?"alert":"status"}><div><strong>{title}</strong>{children&&<div>{children}</div>}</div>{onDismiss&&<IconAction label="Dismiss notice" icon="close" onClick={onDismiss}/>}</div>;
}
export function Tabs({label,items,value,onChange}: {label:string;items:readonly {key:string;label:string;disabled?:boolean}[];value:string;onChange:(key:string)=>void}) {
  // A wrapping single-choice group: normal Tab sequence, not an incomplete ARIA tab widget.
  return <div className="pui-tabs" role="group" aria-label={label}>{items.map(item=><Button key={item.key} disabled={item.disabled} aria-pressed={value===item.key} onClick={()=>onChange(item.key)}>{item.label}</Button>)}</div>;
}
export function Navigation({label,items,value,onNavigate}: {label:string;items:readonly {key:string;label:string;available:boolean;unavailableReason?:string}[];value:string;onNavigate:(key:string)=>void}) {
  return <nav aria-label={label} className="pui-nav">{items.map(item=><Button key={item.key} aria-current={value===item.key?"page":undefined} disabled={!item.available} title={item.unavailableReason} onClick={()=>onNavigate(item.key)}><Icon name="grid"/><span>{item.label}{!item.available&&<small>Unavailable</small>}</span></Button>)}</nav>;
}
export function Popover({label,children,onOpen}: {label:string;children:ReactNode;onOpen?:()=>void}) {
  const id=useId(), anchor=useRef<HTMLButtonElement>(null), panel=useRef<HTMLDivElement>(null);
  const [position,setPosition]=useState({left:16,top:16,width:320,maxHeight:300});
  const [expanded,setExpanded]=useState(false);
  const place=useCallback(()=>{
    const a=anchor.current?.getBoundingClientRect(); if(!a)return;
    const w=window.innerWidth,h=window.innerHeight;
    if(a.bottom<0||a.top>h){panel.current?.hidePopover();return;}
    const width=Math.min(344,w-24),below=h-a.bottom-12,above=a.top-12;
    const maxHeight=Math.max(44,Math.min(380,Math.max(below,above)-12));
    setPosition({left:Math.max(12,Math.min(a.left,w-width-12)),top:below>=above?Math.max(12,a.bottom+8):Math.max(12,a.top-maxHeight-8),width,maxHeight});
  },[]);
  useEffect(()=>{
    const p=panel.current;
    const onToggle=()=>{setExpanded(!!p?.matches(":popover-open"));};
    const move=()=>{if(p?.matches(":popover-open"))place();};
    const escape=(e:KeyboardEvent)=>{if(e.key==="Escape"&&p?.matches(":popover-open")){p.hidePopover();anchor.current?.focus();}};
    p?.addEventListener("toggle",onToggle);window.addEventListener("scroll",move,true);window.addEventListener("resize",move);document.addEventListener("keydown",escape);
    return()=>{p?.removeEventListener("toggle",onToggle);window.removeEventListener("scroll",move,true);window.removeEventListener("resize",move);document.removeEventListener("keydown",escape);};
  },[place]);
  const close=()=>{panel.current?.hidePopover();setExpanded(false);anchor.current?.focus();};
  return <div className="pui-popover-anchor"><Button ref={anchor} aria-expanded={expanded} aria-controls={id} onClick={()=>{if(!panel.current?.matches(":popover-open"))onOpen?.();place();panel.current?.togglePopover();setExpanded(!!panel.current?.matches(":popover-open"));}}>{label} <span aria-hidden="true">⌄</span></Button><div ref={panel} id={id} popover="auto" className="pui-popover" aria-label={label} style={position} onClick={e=>{if(e.target instanceof Element&&e.target.closest("button[data-pui-close]"))close();}}>{children}</div></div>;
}
export function SearchMenu({label,items,onSelect}: {label:string;items:readonly {key:string;label:string}[];onSelect:(key:string)=>void}) {
  const [query,setQuery]=useState("");
  return <Popover label={label}><Field label={`Search ${label.toLowerCase()}`} type="search" value={query} onChange={e=>setQuery(e.target.value)}/><div className="pui-menu">{items.filter(item=>item.label.toLowerCase().includes(query.toLowerCase())).map(item=><Button key={item.key} data-pui-close onClick={()=>onSelect(item.key)}>{item.label}</Button>)}</div>{!items.some(item=>item.label.toLowerCase().includes(query.toLowerCase()))&&<p>No matching sample items.</p>}</Popover>;
}
export function Modal({open,title,onClose,children,drawer=false}: {open:boolean;title:string;onClose:()=>void;children:ReactNode;drawer?:boolean}) {
  const dialog=useRef<HTMLDialogElement>(null),id=useId();
  useEffect(()=>{const d=dialog.current;if(!d)return;if(open&&!d.open)d.showModal();if(!open&&d.open)d.close();},[open]);
  return <dialog ref={dialog} aria-labelledby={id} className={`pui-dialog ${drawer?"pui-drawer":""}`} onCancel={e=>{e.preventDefault();onClose();}} onClick={e=>{if(e.target!==e.currentTarget)return;const r=e.currentTarget.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)onClose();}}>
    <header><h2 id={id}>{title}</h2><IconAction autoFocus label={`Close ${title}`} icon="close" onClick={onClose}/></header><div className="pui-dialog-content">{children}</div>
  </dialog>;
}
export function Confirmation({open,title,onCancel,onConfirm,children,pending=false}: {open:boolean;title:string;onCancel:()=>void;onConfirm:()=>void;children:ReactNode;pending?:boolean}) {
  return <Modal open={open} title={title} onClose={onCancel}>{children}<div className="pui-actions"><Button disabled={pending} onClick={onCancel}>Cancel</Button><Button variant="primary" disabled={pending} onClick={onConfirm}>Confirm demo only</Button></div></Modal>;
}
export type TableRow={key:string;cells:readonly ReactNode[]};
export function DataTable({caption,columns,rows}: {caption:string;columns:readonly string[];rows:readonly TableRow[]}) {
  return <div className="pui-table-scroll" role="region" aria-label={caption} tabIndex={0}><table><caption>{caption}</caption><thead><tr>{columns.map(c=><th scope="col" key={c}>{c}</th>)}</tr></thead><tbody>{rows.map(row=><tr key={row.key}>{row.cells.map((cell,i)=>i===0?<th scope="row" key={i}>{cell}</th>:<td key={i}>{cell}</td>)}</tr>)}</tbody></table></div>;
}
export function Pagination({page,pages,onChange}: {page:number;pages:number;onChange:(page:number)=>void}) {
  return <div className="pui-actions" role="group" aria-label="Record pages"><Button disabled={page<=1} onClick={()=>onChange(page-1)}>Previous</Button><span>Page {page} of {Math.max(1,pages)}</span><Button disabled={page>=pages} onClick={()=>onChange(page+1)}>Next</Button></div>;
}
export function DateField({label,value,onChange}: {label:string;value:string;onChange:(value:string)=>void}) {
  // Native date entry + explicit month/year/day chooser. No timezone conversion.
  const parts=value.split("-").map(Number),[year,setYear]=useState(parts[0]||2026),[month,setMonth]=useState(parts[1]||10),[day,setDay]=useState(parts[2]||13);
  const days=new Date(Date.UTC(year,month,0)).getUTCDate();
  const selectDate=()=>{onChange(`${year}-${String(month).padStart(2,"0")}-${String(Math.min(day,days)).padStart(2,"0")}`);};
  return <div className="pui-date"><Field label={label} type="date" value={value} onChange={e=>{onChange(e.target.value);const p=e.target.value.split("-").map(Number);if(p.length===3){setYear(p[0]);setMonth(p[1]);setDay(p[2]);}}}/><Popover label={`Choose ${label.toLowerCase()}`} onOpen={()=>{const p=value.split("-").map(Number);setYear(p[0]||2026);setMonth(p[1]||1);setDay(p[2]||1);}}><Select label="Month" value={String(month)} onChange={v=>setMonth(Number(v))} options={Array.from({length:12},(_,i)=>({value:String(i+1),label:["January","February","March","April","May","June","July","August","September","October","November","December"][i]}))}/><Select label="Year" value={String(year)} onChange={v=>setYear(Number(v))} options={Array.from({length:201},(_,i)=>({value:String(1900+i),label:String(1900+i)}))}/><Select label="Day" value={String(Math.min(day,days))} onChange={v=>setDay(Number(v))} options={Array.from({length:days},(_,i)=>({value:String(i+1),label:String(i+1)}))}/><Button variant="primary" data-pui-close onClick={selectDate}>Apply date</Button></Popover></div>;
}

