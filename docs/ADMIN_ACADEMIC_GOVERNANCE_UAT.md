# Admin Academic Governance acceptance checklist

Use an Academy Admin account with disposable courses, batches, learners, and assessments.

| Scenario | Expected result |
| --- | --- |
| Create an academic year and term | The term must stay within the year dates; only one year is current. |
| Close a year | The application blocks closure until every related term is closed. |
| Create curriculum module | The module starts as a draft and can be approved/published or returned to draft. |
| Create/activate a grading scheme | Valid grade-band JSON is required; only active schemes are selectable for new assessments. |
| Add a prerequisite | A course cannot require itself or receive the same prerequisite twice; enrolment respects the prerequisite. |
| Create a promotion request | The learner must have an active source enrolment and a different target batch. |
| Approve a promotion | The source enrolment becomes completed and one active target enrolment is created on the effective date. |
| Create an assessment with a scheme | Result grades use the selected active scheme; inactive schemes are rejected. |
| Invalid/cross-academy request | Invalid dates, duplicate rules, inactive grading schemes, and another academy's identifiers are rejected. |

Record the tester, sample records, result, and any defect before the Academic Governance batch is frozen.
