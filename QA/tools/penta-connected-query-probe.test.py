"""Model-free evaluator tests; never activate the proposed model packet."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

import httpx

spec = importlib.util.spec_from_file_location("probe", Path(__file__).with_name("penta-connected-query-probe.py"))
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)


class ProbeTests(unittest.TestCase):
    def setUp(self):
        self.packet = {"expected_query_source": "a" * 64, "fields": {"teacher": ["name", "subject", "status"], "student": ["name", "status"]},
            "limits": {"max_automated_seconds": 900}, "cases": [
                {"id": "teacher", "text": "Show teachers", "expected": {"kind": "search", "entity": "teacher", "filters": {}, "position": None}},
                {"id": "student", "text": "Show students", "expected": {"kind": "search", "entity": "student", "filters": {}, "position": None}}]}

    def execute(self, handler):
        rows = []
        with httpx.Client(base_url="http://core:8000", transport=httpx.MockTransport(handler)) as client:
            summary = probe.run(self.packet, client, rows.append)
        return summary, rows

    def test_wrong_domain_stops_after_one_dispatch_and_retains_reply_and_unrun(self):
        calls = []
        def handler(request):
            calls.append(request)
            return httpx.Response(200, json={"contract": "academy-query.v1", "kind": "search", "entity": "student", "filters": {}, "position": None}, headers={
                "X-Penta-Query-Contract": "academy-query.v1", "X-Penta-Query-Source": "a" * 64,
                "X-Penta-Query-Prompt": "b" * 64, "X-Penta-Mini-Runtime": "synthetic-test-code-identity"})
        summary, rows = self.execute(handler)
        self.assertFalse(summary["accepted"])
        self.assertEqual(len(calls), 1)
        self.assertEqual(summary["unrun"], ["student"])
        self.assertEqual(rows[0]["reply"]["entity"], "student")

    def test_429_is_failure_without_retry_or_fallback(self):
        calls = []
        summary, _ = self.execute(lambda request: (calls.append(request) or httpx.Response(429, json={"detail": "busy"})))
        self.assertFalse(summary["accepted"])
        self.assertEqual(len(calls), 1)

    def test_all_exact_cases_need_bound_code_identity_and_full_completion(self):
        calls = []
        def handler(request):
            case = self.packet["cases"][len(calls)]
            calls.append(json.loads(request.content))
            return httpx.Response(200, json={"contract": "academy-query.v1", **case["expected"]}, headers={
                "X-Penta-Query-Contract": "academy-query.v1", "X-Penta-Query-Source": "a" * 64,
                "X-Penta-Query-Prompt": "b" * 64, "X-Penta-Mini-Runtime": "synthetic-test-code-identity"})
        summary, _ = self.execute(handler)
        self.assertTrue(summary["accepted"])
        self.assertEqual(summary["executed"], 2)
        self.assertEqual(summary["unrun"], [])
        self.assertEqual(calls[0]["text"], "Show teachers")

    def test_extra_model_authority_and_nonexact_filters_cannot_pass(self):
        value = {"contract": "academy-query.v1", **self.packet["cases"][0]["expected"]}
        for mutation in ({"tenant": "foreign"}, {"filters": {"name": "invented"}}, {"kind": "clarify"}):
            changed = copy.deepcopy(value)
            changed.update(mutation)
            self.assertFalse(probe.accepted(self.packet["cases"][0], changed))

    def test_wrong_source_identity_never_passes_even_with_the_correct_plan(self):
        case = self.packet["cases"][0]
        summary, _ = self.execute(lambda request: httpx.Response(200,
            json={"contract": "academy-query.v1", **case["expected"]}, headers={
                "X-Penta-Query-Contract": "academy-query.v1", "X-Penta-Query-Source": "c" * 64,
                "X-Penta-Query-Prompt": "b" * 64, "X-Penta-Mini-Runtime": "synthetic-test-code-identity"}))
        self.assertFalse(summary["accepted"])
        self.assertEqual(summary["executed"], 1)

    def test_timeout_retains_failure_and_does_not_execute_the_next_case(self):
        def handler(request):
            raise httpx.ReadTimeout("synthetic timeout")
        summary, rows = self.execute(handler)
        self.assertFalse(summary["accepted"])
        self.assertEqual(summary["unrun"], ["student"])
        self.assertEqual(rows[0]["error_type"], "ReadTimeout")

    def test_catalog_omission_removes_both_tools_and_context_is_preserved(self):
        case = {"text": "I said teachers", "omit_entities": ["student"],
                "context": {"entity": "student", "filters": {"name": "Mira"}, "result_labels": ["Result 1"]}}
        request = probe.request_for(self.packet, case)
        self.assertEqual({t["entity"] for t in request["tools"]}, {"teacher"})
        self.assertEqual(len(request["tools"]), 2)
        self.assertEqual(request["context"], case["context"])


if __name__ == "__main__":
    unittest.main()
