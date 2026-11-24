"use client";

import { createContext, useContext } from "react";
import type { QueryView } from "@/lib/penta-query-conversation";

/** In-memory projection of already verified host receipts. No extra reads,
 * permissions, browser persistence or model selection are performed here. */
export type PentaWorkspaceSnapshot = Pick<QueryView, "available" | "pending" | "turns" | "entities" | "phase" | "hasMore">;
export const PentaWorkspaceContext = createContext<((view: PentaWorkspaceSnapshot | null) => void) | null>(null);
export const usePentaWorkspaceProjection = () => useContext(PentaWorkspaceContext);
