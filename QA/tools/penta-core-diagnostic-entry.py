"""Evaluation-only transparent transport capture. No synthetic model responses.

Core logic stays unchanged. Only internal model:8080 responses are forwarded;
bounded raw completions stay in the private run directory, never public logs.
"""
import hashlib
import itertools
import json
from pathlib import Path

import httpx

MAX_CAPTURE = 64 * 1024


class CapturedStream(httpx.AsyncByteStream):
    def __init__(self, response, transport, directory, sequence, budget):
        self.response, self.transport, self.directory = response, transport, directory
        self.sequence, self.budget = sequence, budget
        self.parts, self.seen, self.complete, self.closed = [], 0, False, False

    async def __aiter__(self):
        async for chunk in self.response.stream:
            left = max(0, MAX_CAPTURE - self.seen)
            if left:
                self.parts.append(chunk[:left])
            self.seen += len(chunk)
            yield chunk
        self.complete = True

    async def aclose(self):
        if self.closed:
            return
        self.closed = True
        try:
            await self.response.aclose()
        finally:
            await self.transport.aclose()
        if self.sequence is not None:
            raw = b"".join(self.parts)
            self.directory.mkdir(parents=True, exist_ok=True)
            base = self.directory / f"completion-{self.sequence:03d}"
            base.with_suffix(".bin").write_bytes(raw)
            base.with_suffix(".json").write_text(json.dumps({
                "status": self.response.status_code, "budget": self.budget,
                "seen": self.seen, "retained": len(raw), "truncated": self.seen > len(raw),
                "complete": self.complete, "sha256": hashlib.sha256(raw).hexdigest(),
            }), encoding="utf-8")


class DiagnosticTransport(httpx.AsyncBaseTransport):
    def __init__(self, directory, factory=None):
        self.directory = Path(directory)
        self.factory = factory or (lambda: httpx.AsyncHTTPTransport(retries=0, trust_env=False))
        self.sequence = itertools.count(1)

    async def handle_async_request(self, request):
        if request.url.scheme != "http" or request.url.host != "model" or request.url.port != 8080:
            raise ValueError("Diagnostic model target changed")
        completion = request.url.path == "/v1/chat/completions"
        budget, sequence = None, None
        if completion:
            payload = json.loads(request.content)
            schema = payload.get("response_format", {}).get("json_schema", {})
            properties = set(schema.get("schema", {}).get("properties", {}))
            budget = payload.get("max_tokens")
            shape = schema.get("schema", {})
            action = shape.get("properties", {}).get("action", {})
            choices = action.get("enum", [])
            action_scope = (properties == {"action"} and shape.get("type") == "object"
                and shape.get("required") == ["action"] and action.get("type") == "string"
                and isinstance(choices, list) and 2 <= len(choices) <= 32
                and all(isinstance(value, str) and value in ({"clarify", "unsupported"} |
                    {f"{operation}:{entity}" for operation in ("list", "filter", "current", "position")
                     for entity in ("teacher", "student", "batch", "student-fees", "session", "attendance")}) for value in choices)
                and len(choices) == len(set(choices)))
            permitted = {96, 192, 256} if properties == {"kind", "entity", "mode"} or action_scope else {768} if properties == {"kind", "entity", "mode", "filters", "remove", "position"} else set()
            if type(budget) is not int or budget not in permitted or payload.get("temperature") != 0 or payload.get("stream") is not False or schema.get("strict") is not True or schema.get("schema", {}).get("additionalProperties") is not False:
                raise ValueError("Diagnostic generation profile changed")
            if payload.get("chat_template_kwargs", {}).get("enable_thinking") not in (None, False):
                raise ValueError("Diagnostic reasoning profile changed")
            sequence = next(self.sequence)
            if sequence > 28:
                raise ValueError("Diagnostic generation count exceeded")
        transport = self.factory()
        try:
            response = await transport.handle_async_request(request)
        except BaseException:
            await transport.aclose()
            raise
        stream = CapturedStream(response, transport, self.directory,
                                sequence, budget)
        return httpx.Response(response.status_code, headers=response.headers,
                              stream=stream, extensions=response.extensions)


if __name__ == "__main__":
    import uvicorn
    from penta_mini import academy_workspace_api as api
    api.provider.transport = DiagnosticTransport("/qa/private-completions")
    uvicorn.run(api.app, host="0.0.0.0", port=8000, access_log=False)
