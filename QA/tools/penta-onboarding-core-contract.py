"""Validate synthetic Academy SQL context receipts with the actual pinned Core.

Read-only source/receipt checks. No inference, model/native imports, network,
training, business writes or PENTA repository changes.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--penta-root", type=Path, required=True)
    parser.add_argument("--snapshots", type=Path, required=True)
    args = parser.parse_args()
    root = args.penta_root.resolve(strict=True)
    manifest = json.loads((root / "operations/release-manifest-candidate.5.json").read_text(encoding="utf-8"))
    if manifest["version"] != "penta-operational-return-0.3.0-candidate.5":
        raise ValueError("Wrong Core candidate")
    pins = {item["path"]: item["sha256"] for item in manifest["backend"]["files"]}
    for name in ("src/penta_mini/operational/__init__.py", "src/penta_mini/operational/contracts.py",
                 "src/penta_mini/operational/core.py", "src/penta_mini/operational/sdk.py",
                 "src/penta_mini/policy_v1.py"):
        if hashlib.sha256((root / name).read_bytes()).hexdigest() != pins[name]:
            raise ValueError("Pinned Core source changed: " + name)
    path = args.snapshots.resolve(strict=True)
    if "QA" not in path.parts or "EVIDENCE" not in path.parts or path.name != "snapshots.json" or path.stat().st_size > 262144:
        raise ValueError("Expected bounded local synthetic QA receipt")
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(root / "src"))
    from penta_mini.operational.contracts import HostSnapshot, Proposal, clone
    from penta_mini.operational.core import reduce_proposal
    values = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(values, list) or not 1 <= len(values) <= 20:
        raise ValueError("Bounded context receipt required")
    snapshots = [HostSnapshot.model_validate_json(json.dumps(value)) for value in values]
    for snapshot in snapshots:
        clone(snapshot)
        assert len(snapshot.tools) == 1 and len(snapshot.tools[0].fields) == 29
        assert snapshot.tools[0].key == "AcademyDesk.StudentOnboarding.Create"
        assert snapshot.tools[0].family == "AI-04"
        assert snapshot.tools[0].approval_required and snapshot.tools[0].preparation_required
        assert snapshot.scope.tenant_id and snapshot.scope.actor_id and snapshot.scope.context_version >= 1
    base = snapshots[0]
    def proposal(tool, fields, host=base):
        return Proposal.model_validate_json(json.dumps({
            "version": "operational-draft-0.3.0", "request_ref": "synthetic-core-compatibility",
            "base_revision": host.draft.revision, "kind": "prepare", "steps": [{"tool_key": tool,
                "edit": {"mode": "replace", "set": fields, "remove": []}}], "question_fields": []}))
    missing = reduce_proposal(base, proposal(base.tools[0].key, {}))
    assert missing.kind == "clarify" and set(missing.questions) == {"StudentFirstName", "StudentLastName", "DateOfBirth"}
    complete = {"StudentFirstName": {"kind": "text", "value": "Synthetic"},
                "StudentLastName": {"kind": "text", "value": "Contract-only"},
                "DateOfBirth": {"kind": "date", "value": "2000-01-01"}}
    prepared = reduce_proposal(base, proposal(base.tools[0].key, complete))
    assert prepared.kind == "host_prepare" and prepared.candidates[0].edit.set.keys() == complete.keys()
    minor = next(s for s in snapshots if sum(f.required for f in s.tools[0].fields) == 6)
    minor_fields = complete | {"DateOfBirth": {"kind": "date", "value": "2015-01-01"}}
    minor_decision = reduce_proposal(minor, proposal(minor.tools[0].key, minor_fields, minor))
    assert minor_decision.kind == "clarify" and set(minor_decision.questions) == {"ParentFirstName", "ParentLastName", "ParentEmail"}
    for tool, fields in (("ExecuteSQL", complete), (base.tools[0].key, complete | {
            "BranchId": {"kind": "reference", "value": "unregistered-foreign-branch"}})):
        try:
            reduce_proposal(base, proposal(tool, fields))
        except ValueError:
            pass
        else:
            raise AssertionError("Foreign tool/reference accepted by Core")
    assert {"collecting", "approved", "succeeded"} <= {s.draft.status for s in snapshots}
    print(f"PASS: pinned candidate.5 actual HostSnapshot/schema/reducer compatibility; {len(snapshots)} SQL-generated snapshots, missing-field clarification, exact typed preparation, denied tool/foreign reference. No inference or activation.")
    print("Snapshot SHA256=" + hashlib.sha256(path.read_bytes()).hexdigest())


if __name__ == "__main__":
    main()
