"""Check recorded disposable HTTP/SQL transitions against pinned PENTA Core.

No inference, HTTP transport, training, SDK execution port, or PENTA source edit.
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
    if manifest["version"] != "penta-operational-return-0.3.0-candidate.5":
        raise ValueError("Unexpected Core version")
    pins = {item["path"]: item["sha256"] for item in manifest["backend"]["files"]}
    names = ("src/penta_mini/operational/__init__.py", "src/penta_mini/operational/contracts.py",
             "src/penta_mini/operational/core.py", "src/penta_mini/operational/sdk.py",
             "src/penta_mini/operational/dialogue.py", "src/penta_mini/policy_v1.py")
    for name in names:
        if hashlib.sha256((root / name).read_bytes()).hexdigest() != pins[name]:
            raise ValueError("Pinned Core source changed: " + name)
    path = args.receipt.resolve(strict=True)
    if "QA" not in path.parts or "EVIDENCE" not in path.parts or path.name != "core-lifecycle.json" or path.stat().st_size > 262144:
        raise ValueError("Expected bounded synthetic QA lifecycle receipt")
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(root / "src"))
    from penta_mini.operational.contracts import HostSnapshot, clone
    from penta_mini.operational.core import WorkflowDecision
    from penta_mini.operational.dialogue import DialogueSDK
    from penta_mini.operational.sdk import ConnectorDenied

    values = json.loads(path.read_text(encoding="utf-8"))
    if set(values) != {"journeys"} or not 5 <= len(values["journeys"]) <= 20:
        raise ValueError("Complete bounded lifecycle journeys required")

    class RecordedHost:
        def __init__(self, before, after):
            self.before, self.after = before, after
            self.calls = 0

        def current(self, scope):
            assert scope == self.before.scope
            return clone(self.before)

        def transition(self, decision):
            self.calls += 1
            return clone(self.after)

    kinds, starting_states = set(), set()
    for journey in values["journeys"]:
        before = HostSnapshot.model_validate_json(json.dumps(journey["before"]))
        after = HostSnapshot.model_validate_json(json.dumps(journey["after"]))
        decision = WorkflowDecision.model_validate_json(json.dumps(journey["decision"]))
        host = RecordedHost(before, after)
        result = DialogueSDK(host).transition(decision)
        assert result == after and host.calls == 1
        assert before.draft.fields == after.draft.fields
        assert before.draft.tool_key == after.draft.tool_key
        assert after.draft.revision == before.draft.revision + 1
        assert after.draft.operation_ref is None
        kinds.add(decision.kind)
        starting_states.add(before.draft.status)
        # SDK independently rejects stale requests before touching host transition.
        stale = decision.model_copy(update={"base_revision": decision.base_revision + 1})
        denied = RecordedHost(before, after)
        try:
            DialogueSDK(denied).transition(stale)
        except ConnectorDenied:
            assert denied.calls == 0
        else:
            raise AssertionError("SDK accepted stale lifecycle revision")
        wrong = after.model_copy(update={"draft": after.draft.model_copy(update={"status": "collecting" if after.draft.status != "collecting" else "paused"})})
        try:
            DialogueSDK(RecordedHost(before, wrong)).transition(decision)
        except ConnectorDenied:
            pass
        else:
            raise AssertionError("SDK accepted wrong host transition result")
    assert kinds == {"request_pause", "request_resume", "request_cancel"}
    assert {"collecting", "paused", "approved"} <= starting_states
    print(f"PASS: six pinned Core files; actual DialogueSDK.transition accepted {len(values['journeys'])} recorded HTTP/SQL journeys, retained fields and invalidated operation identity; stale requests and wrong results denied. No inference or live transport.")
    print("Lifecycle receipt SHA256=" + hashlib.sha256(path.read_bytes()).hexdigest())


if __name__ == "__main__":
    main()
