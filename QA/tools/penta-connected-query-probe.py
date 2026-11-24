"""Prepared exposed semantic diagnostic. No model loading or domain dispatch.

Run only inside the separately approved local packet; importing this module
does not contact a service. Actual loopback publication/host checks are separate.
"""
import argparse
import json
import time

import httpx


def request_for(packet, case):
    tools = [{"key": f"AcademyDesk.{entity}.{kind}", "entity": entity,
              "kind": kind, "readOnly": True, "fields": fields}
             for entity, fields in packet["fields"].items()
             if entity not in case.get("omit_entities", [])
             for kind in ("search", "open")]
    return {"contract": "academy-query.v1", "text": case["text"],
            "context": case.get("context", {"entity": None, "filters": {}, "result_labels": []}),
            "tools": tools}


def accepted(case, value):
    if not isinstance(value, dict) or set(value) != {"contract", "kind", "entity", "filters", "position"}:
        return False
    if value["contract"] != "academy-query.v1":
        return False
    if "expected" in case:
        return value == {"contract": "academy-query.v1", **case["expected"]}
    return value["kind"] in case["expected_kinds"] and value["filters"] == {} and value["position"] is None


def run(packet, client, report):
    results = []
    started = time.monotonic()
    for case in packet["cases"]:
        if time.monotonic() - started >= packet["limits"]["max_automated_seconds"]:
            break
        began = time.monotonic()
        row = {"id": case["id"], "passed": False}
        try:
            response = client.post("/v1/academy-query/plan", json=request_for(packet, case))
            row["status"] = response.status_code
            row["reply"] = response.json()
            row["code_identity"] = {key: response.headers.get(key) for key in
                ("X-Penta-Query-Contract", "X-Penta-Query-Source", "X-Penta-Query-Prompt", "X-Penta-Mini-Runtime")}
            row["passed"] = response.status_code == 200 and accepted(case, row["reply"])
            row["passed"] = row["passed"] and row["code_identity"]["X-Penta-Query-Contract"] == "academy-query.v1"
            row["passed"] = row["passed"] and all(isinstance(row["code_identity"][k], str) and
                len(row["code_identity"][k]) == 64 and all(c in "0123456789abcdef" for c in row["code_identity"][k])
                for k in ("X-Penta-Query-Source", "X-Penta-Query-Prompt"))
            row["passed"] = row["passed"] and bool(row["code_identity"]["X-Penta-Mini-Runtime"])
            row["passed"] = row["passed"] and row["code_identity"]["X-Penta-Query-Source"] == packet.get("expected_query_source")
        except (ValueError, httpx.HTTPError) as error:
            row["error_type"] = type(error).__name__
        row["seconds"] = round(time.monotonic() - began, 3)
        results.append(row)
        report(row)
        if not row["passed"]:
            break
    result = {"executed": len(results), "passed": sum(r["passed"] for r in results),
              "unrun": [c["id"] for c in packet["cases"][len(results):]],
              "accepted": len(results) == len(packet["cases"]) and all(r["passed"] for r in results)}
    report({"summary": result})
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--packet", required=True)
    parser.add_argument("--base-url", required=True)
    args = parser.parse_args()
    # Candidate is an internal-network alias, never an arbitrary external URL.
    if args.base_url != "http://core:8000":
        parser.error("Only the prepared internal Core endpoint is allowed")
    with open(args.packet, encoding="utf-8") as stream:
        packet = json.load(stream)
    with httpx.Client(base_url=args.base_url, timeout=packet["limits"]["max_case_seconds"], trust_env=False) as client:
        summary = run(packet, client, lambda row: print(json.dumps(row), flush=True))
    raise SystemExit(0 if summary["accepted"] else 1)
