"""Replay retained synthetic SQL/HTTP outputs through the actual PENTA schema/SDK.

No inference, model import/load, network, database operation or Mini source edit.
This checks cross-project compatibility, not a qualified conversational engine.
"""
import hashlib
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True


def main():
    root = Path(__file__).resolve().parents[2]
    assert str(root).lower() == r"d:\academydesk-codex-p0"
    assert len(sys.argv) == 2 and re.fullmatch(r"[0-9a-f]{32}", sys.argv[1])
    evidence = root / "QA" / "EVIDENCE" / ("penta-teacher-core-sql-" + sys.argv[1])
    result = json.loads((evidence / "result.json").read_text(encoding="utf-8"))
    assert result["passed"] and not result["modelInference"] and not result["toolRegistered"]
    source = Path(r"D:\PENTA AI Models\src")
    names = ["penta_mini/operational/contracts.py", "penta_mini/operational/sdk.py",
             "penta_mini/operational/core.py", "penta_mini/policy_v1.py", "penta_mini/protocol.py",
             "penta_mini/connector.py", "penta_mini/operational/__init__.py", "penta_mini/__init__.py"]
    pins = [{"path": str(source / name), "sha256": hashlib.sha256((source / name).read_bytes()).hexdigest()} for name in names]
    sys.path.insert(0, str(source))
    from penta_mini.operational.contracts import HostSnapshot, PreparedOperation, Receipt
    from penta_mini.operational.core import WorkflowDecision
    from penta_mini.operational.sdk import ConnectorSDK, ConnectorDenied

    raw = json.loads((evidence / "contract-samples.json").read_text(encoding="utf-8"))
    current = HostSnapshot.model_validate(raw["current"])
    partial = HostSnapshot.model_validate(raw["partial"])
    prepared = PreparedOperation.model_validate(raw["prepared"])
    approved = PreparedOperation.model_validate(raw["approved"])
    receipt = Receipt.model_validate(raw["receipt"])
    succeeded = HostSnapshot.model_validate(raw["succeeded"])
    assert current.scope == partial.scope == prepared.scope == approved.scope == receipt.scope == succeeded.scope
    assert prepared.status == "awaiting_approval" and approved.status == "approved" and receipt.status == "succeeded"
    assert succeeded.draft.operation_ref == receipt.operation_ref == approved.ref
    assert succeeded.draft.fields == approved.canonical_fields
    checks = 6

    class Replay:
        def __init__(self):
            self.host = partial
            self.operation_value = prepared
            self.calls = 0

        def current(self, scope):
            return self.host

        def prepare(self, decision):
            return [prepared]

        def operation(self, scope, operation_ref):
            return self.operation_value

        def execute_once(self, operation):
            self.calls += 1
            return receipt  # retained result only; no HTTP/SQL write

        def status(self, scope, operation_ref):
            return receipt

    connector = Replay()
    sdk = ConnectorSDK(connector)
    decision = WorkflowDecision.model_validate({
        "request_ref": prepared.request_ref, "scope": partial.scope.model_dump(mode="json"),
        "base_revision": partial.draft.revision, "catalog_revision": partial.catalog_revision,
        "kind": "host_prepare", "questions": [], "invalidates_review": True,
        "candidates": [{"tool_key": prepared.tool_key, "edit": {"mode": "replace", "remove": [],
            "set": {key: value.model_dump(mode="json") for key, value in prepared.canonical_fields.items()}}}],
    })
    assert sdk.prepare(decision) == [prepared]
    checks += 1
    try:
        sdk.execute(partial.scope, prepared.ref, prepared.payload_hash, prepared.draft_revision)
        raise AssertionError("Unapproved execution accepted")
    except ConnectorDenied:
        assert connector.calls == 0
        checks += 1
    approved_host = succeeded.model_copy(update={"draft": succeeded.draft.model_copy(update={"status": "approved"})}, deep=True)
    connector.host = approved_host
    connector.operation_value = approved
    assert sdk.execute(approved.scope, approved.ref, approved.payload_hash, approved.draft_revision) == receipt
    assert connector.calls == 1
    checks += 1
    assert sdk.reconcile(approved.scope, approved.ref, approved.payload_hash, approved.draft_revision) == receipt
    assert connector.calls == 1
    checks += 1
    altered = approved.scope.model_copy(update={"actor_id": "foreign-synthetic-actor"})
    try:
        sdk.execute(altered, approved.ref, approved.payload_hash, approved.draft_revision)
        raise AssertionError("Foreign scope accepted")
    except ConnectorDenied:
        assert connector.calls == 1
        checks += 1
    for pin in pins:
        assert hashlib.sha256(Path(pin["path"]).read_bytes()).hexdigest() == pin["sha256"]
    output = {"passed": True, "checks": checks, "sqlRun": sys.argv[1], "sharedSourcePins": pins,
              "sampleSha256": hashlib.sha256((evidence / "contract-samples.json").read_bytes()).hexdigest(),
              "modelLoaded": False, "inference": False, "liveHostMutation": False, "miniSourceChanged": False,
              "productionApproved": False}
    (evidence / "shared-contract-check.json").write_text(json.dumps(output, indent=2), encoding="utf-8")
    print(json.dumps({"passed": True, "checks": checks, "modelLoaded": False, "liveHostMutation": False}))


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        print(json.dumps({"passed": False, "failureType": type(error).__name__, "detailsWithheld": True}))
        raise SystemExit(1)
