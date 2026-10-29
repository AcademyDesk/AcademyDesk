"""Read-only actual SDK checks over synthetic HTTP/SQL preparation receipts.

This replays recorded host responses, not network calls or real model inference.
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
    receipt = args.receipt.resolve(strict=True)
    assert "QA" in receipt.parts and "EVIDENCE" in receipt.parts and receipt.name == "core-preparation.json"
    assert receipt.stat().st_size <= 262144
    values = json.loads(receipt.read_text(encoding="utf-8"))
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(root / "src"))
    from penta_mini.operational.contracts import HostSnapshot, PreparedOperation
    from penta_mini.operational.core import WorkflowDecision
    from penta_mini.operational.dialogue import ValidationReport, DialogueSDK
    from penta_mini.operational.sdk import ConnectorSDK, ConnectorDenied

    reports = [ValidationReport.model_validate_json(json.dumps(value)) for value in values["validations"]]
    assert {"valid", "invalid", "ambiguous", "secure_step_required"} <= {r.status for r in reports}
    for journey in values["journeys"]:
        before = HostSnapshot.model_validate_json(json.dumps(journey["before"]))
        after = HostSnapshot.model_validate_json(json.dumps(journey["after"]))
        decision = WorkflowDecision.model_validate_json(json.dumps(journey["decision"]))
        operations = [PreparedOperation.model_validate_json(json.dumps(value)) for value in journey["operations"]]

        class RecordedHost:
            def __init__(self):
                self.preparations = 0
                self.executions = 0

            def current(self, scope):
                return after if self.preparations else before

            def prepare(self, actual):
                assert actual == decision
                self.preparations += 1
                return operations

            def operation(self, scope, operation_ref):
                assert operation_ref == operations[0].ref
                return operations[0]

            def execute_once(self, operation):
                self.executions += 1
                raise AssertionError("Preparation cannot execute")

            def validation(self, scope, request_ref):
                return next(r for r in reports if r.request_ref == request_ref)

        port = RecordedHost()
        sdk = ConnectorSDK(port)
        result = sdk.prepare(decision)
        assert result == operations and port.preparations == 1
        assert after.draft.operation_ref == result[0].ref
        assert after.draft.revision == result[0].draft_revision
        assert after.draft.fields == result[0].canonical_fields
        assert after.draft.status == result[0].status == "awaiting_approval"
        valid = next(r for r in reports if r.draft_ref == after.draft.ref and r.draft_revision == after.draft.revision)
        assert DialogueSDK(port).validation(after.scope, valid.request_ref) == valid
        try:
            sdk.execute(after.scope, result[0].ref, result[0].payload_hash, result[0].draft_revision)
        except ConnectorDenied:
            pass
        else:
            raise AssertionError("Unapproved preparation bypassed SDK confirmation")
        assert port.executions == 0
    print(f"PASS: pinned candidate.5 actual ConnectorSDK.prepare + DialogueSDK.validation accepted {len(values['journeys'])} recorded HTTP/SQL journey and {len(reports)} validation reports; canonical defaults/hash/current-draft match; unapproved execute denied. Recorded-port compatibility only, no inference/live SDK transport.")
    print("Receipt SHA256=" + hashlib.sha256(receipt.read_bytes()).hexdigest())


if __name__ == "__main__":
    main()
