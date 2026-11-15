import type { AcademicSearch, SearchKind } from "./penta-enrollment-assistant";
import { schedulingPrompt } from "./penta-class-scheduling";

type WorkspacePrompt = { kind: "lookup"; entity: SearchKind; query: string } |
  { kind: "schedule"; request: ReturnType<typeof schedulingPrompt>; text: string } |
  { kind: "select"; index: number } | { kind: "finance" } | { kind: "clarify"; message: string };
const ordinals = ["first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth"];

export function workspacePrompt(input: string, context: AcademicSearch | null): WorkspacePrompt {
  const text = input.trim();
  if (!text || text.length > 180 || /[\u0000-\u001f\u007f-\u009f]/.test(text))
    return { kind: "clarify", message: "Enter one short, single-line request." };

  const selection = /^(?:open|select|show)(?: me)? (?:the )?(?:(first|second|third|fourth|fifth|sixth|seventh|eighth|ninth|tenth)(?: one| result)?|(?:result )?([1-9]|10)|it)[?.]?$/i.exec(text);
  if (selection) {
    const index = selection[1] ? ordinals.indexOf(selection[1].toLowerCase()) : selection[2] ? Number(selection[2]) - 1 : 0;
    const onlyMatch = !selection[1] && !selection[2];
    if (!context?.rows[index] || onlyMatch && (context.hasMore || context.rows.length !== 1))
      return { kind: "clarify", message: context?.rows.length ? "Which result did you mean? Use its number or refine the search by name." : "Find a student or batch first, then ask to open a result." };
    return { kind: "select", index };
  }

  const lookup = /^(?:find|show(?: me)?|look up|search for) (?:the )?(student|batch)(?: (.+))?$/i.exec(text);
  const refinement = /^try (.+)$/i.exec(text);
  if (lookup || refinement && context) {
    const entity = lookup ? lookup[1].toLowerCase() as SearchKind : context!.kind;
    const query = (lookup?.[2] ?? refinement?.[1] ?? "").trim();
    return query.length >= 2 && query.length <= 80 ? { kind: "lookup", entity, query }
      : { kind: "clarify", message: "Which " + entity + "? Enter 2–80 characters of its name." };
  }

  if (/^(?:schedule|reschedule)\b/i.test(text)) {
    try { return { kind: "schedule", request: schedulingPrompt(text), text }; }
    catch { return { kind: "clarify", message: "Which batch? Try “Schedule a class for Piano” or “Reschedule a class for Piano”." }; }
  }
  return { kind: "finance" };
}
