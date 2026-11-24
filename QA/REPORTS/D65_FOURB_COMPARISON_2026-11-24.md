# D65 — bounded 4B comparison; not accepted or activated

Client date: 24 November 2026. This records the completed, explicitly approved
comparison; it does not authorize another model experiment. It supersedes D64's
historical statement that this one comparison was awaiting owner approval.

## Source and execution

- Academy source: `3540c978cca3bfe7331c2b3446d7421831e1f7e0`.
- Mini source: `d9c3d7550194685d4e3572befbc092d25541ff04`.
- Inactive router SHA256: `d1964268b2bbb497115bdd53c5b2e56d1fb15cae89e5b9a80d4375c6b19de4dc`.
- Existing Qwen3.5-4B Q4_K_M, pinned revision
  `e87f176479d0855a907a41277aca2f8ee7a09523`; 2,740,937,888 bytes;
  verified SHA256 `00fe7986ff5f6b463e62455821146049db6f9313603938a70800d1fb69ef11a4`.
- Run: `cf3f88af5578428faf1984d2c8598d4f`. CPU, four threads, no GPU,
  context 2048, output 256 per generation, reasoning disabled. One 15-minute
  bounded comparison, stop on first semantic failure, no semantic retry.
- Initial setup `817b7588846a46eb961220a67a750280` failed at Docker logging
  configuration before model startup. Retained separately; logging configuration
  was corrected without changing the semantic evaluation profile.

## Actual results

| Case | Expected | Actual | Result |
| --- | --- | --- | --- |
| Create student, create batch, enroll student | Three corresponding operation selections | Correct three selections; 64.154 seconds, four completed generation responses | PASS for selection only |
| Show the teachers in my academy | No executable selection: catalog lacks teacher reads | `AcademyDesk.Teacher.Create`; 13.533 seconds, two completed responses | FAIL |

**One passed, two executed, nine planned, seven unrun. Semantic acceptance FAILED.**
The inference gate remained healthy. No domain read, create, update, delete or
approval execution occurred. Prospective read-tool cases were not reached and
are not implementations. Empty operation-selection arguments do not establish
field extraction, clarification, execution, answer quality or end-to-end coverage.
This artificial write-only catalog cannot establish how a fully connected
all-domain assistant would perform. Both catalog/orchestration and model/profile
limitations remain relevant; changing weight size did not repair integration.

## Preservation and limits

- Private evidence: `.build-check/d65-fourb/run-cf3f88af5578428faf1984d2c8598d4f/`.
  Original responses, evaluator/probe/source packet and owner declaration remain
  private. No raw database backup or credentials are published.
- Original synthetic SQL received COPY_ONLY/CHECKSUM backup and RESTORE VERIFYONLY;
  backup hash `5d3f3517a52bd3813497ea9eab620af18b00be7285c081538bd932b9595b33ad`.
  VERIFYONLY is not an actual restore acceptance test.
- Free RAM before load: 5,445,861,376 bytes; minimum observed: 1,916,645,376 bytes.
  Configured model limit 3,758,096,384 bytes; kernel peak 3,758,108,672 bytes.
  Do not claim the reported peak was strictly below the configured limit.
- Exact original container/image/network/volume inventories preserved; originals
  restored; owned comparison containers/network removed, cleanup errors empty.
- At restoration: owner login 200, 13 students, four teachers, four batches,
  legacy Mini Available; new task catalog 404. Credentials and original keys kept.
- No downloads, training, Azure, paid compute or default activation. 4B comparison
  container removed. Last verified app remained the cached 2B development pilot.
- During the subsequent D66 read-only diagnosis, demo fetch failed and the Docker
  Linux engine pipe was unavailable. Restoration above is historical evidence,
  not a claim that the demo is currently running. No restart/loading was attempted.

Next: [D66 integration diagnosis](D66_PENTA_INTEGRATION_DIAGNOSIS_2026-11-24.md),
before further model changes or application implementation.
