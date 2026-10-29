"""Pinned actual Core save_partial checks over disposable SQL host responses.

Recorded ports only: no inference, live HTTP, training or PENTA file changes.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--penta-root", type=Path, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()
    root = args.penta_root.resolve(strict=True)
    manifest = json.loads((root / "operations/release-manifest-candidate.5.json").read_text(encoding="utf-8"))
    assert manifest["version"] == "penta-operational-return-0.3.0-candidate.5"
    pins = {item["path"]: item["sha256"] for item in manifest["backend"]["files"]}
    for name in ("src/penta_mini/operational/__init__.py", "src/penta_mini/operational/contracts.py",
                 "src/penta_mini/operational/core.py", "src/penta_mini/operational/sdk.py",
                 "src/penta_mini/operational/dialogue.py", "src/penta_mini/policy_v1.py"):
        assert hashlib.sha256((root / name).read_bytes()).hexdigest() == pins[name], "Pinned Core source changed"
    path = args.receipt.resolve(strict=True)
    assert "QA" in path.parts and "EVIDENCE" in path.parts and path.name == "core-partial.json"
    assert path.stat().st_size <= 262144
    values = json.loads(path.read_text(encoding="utf-8"))
    assert set(values) == {"journeys"} and 5 <= len(values["journeys"]) <= 20
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(root / "src"))
    from penta_mini.operational.contracts import HostSnapshot, Proposal, clone
    from penta_mini.operational.core import WorkflowDecision, reduce_proposal
    from penta_mini.operational.dialogue import DialogueSDK
    from penta_mini.operational.sdk import ConnectorDenied

    class RecordedHost:
        def __init__(self, before, after):
            self.before, self.after, self.calls = before, after, 0

        def current(self, scope):
            assert scope == self.before.scope
            return clone(self.before)

        def save_partial(self, decision):
            self.calls += 1
            return clone(self.after)

    starting_states = set()
    for journey in values["journeys"]:
        before = HostSnapshot.model_validate_json(json.dumps(journey["before"]))
        after = HostSnapshot.model_validate_json(json.dumps(journey["after"]))
        decision = WorkflowDecision.model_validate_json(json.dumps(journey["decision"]))
        host = RecordedHost(before, after)
        assert DialogueSDK(host).save_partial(decision) == after and host.calls == 1
        assert after.draft.status == "collecting" and after.draft.operation_ref is None
        assert after.draft.revision == before.draft.revision + 1
        starting_states.add(before.draft.status)
        for key, value in decision.candidates[0].edit.set.items():
            assert after.draft.fields[key] == value
        stale = decision.model_copy(update={"base_revision": decision.base_revision + 1})
        denied = RecordedHost(before, after)
        try:
            DialogueSDK(denied).save_partial(stale)
        except ConnectorDenied:
            assert denied.calls == 0
        else:
            raise AssertionError("Stale partial call reached host")
        wrong = after.model_copy(update={"draft": after.draft.model_copy(update={"status": "paused"})})
        try:
            DialogueSDK(RecordedHost(before, wrong)).save_partial(decision)
        except ConnectorDenied:
            pass
        else:
            raise AssertionError("Wrong partial response accepted")
    first = values["journeys"][0]
    before = HostSnapshot.model_validate_json(json.dumps(first["before"]))
    proposal = Proposal.model_validate_json(json.dumps({
        "version": "operational-draft-0.3.0", "request_ref": first["decision"]["request_ref"],
        "base_revision": before.draft.revision, "kind": "prepare",
        "steps": first["decision"]["candidates"], "question_fields": []}))
    reduced = reduce_proposal(before, proposal)
    assert reduced == WorkflowDecision.model_validate_json(json.dumps(first["decision"]))
    assert {"collecting", "approved"} <= starting_states
    print(f"PASS: six pinned Core files; actual reducer and DialogueSDK.save_partial accepted {len(values['journeys'])} recorded HTTP/SQL journeys, retained values/false flags and collecting status; stale calls and wrong responses denied. No inference/live transport.")
    print("Partial receipt SHA256=" + hashlib.sha256(path.read_bytes()).hexdigest())


if __name__ == "__main__":
    main()
