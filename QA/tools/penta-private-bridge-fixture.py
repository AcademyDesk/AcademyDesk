"""Run-owned loopback transport fixture ONLY. Scripted proposals, no model.

Imports the actual PENTA planner/reducer/private factory. Synthetic qualification
is test-only and cannot be used to qualify or activate a real interpreter.
"""
import json
import os
import re
import sys
from pathlib import Path

if not re.fullmatch(r'[a-f0-9]{32}',os.environ.get('QA_PENTA_BRIDGE_RUN_ID','')):
    raise SystemExit('Run-owned fixture required')
root=Path(os.environ['QA_PENTA_BRIDGE_SOURCE']).resolve()
sys.path[:0]=[str(root/'src'),str(root/'tests')]
from test_operational_contract import Provider, qualification
from penta_mini.operational.planner import QualifiedPlanner
from penta_mini.operational.private_bridge import create_private_app
import uvicorn

class Scripted(Provider):
    async def generate(self,messages,schema):
        body=json.loads(messages[-1]['content'])
        return dict(version='operational-draft-0.3.0',request_ref=body['request_ref'],
                    base_revision=body['base_revision'],kind='unsupported',steps=[],question_fields=[])

app=create_private_app(os.environ['QA_PENTA_BRIDGE_CALLBACK'],os.environ['QA_PENTA_BRIDGE_CHANNEL'],
    QualifiedPlanner(Scripted(),qualification()),paired_activation_approved=True)
uvicorn.run(app,host='127.0.0.1',port=int(sys.argv[1]),log_level='critical',access_log=False)
