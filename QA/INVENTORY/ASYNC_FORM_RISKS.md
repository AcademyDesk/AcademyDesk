# Async form event reset candidates

Source finding: these handlers use event.currentTarget.reset() after await. Browser reproduction is required before closure; no production write was performed. React currentTarget is only set while dispatching the handler.

| Source | Function | Expression |
| --- | --- | --- |
| apps/web/src/app/academic-governance/page.tsx:111 | submit | event.currentTarget.reset() |
| apps/web/src/app/academic-periods/page.tsx:92 | save | event.currentTarget.reset() |
| apps/web/src/app/access-review/page.tsx:106 | grant | event.currentTarget.reset() |
| apps/web/src/app/access-review/sign-off/page.tsx:2 | save | e.currentTarget.reset() |
| apps/web/src/app/compliance/page.tsx:49 | addDocument | event.currentTarget.reset() |
| apps/web/src/app/compliance/page.tsx:56 | addConsent | event.currentTarget.reset() |
| apps/web/src/app/payroll/page.tsx:21 | createProfile | event.currentTarget.reset() |
| apps/web/src/app/payroll/page.tsx:22 | pay | event.currentTarget.reset() |
| apps/web/src/app/platform/control/page.tsx:342 | createCase | event.currentTarget.reset() |
| apps/web/src/app/platform/control/page.tsx:431 | createInvoice | event.currentTarget.reset() |
| apps/web/src/app/platform/control/page.tsx:512 | changeOwnerPassword | event.currentTarget.reset() |
| apps/web/src/app/platform/control/page.tsx:530 | publishAnnouncement | event.currentTarget.reset() |
| apps/web/src/app/platform-services/page.tsx:51 | createCase | event.currentTarget.reset() |
| apps/web/src/app/portal/page.tsx:741 | go | e.currentTarget.reset() |
| apps/web/src/app/student-onboarding/page.tsx:56 | submit | event.currentTarget.reset() |
| apps/web/src/app/teacher/page.tsx:443 | addNote | event.currentTarget.reset() |
| apps/web/src/app/teacher/page.tsx:450 | addFile | event.currentTarget.reset() |
| apps/web/src/app/teacher/page.tsx:543 | create | e.currentTarget.reset() |
| apps/web/src/app/teacher/page.tsx:647 | save | event.currentTarget.reset() |
| apps/web/src/app/teacher-onboarding/page.tsx:68 | submit | event.currentTarget.reset() |
| apps/web/src/app/work-queue/page.tsx:13 | create | event.currentTarget.reset() |
