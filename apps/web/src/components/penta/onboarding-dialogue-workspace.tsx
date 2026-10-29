"use client";

import { useEffect, useRef, useState } from "react";
import { Button, Notice } from "@/vendor/penta-operational/frontend/shared-foundation/src";
import type { Projection, Intent } from "@/vendor/penta-operational/frontend/operational-ui/src";
import { createOnboardingDialogue } from "@/lib/penta-onboarding-dialogue";
import OnboardingOperationWorkspace from "./onboarding-operation-workspace";

type Props = { projection: Projection; theme: "orbit" | "aura" | "prism"; onManual: () => void;
  onNew: () => void; onTheme: (theme: "orbit" | "aura" | "prism") => void };
export default function OnboardingDialogueWorkspace(props: Props) {
  const [projection, setProjection] = useState(props.projection);
  const [phase, setPhase] = useState<"passive" | "testing" | "unavailable" | "unknown">("passive");
  const [busy, setBusy] = useState(false), [pending, setPending] = useState(false);
  const dialogue = useRef<ReturnType<typeof createOnboardingDialogue> | null>(null);
  const abort = useRef<AbortController | null>(null), alive = useRef(true), lock = useRef(false);
  const workspace = useRef<HTMLDivElement | null>(null);
  useEffect(() => { alive.current = true; abort.current = new AbortController();
    return () => { alive.current = false; abort.current?.abort(); dialogue.current?.dispose(); }; }, []);
  useEffect(() => {
    const transcript = workspace.current?.querySelector<HTMLElement>(".op-transcript");
    if (transcript) transcript.scrollTop = transcript.scrollHeight;
  }, [projection]);
  async function act(kind: "connect" | "send" | "recover", intent?: Extract<Intent, { kind: "send" }>, signal?: AbortSignal) {
    if (!alive.current || lock.current || !abort.current) throw new Error("Conversation unavailable.");
    lock.current = true; setBusy(true);
    try {
      dialogue.current ??= createOnboardingDialogue(projection, abort.current.signal);
      if (kind === "connect") {
        const result = await dialogue.current.connect();
        if (alive.current) { setProjection(result.projection); setPhase(result.available ? "testing" : "unavailable"); }
      } else {
        const result = kind === "recover" ? await dialogue.current.recover() : await dialogue.current.send(intent!, signal!);
        if (alive.current) {
          if (result) { setProjection(result); setPhase("testing"); setPending(false); }
          else { setPhase("unknown"); setPending(true); }
        }
      }
    } catch (error) {
      if (alive.current) { setPhase(kind === "connect" ? "unavailable" : "unknown"); setPending(dialogue.current?.pending ?? false); }
      throw error;
    } finally { lock.current = false; if (alive.current) setBusy(false); }
  }
  const click = (kind: "connect" | "recover") => { void act(kind).catch(() => {}); };
  return <>
    {(projection.host.draft?.status === "collecting" || phase === "unknown") && <section aria-label="Onboarding conversation controls"
      style={{ padding: "12px 16px", flexShrink: 0 }} aria-busy={busy}>
      {phase === "passive" ? <><Notice title="Conversational availability is checked separately" tone="info">No message is sent on page load. A real model must pass qualification before normal conversational use.</Notice>
        <Button disabled={busy} onClick={() => click("connect")}>Check conversational availability</Button></> :
        phase === "testing" ? <Notice title="Disposable test interpreter — not qualified AI" tone="warning">This verifies the host conversation interface only. Chat can collect or prepare a draft; it cannot approve or create a student.</Notice> :
          phase === "unavailable" ? <Notice title="Conversational sending is unavailable" tone="warning">No qualified runtime is enabled here. No message was sent. Manual and existing host review remain available.</Notice> :
            <><Notice title="Conversation outcome is unconfirmed" tone="warning">Private details are withheld. Do not send the message again. Checking reads the same request and does not approve or create a student.</Notice>
              {pending && <Button disabled={busy} onClick={() => click("recover")}>Check message result</Button>}</>}
    </section>}
    <div ref={workspace} style={{ display: "contents" }}>{phase !== "unknown" && <OnboardingOperationWorkspace key={projection.context_key} {...props} projection={projection}
      onSend={phase === "testing" ? (intent, signal) => act("send", intent, signal) : undefined} />}</div>
  </>;
}
