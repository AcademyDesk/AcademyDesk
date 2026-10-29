import schema from "./projection.schema.json";
import type { Projection, Scope, FieldSpec, Value } from "./types";

type Schema = { $ref?:string; anyOf?:Schema[]; const?:unknown; enum?:unknown[]; type?:string; properties?:Record<string,Schema>; required?:string[]; additionalProperties?:boolean|Schema; propertyNames?:Schema; items?:Schema; minItems?:number; maxItems?:number; minLength?:number; maxLength?:number; minProperties?:number; maxProperties?:number; minimum?:number; maximum?:number; pattern?:string; $defs?:Record<string,Schema> };
const fail=():never=>{throw new Error("Invalid or unbound host projection");};
function check(s:Schema,v:unknown,depth=0):boolean {
  if(depth>20)return false;
  if(s.$ref){const name=s.$ref.replace("#/$defs/","");return !!schema.$defs[name as keyof typeof schema.$defs]&&check(schema.$defs[name as keyof typeof schema.$defs] as Schema,v,depth+1);}
  if(s.anyOf&&!s.anyOf.some(x=>check(x,v,depth+1)))return false;
  if("const" in s&&v!==s.const)return false;
  if(s.enum&&!s.enum.includes(v))return false;
  if(s.type==="null")return v===null;
  if(s.type==="boolean")return typeof v==="boolean";
  if(s.type==="integer"||s.type==="number")return typeof v==="number"&&Number.isFinite(v)&&(s.type!=="integer"||Number.isSafeInteger(v))&&(s.minimum===undefined||v>=s.minimum)&&(s.maximum===undefined||v<=s.maximum);
  if(s.type==="string")return typeof v==="string"&&(s.minLength===undefined||[...v].length>=s.minLength)&&(s.maxLength===undefined||[...v].length<=s.maxLength)&&(!s.pattern||new RegExp(s.pattern,"u").test(v));
  if(s.type==="array")return Array.isArray(v)&&(s.minItems===undefined||v.length>=s.minItems)&&(s.maxItems===undefined||v.length<=s.maxItems)&&(!s.items||v.every(x=>check(s.items!,x,depth+1)));
  if(s.type==="object"){
    if(v===null||typeof v!=="object"||Array.isArray(v))return false;
    const obj=v as Record<string,unknown>,keys=Object.keys(obj),p=s.properties||{};
    return (s.minProperties===undefined||keys.length>=s.minProperties)&&(s.maxProperties===undefined||keys.length<=s.maxProperties)&&(!s.required||s.required.every(k=>Object.hasOwn(obj,k)))&&keys.every(k=>{
      if(s.propertyNames&&!check(s.propertyNames,k,depth+1))return false;
      if(Object.hasOwn(p,k))return check(p[k],obj[k],depth+1);
      return s.additionalProperties===false?false:typeof s.additionalProperties==="object"?check(s.additionalProperties,obj[k],depth+1):true;
    });
  }
  return true;
}
const same=(a:Scope,b:Scope)=>a.tenant_id===b.tenant_id&&a.actor_id===b.actor_id&&a.connection_id===b.connection_id&&a.conversation_id===b.conversation_id&&a.authority_version===b.authority_version&&a.context_version===b.context_version;
const unique=(v:string[])=>new Set(v).size===v.length;
function canonical(v:unknown):string {return JSON.stringify(v===null||typeof v!=="object"?v:Array.isArray(v)?v.map(x=>JSON.parse(canonical(x))):Object.fromEntries(Object.entries(v).sort(([a],[b])=>a.localeCompare(b)).map(([k,x])=>[k,JSON.parse(canonical(x))])));}
const instant=(v:string)=>{if(!/(Z|[+-]\d\d:\d\d)$/.test(v)||!Number.isFinite(Date.parse(v)))fail();return Date.parse(v);};
function safeKey(key:string):boolean {const k=key.toLowerCase().replace(/[^a-z0-9]/g,"");return !["password","secret","credential","accesstoken","refreshtoken","sql","script","shell","endpoint","callbackurl"].some(x=>k.includes(x))&&!["role","roles","permissions","permission","tenantid","academyid","actorid","connectionid","authorityversion","approval","approved"].includes(k);}
function checkSpec(f:FieldSpec):boolean {return safeKey(f.key)&&unique(f.choices.map(c=>c.id))&&(f.kind==="reference"||f.choices.length===0)&&(f.kind==="rows"?f.columns.length>0&&unique(f.columns.map(c=>c.key))&&f.columns.every(c=>c.kind!=="rows"&&checkSpec(c)):f.columns.length===0);}
function fieldValue(spec:FieldSpec,value:Value):boolean {
  if(spec.kind!==value.kind)return false;
  if(value.kind==="text")return !!value.value.trim()&&value.value.length<=spec.max_length;
  if(value.kind==="reference")return spec.choices.some(c=>c.id===value.value);
  if(value.kind==="date")return /^\d{4}-\d\d-\d\d$/.test(value.value)&&!isNaN(Date.parse(value.value))&&new Date(value.value).toISOString().slice(0,10)===value.value;
  if(value.kind==="rows")return value.value.every(row=>Object.entries(row).every(([k,v])=>{const col=spec.columns.find(c=>c.key===k);return !!col&&fieldValue(col,v);})&&spec.columns.filter(c=>c.required).every(c=>Object.hasOwn(row,c.key)));
  return true;
}

