"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import TeacherReviewWorkspace from "@/components/penta/teacher-review-workspace";

function BoundTeacherReview() {
  const params = useSearchParams();
  const valid = params.size === 2 && params.getAll("conversationId").length === 1 && params.getAll("draftId").length === 1;
  const conversation = valid ? params.get("conversationId") ?? "" : "";
  const draft = valid ? params.get("draftId") ?? "" : "";
  return <TeacherReviewWorkspace key={`${conversation}:${draft}`} conversation={conversation} draft={draft} />;
}
export default function TeacherReviewPage() {
  return <Suspense fallback={<main aria-busy="true">Checking the protected teacher workspace…</main>}><BoundTeacherReview /></Suspense>;
}
