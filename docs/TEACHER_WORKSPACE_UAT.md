# Teacher workspace acceptance checklist

Run these checks using the development Teacher account after starting the API and web app.

| Scenario | Expected result |
| --- | --- |
| Teacher opens `/teacher` on desktop and phone | Responsive workspace shows only Today, Classes, Learners, Tasks, and More controls. |
| Teacher selects an assigned class | Only its class roster and attendance records are available. |
| Teacher marks attendance | Valid statuses save; the learner record refreshes; another teacher's session is denied. |
| Teacher publishes an assignment | Only an assigned batch can be selected; students can see published work. |
| Teacher creates a lesson plan | Plan is scoped to an assigned batch and delivery status can be updated. |
| Teacher creates an assessment and records results | Scores outside the allowed range or learners outside the batch are rejected. |
| Teacher reviews a submission or practice log | Feedback is visible to the linked student only. |
| Teacher requests leave or updates profile | The request/profile is scoped to the signed-in teacher. |
| Teacher requests an Admin or Finance URL | The request returns `403 Forbidden`; no academy-wide data is exposed. |

Record the tester, device, sample class, and defects before freezing the Teacher workspace.
