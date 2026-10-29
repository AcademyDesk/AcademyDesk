# E5-R52 — owner-requested corrected build retry

20 October2026. User: “try again and move on.” One local guarded retry,
run `e5r49-e250268a9cbc`, evidence
`D:/PENTA-Models/operations/e5r49-build/e5r49-e250268a9cbc`.

## Final result — build/binary PASS; model NOT ACCEPTED

Build `e5r49-e250268a9cbc` completed2263.60s,compile0/prepare0/no OOM,
actual single-job compilation2122.20s,inside original2400s cap. All39candidate
artifacts recorded/hashed. Owned builder removed;original25container/config/
host/mount/state records exact and healthy/four idle slots restored.

Actual readonly binary probes all exit0:11382/source revision matches,native
ELF architecture75 only,QuadroT1000 detected,loader initialized
`/candidate/libggml-cuda.so`. Original containers unchanged by these no-model
probes. Verification receipt at run `verification/verification.json`.

Initial one-shot continuation correctly stopped at trial launcher exit2:wrapper
`--trial <path>` leaked into existing `--run`-only argument parser. No model was
loaded in that failed invocation. Wrapper now supplies only existing `--run` to
the inner runner and restores caller arguments in finally;42focused testsPASS/
0.65s,including success/failure argument restoration. No evaluator/oracle/limits
changed. Original failed continuation.json retained,not rewritten as success.

Corrected invocation then completed once,exit0,trial `e5r36-1e764c8e8d11`,195.28s.
Actual candidate mapped library/hash/offload/batch proofs verified before any
generation:25GPUlayers/128logical+microbatch,2048context/512output/4threads,
150sload/30srequest/original guards/scoring/quarantine unchanged,not instrumented.
All12tokenizer fits and12requests completed;1exact/8reducer-rejected/3semantic
mismatches/0unrun. Median request9.658s,max10.813s,min4.393s. This demonstrates
completed bounded inference on this candidate,NOT acceptable semantics or broad
performance/cold-start/device/production qualification. No claim of speed factor
from traces with differing instrumentation/history. `decision=NOT_ACCEPTED`.
Model requests use synthetic inputs only;no host execution/SQL/customer records.

Trial original25/config/host/mount/state and four idle slots restored;frozen59
source pins unchanged. `trial-corrected.txt` plus actual trial/evaluation receipts
are authoritative for the corrected invocation. No model activation/default,
Academy application/DB/Git/main/Azure changes. No active background build/trial.
Next:[offline failure disposition and inactive candidate](PENTA_PLAN_DIAGNOSIS_E5R53.md).

Existing corrected helper reused without compiler/configuration changes.
Same verified original salvage/source/full package inventory,3.5GiB available
RAM guard/three consecutive samples/45s,2.5GiBmemory/swap,2CPU,onejob,
180s configure/2400s compile/2700s outer caps and exact service restoration.
No Docker restart,guard relaxation,model activation,driver changes or Azure use.

- [x] Retry explicitly requested;original idle pair verified before pause.
- [x] Verified archive/source and prior corrected helper retained.
- [x] Memory guard passed:4196102144,4231786496,4171104256bytes consecutive.
- [x] Full installed package inventory matches original;network disconnected.
- [x] Actual CMake regeneration succeeded;configuration checks passed both sides.
- [x] Previously missing CPU output directory verified present in owned builder.
- [x] CUDA library linked and formerly failing CPU target compiled;actual fix verified.
- [x] Complete corrected build within original cap.
- [x] Original25exact/healthy4idle restoration at final build and trial receipts.
- [x] Binary hashes/version/native75/device/library proof.
- [x] Unchanged bounded uninstrumented model evaluation completed12/12.
- [ ] Semantic qualification:NOT ACCEPTED (1/12exact),not model/production ready.

One-shot continuation routes only this candidate,with fresh e5r52-continuation
output. R49/R50/R51 failures/blocks remain recorded. Prior212focused test count
retained,not a new enterprise run. Current tests and final results recorded below
when finished.24resume/route testsPASS/0.48s after exact candidate binding. Initial
invocation had8passes/16test-setup errors from a missing D:test parent;created only
that directory and reran using a fresh basetemp,original error XML retained.
All app/domain/semantic/role/device/Manual/enterprise/release gates
and BUG-DATA-0001 remain OPEN. No main/stage/commit/push/default/DB changes.

## Bounded progress preservation

At about30minutes,existing tested archive method reused against the exact owned
builder via `preserve_build_e5r52.py`;no compiler settings/restart/retry involved.
Final-salvage output under this run:5515files/368194718payload bytes/0changed files,
54.1866s (60s internal/90s client cap),SHA256
`845391341f1eed51799952b8cac48bdc3012fbc8a81275a90414c0df7f7b64f5`.
Exclusive regular-file stable-age copy/2GiB cap/original archive code unchanged.
Unqualified cache only;independent revalidation required before any future reuse.
