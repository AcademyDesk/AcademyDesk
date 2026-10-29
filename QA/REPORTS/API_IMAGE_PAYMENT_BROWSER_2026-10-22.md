# Linked Payments browser checkpoint — 22 October 2026

Status: **NATIVE EXPORTED-PAGE / SQL BROWSER CHECK PASS, 4/4 JOURNEYS**.
Existing BUG-DATA-0001 live Payments acceptance gap, not a new test plan or a
claim that API-only success closes browser/device/release acceptance.

## Accepted successor

SQL/API run `294dd8d8dbe34109a54f80e0f59c2d1c`, window
`2df977e56b5d467ea0beaaa378439399`:45 ordinary seed/security checks plus four
real browser payment submissions.390/1440px light both returned201 with exact
success feedback;390/1440px dark stale submissions both returned400 with
`Payment exceeds the invoice balance.` All four visible pickers collapsed;
forms reset/disabled and SQL retained exactly two rows/1000collected/Paid/zero
balance. No429, page exceptions or horizontal overflow.628export hashes/current
web and API source match accepted builds. Final synthetic count2academies,
5identities,8payments. No response mocks or injected authentication tokens.

Window admitted3.039GiB, runtime exit0, exact originals restored/API running/
inference healthy; full container/volume/network/image inventories preserved
(29containers41volumes). Only owned synthetic containers/network removed.
Screenshots: `QA/EVIDENCE/api-image-sql-294dd8d8dbe34109a54f80e0f59c2d1c/`.
Mobile-light and desktop-dark screenshots were visually inspected. Desktop
capture reflects the scrolled submit position, not a clipped page header.

The first runtime attempt `1ab7931e4a85424c8bcda0d096812b31`, window
`feb7bd3472064ab6b83eedadee4de091`, failed after43fixture checks before any browser
payment. Source inspection found the actual login uses`?useCookies=false`, absent
from the QA relay's exact allowlist. Added regression first failed1/2, then2/2
passed with only that exact query variant admitted; other login/refresh query
variants remain refused. Added value-free phase/request diagnostics. No app,
policy, original race runner, image or acceptance requirement changed. Earlier
failure retained; its owned cleanup/original restoration/inventories PASS.

Read-only pinned qualification now includes this native browser receipt and
negative portable tests for missing cases, mocked/injected auth, incorrect SQL,
wrong statuses, missing actual submits, page errors and rate limiting.
BUG-DATA-0001 remains **OPEN**:physicalAndroid/iOS, broader faults/roles and all
enterprise/security/model/release gates are separate. This is not90%AI acceptance
or production approval. No commit,push,main/Mini change or Azure deployment.

QA-only helper uses the already accepted production export build
`bad444b6c61e45f680ed62f76d35dcf4` and immutable current API build
`0a5f8d2c708a40a994dd97e9f6d12425`. Export preflight verifies current217-source
pins against its accepted build receipt and628exported file byte hashes against
the accepted static-runtime receipt before any browser or SQL startup. No new
application/dependency build, source alteration or runtime model call is required.

## Real chain and acceptance exercised by the successor

Owned loopback static server → actual user-visible login form → a byte-preserving
private HTTP relay → real candidate Kestrel/Identity/domain endpoints → disposable
SQL. The API container/network have no public ports. The relay forwards actual
requests/responses, not canned data; tokens come only from successful UI login,
not injected localStorage. Nonlocal traffic and all unapproved writes are refused.
Theme selection alone is set before load. No customer credentials/data or external
site is involved. Relay paths reject static escapes; only login/refresh and exact
owned-academy paymentPOSTs are allowed browser writes.

Four journeys:390/1440px × light/dark. Each starts with an API-created
invoice1000/reconciled600 and current balance400. Visible invoice picker must
collapse after selection. Light cases submit400through the UI and require201,
truthful success feedback/reset/disabled empty form. Dark cases get a real competing
API payment400 after page selection; UI submission must return400/exact balance
message, refresh/reset instead of falsely claiming success. Every case must read
back exactly two payment rows/1000collected/Paid/balance0 from real SQL, no429,
page exceptions or horizontal overflow. Screenshots only contain synthetic data.

## Retained initial admission evidence (superseded only by the accepted run above)

Window`e0a5ee94da0f4478bffc0afa1f37ead3` refused unchanged3GiB guard at
2,997,305,344bytes (~2.792GiB), before helper/browser/SQL startup. Exit1,
restorationPassedtrue/inventoryPreservedtrue. Both original PENTA service IDs/
images restored/API running/inference healthy/full inventories preserved,
29containers41volumes. No restart/guard reduction/unrelated app termination.

Two relay/path tests and full existing combined tool suite PASS. Separate export
preflight verified628files/current source pins with browserStartedfalse/sqlStartedfalse.
This initial preflight was NOT live browser acceptance. The accepted successor
above ran only after genuine memory recovery
under the existing bounded stop/test/immediate-restore approval. No repeated
unchanged retry, no issue closure, no physical-device/Azure/model acceptance.

Sol High for real API/SQL/browser integration; Medium for subsequent settled
visual work. Main/Mini/staging/commits/push/production data remain untouched.