/** Defense for accidental/foreign projections. Gateway authorization stays independent. */
export function validateProjection(input:unknown):Projection {
  const raw=JSON.stringify(input);if(!raw||raw.length>262144)fail();
  const data:unknown=JSON.parse(raw);if(!check(schema as Schema,data))fail();
  const p=data as Projection,h=p.host,d=h.draft,o=p.operation,r=p.receipt;
  if(!unique(p.capabilities.map(c=>c.key))||!unique(p.models.map(m=>m.ref))||!unique(p.messages.map(m=>m.ref))||!unique(p.history.map(x=>x.ref))||!unique(h.tools.map(t=>t.key)))fail();
  if(!p.capabilities.some(c=>c.key===p.selected_capability))fail();
  for(const t of h.tools){if(!unique(t.fields.map(f=>f.key))||!unique(t.capability)||t.fields.some(f=>!checkSpec(f))||(t.effect!=="read"&&(!t.preparation_required||!t.approval_required)))fail();const k=t.key.toLowerCase().replace(/[^a-z0-9]/g,"");if(["executesql","runshell","grantrole","changepermission","configuresecurity","deleteaudit","password","secretvault"].some(x=>k.includes(x)))fail();}
  if(p.selected_model_ref&&!p.models.some(m=>m.ref===p.selected_model_ref&&m.qualified))fail();
  const now=instant(h.as_of);
  const currentSources=new Set(h.evidence.filter(e=>e.status==="current"&&instant(e.expires_at)>now).map(e=>e.source_ref));
  for(const m of p.messages)if(m.kind==="observed"&&(!m.source_refs.length||m.source_refs.some(s=>!currentSources.has(s))))fail();
  for(const item of [p.observation,p.validation,o,r])if(item&&!same(item.scope,h.scope))fail();
  if(p.validation&&(!d||p.validation.draft_ref!==d.ref||p.validation.draft_revision!==d.revision))fail();
  if(o&&(!d||o.draft_ref!==d.ref||o.draft_revision!==d.revision||d.operation_ref!==o.ref||canonical(o.canonical_fields)!==canonical(d.fields)))fail();
  if(o){const t=h.tools.find(t=>t.key===o.tool_key);if(!t||t.revision!==o.tool_revision||Object.entries(o.canonical_fields).some(([k,v])=>{const f=t.fields.find(f=>f.key===k);return !f||!fieldValue(f,v);}))fail();}
  if(r&&(!o||r.operation_ref!==o.ref||r.payload_hash!==o.payload_hash||r.draft_revision!==o.draft_revision))fail();
  if(r?.status==="succeeded"&&(!r.receipt_ref||!r.audit_ref||r.error_code!=="none"))fail();
  if(r?.status==="unknown"&&r.error_code!=="unknown")fail();
  if(p.controls.confirm&&(!o||!d||!["awaiting_approval","approved"].includes(d.status)||!["awaiting_approval","approved"].includes(o.status)||!o.approval_ref||!o.approval_expires_at||instant(o.approval_expires_at)<=now))fail();
  if(h.availability!=="ready"&&[p.controls.send,p.controls.prepare,p.controls.confirm,p.controls.cancel,p.controls.pause,p.controls.resume,p.controls.secure_media].some(Boolean))fail();
  if(h.availability==="denied"&&(h.tools.length||h.evidence.length||d))fail();
  if(h.availability==="denied"&&(p.messages.length||p.history.length||p.observation||p.validation||o||r))fail();
  for(const e of h.evidence)if(!same(e.scope,h.scope)||instant(e.as_of)>now||instant(e.expires_at)<=instant(e.as_of))fail();
  if(p.observation){
    if(!unique(p.observation.artifacts.map(a=>a.ref)))fail();
    for(const a of p.observation.artifacts){
      if(!same(a.scope,h.scope)||!unique(a.provenance.source_refs))fail();instant(a.provenance.as_of);
      if(a.kind==="table"&&(!unique(a.columns.map(c=>c.key))||a.rows.some(row=>Object.entries(row).some(([k,v])=>{const f=a.columns.find(c=>c.key===k);return !f||!fieldValue(f,v);}))))fail();
    }
  }
  return p;
}
