import asyncio
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import httpx

spec = importlib.util.spec_from_file_location("entry", Path(__file__).with_name("penta-core-diagnostic-entry.py"))
entry = importlib.util.module_from_spec(spec); spec.loader.exec_module(entry)


def payload(budget=96):
    return {"max_tokens": budget, "temperature": 0, "stream": False,
            "response_format": {"json_schema": {"strict": True, "schema": {
                "properties": {key: {} for key in ("kind", "entity", "mode")}, "additionalProperties": False}}}}


class EntryTests(unittest.TestCase):
    def test_actual_bytes_are_forwarded_and_private_metadata_records_complete_reply(self):
        async def run():
            with tempfile.TemporaryDirectory() as directory:
                calls = []
                def respond(request):
                    calls.append(request)
                    return httpx.Response(200, content=b'{"synthetic":"UNCHANGED"}', headers={"content-type": "application/json"})
                transport = entry.DiagnosticTransport(directory, lambda: httpx.MockTransport(respond))
                async with httpx.AsyncClient(transport=transport) as client:
                    response = await client.post("http://model:8080/v1/chat/completions", json=payload())
                self.assertEqual(response.content, b'{"synthetic":"UNCHANGED"}')
                self.assertEqual(len(calls), 1)
                self.assertEqual(Path(directory, 'completion-001.bin').read_bytes(), response.content)
                meta = json.loads(Path(directory, 'completion-001.json').read_text())
                self.assertTrue(meta['complete']); self.assertFalse(meta['truncated']); self.assertEqual(meta['budget'], 96)
        asyncio.run(run())

    def test_profile_and_target_rejections_do_not_dispatch(self):
        async def run():
            with tempfile.TemporaryDirectory() as directory:
                def factory():
                    raise AssertionError('Must not dispatch')
                async with httpx.AsyncClient(transport=entry.DiagnosticTransport(directory, factory)) as client:
                    for url, body in [('http://outside:8080/v1/chat/completions', payload()),
                                      ('http://model:8080/v1/chat/completions', payload(999))]:
                        with self.assertRaises(ValueError):
                            await client.post(url, json=body)
                    for field, value in [('temperature', 1), ('stream', True), ('chat_template_kwargs', {'enable_thinking': True})]:
                        body = payload(); body[field] = value
                        with self.assertRaises(ValueError):
                            await client.post('http://model:8080/v1/chat/completions', json=body)
                    body = payload(); body['response_format']['json_schema']['strict'] = False
                    with self.assertRaises(ValueError):
                        await client.post('http://model:8080/v1/chat/completions', json=body)
                self.assertEqual(list(Path(directory).iterdir()), [])
        asyncio.run(run())

    def test_preflight_is_forwarded_without_recording_template_or_token_data(self):
        async def run():
            with tempfile.TemporaryDirectory() as directory:
                async with httpx.AsyncClient(transport=entry.DiagnosticTransport(directory,
                        lambda: httpx.MockTransport(lambda _: httpx.Response(200, content=b'PRIVATE TEMPLATE DATA')))) as client:
                    response = await client.post('http://model:8080/apply-template', json={'messages': []})
                self.assertEqual(response.content, b'PRIVATE TEMPLATE DATA')
                self.assertEqual(list(Path(directory).iterdir()), [])
        asyncio.run(run())

    def test_generation_count_cannot_exceed_two_calls_per_fourteen_cases(self):
        async def run():
            with tempfile.TemporaryDirectory() as directory:
                calls = []
                def respond(request):
                    calls.append(request); return httpx.Response(200, content=b'{}')
                async with httpx.AsyncClient(transport=entry.DiagnosticTransport(directory, lambda: httpx.MockTransport(respond))) as client:
                    for _ in range(28):
                        await client.post('http://model:8080/v1/chat/completions', json=payload())
                    with self.assertRaises(ValueError):
                        await client.post('http://model:8080/v1/chat/completions', json=payload())
                self.assertEqual(len(calls), 28)
        asyncio.run(run())

    def test_large_stream_is_forwarded_but_capture_stays_bounded(self):
        async def run():
            with tempfile.TemporaryDirectory() as directory:
                content = b'x' * (entry.MAX_CAPTURE + 1024)
                async with httpx.AsyncClient(transport=entry.DiagnosticTransport(directory,
                        lambda: httpx.MockTransport(lambda _: httpx.Response(200, content=content)))) as client:
                    response = await client.post('http://model:8080/v1/chat/completions', json=payload())
                self.assertEqual(response.content, content)
                self.assertEqual(Path(directory, 'completion-001.bin').stat().st_size, entry.MAX_CAPTURE)
                self.assertTrue(json.loads(Path(directory, 'completion-001.json').read_text())['truncated'])
        asyncio.run(run())


if __name__ == '__main__': unittest.main()
