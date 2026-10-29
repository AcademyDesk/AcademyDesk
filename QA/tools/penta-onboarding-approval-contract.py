"""Actual pinned SDK checks over recorded disposable SQL approval/receipt data.

No inference, HTTP transport, human approval simulation claim or PENTA edits.
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
                 "src/penta_mini/policy_v1.py"):
        assert hashlib.sha256((root / name).read_bytes()).hexdigest() == pins[name], "Pinned Core source changed"
    path = args.receipt.resolve(strict=True)
    assert "QA" in path.parts and "EVIDENCE" in path.parts and path.name == "core-approval.json"
    assert path.stat().st_size <= 262144
    values = json.loads(path.read_text(encoding="utf-8"))
    assert set(values) == {"journeys"} and 1 <= len(values["journeys"]) <= 10
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(root / "src"))
    from penta_mini.operational.contracts import HostSnapshot, PreparedOperation, Receipt, clone
    from penta_mini.operational.sdk import ConnectorSDK, ConnectorDenied, ReconciliationRequired

    for journey in values["journeys"]:
        before = HostSnapshot.model_validate_json(json.dumps(journey["before"]))
        after = HostSnapshot.model_validate_json(json.dumps(journey["after"]))
        approved = PreparedOperation.model_validate_json(json.dumps(journey["approved"]))
        finished = PreparedOperation.model_validate_json(json.dumps(journey["finishedOperation"]))
        receipt = Receipt.model_validate_json(json.dumps(journey["receipt"]))
        assert approved.status == before.draft.status == "approved"
        assert approved.approval_ref and approved.approval_expires_at
        assert after.draft.status == finished.status == receipt.status == "succeeded"
        assert receipt.receipt_ref and receipt.audit_ref and len(receipt.source_refs) == 1

        class RecordedHost:
            def __init__(self, lost_response=False):
                self.calls, self.status_calls = 0, 0
                self.lost_response = lost_response

            def current(self, scope):
                assert scope == before.scope
                return clone(after if self.calls else before)

            def operation(self, scope, ref):
                assert scope == before.scope and ref == approved.ref
                return clone(finished if self.calls else approved)

            def execute_once(self, operation):
                assert operation == approved
                self.calls += 1
                if self.lost_response:
                    raise TimeoutError("Synthetic lost response")
                return clone(receipt)

            def status(self, scope, ref):
                assert scope == before.scope and ref == approved.ref
                self.status_calls += 1
                return clone(receipt)

        host = RecordedHost()
        sdk = ConnectorSDK(host)
        for revision, digest in ((approved.draft_revision + 1, approved.payload_hash), (approved.draft_revision, "0" * 64)):
            try:
                sdk.execute(before.scope, approved.ref, digest, revision)
            except ConnectorDenied:
                assert host.calls == 0
            else:
                raise AssertionError("Altered execution binding accepted")
        assert sdk.execute(before.scope, approved.ref, approved.payload_hash, approved.draft_revision) == receipt
        assert host.calls == 1
        assert sdk.execute(before.scope, approved.ref, approved.payload_hash, approved.draft_revision) == receipt
        assert host.calls == 1 and host.status_calls == 1
        lost = RecordedHost(True)
        lost_sdk = ConnectorSDK(lost)
        try:
            lost_sdk.execute(before.scope, approved.ref, approved.payload_hash, approved.draft_revision)
        except ReconciliationRequired:
            assert lost.calls == 1
        else:
            raise AssertionError("Uncertain execution fabricated a response")
        assert lost_sdk.reconcile(before.scope, approved.ref, approved.payload_hash, approved.draft_revision) == receipt
        assert lost.calls == 1 and lost.status_calls == 1
    print(f"PASS: five pinned Core files; actual ConnectorSDK.execute/reconcile accepted {len(values['journeys'])} recorded HTTP/SQL approval/result journey; exact receipt, altered binding denial, terminal/lost-response reconciliation without redispatch. No inference or live SDK transport.")
    print("Approval receipt SHA256=" + hashlib.sha256(path.read_bytes()).hexdigest())


if __name__ == "__main__":
    main()
