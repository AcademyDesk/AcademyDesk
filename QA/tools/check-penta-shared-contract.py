"""Compare Academy's retained fixtures with an explicit Mini/Core checkout.

Deterministic compatibility only: no inference/provider import, network, SQL,
connector execution, training export or modification of Mini source.
"""
import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path

sys.dont_write_bytecode = True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mini-root", required=True, type=Path)
    args = parser.parse_args()
    academy = Path(__file__).resolve().parents[2]
    mini = args.mini_root.resolve(strict=True)
    source = mini / "src"
    names = ["penta_mini/operational/__init__.py", "penta_mini/operational/contracts.py",
             "penta_mini/__init__.py"]
    pins = [{"path": name, "sha256": hashlib.sha256((source / name).read_bytes()).hexdigest()}
            for name in names]
    head = subprocess.check_output(["git", "-C", str(mini), "rev-parse", "HEAD"], text=True).strip()
    sys.path.insert(0, str(source))
    from penta_mini.operational import VERSION
    from penta_mini.operational.contracts import PreparedOperation, Receipt, digest
    from pydantic import ValidationError

    if VERSION != "operational-draft-0.3.0":
        raise ValueError("Unexpected shared contract version")
    fixtures = academy / "tests" / "AcademyDesk.Api.Tests" / "Fixtures"
    vectors_path = fixtures / "penta-operational-payload-vectors.json"
    vectors = json.loads(vectors_path.read_text(encoding="utf-8"))
    checks = []
    for vector in vectors:
        canonical = json.dumps(vector["payload"], sort_keys=True, ensure_ascii=True, separators=(",", ":"))
        if canonical != vector["canonical"] or digest(vector["payload"]) != vector["sha256"]:
            raise ValueError("Cross-language canonical payload mismatch")
        checks.append("canonical-" + vector["id"])
    prepared_path = fixtures / "penta-prepared-operation.json"
    receipt_path = fixtures / "penta-committed-receipt.json"
    prepared_raw = json.loads(prepared_path.read_text(encoding="utf-8"))
    receipt_raw = json.loads(receipt_path.read_text(encoding="utf-8"))
    prepared = PreparedOperation.model_validate(prepared_raw)
    receipt = Receipt.model_validate(receipt_raw)
    if prepared.status != "awaiting_approval" or receipt.status != "succeeded":
        raise ValueError("Fixture lifecycle mismatch")
    if prepared.scope != receipt.scope or prepared.ref != receipt.operation_ref:
        raise ValueError("Fixture identity mismatch")
    if prepared.payload_hash != receipt.payload_hash or prepared.draft_revision != receipt.draft_revision:
        raise ValueError("Fixture revision/payload mismatch")
    if not receipt.audit_ref:
        raise ValueError("Committed sample lacks audit reference")
    checks.extend(["prepared-shared-model", "receipt-shared-model", "scope-operation-revision-binding"])
    for model, sample in [(PreparedOperation, prepared_raw), (Receipt, receipt_raw)]:
        changed = dict(sample, model_authorized=True)
        try:
            model.model_validate(changed)
        except ValidationError:
            checks.append(model.__name__ + "-unknown-authority-rejected")
        else:
            raise ValueError("Unknown authority field accepted")
    if any(name.startswith(("torch", "transformers", "penta_mini.inference")) for name in sys.modules):
        raise ValueError("Contract check imported an inference dependency")
    for pin in pins:
        if hashlib.sha256((source / pin["path"]).read_bytes()).hexdigest() != pin["sha256"]:
            raise ValueError("Mini source changed during contract check")
    output = academy / ".build-check" / "penta-contracts" / "shared-contract-result.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    result = {"passed": True, "checks": checks, "miniCommit": head, "sharedSourcePins": pins,
              "fixturePins": [{"path": str(p.relative_to(academy)), "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}
                              for p in [vectors_path, prepared_path, receipt_path]],
              "modelLoaded": False, "inference": False, "domainExecuted": False,
              "miniSourceChanged": False, "productionAccepted": False}
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": True, "checks": len(checks), "miniCommit": head,
                      "modelLoaded": False, "productionAccepted": False}))


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        # Report a safe category, not payloads, machine credentials or source text.
        print(json.dumps({"passed": False, "failureType": type(error).__name__, "detailsWithheld": True}))
        raise SystemExit(1)
