"use client";

import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import OnboardingReviewWorkspace from "@/components/penta/onboarding-review-workspace";
import OnboardingStartWorkspace from "@/components/penta/onboarding-start-workspace";

function BoundReview() {
  const params = useSearchParams();
  const conversation = params.get("conversationId") ?? "";
  const draft = params.get("draftId") ?? "";
  if (!conversation && !draft && (params.size === 0 || params.size === 1 && params.has("startRequestId"))) return <OnboardingStartWorkspace />;
  // Query IDs are untrusted lookup keys. Never accept role, tenant or actor here.
  return <OnboardingReviewWorkspace key={`${conversation}:${draft}`} conversation={conversation} draft={draft} />;
}
export default function OnboardingReviewPage() {
  return <Suspense fallback={<main aria-busy="true">Checking the protected workspace…</main>}><BoundReview /></Suspense>;
}
