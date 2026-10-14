// Host presentation projection, never authentication/authorization evidence.
// No tokens, raw model roles, source URLs, persisted conversation, or transports.
export type Portal = "platform-owner" | "academy-admin" | "teacher" | "student";
export type Mode = "ai" | "manual";
export type ViewState = "ready" | "loading" | "empty" | "denied" | "unavailable" | "error" | "pending" | "success" | "unknown";
export type NavigationItem = { key:string; label:string; available:boolean; unavailableReason?:string };
export type ContextPresentation = {
  portal: Portal; label:string; scopeLabel:string;
  /** Host-owned epoch; remount dependent presentation on approved context changes. */
  contextKey:string;
  aiAvailable:boolean; aiUnavailableReason?:string;
  navigation:readonly NavigationItem[];
};
export type HostPresentationAdapter = {
  context:ContextPresentation;
  onNavigate:(key:string)=>void;
  onThemeRequest?:(theme:import("./tokens").Theme)=>void;
  /** Academy-only; host owns approval, drafts and lifecycle before changing mode. */
  modeControl?: { value:Mode; onRequest:(mode:Mode)=>void };
  onOpenSource?:(sourceKey:string)=>void;
  onOpenConnectedApp?:()=>void;
};
export const stateCopy: Record<ViewState,{title:string;description:string;tone:"info"|"success"|"warning"|"danger"}> = {
  ready:{title:"Synthetic source",description:"Sample records only. No connected system is queried.",tone:"info"},
  loading:{title:"Loading example",description:"Simulated loading; no request is running.",tone:"info"},
  empty:{title:"No sample records",description:"This synthetic query has no results.",tone:"info"},
  denied:{title:"Access denied example",description:"The mock host has withheld these records.",tone:"danger"},
  unavailable:{title:"AI unavailable example",description:"Manual controls remain independently available.",tone:"warning"},
  error:{title:"Request failed example",description:"Your draft is retained. No automatic retry.",tone:"danger"},
  pending:{title:"Pending example",description:"Waiting state only; duplicate submission is disabled.",tone:"info"},
  success:{title:"Synthetic confirmation",description:"Local demo state only. No record was saved.",tone:"success"},
  unknown:{title:"UNKNOWN OUTCOME example",description:"A real operation would require host reconciliation. Do not repeat it.",tone:"warning"}
};
