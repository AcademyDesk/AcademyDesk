# Admin Access & Compliance acceptance checklist

| Scenario | Expected result |
| --- | --- |
| Create a staff member | The role, academy scope, account state, and optional teacher link appear in Staff and Access Review. |
| Create a Finance user | The user sees only Finance navigation and cannot request unrelated academy modules. |
| Offboard a staff account | The account becomes inactive, lockout is applied, and active sessions are invalidated. |
| Create an academy role | The permission bundle is saved in the role register and is visible during access review. |
| Sign off an access review | Review notes are retained; optional remediation creates an operational task. |
| Register/review a document | Document status changes only to an allowed review state; review date and expiry remain visible. |
| Record/withdraw consent | Withdrawal keeps the consent evidence record and stores the withdrawal timestamp. |
| Cross-academy access | Another academy's staff, document, consent, or review records return `403 Forbidden`. |

Record the tester, sample staff account, record IDs, outcome, and defects before this batch is frozen.
