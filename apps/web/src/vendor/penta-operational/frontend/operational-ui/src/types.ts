export const UI_VERSION = "operational-ui-0.3.0" as const;
export type Capability = "Pulse"|"Executor"|"Navigator"|"Twin"|"Autopilot";
export type Scope = {tenant_id:string; actor_id:string; connection_id:string; conversation_id:string; authority_version:number; context_version:number};
export type Scalar = {kind:"text"|"date"|"decimal"|"reference";value:string}|{kind:"integer";value:number}|{kind:"boolean";value:boolean};
export type Value = Scalar|{kind:"rows";value:Record<string,Scalar>[]};
export type FieldSpec = {key:string;kind:Value["kind"];label:string;required:boolean;clearable:boolean;choices:{id:string;label:string}[];columns:FieldSpec[];max_length:number};
export type Tool = {key:string;revision:number;family:string;capability:Capability[];description:string;effect:"read"|"write"|"external"|"bulk"|"schedule";fields:FieldSpec[];preparation_required:boolean;approval_required:boolean;max_items:number};
export type Draft = {ref:string;revision:number;tool_key:string;fields:Record<string,Value>;status:"collecting"|"prepared"|"awaiting_approval"|"approved"|"executing"|"succeeded"|"failed"|"cancelled"|"paused"|"unknown";operation_ref:string|null};
export type Host = {version:"operational-draft-0.3.0";scope:Scope;as_of:string;catalog_revision:number;tools:Tool[];draft:Draft|null;evidence:{ref:string;scope:Scope;source_ref:string;as_of:string;expires_at:string;coverage:"complete"|"partial"|"unknown";status:"current"|"revoked"|"stale";facts:Record<string,Scalar>}[];availability:"ready"|"denied"|"unavailable"|"stale"};
export type Provenance = {source_refs:string[];as_of:string;coverage:"complete"|"partial"|"unknown"};
type ArtifactBase = {ref:string;scope:Scope;title:string;provenance:Provenance};
export type Artifact = ArtifactBase & ({kind:"text";text:string}|{kind:"table";columns:FieldSpec[];rows:Record<string,Scalar>[]}|{kind:"chart";chart:"bar"|"line";unit:string;points:{label:string;value:string}[]});
export type Operation = {ref:string;request_ref:string;scope:Scope;tool_key:string;tool_revision:number;draft_ref:string;draft_revision:number;payload_hash:string;canonical_fields:Record<string,Value>;preview:string;approval_ref:string|null;approval_expires_at:string|null;status:Exclude<Draft["status"],"collecting"|"paused">};
export type Receipt = {operation_ref:string;scope:Scope;payload_hash:string;draft_revision:number;status:Operation["status"];receipt_ref:string|null;audit_ref:string|null;source_refs:string[];error_code:"none"|"denied"|"invalid"|"unavailable"|"conflict"|"unknown"};
export type Issue = {field:string|null;code:"required"|"invalid"|"duplicate_candidates"|"ambiguous_reference"|"secure_host_step"|"business_rule";choices:{id:string;label:string}[]};
export type Projection = {
  version:typeof UI_VERSION;context_key:string;projection_revision:number;workspace_label:string;actor_label:string;role_label:string;surface:"academy"|"standalone";
  host:Host;capabilities:{key:Capability;available:boolean;reason:string}[];selected_capability:Capability;
  models:{ref:string;label:string;qualified:boolean}[];selected_model_ref:string|null;
  messages:{ref:string;kind:"user"|"observed"|"hypothesis"|"recommendation"|"status";text:string;source_refs:string[]}[];
  history:{ref:string;title:string}[];
  observation:{version:"operational-draft-0.3.0";scope:Scope;request_ref:string;artifacts:Artifact[]}|null;
  validation:{request_ref:string;scope:Scope;draft_ref:string;draft_revision:number;status:"invalid"|"ambiguous"|"secure_step_required"|"valid";issues:Issue[];source_refs:string[]}|null;
  operation:Operation|null;receipt:Receipt|null;
  controls:Record<"send"|"prepare"|"confirm"|"cancel"|"pause"|"resume"|"reconcile"|"history"|"feedback"|"secure_media",boolean>;
};
export type Intent =
  | {kind:"new_conversation"}
  | {kind:"send";text:string;capability:Capability;base_revision:number}
  | {kind:"edit";draft_ref:string;revision:number;edit:{mode:"refine";set:Record<string,Value>;remove:string[]}}
  | {kind:"confirm";operation_ref:string;approval_ref:string;revision:number;payload_hash:string}
  | {kind:"cancel"|"pause"|"resume";draft_ref:string;revision:number}
  | {kind:"status";operation_ref:string;revision:number;payload_hash:string}
  | {kind:"history";conversation_ref:string}
  | {kind:"feedback";message_ref:string;text:string}
  | {kind:"select_model";model_ref:string}
  | {kind:"select_capability";capability:Capability}
  | {kind:"source";source_ref:string}
  | {kind:"open_secure_media"};
export type HostAdapter = {onIntent:(intent:Intent,signal:AbortSignal)=>Promise<void>;onManual?:()=>void;onTheme?:(theme:"orbit"|"aura"|"prism")=>void};
