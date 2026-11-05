"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import TeacherStartWorkspace from "@/components/penta/teacher-start-workspace";
import TeacherChatWorkspace from "@/components/penta/teacher-chat-workspace";

function BoundChat() {
  const params = useSearchParams(), conversation = params.get("conversationId") ?? "", draft = params.get("draftId") ?? "";
  if (!conversation && !draft && (params.size === 0 || params.size === 1 && params.has("teacherStartRequestId"))) return <TeacherStartWorkspace />;
  return <TeacherChatWorkspace key={`${conversation}:${draft}`} conversation={conversation} draft={draft} />;
}
export default function TeacherConversationPage() {
  return <Suspense fallback={<main aria-busy="true">Checking the protected teacher conversation…</main>}><BoundChat /></Suspense>;
}
