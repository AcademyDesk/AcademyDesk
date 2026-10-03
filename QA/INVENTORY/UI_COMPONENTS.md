# UI element source occurrences

Includes buttons/actions/links, menus, cards, tables, lists and shared overlays. Props retain handlers and conditions for tracing, not evaluated DOM. CSS effects and dynamic states need browser testing.

| Source | Route | Element | Text | Props / handler |
| --- | --- | --- | --- | --- |
| apps/web/src/app/academic-governance/page.tsx:177 | /academic-governance | nav |  | {} |
| apps/web/src/app/academic-governance/page.tsx:178 | /academic-governance | Link | Periods | {"href":"\"/academic-periods\""} |
| apps/web/src/app/academic-governance/page.tsx:179 | /academic-governance | Link | Curriculum | {"href":"\"/curriculum\""} |
| apps/web/src/app/academic-governance/page.tsx:180 | /academic-governance | Link | Promotions | {"href":"\"/batch-promotions\""} |
| apps/web/src/app/academic-governance/page.tsx:189 | /academic-governance | article |  | {} |
| apps/web/src/app/academic-governance/page.tsx:193 | /academic-governance | article |  | {} |
| apps/web/src/app/academic-governance/page.tsx:197 | /academic-governance | article |  | {} |
| apps/web/src/app/academic-governance/page.tsx:237 | /academic-governance | button | Create scheme | {"disabled":"{saving}","className":"\"enterprise-action-button governance-action\""} |
| apps/web/src/app/academic-governance/page.tsx:256 | /academic-governance | StandardSelectField |  | {"name":"\"courseId\"","value":"{courseId}","onChange":"{setCourseId}","placeholder":"\"Course to unlock\"","options":"{courseOptions}"} |
| apps/web/src/app/academic-governance/page.tsx:263 | /academic-governance | StandardSelectField |  | {"name":"\"requiredCourseId\"","value":"{requiredCourseId}","onChange":"{setRequiredCourseId}","placeholder":"\"Required completed course\"","options":"{courseOptions}"} |
| apps/web/src/app/academic-governance/page.tsx:270 | /academic-governance | button | Save prerequisite | {"disabled":"{saving}","className":"\"enterprise-action-button governance-action\""} |
| apps/web/src/app/academic-governance/page.tsx:290 | /academic-governance | ul |  | {} |
| apps/web/src/app/academic-governance/page.tsx:301 | /academic-governance | button |  | {"disabled":"{saving}","type":"\"button\"","onClick":"{() => void toggleScheme(item)}"} |
| apps/web/src/app/academic-governance/page.tsx:325 | /academic-governance | ul |  | {} |
| apps/web/src/app/academic-periods/page.tsx:157 | /academic-periods | StandardDateField |  | {"name":"\"year-start\"","label":"\"Start date\"","value":"{yearStart}","onChange":"{setYearStart}","required":"true"} |
| apps/web/src/app/academic-periods/page.tsx:164 | /academic-periods | StandardDateField |  | {"name":"\"year-end\"","label":"\"End date\"","value":"{yearEnd}","onChange":"{setYearEnd}","required":"true"} |
| apps/web/src/app/academic-periods/page.tsx:176 | /academic-periods | button | Create year | {"className":"\"enterprise-action-button periods-action\""} |
| apps/web/src/app/academic-periods/page.tsx:192 | /academic-periods | StandardSelectField |  | {"name":"\"academicYearId\"","value":"{termYearId}","onChange":"{setTermYearId}","placeholder":"\"Select academic year\"","options":"{years .filter((year) => !year.isClosed) .map((year) => ({ value: year.id, label: year.name }))}"} |
| apps/web/src/app/academic-periods/page.tsx:206 | /academic-periods | StandardDateField |  | {"name":"\"term-start\"","label":"\"Start date\"","value":"{termStart}","onChange":"{setTermStart}","required":"true"} |
| apps/web/src/app/academic-periods/page.tsx:213 | /academic-periods | StandardDateField |  | {"name":"\"term-end\"","label":"\"End date\"","value":"{termEnd}","onChange":"{setTermEnd}","required":"true"} |
| apps/web/src/app/academic-periods/page.tsx:221 | /academic-periods | button | Create term | {"className":"\"enterprise-action-button periods-action\""} |
| apps/web/src/app/academic-periods/page.tsx:239 | /academic-periods | table |  | {} |
| apps/web/src/app/academic-periods/page.tsx:270 | /academic-periods | button | Close year | {"type":"\"button\"","onClick":"{() => void close(\"years\", year.id)}"} |
| apps/web/src/app/academic-periods/page.tsx:296 | /academic-periods | ul |  | {} |
| apps/web/src/app/academic-periods/page.tsx:313 | /academic-periods | button | Close term | {"type":"\"button\"","onClick":"{() => void close(\"terms\", term.id)}"} |
| apps/web/src/app/access-review/page.tsx:156 | /access-review | StandardSelectField |  | {"name":"\"userId\"","value":"{userId}","onChange":"{setUserId}","placeholder":"\"Select staff member\"","options":"{staff .filter((person) => person.isActive) .map((person) => ({ value: person.id, label: '${person.displayName} · ${person.roles.join(\", \") \|\| \"No role\"}', }))}"} |
| apps/web/src/app/access-review/page.tsx:188 | /access-review | StandardDateField |  | {"name":"\"expiresAtUtc\"","label":"\"Expires on\"","value":"{expiryDate}","onChange":"{setExpiryDate}","required":"true"} |
| apps/web/src/app/access-review/page.tsx:200 | /access-review | button | Grant access | {"className":"\"enterprise-action-button access-grant-button\"","disabled":"{!academy}"} |
| apps/web/src/app/access-review/page.tsx:219 | /access-review | ul |  | {} |
| apps/web/src/app/access-review/page.tsx:235 | /access-review | button | Revoke access | {"type":"\"button\"","onClick":"{() => void revoke(grant.id)}"} |
| apps/web/src/app/access-review/sign-off/page.tsx:2 | /access-review/sign-off | button | Record sign-off | {"className":"\"mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950\""} |
| apps/web/src/app/activity/page.tsx:96 | /activity | StandardSelectField |  | {"name":"\"activity-record-type\"","value":"{entity}","onChange":"{setEntity}","placeholder":"\"All record types\"","options":"{entityTypes.map((value) => ({ value, label: label(value) }))}"} |
| apps/web/src/app/activity/page.tsx:103 | /activity | StandardSelectField |  | {"name":"\"activity-period\"","value":"{period}","onChange":"{setPeriod}","placeholder":"\"All time\"","options":"{[\"Today\", \"7 days\", \"30 days\"].map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/activity/page.tsx:110 | /activity | StandardDateField |  | {"name":"\"activity-date\"","value":"{activityDate}","onChange":"{setActivityDate}","label":"\"Activity date\""} |
| apps/web/src/app/activity/page.tsx:111 | /activity | button | Clear filters | {"onClick":"{() => { setQuery(\"\"); setEntity(\"\"); setPeriod(\"\"); setActivityDate(\"\"); }}","className":"\"workspace-activity-clear\""} |
| apps/web/src/app/activity/page.tsx:126 | /activity | ul |  | {"className":"\"workspace-activity-list\""} |
| apps/web/src/app/activity/page.tsx:143 | /activity | details |  | {} |
| apps/web/src/app/activity/page.tsx:144 | /activity | summary | View details | {} |
| apps/web/src/app/admin/control/page.tsx:14 | /admin/control | article |  | {} |
| apps/web/src/app/admin/control/page.tsx:14 | /admin/control | article |  | {} |
| apps/web/src/app/admin/control/page.tsx:14 | /admin/control | article |  | {} |
| apps/web/src/app/admin/control/page.tsx:14 | /admin/control | StandardSelectField |  | {"name":"\"countryCode\"","value":"{countryCode}","onChange":"{setCountryCode}","placeholder":"\"Choose country\"","options":"{[{ value: \"IN\", label: \"India (IN)\" }, { value: \"AE\", label: \"United Arab Emirates (AE)\" }, { value: \"GB\", label: \"United Kingdom (GB)\" }, { value: \"US\", label: \"United States (US)\" }]}"} |
| apps/web/src/app/admin/control/page.tsx:14 | /admin/control | StandardSelectField |  | {"name":"\"timeZone\"","value":"{timeZone}","onChange":"{setTimeZone}","placeholder":"\"Choose time zone\"","options":"{[{ value: \"Asia/Kolkata\", label: \"India Standard Time (IST)\" }, { value: \"Asia/Dubai\", label: \"Gulf Standard Time (GST)\" }, { value: \"Europe/London\", label: \"United Kingdom time\" }, { value: \"America/New_York\", label: \"Eastern time (US)\" }]}"} |
| apps/web/src/app/admin/control/page.tsx:14 | /admin/control | button |  | {"disabled":"{saving}","className":"\"enterprise-action-button admin-controls-wide\""} |
| apps/web/src/app/admin/control/page.tsx:14 | /admin/control | ul |  | {"className":"\"admin-guardrails\""} |
| apps/web/src/app/admin/control/page.tsx:14 | /admin/control | Link | Open academy activity log | {"href":"\"/activity\"","className":"\"admin-controls-link\""} |
| apps/web/src/app/admin-intelligence/page.tsx:15 | /admin-intelligence | article |  | {} |
| apps/web/src/app/admin-intelligence/page.tsx:15 | /admin-intelligence | article |  | {} |
| apps/web/src/app/admin-intelligence/page.tsx:15 | /admin-intelligence | article |  | {} |
| apps/web/src/app/admin-intelligence/page.tsx:15 | /admin-intelligence | article |  | {} |
| apps/web/src/app/admin-intelligence/page.tsx:15 | /admin-intelligence | ul |  | {} |
| apps/web/src/app/admin-intelligence/page.tsx:15 | /admin-intelligence | Link | View student | {"href":"{'/student-profile?id=${item.studentId}'}"} |
| apps/web/src/app/admin-intelligence/page.tsx:15 | /admin-intelligence | ul |  | {} |
| apps/web/src/app/admin-intelligence/page.tsx:15 | /admin-intelligence | ul |  | {} |
| apps/web/src/app/admin-intelligence/page.tsx:15 | /admin-intelligence | ul |  | {} |
| apps/web/src/app/assessment-governance/page.tsx:66 | /assessment-governance | ul |  | {} |
| apps/web/src/app/assessments/page.tsx:235 | /assessments | StandardSelectField |  | {"name":"\"batch\"","value":"{batchId}","onChange":"{setBatchId}","placeholder":"\"Select batch\"","options":"{batches.map((batch) => ({ value: batch.id, label: batch.name, }))}"} |
| apps/web/src/app/assessments/page.tsx:255 | /assessments | StandardSelectField |  | {"name":"\"assessment-type\"","value":"{type}","onChange":"{setType}","placeholder":"\"Assessment type\"","options":"{assessmentTypes.map((value) => ({ value, label: value, }))}"} |
| apps/web/src/app/assessments/page.tsx:277 | /assessments | StandardSelectField |  | {"name":"\"grading-scheme\"","value":"{gradingSchemeId}","onChange":"{setGradingSchemeId}","placeholder":"\"No scheme — manual grade\"","options":"{gradingSchemes.map((scheme) => ({ value: scheme.id, label: '${scheme.name} · pass ${scheme.passingPercent}%', }))}"} |
| apps/web/src/app/assessments/page.tsx:288 | /assessments | StandardDateField |  | {"name":"\"scheduled-date\"","label":"\"Scheduled date\"","value":"{scheduledDate}","onChange":"{setScheduledDate}"} |
| apps/web/src/app/assessments/page.tsx:294 | /assessments | StandardTimeField |  | {"name":"\"scheduled-time\"","label":"\"Start time\"","value":"{scheduledTime}","onChange":"{setScheduledTime}"} |
| apps/web/src/app/assessments/page.tsx:301 | /assessments | button | Create assessment | {"disabled":"{!academy \|\| !batchId}","className":"\"enterprise-action-button assessments-action\""} |
| apps/web/src/app/assessments/page.tsx:317 | /assessments | StandardSelectField |  | {"name":"\"assessment\"","value":"{assessmentId}","onChange":"{setAssessmentId}","placeholder":"\"Select assessment\"","options":"{assessments.map((assessment) => ({ value: assessment.id, label: '${assessment.title} · ${batchName(assessment.batchId)} · /${assessment.maxScore}', }))}"} |
| apps/web/src/app/assessments/page.tsx:334 | /assessments | ul |  | {} |
| apps/web/src/app/assessments/page.tsx:405 | /assessments | button |  | {"disabled":"{saving \|\| !score}","type":"\"button\"","onClick":"{() => void onSave(student.id, Number(score), grade, remarks)}"} |
| apps/web/src/app/assignments/page.tsx:28 | /assignments | button | Create assignment | {"disabled":"{!academy \|\| !batchId}","className":"\"mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60\""} |
| apps/web/src/app/assignments/page.tsx:28 | /assignments | ul |  | {"className":"\"mt-5 space-y-3\""} |
| apps/web/src/app/attendance/page.tsx:75 | /attendance | StandardSelectField |  | {"name":"\"session\"","value":"{sessionId}","onChange":"{setSessionId}","placeholder":"\"Select a class\"","options":"{sessions.map((session) => ({ value: session.id, label: sessionLabel(session, batches) }))}"} |
| apps/web/src/app/attendance/page.tsx:76 | /attendance | ul |  | {"className":"\"attendance-roster\""} |
| apps/web/src/app/attendance/page.tsx:76 | /attendance | StandardSelectField |  | {"name":"{'attendance-${student.id}'}","value":"{record?.status ?? \"\"}","onChange":"{(status) => { if (savingId !== student.id) void mark(student.id, status); }}","placeholder":"\"Select status\"","options":"{statuses.map((status) => ({ value: status, label: status }))}"} |
| apps/web/src/app/attendance/page.tsx:76 | /attendance | button |  | {"disabled":"{savingId === student.id}","onClick":"{() => { const status = record?.status; if (status) void mark(student.id, status); }}","className":"\"attendance-save\""} |
| apps/web/src/app/batch-promotions/page.tsx:210 | /batch-promotions | StandardSelectField |  | {"name":"\"student\"","value":"{studentId}","onChange":"{(id) => { setStudentId(id); setSourceBatchId(\"\"); }}","placeholder":"\"Select student\"","options":"{students.map((student) => ({ value: student.id, label: '${student.firstName} ${student.lastName}', }))}"} |
| apps/web/src/app/batch-promotions/page.tsx:223 | /batch-promotions | StandardSelectField |  | {"name":"\"source\"","value":"{sourceBatchId}","onChange":"{setSourceBatchId}","placeholder":"\"Select active source batch\"","options":"{activeSourceBatches.map((id) => ({ value: id, label: batch(id), }))}"} |
| apps/web/src/app/batch-promotions/page.tsx:233 | /batch-promotions | StandardSelectField |  | {"name":"\"target\"","value":"{targetBatchId}","onChange":"{setTargetBatchId}","placeholder":"\"Select target batch\"","options":"{batches .filter((item) => item.id !== sourceBatchId) .map((item) => ({ value: item.id, label: '${item.name} · ${item.activeEnrolments ?? 0}/${item.capacity}', }))}"} |
| apps/web/src/app/batch-promotions/page.tsx:245 | /batch-promotions | StandardDateField |  | {"name":"\"effective\"","label":"\"Effective date\"","value":"{effectiveDate}","onChange":"{setEffectiveDate}","required":"true"} |
| apps/web/src/app/batch-promotions/page.tsx:259 | /batch-promotions | button |  | {"disabled":"{saving \|\| !academy}","className":"\"enterprise-action-button governance-action\""} |
| apps/web/src/app/batch-promotions/page.tsx:278 | /batch-promotions | ul |  | {} |
| apps/web/src/app/batch-promotions/page.tsx:294 | /batch-promotions | button | Approve | {"disabled":"{saving}","type":"\"button\"","onClick":"{() => void decide(item, \"Approved\")}"} |
| apps/web/src/app/batch-promotions/page.tsx:301 | /batch-promotions | button | Reject | {"disabled":"{saving}","type":"\"button\"","onClick":"{() => void decide(item, \"Rejected\")}"} |
| apps/web/src/app/batches/page.tsx:45 | /batches | StandardInteractiveTile |  | {"className":"\"batch-overview-kpi\"","label":"{label}","value":"{value}","detail":"{note ?? \"View details\"}","onClick":"{onClick}"} |
| apps/web/src/app/batches/page.tsx:260 | /batches | Link | Create Class / Batch | {"href":"\"/batch-setup\"","className":"\"enterprise-action-button\""} |
| apps/web/src/app/batches/page.tsx:273 | /batches | Metric |  | {"label":"\"Active classes\"","value":"{active.length}","note":"{'${oneToOne.length} 1:1 · ${groupClasses.length} group'}","onClick":"{() => setDetail(\"classes\")}"} |
| apps/web/src/app/batches/page.tsx:274 | /batches | Metric |  | {"label":"\"Teacher assignment\"","value":"{assigned.length}","note":"{'${unassigned.length} unassigned'}","onClick":"{() => setDetail(\"teachers\")}"} |
| apps/web/src/app/batches/page.tsx:280 | /batches | Metric |  | {"label":"\"Capacity\"","value":"{capacityGaps.length}","note":"\"Classes with places open\"","onClick":"{() => setDetail(\"capacity\")}"} |
| apps/web/src/app/batches/page.tsx:287 | /batches | StandardDetailModal |  | {"eyebrow":"\"Class & batch\"","title":"\"Active classes\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/batches/page.tsx:288 | /batches | StandardDetailModal |  | {"eyebrow":"\"Class & batch\"","title":"\"Teaching assignment\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/batches/page.tsx:289 | /batches | StandardDetailModal |  | {"eyebrow":"\"Class & batch\"","title":"\"Classes with places open\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/batches/page.tsx:293 | /batches | ul |  | {"className":"\"batch-overview-list\""} |
| apps/web/src/app/batches/page.tsx:307 | /batches | Link | Manage class | {"href":"\"/batch-setup\"","className":"\"batch-overview-row-action\""} |
| apps/web/src/app/batches/page.tsx:340 | /batches | StandardSelectField |  | {"name":"\"academy\"","value":"{academyId}","onChange":"{setAcademyId}","placeholder":"\"Select academy\"","options":"{academies.map((academy) => ({ value: academy.id, label: academy.name }))}"} |
| apps/web/src/app/batches/page.tsx:352 | /batches | StandardSelectField |  | {"name":"\"course\"","value":"{courseId}","onChange":"{setCourseId}","placeholder":"\"Select course\"","options":"{courses.map((course) => ({ value: course.id, label: '${course.name} · ${course.academyType}' }))}"} |
| apps/web/src/app/batches/page.tsx:372 | /batches | StandardSelectField |  | {"name":"\"branch\"","value":"{branchId}","onChange":"{setBranchId}","placeholder":"\"No branch assigned\"","options":"{branches.map((branch) => ({ value: branch.id, label: branch.name }))}"} |
| apps/web/src/app/batches/page.tsx:390 | /batches | StandardSelectField |  | {"name":"\"class-type\"","value":"{classType}","onChange":"{setClassType}","placeholder":"\"Class type\"","options":"{[{ value: \"Group\", label: \"Group class\" }, { value: \"OneToOne\", label: \"1:1 class\" }]}"} |
| apps/web/src/app/batches/page.tsx:397 | /batches | StandardSelectField |  | {"name":"\"delivery-mode\"","value":"{deliveryMode}","onChange":"{setDeliveryMode}","placeholder":"\"Delivery mode\"","options":"{[{ value: \"InPerson\", label: \"Offline\" }, { value: \"Online\", label: \"Online\" }, { value: \"Hybrid\", label: \"Hybrid\" }]}"} |
| apps/web/src/app/batches/page.tsx:494 | /batches | StandardDateField |  | {"name":"\"batch-start-date\"","label":"\"Batch start date\"","value":"{startDate}","onChange":"{setStartDate}"} |
| apps/web/src/app/batches/page.tsx:495 | /batches | button | Create batch | {"className":"\"mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300\""} |
| apps/web/src/app/batches/page.tsx:505 | /batches | ul |  | {"className":"\"mt-4 space-y-3\""} |
| apps/web/src/app/batches/page.tsx:518 | /batches | StandardSelectField |  | {"name":"\"edit-course\"","value":"{editCourseId}","onChange":"{setEditCourseId}","placeholder":"\"Select course\"","options":"{courses.map((course) => ({ value: course.id, label: course.name }))}"} |
| apps/web/src/app/batches/page.tsx:525 | /batches | StandardSelectField |  | {"name":"\"edit-teacher\"","value":"{editTeacherId}","onChange":"{setEditTeacherId}","placeholder":"\"No teacher\"","options":"{teachers.map((teacher) => ({ value: teacher.id, label: '${teacher.firstName} ${teacher.lastName}' }))}"} |
| apps/web/src/app/batches/page.tsx:532 | /batches | StandardSelectField |  | {"name":"\"edit-branch\"","value":"{editBranchId}","onChange":"{setEditBranchId}","placeholder":"\"No branch\"","options":"{branches.map((branch) => ({ value: branch.id, label: branch.name }))}"} |
| apps/web/src/app/batches/page.tsx:547 | /batches | StandardDateField |  | {"name":"\"edit-batch-start-date\"","label":"\"Batch start date\"","value":"{editStartDate}","onChange":"{setEditStartDate}"} |
| apps/web/src/app/batches/page.tsx:550 | /batches | button | Save | {"onClick":"{() => void saveBatch(batch)}","disabled":"{savingId === batch.id}","className":"\"rounded-lg bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950\""} |
| apps/web/src/app/batches/page.tsx:557 | /batches | button | Cancel | {"onClick":"{() => setEditingId(null)}","className":"\"rounded-lg border border-slate-700 px-3 py-2 text-sm\""} |
| apps/web/src/app/batches/page.tsx:594 | /batches | button | Edit | {"onClick":"{() => beginEdit(batch)}","className":"\"rounded-lg border border-slate-700 px-3 py-1.5 text-sm\""} |
| apps/web/src/app/batches/page.tsx:600 | /batches | button |  | {"onClick":"{() => void toggleActive(batch)}","disabled":"{savingId === batch.id}","className":"\"rounded-lg border border-slate-700 px-3 py-1.5 text-sm\""} |
| apps/web/src/app/branches/page.tsx:18 | /branches | article |  | {} |
| apps/web/src/app/branches/page.tsx:18 | /branches | article |  | {} |
| apps/web/src/app/branches/page.tsx:18 | /branches | article |  | {} |
| apps/web/src/app/branches/page.tsx:18 | /branches | StandardSelectField |  | {"name":"\"academy\"","value":"{academyId}","onChange":"{setAcademyId}","placeholder":"\"Select academy\"","options":"{academies.map(academy => ({ value: academy.id, label: academy.name }))}"} |
| apps/web/src/app/branches/page.tsx:18 | /branches | button | Create branch | {"className":"\"enterprise-action-button branches-wide\""} |
| apps/web/src/app/branches/page.tsx:18 | /branches | ul |  | {"className":"\"branches-list\""} |
| apps/web/src/app/branches/page.tsx:18 | /branches | button | Save | {"onClick":"{() => void saveBranch(branch)}","disabled":"{savingId === branch.id}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/branches/page.tsx:18 | /branches | button | Cancel | {"onClick":"{() => setEditingId(null)}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/branches/page.tsx:18 | /branches | button | Edit | {"onClick":"{() => beginEdit(branch)}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/branches/page.tsx:18 | /branches | button |  | {"onClick":"{() => void toggleActive(branch)}","disabled":"{savingId === branch.id}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/calendar/page.tsx:85 | /calendar | a |  | {"href":"{item.href}","className":"\"workspace-calendar-agenda-action\"","target":"{item.opensExternally ? \"_blank\" : undefined}","rel":"{item.opensExternally ? \"noreferrer\" : undefined}"} |
| apps/web/src/app/calendar/page.tsx:108 | /calendar | button |  | {"type":"\"button\"","onClick":"{() => setStudentsExpanded((value) => !value)}"} |
| apps/web/src/app/calendar/page.tsx:114 | /calendar | ul |  | {"className":"\"workspace-calendar-student-list\""} |
| apps/web/src/app/calendar/page.tsx:261 | /calendar | button | ‹ | {"onClick":"{() => setMonth(new Date(month.getFullYear(), month.getMonth() - 1, 1)) }","className":"\"calendar-nav\"","aria-label":"\"Previous month\""} |
| apps/web/src/app/calendar/page.tsx:270 | /calendar | button | Today | {"onClick":"{() => setMonth(new Date(today.getFullYear(), today.getMonth(), 1)) }","className":"\"calendar-today\""} |
| apps/web/src/app/calendar/page.tsx:278 | /calendar | button | › | {"onClick":"{() => setMonth(new Date(month.getFullYear(), month.getMonth() + 1, 1)) }","className":"\"calendar-nav\"","aria-label":"\"Next month\""} |
| apps/web/src/app/calendar/page.tsx:294 | /calendar | button |  | {"key":"{value}","onClick":"{() => setFilter(value)}","className":"{filter === value ? \"calendar-filter active\" : \"calendar-filter\"}","aria-pressed":"{filter === value}"} |
| apps/web/src/app/calendar/page.tsx:324 | /calendar | a |  | {"key":"{'${item.type}-${item.id}'}","href":"{item.href}","className":"{'calendar-event ${item.type.toLowerCase().replace(\"-\", \"\")}'}","title":"{'${item.title} · ${item.detail}'}","target":"{item.opensExternally ? \"_blank\" : undefined}","rel":"{item.opensExternally ? \"noreferrer\" : undefined}"} |
| apps/web/src/app/calendar/page.tsx:359 | /calendar | button |  | {"type":"\"button\"","className":"\"workspace-calendar-agenda-toggle\"","aria-expanded":"{agendaExpanded}","aria-controls":"\"calendar-month-agenda-list\"","onClick":"{() => setAgendaExpanded((expanded) => !expanded)}"} |
| apps/web/src/app/calendar/page.tsx:372 | /calendar | ul |  | {"id":"\"calendar-month-agenda-list\"","className":"\"workspace-calendar-agenda-list\""} |
| apps/web/src/app/certificates/page.tsx:238 | /certificates | button | Change logo | {"type":"\"button\"","className":"\"enterprise-action-button-secondary\"","onClick":"{() => fileInput.current?.click()}","disabled":"{savingBrand}"} |
| apps/web/src/app/certificates/page.tsx:282 | /certificates | button |  | {"className":"\"enterprise-action-button\"","disabled":"{savingBrand}"} |
| apps/web/src/app/certificates/page.tsx:298 | /certificates | StandardSelectField |  | {"name":"\"studentId\"","value":"{studentId}","onChange":"{setStudentId}","placeholder":"\"Select student\"","options":"{students.map((student) => ({ value: student.id, label: '${student.firstName} ${student.lastName}', }))}"} |
| apps/web/src/app/certificates/page.tsx:311 | /certificates | StandardSelectField |  | {"name":"\"batchId\"","value":"{batchId}","onChange":"{setBatchId}","placeholder":"\"No class or batch\"","options":"{batches.map((batch) => ({ value: batch.id, label: batch.name, }))}"} |
| apps/web/src/app/certificates/page.tsx:330 | /certificates | StandardDateField |  | {"name":"\"issuedDate\"","value":"{issuedDate}","onChange":"{setIssuedDate}","label":"\"Issue date\""} |
| apps/web/src/app/certificates/page.tsx:338 | /certificates | StandardSelectField |  | {"name":"\"themeKey\"","value":"{themeKey}","onChange":"{setThemeKey}","placeholder":"\"Select theme\"","options":"{themes.map(([key, name, category]) => ({ value: key, label: '${category} · ${name}', }))}"} |
| apps/web/src/app/certificates/page.tsx:356 | /certificates | button | Issue certificate | {"className":"\"enterprise-action-button\""} |
| apps/web/src/app/certificates/page.tsx:366 | /certificates | button | Print / save PDF | {"type":"\"button\"","className":"\"enterprise-action-button-secondary\"","onClick":"{() => window.print()}"} |
| apps/web/src/app/certificates/page.tsx:374 | /certificates | article |  | {"className":"{'certificate-preview theme-${themeKey}'}","style":"{ { \"--academy-certificate-accent\": branding?.accentColor ?? \"#0F6CBD\", } as CSSProperties }"} |
| apps/web/src/app/certificates/page.tsx:433 | /certificates | table |  | {} |
| apps/web/src/app/communication-preferences/page.tsx:123 | /communication-preferences | StandardSelectField |  | {"name":"\"recipientType\"","value":"{recipientType}","onChange":"{(value) => { setRecipientType(value); setRecipientId(\"\"); }}","placeholder":"\"Select contact type\"","options":"{[ { value: \"Guardian\", label: \"Parent\" }, { value: \"Student\", label: \"Student\" }, ]}"} |
| apps/web/src/app/communication-preferences/page.tsx:139 | /communication-preferences | StandardSelectField |  | {"name":"\"recipientId\"","value":"{recipientId}","onChange":"{setRecipientId}","placeholder":"{'Select ${recipientType.toLowerCase()}'}","options":"{people.map((person) => ({ value: person.id, label: '${person.firstName} ${person.lastName}${person.email ? ' · ${person.email}' : \"\"}', }))}"} |
| apps/web/src/app/communication-preferences/page.tsx:202 | /communication-preferences | button | Save preferences | {"disabled":"{!academy \|\| !recipientId}","className":"\"enterprise-action-button preferences-wide\""} |
| apps/web/src/app/communication-settings/page.tsx:21 | /communication-settings | button | Save settings | {"disabled":"{!academy}","className":"\"mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 disabled:opacity-60\""} |
| apps/web/src/app/communications/page.tsx:188 | /communications | StandardSelectField |  | {"name":"\"recipientType\"","value":"{recipientType}","onChange":"{(value) => { setRecipientType(value); setRecipientId(\"\"); }}","placeholder":"\"Select message type\"","options":"{[ { value: \"Guardian\", label: \"Parent\" }, { value: \"Student\", label: \"Student\" }, { value: \"Teacher\", label: \"Teacher\" }, { value: \"Academy\", label: \"Portal banner message\" }, ]}"} |
| apps/web/src/app/communications/page.tsx:208 | /communications | StandardSelectField |  | {"name":"\"announcementAudience\"","value":"{announcementAudience}","onChange":"{setAnnouncementAudience}","placeholder":"\"Select audience\"","options":"{[ { value: \"Student\", label: \"Students\" }, { value: \"Teacher\", label: \"Teachers\" }, { value: \"Student,Teacher\", label: \"Students and teachers\", }, ]}"} |
| apps/web/src/app/communications/page.tsx:234 | /communications | StandardDateField |  | {"name":"\"announcementStartDate\"","value":"{announcementStartDate}","onChange":"{setAnnouncementStartDate}","label":"\"Banner start date\""} |
| apps/web/src/app/communications/page.tsx:240 | /communications | StandardTimeField |  | {"name":"\"announcementStartTime\"","value":"{announcementStartTime}","onChange":"{setAnnouncementStartTime}","label":"\"Banner start time\""} |
| apps/web/src/app/communications/page.tsx:251 | /communications | StandardSelectField |  | {"name":"\"recipientId\"","value":"{recipientId}","onChange":"{setRecipientId}","placeholder":"{'Select ${recipientType.toLowerCase()}'}","options":"{recipientOptions}"} |
| apps/web/src/app/communications/page.tsx:261 | /communications | StandardSelectField |  | {"name":"\"templateId\"","value":"{templateId}","onChange":"{chooseTemplate}","placeholder":"\"Manual message\"","options":"{templates .filter((item) => item.isActive) .map((item) => ({ value: item.id, label: '${item.channel} · ${item.name}', }))}"} |
| apps/web/src/app/communications/page.tsx:311 | /communications | StandardSelectField |  | {"name":"\"channel\"","value":"{channel}","onChange":"{setChannel}","placeholder":"\"Select channel\"","options":"{[ { value: \"InApp\", label: \"In-app notification\" }, { value: \"Email\", label: \"Email\" }, { value: \"WhatsApp\", label: \"WhatsApp\" }, ]}"} |
| apps/web/src/app/communications/page.tsx:323 | /communications | StandardDateField |  | {"name":"\"scheduledDate\"","value":"{scheduledDate}","onChange":"{setScheduledDate}","label":"\"Schedule date\""} |
| apps/web/src/app/communications/page.tsx:329 | /communications | StandardTimeField |  | {"name":"\"scheduledTime\"","value":"{scheduledTime}","onChange":"{setScheduledTime}","label":"\"Schedule time\""} |
| apps/web/src/app/communications/page.tsx:337 | /communications | button |  | {"disabled":"{!academy \|\| (!isAnnouncement && !recipientId)}","className":"\"enterprise-action-button messages-wide\""} |
| apps/web/src/app/communications/page.tsx:356 | /communications | ul |  | {} |
| apps/web/src/app/compliance/page.tsx:73 | /compliance | button | Register for review | {"className":"\"mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950\""} |
| apps/web/src/app/compliance/page.tsx:74 | /compliance | button | Record consent | {"className":"\"mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950\""} |
| apps/web/src/app/compliance/page.tsx:76 | /compliance | table |  | {"className":"\"min-w-full text-left text-sm\""} |
| apps/web/src/app/compliance/page.tsx:76 | /compliance | button | Approve | {"onClick":"{() => review(document, \"Approved\")}","className":"\"mr-2 text-cyan-300\""} |
| apps/web/src/app/compliance/page.tsx:76 | /compliance | button | Reject | {"onClick":"{() => review(document, \"Rejected\")}","className":"\"text-rose-300\""} |
| apps/web/src/app/compliance/page.tsx:77 | /compliance | table |  | {"className":"\"min-w-full text-left text-sm\""} |
| apps/web/src/app/compliance/page.tsx:77 | /compliance | button | Granted · withdraw | {"onClick":"{() => withdraw(consent)}","className":"\"text-amber-300\""} |
| apps/web/src/app/compliance/page.tsx:81 | /compliance | StandardSelectField |  | {"name":"\"personType\"","value":"{personType}","onChange":"{value => { setPersonType(value); setPersonId(\"\"); }}","placeholder":"\"Choose record type\"","options":"{[{ value: \"student\", label: \"Student\" }, { value: \"guardian\", label: \"Parent\" }]}"} |
| apps/web/src/app/compliance/page.tsx:81 | /compliance | StandardSelectField |  | {"name":"\"personId\"","value":"{personId}","onChange":"{setPersonId}","placeholder":"\"Select person\"","options":"{people.map(person => ({ value: person.id, label: label(person) }))}"} |
| apps/web/src/app/compliance/page.tsx:82 | /compliance | StandardDateField |  | {"name":"\"expiryDate\"","value":"{expiryDate}","onChange":"{setExpiryDate}","label":"\"Expiry date\""} |
| apps/web/src/app/compliance/page.tsx:82 | /compliance | StandardSelectField |  | {"name":"\"visibility\"","value":"{visibility}","onChange":"{setVisibility}","placeholder":"\"Choose visibility\"","options":"{[{ value: \"AdminOnly\", label: \"Administrators only\" }, { value: \"StaffRestricted\", label: \"Restricted staff\" }]}"} |
| apps/web/src/app/courses/page.tsx:150 | /courses | StandardSelectField |  | {"name":"\"academy\"","value":"{academyId}","onChange":"{setAcademyId}","placeholder":"\"Select academy\"","options":"{academies.map((academy) => ({ value: academy.id, label: academy.name, }))}"} |
| apps/web/src/app/courses/page.tsx:179 | /courses | StandardSelectField |  | {"name":"\"course-type\"","value":"{type}","onChange":"{setType}","placeholder":"\"Course type\"","options":"{typeOptions}"} |
| apps/web/src/app/courses/page.tsx:194 | /courses | button | Create course | {"className":"\"enterprise-action-button courses-create-button\""} |
| apps/web/src/app/courses/page.tsx:212 | /courses | ul |  | {} |
| apps/web/src/app/courses/page.tsx:226 | /courses | StandardSelectField |  | {"name":"{'course-type-${course.id}'}","value":"{editType}","onChange":"{setEditType}","placeholder":"\"Course type\"","options":"{typeOptions}"} |
| apps/web/src/app/courses/page.tsx:245 | /courses | button | Save | {"type":"\"button\"","onClick":"{() => void saveCourse(course)}","disabled":"{savingId === course.id}"} |
| apps/web/src/app/courses/page.tsx:252 | /courses | button | Cancel | {"type":"\"button\"","onClick":"{() => setEditingId(null)}"} |
| apps/web/src/app/courses/page.tsx:273 | /courses | button | Edit | {"type":"\"button\"","onClick":"{() => beginEdit(course)}"} |
| apps/web/src/app/courses/page.tsx:279 | /courses | button |  | {"type":"\"button\"","onClick":"{() => void toggleActive(course)}","disabled":"{savingId === course.id}"} |
| apps/web/src/app/curriculum/page.tsx:127 | /curriculum | StandardSelectField |  | {"name":"\"course\"","value":"{courseId}","onChange":"{setCourseId}","placeholder":"\"Select course\"","options":"{courses.map((course) => ({ value: course.id, label: course.name, }))}"} |
| apps/web/src/app/curriculum/page.tsx:155 | /curriculum | button | Save draft | {"className":"\"enterprise-action-button curriculum-action\""} |
| apps/web/src/app/curriculum/page.tsx:189 | /curriculum | table |  | {} |
| apps/web/src/app/curriculum/page.tsx:213 | /curriculum | button |  | {"type":"\"button\"","onClick":"{() => void publish(module)}"} |
| apps/web/src/app/dashboard/page.tsx:184 | /dashboard | Link | Add student | {"href":"\"/student-onboarding\"","className":"\"enterprise-primary-action\""} |
| apps/web/src/app/dashboard/page.tsx:197 | /dashboard | StandardInteractiveTile |  | {"label":"\"Active students\"","value":"{data.students}","detail":"\"View active student names\"","onClick":"{() => setDetail(\"students\")}","className":"\"enterprise-kpi\""} |
| apps/web/src/app/dashboard/page.tsx:198 | /dashboard | StandardInteractiveTile |  | {"label":"\"Classes today\"","value":"{data.classesToday}","detail":"\"View scheduled classes\"","onClick":"{() => setDetail(\"classes\")}","className":"\"enterprise-kpi\""} |
| apps/web/src/app/dashboard/page.tsx:199 | /dashboard | StandardInteractiveTile |  | {"label":"\"Attendance rate\"","value":"{'${attendanceRate}%'}","detail":"\"View attendance by student\"","onClick":"{() => setDetail(\"attendance\")}","className":"\"enterprise-kpi\""} |
| apps/web/src/app/dashboard/page.tsx:200 | /dashboard | StandardInteractiveTile |  | {"label":"\"Outstanding fees\"","value":"{formatRupees(data.outstandingBalance + payrollDue)}","detail":"\"View student fees and teacher salary due\"","onClick":"{() => setDetail(\"outstanding\")}","className":"\"enterprise-kpi\""} |
| apps/web/src/app/dashboard/page.tsx:206 | /dashboard | Link | Calendar | {"href":"\"/calendar\""} |
| apps/web/src/app/dashboard/page.tsx:213 | /dashboard | table |  | {"className":"\"enterprise-schedule-table\""} |
| apps/web/src/app/dashboard/page.tsx:252 | /dashboard | Link | Activity log | {"href":"\"/activity\""} |
| apps/web/src/app/dashboard/page.tsx:255 | /dashboard | Link |  | {"href":"\"/leads\"","className":"\"enterprise-activity-item\""} |
| apps/web/src/app/dashboard/page.tsx:266 | /dashboard | Link |  | {"href":"\"/staff\"","className":"\"enterprise-activity-item\""} |
| apps/web/src/app/dashboard/page.tsx:275 | /dashboard | Link |  | {"href":"\"/communication-settings\"","className":"\"enterprise-activity-item\""} |
| apps/web/src/app/dashboard/page.tsx:286 | /dashboard | Link |  | {"href":"\"/activity\"","key":"{item.id}","className":"\"enterprise-activity-item\""} |
| apps/web/src/app/dashboard/page.tsx:303 | /dashboard | StandardDetailModal |  | {"eyebrow":"\"Workspace overview\"","title":"\"Active students\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/dashboard/page.tsx:304 | /dashboard | StandardDetailModal |  | {"eyebrow":"\"Workspace overview\"","title":"\"Classes today\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/dashboard/page.tsx:305 | /dashboard | StandardDetailModal |  | {"eyebrow":"\"Workspace overview\"","title":"\"Attendance by student\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/dashboard/page.tsx:306 | /dashboard | StandardDetailModal |  | {"eyebrow":"\"Workspace overview\"","title":"\"Outstanding fees and salary\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/dashboard/page.tsx:310 | /dashboard | article |  | {"key":"{'${title}-${index}'}"} |
| apps/web/src/app/data-operations/page.tsx:17 | /data-operations | article |  | {} |
| apps/web/src/app/data-operations/page.tsx:17 | /data-operations | article |  | {} |
| apps/web/src/app/data-operations/page.tsx:17 | /data-operations | article |  | {} |
| apps/web/src/app/data-operations/page.tsx:17 | /data-operations | article |  | {"key":"{resource}"} |
| apps/web/src/app/data-operations/page.tsx:17 | /data-operations | button | Download CSV | {"onClick":"{() => void download(resource)}","disabled":"{!academy}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/data-operations/page.tsx:17 | /data-operations | button |  | {"onClick":"{() => void validate()}","disabled":"{!rows.length \|\| validating}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/data-operations/page.tsx:17 | /data-operations | button |  | {"onClick":"{() => void importRows()}","disabled":"{!rows.length \|\| importing}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/enrollments/page.tsx:62 | /enrollments | StandardDateField |  | {"name":"\"startDate\"","label":"\"Start date\"","value":"{startDate}","onChange":"{setStartDate}"} |
| apps/web/src/app/enrollments/page.tsx:63 | /enrollments | button | Enrol student | {"disabled":"{!academy \|\| !students.length \|\| !batches.length}","className":"\"mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60\""} |
| apps/web/src/app/enrollments/page.tsx:65 | /enrollments | ul |  | {"className":"\"mt-5 space-y-3\""} |
| apps/web/src/app/events/page.tsx:128 | /events | StandardSelectField |  | {"name":"\"event-type\"","value":"{type}","onChange":"{setType}","placeholder":"\"Event type\"","options":"{eventTypes.map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/events/page.tsx:135 | /events | StandardSelectField |  | {"name":"\"branch\"","value":"{branchId}","onChange":"{setBranchId}","placeholder":"\"No branch\"","options":"{branches.map((branch) => ({ value: branch.id, label: branch.name, }))}"} |
| apps/web/src/app/events/page.tsx:146 | /events | StandardDateField |  | {"name":"\"start-date\"","label":"\"Start date\"","value":"{startDate}","onChange":"{setStartDate}","required":"true"} |
| apps/web/src/app/events/page.tsx:153 | /events | StandardTimeField |  | {"name":"\"start-time\"","label":"\"Start time\"","value":"{startTime}","onChange":"{setStartTime}"} |
| apps/web/src/app/events/page.tsx:161 | /events | StandardDateField |  | {"name":"\"end-date\"","label":"\"End date\"","value":"{endDate}","onChange":"{setEndDate}","required":"true"} |
| apps/web/src/app/events/page.tsx:168 | /events | StandardTimeField |  | {"name":"\"end-time\"","label":"\"End time\"","value":"{endTime}","onChange":"{setEndTime}"} |
| apps/web/src/app/events/page.tsx:183 | /events | button | Plan event | {"className":"\"enterprise-action-button events-action\""} |
| apps/web/src/app/events/page.tsx:199 | /events | ul |  | {} |
| apps/web/src/app/expenses/page.tsx:18 | /expenses | article |  | {} |
| apps/web/src/app/expenses/page.tsx:18 | /expenses | article |  | {} |
| apps/web/src/app/expenses/page.tsx:18 | /expenses | article |  | {} |
| apps/web/src/app/expenses/page.tsx:18 | /expenses | StandardSelectField |  | {"name":"\"expenseCategory\"","value":"{category}","onChange":"{setCategory}","placeholder":"\"Choose category\"","options":"{categories.map(value => ({ value, label: value === \"TeacherPayout\" ? \"Teacher payout\" : value }))}"} |
| apps/web/src/app/expenses/page.tsx:18 | /expenses | StandardSelectField |  | {"name":"\"branchId\"","value":"{branchId}","onChange":"{setBranchId}","placeholder":"\"All academy / no branch\"","options":"{branches.map(branch => ({ value: branch.id, label: branch.name }))}"} |
| apps/web/src/app/expenses/page.tsx:18 | /expenses | StandardDateField |  | {"name":"\"expenseDate\"","value":"{expenseDate}","onChange":"{setExpenseDate}","label":"\"Expense date\""} |
| apps/web/src/app/expenses/page.tsx:18 | /expenses | button | Record expense | {"disabled":"{!academy \|\| !description \|\| !amount}","className":"\"enterprise-action-button expenses-action expenses-wide\""} |
| apps/web/src/app/expenses/page.tsx:18 | /expenses | StandardSelectField |  | {"name":"\"expenseFilter\"","value":"{filter}","onChange":"{setFilter}","placeholder":"\"Filter category\"","options":"{[{ value: \"All\", label: 'All expenses (${expenses.length})' }, ...categories.map(value => ({ value, label: value === \"TeacherPayout\" ? \"Teacher payout\" : value }))]}"} |
| apps/web/src/app/expenses/page.tsx:18 | /expenses | ul |  | {"className":"\"expenses-list\""} |
| apps/web/src/app/fee-plans/page.tsx:103 | /fee-plans | article |  | {} |
| apps/web/src/app/fee-plans/page.tsx:103 | /fee-plans | article |  | {} |
| apps/web/src/app/fee-plans/page.tsx:103 | /fee-plans | article |  | {} |
| apps/web/src/app/fee-plans/page.tsx:103 | /fee-plans | article |  | {} |
| apps/web/src/app/fee-plans/page.tsx:106 | /fee-plans | StandardSelectField |  | {"name":"\"frequency\"","value":"{frequency}","onChange":"{setFrequency}","placeholder":"\"Choose billing cycle\"","options":"{frequencies}"} |
| apps/web/src/app/fee-plans/page.tsx:106 | /fee-plans | button |  | {"disabled":"{!academy \|\| savingId === \"new\"}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/fee-plans/page.tsx:107 | /fee-plans | ul |  | {} |
| apps/web/src/app/fee-plans/page.tsx:107 | /fee-plans | button | Edit | {"type":"\"button\"","onClick":"{() => setEditingId(plan.id)}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/fee-plans/page.tsx:107 | /fee-plans | button | Deactivate | {"type":"\"button\"","disabled":"{savingId === plan.id}","onClick":"{() => void savePlan(plan, plan, false)}","className":"\"fee-plan-deactivate\""} |
| apps/web/src/app/fee-plans/page.tsx:116 | /fee-plans | StandardSelectField |  | {"name":"\"frequency\"","value":"{frequency}","onChange":"{setFrequency}","placeholder":"\"Choose billing cycle\"","options":"{frequencies}"} |
| apps/web/src/app/fee-plans/page.tsx:116 | /fee-plans | button | Save | {"disabled":"{saving}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/fee-plans/page.tsx:116 | /fee-plans | button | Cancel | {"type":"\"button\"","onClick":"{onCancel}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/fee-reminders/page.tsx:5 | /fee-reminders | article |  | {} |
| apps/web/src/app/fee-reminders/page.tsx:5 | /fee-reminders | article |  | {} |
| apps/web/src/app/fee-reminders/page.tsx:5 | /fee-reminders | article |  | {} |
| apps/web/src/app/fee-reminders/page.tsx:5 | /fee-reminders | button |  | {"disabled":"{!due.length\|\|!!busy}","onClick":"{()=>void queue()}"} |
| apps/web/src/app/fee-reminders/page.tsx:5 | /fee-reminders | button |  | {"key":"{x}","onClick":"{()=>setFilter(x)}","data-active":"{filter===x}"} |
| apps/web/src/app/fee-reminders/page.tsx:5 | /fee-reminders | ul |  | {} |
| apps/web/src/app/fee-reminders/page.tsx:5 | /fee-reminders | button |  | {"disabled":"{!!busy}","onClick":"{()=>void queue(x.id)}"} |
| apps/web/src/app/fee-reminders/page.tsx:5 | /fee-reminders | ul |  | {} |
| apps/web/src/app/finance/page.tsx:170 | /finance | Link | Issue invoice | {"href":"\"/invoices\"","className":"\"enterprise-action-button\""} |
| apps/web/src/app/finance/page.tsx:173 | /finance | Link | Record payment | {"href":"\"/payments\"","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/finance/page.tsx:212 | /finance | Link | Finance controls | {"href":"\"/finance-governance\"","className":"\"text-sm font-medium text-cyan-300\""} |
| apps/web/src/app/finance/page.tsx:225 | /finance | table |  | {"className":"\"min-w-full text-left text-sm\""} |
| apps/web/src/app/finance/page.tsx:265 | /finance | button |  | {"type":"\"button\"","disabled":"{!academy \|\| exporting !== undefined}","onClick":"{() => void downloadExport(\"invoices\")}","className":"\"rounded border border-slate-700 px-4 py-3 text-left text-sm hover:border-cyan-400 disabled:opacity-60\""} |
| apps/web/src/app/finance/page.tsx:274 | /finance | button |  | {"type":"\"button\"","disabled":"{!academy \|\| exporting !== undefined}","onClick":"{() => void downloadExport(\"payments\")}","className":"\"rounded border border-slate-700 px-4 py-3 text-left text-sm hover:border-cyan-400 disabled:opacity-60\""} |
| apps/web/src/app/finance/page.tsx:291 | /finance | Link | Finance summary | {"href":"\"/finance-summary\"","className":"\"text-sm font-medium text-cyan-300\""} |
| apps/web/src/app/finance/page.tsx:300 | /finance | Link |  | {"key":"{'${href}-${label}'}","href":"{href}","className":"\"surface-panel rounded-xl p-4 transition hover:border-cyan-400\""} |
| apps/web/src/app/finance-adjustments/page.tsx:151 | /finance-adjustments | Link | Approval queue | {"href":"\"/finance-governance\"","className":"\"text-sm font-medium text-cyan-300\""} |
| apps/web/src/app/finance-adjustments/page.tsx:179 | /finance-adjustments | StandardSelectField |  | {"name":"\"invoiceId\"","value":"{requestInvoiceId}","onChange":"{setRequestInvoiceId}","placeholder":"\"Select invoice…\"","options":"{invoices.map((invoice) => ({ value: invoice.id, label: '${invoice.invoiceNumber} · ${money(invoice.totalAmount)}' }))}"} |
| apps/web/src/app/finance-adjustments/page.tsx:189 | /finance-adjustments | StandardSelectField |  | {"name":"\"type\"","value":"{requestType}","onChange":"{setRequestType}","placeholder":"\"Choose adjustment type\"","options":"{[ { value: \"Discount\", label: \"Discount\" }, { value: \"Scholarship\", label: \"Scholarship\" }, { value: \"Concession\", label: \"Concession\" }, { value: \"Refund\", label: \"Refund\" }, { value: \"CreditNote\", label: \"Credit note\" }, ]}"} |
| apps/web/src/app/finance-adjustments/page.tsx:224 | /finance-adjustments | button |  | {"disabled":"{!academy \|\| !requestInvoiceId \|\| submitting}","className":"\"enterprise-action-button mt-5 disabled:opacity-60\""} |
| apps/web/src/app/finance-adjustments/page.tsx:240 | /finance-adjustments | StandardSelectField |  | {"name":"\"adjustmentFilter\"","value":"{filter}","onChange":"{setFilter}","placeholder":"\"Filter requests\"","options":"{[ { value: \"All\", label: 'All (${counts.All})' }, { value: \"PendingApproval\", label: 'Pending (${counts.PendingApproval})' }, { value: \"Approved\", label: 'Approved (${counts.Approved})' }, { value: \"Rejected\", label: 'Rejected (${counts.Rejected})' }, ]}"} |
| apps/web/src/app/finance-adjustments/page.tsx:264 | /finance-adjustments | table |  | {"className":"\"min-w-full text-left text-sm\""} |
| apps/web/src/app/finance-governance/page.tsx:67 | /finance-governance | Link | Finance workspace → | {"href":"\"/finance\"","className":"\"text-sm font-medium text-cyan-300\""} |
| apps/web/src/app/finance-governance/page.tsx:70 | /finance-governance | button |  | {"type":"\"button\"","onClick":"{() => setEditingDocuments((value) => !value)}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/finance-governance/page.tsx:70 | /finance-governance | button | Save document setup | {"className":"\"enterprise-action-button mt-5\""} |
| apps/web/src/app/finance-governance/page.tsx:71 | /finance-governance | button | Cancel | {"type":"\"button\"","className":"\"text-sm text-slate-400\"","onClick":"{() => setSelectedInvoice(undefined)}"} |
| apps/web/src/app/finance-governance/page.tsx:71 | /finance-governance | button |  | {"disabled":"{saving}","className":"\"mt-4 rounded bg-cyan-400 px-4 py-2 font-semibold text-slate-950 disabled:opacity-60\""} |
| apps/web/src/app/finance-governance/page.tsx:73 | /finance-governance | Link | Reminders → | {"href":"\"/fee-reminders\"","className":"\"text-sm text-cyan-300\""} |
| apps/web/src/app/finance-governance/page.tsx:73 | /finance-governance | button | Create follow-up → | {"type":"\"button\"","onClick":"{() => setSelectedInvoice(item)}","className":"\"mt-3 text-sm font-medium text-cyan-300\""} |
| apps/web/src/app/finance-governance/page.tsx:74 | /finance-governance | button | Approve | {"disabled":"{saving}","onClick":"{() => void decide(item.id, true)}","className":"\"text-sm font-medium text-emerald-300 disabled:opacity-60\""} |
| apps/web/src/app/finance-governance/page.tsx:74 | /finance-governance | button | Reject | {"disabled":"{saving}","onClick":"{() => void decide(item.id, false)}","className":"\"text-sm font-medium text-rose-300 disabled:opacity-60\""} |
| apps/web/src/app/finance-governance/page.tsx:76 | /finance-governance | button | Save control | {"disabled":"{saving}","className":"\"rounded border border-cyan-400 px-3 py-2 text-sm font-medium text-cyan-200 disabled:opacity-60\""} |
| apps/web/src/app/finance-governance/page.tsx:81 | /finance-governance | article |  | {"className":"{'document-full-preview document-template-${theme.toLowerCase()}'}"} |
| apps/web/src/app/finance-policy/page.tsx:15 | /finance-policy | button | Save invoice settings | {"className":"\"enterprise-action-button mt-5\""} |
| apps/web/src/app/finance-reconciliation/page.tsx:67 | /finance-reconciliation | Link | Finance overview | {"href":"\"/finance\"","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/finance-reconciliation/page.tsx:69 | /finance-reconciliation | article |  | {} |
| apps/web/src/app/finance-reconciliation/page.tsx:69 | /finance-reconciliation | article |  | {} |
| apps/web/src/app/finance-reconciliation/page.tsx:69 | /finance-reconciliation | article |  | {} |
| apps/web/src/app/finance-reconciliation/page.tsx:69 | /finance-reconciliation | article |  | {} |
| apps/web/src/app/finance-reconciliation/page.tsx:70 | /finance-reconciliation | button | Cancel | {"type":"\"button\"","onClick":"{() => { setSelectedId(undefined); setReference(\"\"); }}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/finance-reconciliation/page.tsx:70 | /finance-reconciliation | button |  | {"disabled":"{saving}","className":"\"enterprise-action-button reconciliation-submit\""} |
| apps/web/src/app/finance-reconciliation/page.tsx:71 | /finance-reconciliation | StandardSelectField |  | {"name":"\"reconciliationStatus\"","value":"{filter}","onChange":"{setFilter}","placeholder":"\"Filter status\"","options":"{[{ value: \"Unreconciled\", label: 'Unreconciled (${unreconciled.length})' }, { value: \"Reconciled\", label: 'Reconciled (${reconciled.length})' }, { value: \"All\", label: \"All payments\" }]}"} |
| apps/web/src/app/finance-reconciliation/page.tsx:71 | /finance-reconciliation | StandardDateField |  | {"name":"\"reconciliationRecordedOn\"","value":"{recordedOn}","onChange":"{setRecordedOn}","label":"\"Recorded on\""} |
| apps/web/src/app/finance-reconciliation/page.tsx:71 | /finance-reconciliation | table |  | {} |
| apps/web/src/app/finance-reconciliation/page.tsx:71 | /finance-reconciliation | button | Reconcile | {"type":"\"button\"","onClick":"{() => { setSelectedId(item.id); setReference(\"\"); }}","className":"\"reconciliation-link\""} |
| apps/web/src/app/finance-summary/page.tsx:83 | /finance-summary | article |  | {"key":"{card.label}"} |
| apps/web/src/app/guardian-profile/page.tsx:180 | /guardian-profile | button |  | {"onClick":"{() => void save()}","disabled":"{saving}","className":"\"rounded-lg bg-cyan-400 px-4 py-2 text-sm font-semibold text-slate-950 disabled:opacity-60\""} |
| apps/web/src/app/guardians/page.tsx:251 | /guardians | button | Add parent | {"disabled":"{!academy}","className":"\"mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 disabled:opacity-60\""} |
| apps/web/src/app/guardians/page.tsx:301 | /guardians | button | Link parent | {"disabled":"{!academy \|\| !studentId \|\| !guardianId}","className":"\"mt-5 w-full rounded-lg border border-cyan-400 px-4 py-2.5 font-semibold text-cyan-200 disabled:opacity-60\""} |
| apps/web/src/app/guardians/page.tsx:314 | /guardians | ul |  | {"className":"\"mt-4 grid gap-3 md:grid-cols-2\""} |
| apps/web/src/app/guardians/page.tsx:351 | /guardians | Link | Open record | {"href":"{'/guardian-profile?guardianId=${guardian.id}'}","className":"\"rounded-lg border border-slate-700 px-3 py-1.5 text-sm text-cyan-300 hover:border-cyan-400\""} |
| apps/web/src/app/guardians/page.tsx:357 | /guardians | button |  | {"onClick":"{() => void saveGuardian(guardian)}","disabled":"{savingId === guardian.id}","className":"\"rounded-lg bg-cyan-400 px-3 py-2 text-sm font-semibold text-slate-950 disabled:opacity-50\""} |
| apps/web/src/app/guardians/page.tsx:364 | /guardians | button | Cancel | {"onClick":"{() => setEditingId(null)}","className":"\"rounded-lg border border-slate-700 px-3 py-2 text-sm\""} |
| apps/web/src/app/guardians/page.tsx:395 | /guardians | button | Edit | {"onClick":"{() => beginEdit(guardian)}","className":"\"rounded-lg border border-slate-700 px-3 py-1.5 text-sm hover:border-cyan-400\""} |
| apps/web/src/app/guardians/page.tsx:401 | /guardians | button |  | {"onClick":"{() => void toggleActive(guardian)}","disabled":"{savingId === guardian.id}","className":"\"rounded-lg border border-slate-700 px-3 py-1.5 text-sm hover:border-amber-400 disabled:opacity-50\""} |
| apps/web/src/app/guardians/page.tsx:424 | /guardians | ul |  | {"className":"\"mt-4 space-y-3\""} |
| apps/web/src/app/holidays/page.tsx:98 | /holidays | button | Add India holidays 2026 | {"type":"\"button\"","onClick":"{() => void addDefaults()}","disabled":"{!academy}"} |
| apps/web/src/app/holidays/page.tsx:129 | /holidays | StandardDateField |  | {"name":"\"holiday-date\"","label":"\"Date\"","value":"{form.holidayDate}","onChange":"{(holidayDate) => setForm({ ...form, holidayDate })}","required":"true"} |
| apps/web/src/app/holidays/page.tsx:136 | /holidays | button | Add holiday | {"className":"\"enterprise-action-button holidays-action\""} |
| apps/web/src/app/holidays/page.tsx:152 | /holidays | ul |  | {} |
| apps/web/src/app/holidays/page.tsx:164 | /holidays | button | Remove | {"type":"\"button\"","onClick":"{() => void remove(holiday.id)}"} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | article |  | {} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | article |  | {} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | article |  | {} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | article |  | {} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | button | Issue invoice | {"disabled":"{!academy \|\| !students.length}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | table |  | {} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | button | Preview / Print | {"type":"\"button\"","onClick":"{() => setSelected(invoice)}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | button | Print / Save PDF | {"type":"\"button\"","onClick":"{() => window.print()}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | button | Close preview | {"type":"\"button\"","onClick":"{() => setSelected(undefined)}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/invoices/page.tsx:21 | /invoices | table |  | {"className":"\"invoice-paper-table\""} |
| apps/web/src/app/leads/page.tsx:231 | /leads | StandardDateField |  | {"name":"\"dateOfBirth\"","label":"\"Date of birth\"","value":"{dateOfBirth}","onChange":"{setDateOfBirth}"} |
| apps/web/src/app/leads/page.tsx:254 | /leads | StandardSelectField |  | {"name":"\"source\"","value":"{source}","onChange":"{setSource}","placeholder":"\"Lead source\"","options":"{sources.map((value) => ({ value, label: value === \"WalkIn\" ? \"Walk-in\" : value, }))}"} |
| apps/web/src/app/leads/page.tsx:265 | /leads | StandardDateField |  | {"name":"\"follow-up-date\"","label":"\"Follow-up date\"","value":"{followUpDate}","onChange":"{setFollowUpDate}"} |
| apps/web/src/app/leads/page.tsx:271 | /leads | StandardTimeField |  | {"name":"\"follow-up-time\"","label":"\"Follow-up time\"","value":"{followUpTime}","onChange":"{setFollowUpTime}"} |
| apps/web/src/app/leads/page.tsx:286 | /leads | button | Add lead | {"disabled":"{!academy}","className":"\"enterprise-action-button leads-add-button\""} |
| apps/web/src/app/leads/page.tsx:305 | /leads | ul |  | {} |
| apps/web/src/app/leads/page.tsx:316 | /leads | StandardSelectField |  | {"name":"{'lead-stage-${lead.id}'}","value":"{lead.stage}","onChange":"{(stage) => void setStage(lead, stage)}","placeholder":"\"Stage\"","options":"{stages.map((value) => ({ value, label: display(value), }))}","disabled":"{ Boolean(lead.convertedStudentId) \|\| savingId === lead.id }"} |
| apps/web/src/app/leads/page.tsx:350 | /leads | button |  | {"type":"\"button\"","disabled":"{ Boolean(lead.convertedStudentId) \|\| savingId === lead.id }","onClick":"{() => void convert(lead)}"} |
| apps/web/src/app/leave/page.tsx:4 | /leave | button | Submit request | {"className":"\"mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950\""} |
| apps/web/src/app/leave/page.tsx:4 | /leave | ul |  | {"className":"\"mt-4 space-y-3\""} |
| apps/web/src/app/leave/page.tsx:4 | /leave | button | Approve | {"onClick":"{()=>void decide(x,\"Approved\")}","className":"\"text-sm text-cyan-200\""} |
| apps/web/src/app/leave/page.tsx:4 | /leave | button | Reject | {"onClick":"{()=>void decide(x,\"Rejected\")}","className":"\"text-sm text-rose-200\""} |
| apps/web/src/app/lesson-plans/page.tsx:1 | /lesson-plans | button | Create plan | {"className":"\"mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2 font-semibold text-slate-950\""} |
| apps/web/src/app/lesson-plans/page.tsx:1 | /lesson-plans | ul |  | {"className":"\"mt-4 space-y-3\""} |
| apps/web/src/app/login/page.tsx:101 | /login | button |  | {"type":"\"button\"","onClick":"{() => setShowPassword((current) => !current)}","aria-label":"{showPassword ? \"Hide password\" : \"Show password\"}","aria-pressed":"{showPassword}"} |
| apps/web/src/app/login/page.tsx:105 | /login | button |  | {"disabled":"{busy}","className":"\"auth-submit\""} |
| apps/web/src/app/makeup/page.tsx:30 | /makeup | StandardSelectField |  | {"name":"\"student\"","value":"{studentId}","onChange":"{setStudentId}","placeholder":"\"Select student\"","options":"{students.map((item) => ({ value: item.id, label: name(students, item.id) }))}"} |
| apps/web/src/app/makeup/page.tsx:31 | /makeup | StandardSelectField |  | {"name":"\"batch\"","value":"{batchId}","onChange":"{setBatchId}","placeholder":"\"Select class or batch\"","options":"{batches.map((item) => ({ value: item.id, label: name(batches, item.id) }))}"} |
| apps/web/src/app/makeup/page.tsx:32 | /makeup | StandardSelectField |  | {"name":"\"teacher\"","value":"{teacherId}","onChange":"{setTeacherId}","placeholder":"\"Use class teacher\"","options":"{teachers.map((item) => ({ value: item.id, label: name(teachers, item.id) }))}"} |
| apps/web/src/app/makeup/page.tsx:33 | /makeup | StandardSelectField |  | {"name":"\"scheduling-mode\"","value":"{mode}","onChange":"{(value) => setMode(value as \"Manual\" \| \"NextScheduled\")}","placeholder":"\"Scheduling method\"","options":"{[{ value: \"Manual\", label: \"Choose date and time\" }, { value: \"NextScheduled\", label: \"Use next scheduled class\" }]}"} |
| apps/web/src/app/makeup/page.tsx:34 | /makeup | StandardDateField |  | {"name":"\"makeup-date\"","label":"\"Make-up date\"","value":"{start.slice(0, 10)}","onChange":"{setDate}","required":"true"} |
| apps/web/src/app/makeup/page.tsx:34 | /makeup | StandardTimeField |  | {"name":"\"makeup-time\"","label":"\"Time (IST)\"","value":"{start.slice(11, 16) \|\| \"09:00\"}","onChange":"{(time) => setStart('${start.slice(0, 10) \|\| new Date().toISOString().slice(0, 10)}T${time}')}","intervalMinutes":"{15}"} |
| apps/web/src/app/makeup/page.tsx:35 | /makeup | StandardSelectField |  | {"name":"\"delivery-mode\"","value":"{deliveryMode}","onChange":"{setDeliveryMode}","placeholder":"\"Delivery mode\"","options":"{[{ value: \"Offline\", label: \"Offline\" }, { value: \"Online\", label: \"Online\" }, { value: \"Hybrid\", label: \"Hybrid\" }]}"} |
| apps/web/src/app/makeup/page.tsx:38 | /makeup | button |  | {"disabled":"{saving}","className":"\"enterprise-action-button makeup-create\""} |
| apps/web/src/app/makeup/page.tsx:39 | /makeup | ul |  | {} |
| apps/web/src/app/makeup/page.tsx:39 | /makeup | StandardSelectField |  | {"name":"{'makeup-status-${item.id}'}","value":"{item.status}","onChange":"{(status) => void updateStatus(item, status)}","placeholder":"\"Status\"","options":"{[\"Scheduled\", \"Completed\", \"Cancelled\"].map((status) => ({ value: status, label: status }))}"} |
| apps/web/src/app/meeting-links/page.tsx:197 | /meeting-links | article |  | {} |
| apps/web/src/app/meeting-links/page.tsx:198 | /meeting-links | article |  | {} |
| apps/web/src/app/meeting-links/page.tsx:199 | /meeting-links | article |  | {} |
| apps/web/src/app/meeting-links/page.tsx:208 | /meeting-links | StandardSelectField |  | {"name":"\"provider\"","value":"{provider}","onChange":"{(x) => setProvider(x as Provider)}","placeholder":"\"Meeting provider\"","options":"{(Object.keys(providerName) as Provider[]).map((x) => ({ value: x, label: providerName[x], }))}"} |
| apps/web/src/app/meeting-links/page.tsx:230 | /meeting-links | StandardSelectField |  | {"name":"\"batch\"","value":"{batchId}","onChange":"{(x) => { setBatchId(x); setAttendeeIds([]); }}","placeholder":"\"Online or Hybrid batch without a link\"","options":"{batches .filter((b) => eligibleBatchIds.includes(b.id)) .map((b) => ({ value: b.id, label: b.name }))}"} |
| apps/web/src/app/meeting-links/page.tsx:242 | /meeting-links | StandardSelectField |  | {"name":"\"teacher\"","value":"{teacherId}","onChange":"{setTeacherId}","placeholder":"\"Organizer / teacher\"","options":"{teachers.map((t) => ({ value: t.id, label: '${name(t)}${t.email ? ' · ${t.email}' : \"\"}', }))}"} |
| apps/web/src/app/meeting-links/page.tsx:255 | /meeting-links | StandardSelectField |  | {"name":"\"invite-student\"","value":"{studentToAdd}","onChange":"{addStudent}","placeholder":"{ batchId ? \"Select enrolled student\" : \"Select an eligible batch first\" }","options":"{eligibleStudents .filter((s) => !attendeeIds.includes(s.id)) .map((s) => ({ value: s.id, label: '${name(s)}${s.email ? ' · ${s.email}' : \"\"}', }))}"} |
| apps/web/src/app/meeting-links/page.tsx:279 | /meeting-links | button | × | {"type":"\"button\"","key":"{s.id}","onClick":"{() => setAttendeeIds((x) => x.filter((id) => id !== s.id)) }"} |
| apps/web/src/app/meeting-links/page.tsx:300 | /meeting-links | StandardDateField |  | {"name":"\"date\"","label":"\"Date\"","value":"{date}","onChange":"{setDate}","required":"true"} |
| apps/web/src/app/meeting-links/page.tsx:307 | /meeting-links | StandardTimeField |  | {"name":"\"start-time\"","label":"\"Start time\"","value":"{time}","onChange":"{setTime}"} |
| apps/web/src/app/meeting-links/page.tsx:315 | /meeting-links | StandardSelectField |  | {"name":"\"duration\"","value":"{duration}","onChange":"{setDuration}","placeholder":"\"Select duration\"","options":"{[\"30\", \"45\", \"60\", \"90\", \"120\"].map((x) => ({ value: x, label: '${x} minutes', }))}"} |
| apps/web/src/app/meeting-links/page.tsx:328 | /meeting-links | StandardSelectField |  | {"name":"\"recurrence\"","value":"{recurrence}","onChange":"{setRecurrence}","placeholder":"\"Schedule\"","options":"{[ { value: \"Once\", label: \"One-time class\" }, { value: \"Weekly\", label: \"Weekly\" }, { value: \"Custom\", label: \"Custom recurrence\" }, ]}"} |
| apps/web/src/app/meeting-links/page.tsx:339 | /meeting-links | StandardSelectField |  | {"name":"\"access\"","value":"{access}","onChange":"{setAccess}","placeholder":"\"Who can join\"","options":"{[ { value: \"InviteesOnly\", label: \"Invitees only\" }, { value: \"Organisation\", label: \"Organisation and invitees\", }, { value: \"Open\", label: \"Anyone with the link\" }, ]}"} |
| apps/web/src/app/meeting-links/page.tsx:363 | /meeting-links | StandardSelectField |  | {"name":"\"chat\"","value":"{chat}","onChange":"{setChat}","placeholder":"\"In-meeting chat\"","options":"{[ { value: \"Disabled\", label: \"Chat disabled\" }, { value: \"HostsOnly\", label: \"Hosts only\" }, { value: \"Enabled\", label: \"Everyone\" }, ]}"} |
| apps/web/src/app/meeting-links/page.tsx:374 | /meeting-links | StandardSelectField |  | {"name":"\"recording\"","value":"{recording}","onChange":"{setRecording}","placeholder":"\"Recording\"","options":"{[ { value: \"Off\", label: \"Recording off\" }, { value: \"Auto\", label: \"Record automatically\" }, { value: \"Host\", label: \"Host decides\" }, ]}"} |
| apps/web/src/app/meeting-links/page.tsx:387 | /meeting-links | button |  | {"className":"\"enterprise-action-button\"","disabled":"{!connected}"} |
| apps/web/src/app/meeting-links/page.tsx:407 | /meeting-links | a | Connect provider | {"href":"\"/communication-settings\""} |
| apps/web/src/app/meeting-links/page.tsx:417 | /meeting-links | StandardDateField |  | {"name":"\"created-from\"","label":"\"Created from\"","value":"{from}","onChange":"{setFrom}"} |
| apps/web/src/app/meeting-links/page.tsx:423 | /meeting-links | StandardDateField |  | {"name":"\"created-to\"","label":"\"Created to\"","value":"{to}","onChange":"{setTo}"} |
| apps/web/src/app/meeting-links/page.tsx:437 | /meeting-links | ul |  | {} |
| apps/web/src/app/meeting-links/page.tsx:448 | /meeting-links | a | Open meeting | {"href":"{m.joinUrl}"} |
| apps/web/src/app/meeting-links/page.tsx:449 | /meeting-links | button | Edit details | {"type":"\"button\"","onClick":"{() => edit(m)}"} |
| apps/web/src/app/message-templates/page.tsx:175 | /message-templates | button | Add selected ( ) | {"type":"\"button\"","className":"\"enterprise-action-button\"","disabled":"{!academy \|\| !selectedIds.length}","onClick":"{addSelected}"} |
| apps/web/src/app/message-templates/page.tsx:185 | /message-templates | button | WhatsApp | {"type":"\"button\"","data-active":"{selectedChannel === \"WhatsApp\"}","onClick":"{() => setSelectedChannel(\"WhatsApp\")}"} |
| apps/web/src/app/message-templates/page.tsx:192 | /message-templates | button | Email | {"type":"\"button\"","data-active":"{selectedChannel === \"Email\"}","onClick":"{() => setSelectedChannel(\"Email\")}"} |
| apps/web/src/app/message-templates/page.tsx:210 | /message-templates | button |  | {"type":"\"button\"","onClick":"{() => toggleGroup(groupItems)}"} |
| apps/web/src/app/message-templates/page.tsx:248 | /message-templates | StandardSelectField |  | {"name":"\"channel\"","value":"{form.channel}","onChange":"{(value) => setForm({ ...form, channel: value })}","placeholder":"\"Select channel\"","options":"{[ { value: \"WhatsApp\", label: \"WhatsApp\" }, { value: \"Email\", label: \"Email\" }, ]}"} |
| apps/web/src/app/message-templates/page.tsx:261 | /message-templates | StandardSelectField |  | {"name":"\"templateGroup\"","value":"{form.templateGroup}","onChange":"{(value) => setForm({ ...form, templateGroup: value }) }","placeholder":"\"Select group\"","options":"{[ \"Finance\", \"Admissions\", \"Classes\", \"Academic\", \"Academy updates\", \"General\", ].map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/message-templates/page.tsx:300 | /message-templates | StandardSelectField |  | {"name":"\"category\"","value":"{form.category}","onChange":"{(value) => setForm({ ...form, category: value })}","placeholder":"\"Select category\"","options":"{[ \"Utility\", \"Marketing\", \"Authentication\", \"Transactional\", ].map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/message-templates/page.tsx:315 | /message-templates | StandardSelectField |  | {"name":"\"status\"","value":"{form.status}","onChange":"{(value) => setForm({ ...form, status: value })}","placeholder":"\"Select status\"","options":"{[\"Draft\", \"Approved\", \"Disabled\"].map((value) => ({ value, label: value, }))}"} |
| apps/web/src/app/message-templates/page.tsx:376 | /message-templates | button | Save template | {"disabled":"{!academy}","className":"\"enterprise-action-button templates-wide\""} |
| apps/web/src/app/message-templates/page.tsx:395 | /message-templates | ul |  | {} |
| apps/web/src/app/music/page.tsx:215 | /music | StandardSelectField |  | {"name":"\"difficulty\"","value":"{difficulty}","onChange":"{setDifficulty}","placeholder":"\"Difficulty\"","options":"{difficulties.map((value) => ({ value, label: value, }))}"} |
| apps/web/src/app/music/page.tsx:226 | /music | button | Add piece | {"className":"\"enterprise-action-button music-action\""} |
| apps/web/src/app/music/page.tsx:239 | /music | StandardSelectField |  | {"name":"\"student\"","value":"{studentId}","onChange":"{setStudentId}","placeholder":"\"Select student\"","options":"{students.map((student) => ({ value: student.id, label: '${student.firstName} ${student.lastName}', }))}"} |
| apps/web/src/app/music/page.tsx:249 | /music | StandardSelectField |  | {"name":"\"piece\"","value":"{pieceId}","onChange":"{setPieceId}","placeholder":"\"Select piece\"","options":"{pieces.map((item) => ({ value: item.id, label: '${item.title} · ${item.difficulty}', }))}"} |
| apps/web/src/app/music/page.tsx:259 | /music | StandardDateField |  | {"name":"\"target\"","label":"\"Target date\"","value":"{targetDate}","onChange":"{setTargetDate}"} |
| apps/web/src/app/music/page.tsx:265 | /music | button | Assign piece | {"className":"\"enterprise-action-button music-action\""} |
| apps/web/src/app/music/page.tsx:282 | /music | ul |  | {} |
| apps/web/src/app/music/page.tsx:295 | /music | StandardSelectField |  | {"name":"{'progress-${item.id}'}","value":"{item.status}","onChange":"{(status) => void updateProgress(item, status)}","placeholder":"\"Progress\"","options":"{progressStates.map((value) => ({ value, label: label(value), }))}"} |
| apps/web/src/app/payments/page.tsx:60 | /payments | article |  | {} |
| apps/web/src/app/payments/page.tsx:60 | /payments | article |  | {} |
| apps/web/src/app/payments/page.tsx:60 | /payments | article |  | {} |
| apps/web/src/app/payments/page.tsx:60 | /payments | article |  | {} |
| apps/web/src/app/payments/page.tsx:63 | /payments | article |  | {"className":"\"payments-selected\""} |
| apps/web/src/app/payments/page.tsx:67 | /payments | button | Record payment | {"disabled":"{!academy \|\| !invoiceId \|\| !amount}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/payments/page.tsx:68 | /payments | ul |  | {} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | article |  | {} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | article |  | {} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | article |  | {} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | StandardSelectField |  | {"name":"\"workerType\"","value":"{workerType}","onChange":"{value => { setWorkerType(value as \"Teacher\" \| \"Staff\"); setWorkerId(\"\"); }}","placeholder":"\"Choose worker type\"","options":"{[{ value: \"Teacher\", label: \"Teacher\" }, { value: \"Staff\", label: \"Staff\" }]}"} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | StandardSelectField |  | {"name":"\"workerId\"","value":"{workerId}","onChange":"{setWorkerId}","placeholder":"{'Select ${workerType.toLowerCase()}'}","options":"{workers.map(worker => ({ value: worker.id, label: worker.name }))}"} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | StandardSelectField |  | {"name":"\"paymentModel\"","value":"{paymentModel}","onChange":"{value => setPaymentModel(value as \"Monthly\" \| \"SessionBlock\")}","placeholder":"\"Choose payment cycle\"","options":"{[{ value: \"Monthly\", label: \"Monthly salary\" }, { value: \"SessionBlock\", label: \"Completed-session cycle\" }]}"} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | StandardDateField |  | {"name":"\"effectiveFrom\"","value":"{effectiveFrom}","onChange":"{setEffectiveFrom}","label":"\"Effective from\"","required":"true"} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | button | Save payout profile | {"className":"\"enterprise-action-button payroll-action\"","disabled":"{!academy \|\| !workerId \|\| !effectiveFrom}"} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | StandardSelectField |  | {"name":"\"payrollProfileId\"","value":"{payoutProfileId}","onChange":"{setPayoutProfileId}","placeholder":"\"Select active profile\"","options":"{activeProfiles.map(profile => ({ value: profile.id, label: '${profile.workerName} · ${profile.paymentModel === \"Monthly\" ? 'Monthly ${money(profile.monthlyAmount ?? 0)}' : '${profile.sessionsPerCycle} sessions · ${money(profile.amountPerCycle ?? 0)}'}' }))}"} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | StandardSelectField |  | {"name":"\"paymentMethod\"","value":"{paymentMethod}","onChange":"{setPaymentMethod}","placeholder":"\"Choose payment method\"","options":"{[{ value: \"BankTransfer\", label: \"Bank transfer\" }, { value: \"UPI\", label: \"UPI\" }, { value: \"Cash\", label: \"Cash\" }, { value: \"Cheque\", label: \"Cheque\" }]}"} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | button | Record payout &amp; generate payslip | {"className":"\"enterprise-action-button payroll-action payroll-wide\"","disabled":"{!academy \|\| !payoutProfileId}"} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | table |  | {} |
| apps/web/src/app/payroll/page.tsx:24 | /payroll | button | Print / Save PDF | {"type":"\"button\"","onClick":"{() => window.print()}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/platform/control/page.tsx:560 | /platform/control | nav |  | {"className":"\"platform-nav\"","aria-label":"\"Platform navigation\""} |
| apps/web/src/app/platform/control/page.tsx:564 | /platform/control | button |  | {"type":"\"button\"","key":"{item.label}","data-active":"{item.tab === tab}","onClick":"{() => selectWorkspace(item.tab)}"} |
| apps/web/src/app/platform/control/page.tsx:574 | /platform/control | Link |  | {"href":"{item.href}","key":"{item.label}","data-active":"{false}"} |
| apps/web/src/app/platform/control/page.tsx:589 | /platform/control | button | ⌕ | {"type":"\"button\"","className":"\"enterprise-icon-button\"","aria-label":"\"Search academy portfolio\""} |
| apps/web/src/app/platform/control/page.tsx:596 | /platform/control | button | ♧ | {"type":"\"button\"","className":"\"enterprise-icon-button\"","aria-label":"\"Platform notifications\""} |
| apps/web/src/app/platform/control/page.tsx:604 | /platform/control | details |  | {"className":"\"platform-profile enterprise-profile\""} |
| apps/web/src/app/platform/control/page.tsx:605 | /platform/control | summary |  | {"className":"\"enterprise-profile-trigger\"","aria-label":"\"Open Platform Owner profile menu\""} |
| apps/web/src/app/platform/control/page.tsx:612 | /platform/control | button | Sign out | {"type":"\"button\"","onClick":"{signOut}"} |
| apps/web/src/app/platform/control/page.tsx:619 | /platform/control | nav |  | {"className":"\"platform-mobile-nav\"","aria-label":"\"Platform navigation\""} |
| apps/web/src/app/platform/control/page.tsx:621 | /platform/control | Link |  | {"href":"{item.href}","key":"{item.label}","aria-current":"{(\"tab\" in item ? item.tab === tab : false) ? \"page\" : undefined}"} |
| apps/web/src/app/platform/control/page.tsx:651 | /platform/control | button |  | {"disabled":"{busy}","className":"\"enterprise-action-button tenant-intake-wide\""} |
| apps/web/src/app/platform/control/page.tsx:652 | /platform/control | StandardSelectField |  | {"name":"\"academy\"","value":"{selectedAcademy}","onChange":"{setSelectedAcademy}","placeholder":"\"Select academy\"","options":"{academies.map((academy) => ({ value: academy.id, label: academy.name }))}"} |
| apps/web/src/app/platform/control/page.tsx:659 | /platform/control | button |  | {"disabled":"{busy \|\| !selectedAcademy}","className":"\"enterprise-action-button tenant-intake-wide\""} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | button | Edit information | {"type":"\"button\"","className":"\"enterprise-secondary-button\"","onClick":"{() => { setOnboardingSection(\"Personal\"); selectWorkspace(\"Tenant onboarding\"); }}"} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | StandardSelectField |  | {"name":"\"academy\"","value":"{selectedAcademy}","onChange":"{setSelectedAcademy}","placeholder":"\"Select academy\"","options":"{academies.map((academy) => ({ value: academy.id, label: academy.name }))}"} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | article |  | {} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | article |  | {} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | article |  | {} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | article |  | {} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | article |  | {} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | article |  | {} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | article |  | {} |
| apps/web/src/app/platform/control/page.tsx:660 | /platform/control | article |  | {} |
| apps/web/src/app/platform/control/page.tsx:664 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setTenantModal(\"all\")}"} |
| apps/web/src/app/platform/control/page.tsx:665 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setTenantModal(\"active\")}"} |
| apps/web/src/app/platform/control/page.tsx:666 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setTenantModal(\"trial\")}"} |
| apps/web/src/app/platform/control/page.tsx:667 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setTenantModal(\"attention\")}"} |
| apps/web/src/app/platform/control/page.tsx:672 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:672 | /platform/control | button |  | {"type":"\"button\"","data-selected":"{academy.id === selectedAcademy}","onClick":"{() => setSelectedAcademy(academy.id)}"} |
| apps/web/src/app/platform/control/page.tsx:677 | /platform/control | StandardSelectField |  | {"name":"\"academy\"","value":"{selectedAcademy}","onChange":"{setSelectedAcademy}","placeholder":"\"Select academy\"","options":"{academies.map((academy) => ({ value: academy.id, label: academy.name }))}"} |
| apps/web/src/app/platform/control/page.tsx:680 | /platform/control | StandardSelectField |  | {"name":"\"plan\"","value":"{tenantPlan}","onChange":"{setTenantPlan}","placeholder":"\"Choose plan\"","options":"{[\"Trial\", \"Launch\", \"Growth\", \"Professional\", \"Enterprise\"].map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/platform/control/page.tsx:681 | /platform/control | StandardSelectField |  | {"name":"\"status\"","value":"{tenantStatus}","onChange":"{setTenantStatus}","placeholder":"\"Choose status\"","options":"{[\"Trial\", \"Active\", \"Past due\", \"Cancelled\"].map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/platform/control/page.tsx:682 | /platform/control | StandardDateField |  | {"name":"\"endsAt\"","value":"{tenantEndsAt}","onChange":"{setTenantEndsAt}","label":"\"Subscription end date\""} |
| apps/web/src/app/platform/control/page.tsx:684 | /platform/control | button |  | {"disabled":"{busy}","className":"\"enterprise-action-button platform-tenant-action\""} |
| apps/web/src/app/platform/control/page.tsx:688 | /platform/control | article |  | {"className":"\"platform-modal platform-tenant-modal\"","role":"\"dialog\"","aria-modal":"\"true\"","aria-labelledby":"\"tenant-modal-title\""} |
| apps/web/src/app/platform/control/page.tsx:688 | /platform/control | button | × | {"type":"\"button\"","aria-label":"\"Close tenant details\"","onClick":"{() => setTenantModal(null)}"} |
| apps/web/src/app/platform/control/page.tsx:688 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:693 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setAnnouncementModal(\"active\")}"} |
| apps/web/src/app/platform/control/page.tsx:694 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setAnnouncementModal(\"recent\")}"} |
| apps/web/src/app/platform/control/page.tsx:700 | /platform/control | StandardSelectField |  | {"name":"\"academyId\"","value":"{announcementAcademyId}","onChange":"{setAnnouncementAcademyId}","placeholder":"\"Select academy\"","options":"{academies.map((academy) => ({ value: academy.id, label: academy.name }))}"} |
| apps/web/src/app/platform/control/page.tsx:703 | /platform/control | StandardSelectField |  | {"name":"\"hours\"","value":"{announcementHours}","onChange":"{setAnnouncementHours}","placeholder":"\"Select duration\"","options":"{[[\"1\", \"1 hour\"], [\"4\", \"4 hours\"], [\"8\", \"8 hours\"], [\"24\", \"24 hours\"], [\"48\", \"2 days\"], [\"72\", \"3 days\"], [\"168\", \"7 days\"]].map(([value, label]) => ({ value, label }))}"} |
| apps/web/src/app/platform/control/page.tsx:705 | /platform/control | button |  | {"disabled":"{busy \|\| !announcementAcademyId}","className":"\"enterprise-action-button platform-announcement-action\""} |
| apps/web/src/app/platform/control/page.tsx:710 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:713 | /platform/control | article |  | {"className":"\"platform-modal platform-announcement-modal\"","role":"\"dialog\"","aria-modal":"\"true\"","aria-labelledby":"\"announcement-modal-title\""} |
| apps/web/src/app/platform/control/page.tsx:713 | /platform/control | button | × | {"type":"\"button\"","aria-label":"\"Close announcement details\"","onClick":"{() => setAnnouncementModal(null)}"} |
| apps/web/src/app/platform/control/page.tsx:713 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:718 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setAdminModal(\"all\")}"} |
| apps/web/src/app/platform/control/page.tsx:719 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setAdminModal(\"active\")}"} |
| apps/web/src/app/platform/control/page.tsx:720 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setAdminModal(\"inactive\")}"} |
| apps/web/src/app/platform/control/page.tsx:721 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setAdminModal(\"academies\")}"} |
| apps/web/src/app/platform/control/page.tsx:726 | /platform/control | table |  | {} |
| apps/web/src/app/platform/control/page.tsx:743 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => void adminActive(admin)}","disabled":"{busy}"} |
| apps/web/src/app/platform/control/page.tsx:745 | /platform/control | button | Reset password | {"type":"\"button\"","onClick":"{() => void adminPassword(admin)}","disabled":"{busy}"} |
| apps/web/src/app/platform/control/page.tsx:752 | /platform/control | article |  | {"className":"\"platform-modal platform-admin-modal\"","role":"\"dialog\"","aria-modal":"\"true\"","aria-labelledby":"\"admin-modal-title\""} |
| apps/web/src/app/platform/control/page.tsx:752 | /platform/control | button | × | {"type":"\"button\"","aria-label":"\"Close administrator details\"","onClick":"{() => setAdminModal(null)}"} |
| apps/web/src/app/platform/control/page.tsx:752 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:758 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setInvoiceModal(\"all\")}"} |
| apps/web/src/app/platform/control/page.tsx:759 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setInvoiceModal(\"open\")}"} |
| apps/web/src/app/platform/control/page.tsx:760 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setInvoiceModal(\"overdue\")}"} |
| apps/web/src/app/platform/control/page.tsx:761 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setInvoiceModal(\"paid\")}"} |
| apps/web/src/app/platform/control/page.tsx:767 | /platform/control | StandardSelectField |  | {"name":"\"academyId\"","value":"{invoiceAcademyId}","onChange":"{setInvoiceAcademyId}","placeholder":"\"Select academy\"","options":"{academies.map((academy) => ({ value: academy.id, label: academy.name }))}"} |
| apps/web/src/app/platform/control/page.tsx:770 | /platform/control | StandardSelectField |  | {"name":"\"currency\"","value":"{invoiceCurrency}","onChange":"{setInvoiceCurrency}","placeholder":"\"Select currency\"","options":"{[\"INR\", \"USD\", \"GBP\", \"EUR\"].map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/platform/control/page.tsx:771 | /platform/control | StandardDateField |  | {"name":"\"periodStart\"","value":"{invoicePeriodStart}","onChange":"{setInvoicePeriodStart}","label":"\"Period start\"","required":"true"} |
| apps/web/src/app/platform/control/page.tsx:772 | /platform/control | StandardDateField |  | {"name":"\"periodEnd\"","value":"{invoicePeriodEnd}","onChange":"{setInvoicePeriodEnd}","label":"\"Period end\"","required":"true"} |
| apps/web/src/app/platform/control/page.tsx:773 | /platform/control | StandardDateField |  | {"name":"\"dueDate\"","value":"{invoiceDueDate}","onChange":"{setInvoiceDueDate}","label":"\"Due date\"","required":"true"} |
| apps/web/src/app/platform/control/page.tsx:774 | /platform/control | button |  | {"disabled":"{busy \|\| !invoiceAcademyId \|\| !invoicePeriodStart \|\| !invoicePeriodEnd \|\| !invoiceDueDate}","className":"\"enterprise-action-button platform-billing-action\""} |
| apps/web/src/app/platform/control/page.tsx:779 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:779 | /platform/control | StandardSelectField |  | {"name":"{'invoice-status-${item.id}'}","value":"{invoiceStatuses[item.id] ?? item.status}","onChange":"{(value) => { setInvoiceStatuses((current) => ({ ...current, [item.id]: value })); void updateInvoice(item, value); }}","placeholder":"\"Update status\"","options":"{[\"Draft\", \"Issued\", \"Payment submitted\", \"Overdue\", \"Paid\", \"Void\"].map((value) => ({ value, label: value }))}","disabled":"{busy}"} |
| apps/web/src/app/platform/control/page.tsx:782 | /platform/control | article |  | {"className":"\"platform-modal platform-invoice-modal\"","role":"\"dialog\"","aria-modal":"\"true\"","aria-labelledby":"\"invoice-modal-title\""} |
| apps/web/src/app/platform/control/page.tsx:782 | /platform/control | button | × | {"type":"\"button\"","aria-label":"\"Close invoice details\"","onClick":"{() => setInvoiceModal(null)}"} |
| apps/web/src/app/platform/control/page.tsx:782 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:788 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setCaseModal(\"all\")}"} |
| apps/web/src/app/platform/control/page.tsx:789 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setCaseModal(\"open\")}"} |
| apps/web/src/app/platform/control/page.tsx:790 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setCaseModal(\"urgent\")}"} |
| apps/web/src/app/platform/control/page.tsx:791 | /platform/control | button |  | {"type":"\"button\"","onClick":"{() => setCaseModal(\"resolved\")}"} |
| apps/web/src/app/platform/control/page.tsx:797 | /platform/control | StandardSelectField |  | {"name":"\"academyId\"","value":"{supportAcademyId}","onChange":"{setSupportAcademyId}","placeholder":"\"Select academy\"","options":"{academies.map((academy) => ({ value: academy.id, label: academy.name }))}"} |
| apps/web/src/app/platform/control/page.tsx:799 | /platform/control | StandardSelectField |  | {"name":"\"priority\"","value":"{supportPriority}","onChange":"{setSupportPriority}","placeholder":"\"Select priority\"","options":"{[\"Low\", \"Normal\", \"High\", \"Urgent\"].map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/platform/control/page.tsx:801 | /platform/control | button |  | {"disabled":"{busy \|\| !supportAcademyId}","className":"\"enterprise-action-button platform-support-action\""} |
| apps/web/src/app/platform/control/page.tsx:806 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:806 | /platform/control | StandardSelectField |  | {"name":"{'case-status-${item.id}'}","value":"{caseStatuses[item.id] ?? item.status}","onChange":"{(value) => { setCaseStatuses((current) => ({ ...current, [item.id]: value })); void updateCase(item, value); }}","placeholder":"\"Update status\"","options":"{[\"Open\", \"In progress\", \"Resolved\", \"Closed\"].map((value) => ({ value, label: value }))}","disabled":"{busy}"} |
| apps/web/src/app/platform/control/page.tsx:809 | /platform/control | article |  | {"className":"\"platform-modal platform-case-modal\"","role":"\"dialog\"","aria-modal":"\"true\"","aria-labelledby":"\"case-modal-title\""} |
| apps/web/src/app/platform/control/page.tsx:809 | /platform/control | button | × | {"type":"\"button\"","aria-label":"\"Close support case details\"","onClick":"{() => setCaseModal(null)}"} |
| apps/web/src/app/platform/control/page.tsx:809 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:816 | /platform/control | button |  | {"type":"\"button\"","className":"\"platform-owner-photo\"","onClick":"{() => profileImageInput.current?.click()}","disabled":"{busy}"} |
| apps/web/src/app/platform/control/page.tsx:823 | /platform/control | button | Save personal details | {"disabled":"{busy}"} |
| apps/web/src/app/platform/control/page.tsx:825 | /platform/control | button | Update password | {"disabled":"{busy}"} |
| apps/web/src/app/platform/control/page.tsx:832 | /platform/control | StandardSelectField |  | {"name":"\"currency\"","value":"{settingsCurrency}","onChange":"{setSettingsCurrency}","placeholder":"\"Select currency\"","options":"{[\"INR\", \"USD\", \"GBP\", \"EUR\"].map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/platform/control/page.tsx:837 | /platform/control | button |  | {"disabled":"{busy}","className":"\"enterprise-action-button platform-settings-action\""} |
| apps/web/src/app/platform/control/page.tsx:842 | /platform/control | StandardSelectField |  | {"name":"\"activityScope\"","value":"{activityScope}","onChange":"{setActivityScope}","placeholder":"\"All operations\"","options":"{[{ value: \"Platform owner\", label: \"Platform Owner\" }, { value: \"Academy admin\", label: \"Academy Admin\" }]}"} |
| apps/web/src/app/platform/control/page.tsx:842 | /platform/control | StandardDateField |  | {"name":"\"activityFrom\"","label":"\"From date\"","value":"{activityFrom}","onChange":"{setActivityFrom}"} |
| apps/web/src/app/platform/control/page.tsx:842 | /platform/control | StandardDateField |  | {"name":"\"activityTo\"","label":"\"To date\"","value":"{activityTo}","onChange":"{setActivityTo}"} |
| apps/web/src/app/platform/control/page.tsx:842 | /platform/control | StandardSelectField |  | {"name":"\"deleteLogMonth\"","value":"{deleteLogMonth}","onChange":"{setDeleteLogMonth}","placeholder":"\"Select month\"","options":"{[\"January\",\"February\",\"March\",\"April\",\"May\",\"June\",\"July\",\"August\",\"September\",\"October\",\"November\",\"December\"].map((label, index) => ({ value: String(index + 1), label }))}"} |
| apps/web/src/app/platform/control/page.tsx:842 | /platform/control | StandardSelectField |  | {"name":"\"deleteLogYear\"","value":"{deleteLogYear}","onChange":"{setDeleteLogYear}","placeholder":"\"Select year\"","options":"{Array.from({ length: 6 }, (_, index) => String(new Date().getFullYear() - index)).map((year) => ({ value: year, label: year }))}"} |
| apps/web/src/app/platform/control/page.tsx:842 | /platform/control | button | Delete records | {"type":"\"button\"","className":"\"enterprise-danger-button\"","disabled":"{!deleteLogMonth \|\| !deleteLogYear}","onClick":"{() => void deleteActivityLogs().catch((error) => setMessage(error.message))}"} |
| apps/web/src/app/platform/control/page.tsx:842 | /platform/control | ul |  | {"className":"\"platform-activity-list\""} |
| apps/web/src/app/platform/control/page.tsx:846 | /platform/control | button |  | {"key":"{item.key}","type":"\"button\"","onClick":"{() => setHealthModal(item.key)}"} |
| apps/web/src/app/platform/control/page.tsx:851 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:855 | /platform/control | ul |  | {} |
| apps/web/src/app/platform/control/page.tsx:858 | /platform/control | article |  | {"className":"\"platform-modal platform-health-modal\"","role":"\"dialog\"","aria-modal":"\"true\"","aria-labelledby":"\"health-modal-title\""} |
| apps/web/src/app/platform/control/page.tsx:858 | /platform/control | button | × | {"type":"\"button\"","aria-label":"\"Close service details\"","onClick":"{() => setHealthModal(null)}"} |
| apps/web/src/app/platform/page.tsx:187 | /platform | nav |  | {"className":"\"platform-nav\"","aria-label":"\"Platform navigation\""} |
| apps/web/src/app/platform/page.tsx:190 | /platform | Link |  | {"href":"{item.href}","key":"{item.label}","data-active":"{item.label === \"Overview\"}"} |
| apps/web/src/app/platform/page.tsx:208 | /platform | button | ⌕ | {"type":"\"button\"","className":"\"enterprise-icon-button\"","aria-label":"\"Search academy portfolio\"","onClick":"{() => document.getElementById(\"academy-search\")?.focus()}"} |
| apps/web/src/app/platform/page.tsx:216 | /platform | button | ♧ | {"type":"\"button\"","className":"\"enterprise-icon-button\"","aria-label":"\"Platform notifications\""} |
| apps/web/src/app/platform/page.tsx:224 | /platform | details |  | {"className":"\"platform-profile enterprise-profile\""} |
| apps/web/src/app/platform/page.tsx:225 | /platform | summary |  | {"className":"\"enterprise-profile-trigger\"","aria-label":"\"Open Platform Owner profile menu\""} |
| apps/web/src/app/platform/page.tsx:233 | /platform | button | Sign out | {"type":"\"button\"","onClick":"{signOut}"} |
| apps/web/src/app/platform/page.tsx:240 | /platform | nav |  | {"className":"\"platform-mobile-nav\"","aria-label":"\"Platform navigation\""} |
| apps/web/src/app/platform/page.tsx:242 | /platform | Link |  | {"href":"{item.href}","key":"{item.label}","aria-current":"{item.label === \"Overview\" ? \"page\" : undefined}"} |
| apps/web/src/app/platform/page.tsx:261 | /platform | button | ＋ Onboard academy | {"type":"\"button\"","className":"\"enterprise-primary-action\"","onClick":"{() => setOnboardingOpen(true)}"} |
| apps/web/src/app/platform/page.tsx:269 | /platform | button |  | {"key":"{tile.key}","type":"\"button\"","className":"{tile.className}","onClick":"{() => setOverviewModal(tile.key)}","aria-haspopup":"\"dialog\""} |
| apps/web/src/app/platform/page.tsx:272 | /platform | article |  | {"id":"\"portfolio\"","className":"\"platform-panel platform-portfolio\""} |
| apps/web/src/app/platform/page.tsx:289 | /platform | table |  | {} |
| apps/web/src/app/platform/page.tsx:324 | /platform | button |  | {"type":"\"button\"","disabled":"{busy}","onClick":"{() => void setStatus(academy)}"} |
| apps/web/src/app/platform/page.tsx:348 | /platform | article |  | {"className":"\"platform-modal platform-overview-modal\"","role":"\"dialog\"","aria-modal":"\"true\"","aria-labelledby":"\"overview-tile-title\"","onMouseDown":"{(event) => event.stopPropagation()}"} |
| apps/web/src/app/platform/page.tsx:349 | /platform | button | × | {"type":"\"button\"","aria-label":"\"Close detail\"","onClick":"{() => setOverviewModal(null)}"} |
| apps/web/src/app/platform/page.tsx:350 | /platform | ul |  | {"className":"\"platform-overview-detail-list\""} |
| apps/web/src/app/platform/page.tsx:350 | /platform | Link | Open Support | {"href":"\"/platform/control?tab=Support\"","onClick":"{() => setOverviewModal(null)}"} |
| apps/web/src/app/platform/page.tsx:350 | /platform | Link | Open Billing | {"href":"\"/platform/control?tab=Billing\"","onClick":"{() => setOverviewModal(null)}"} |
| apps/web/src/app/platform/page.tsx:356 | /platform | article |  | {"className":"\"platform-modal platform-onboarding-modal\"","role":"\"dialog\"","aria-modal":"\"true\"","aria-labelledby":"\"onboard-academy-title\"","onMouseDown":"{(event) => event.stopPropagation()}"} |
| apps/web/src/app/platform/page.tsx:357 | /platform | button | × | {"type":"\"button\"","aria-label":"\"Close onboarding\"","disabled":"{busy}","onClick":"{() => setOnboardingOpen(false)}"} |
| apps/web/src/app/platform/page.tsx:365 | /platform | button |  | {"className":"\"enterprise-action-button platform-onboarding-wide\"","disabled":"{busy}"} |
| apps/web/src/app/platform-services/page.tsx:65 | /platform-services | article |  | {} |
| apps/web/src/app/platform-services/page.tsx:65 | /platform-services | article |  | {} |
| apps/web/src/app/platform-services/page.tsx:65 | /platform-services | article |  | {} |
| apps/web/src/app/platform-services/page.tsx:67 | /platform-services | ul |  | {"className":"\"platform-invoice-list\""} |
| apps/web/src/app/platform-services/page.tsx:67 | /platform-services | button | Submit payment | {"type":"\"button\"","disabled":"{busy}","className":"\"enterprise-action-button\"","onClick":"{() => void submitPayment(invoice)}"} |
| apps/web/src/app/platform-services/page.tsx:68 | /platform-services | StandardSelectField |  | {"name":"\"priority\"","value":"{priority}","onChange":"{setPriority}","placeholder":"\"Select priority\"","options":"{[\"Low\", \"Normal\", \"High\", \"Critical\"].map(value => ({ value, label: value }))}"} |
| apps/web/src/app/platform-services/page.tsx:68 | /platform-services | button | Send support request | {"disabled":"{busy}","className":"\"enterprise-action-button platform-services-wide\""} |
| apps/web/src/app/platform-services/page.tsx:70 | /platform-services | ul |  | {} |
| apps/web/src/app/platform-services/page.tsx:70 | /platform-services | button | Send update | {"type":"\"button\"","disabled":"{busy \|\| !responses[item.id]?.trim()}","className":"\"enterprise-action-button enterprise-action-button-secondary\"","onClick":"{() => void respond(item)}"} |
| apps/web/src/app/portal/page.tsx:174 | /portal | a |  | {"href":"\"/portal\"","className":"\"enterprise-brand\""} |
| apps/web/src/app/portal/page.tsx:178 | /portal | nav |  | {"className":"\"enterprise-nav-section teacher-portal-nav learner-portal-nav\"","aria-label":"\"Student workspace\""} |
| apps/web/src/app/portal/page.tsx:181 | /portal | button |  | {"key":"{k}","data-active":"{t === k}","onClick":"{() => setT(k)}"} |
| apps/web/src/app/portal/page.tsx:245 | /portal | nav |  | {"className":"\"learner-bottom-nav\""} |
| apps/web/src/app/portal/page.tsx:247 | /portal | button |  | {"key":"{k}","data-active":"{t === k}","onClick":"{() => setT(k)}"} |
| apps/web/src/app/portal/page.tsx:270 | /portal | button |  | {"type":"\"button\"","className":"\"enterprise-profile-trigger\"","onClick":"{() => setOpen((value) => !value)}","aria-label":"\"Open profile menu\"","aria-expanded":"{open}"} |
| apps/web/src/app/portal/page.tsx:270 | /portal | button | Edit profile picture | {"type":"\"button\"","className":"\"enterprise-profile-menu-action\"","onClick":"{() => input.current?.click()}"} |
| apps/web/src/app/portal/page.tsx:270 | /portal | button | Sign out | {"type":"\"button\"","className":"\"enterprise-profile-menu-action\"","onClick":"{signOut}"} |
| apps/web/src/app/portal/page.tsx:294 | /portal | button | ♢ | {"type":"\"button\"","className":"\"learner-notification-button\"","onClick":"{() => void openNotifications()}","aria-label":"\"Open notifications\"","aria-expanded":"{open}"} |
| apps/web/src/app/portal/page.tsx:295 | /portal | article |  | {"key":"{notice.id}"} |
| apps/web/src/app/portal/page.tsx:471 | /portal | article |  | {"className":"\"learner-kpi\""} |
| apps/web/src/app/portal/page.tsx:490 | /portal | article |  | {"className":"\"learner-row\""} |
| apps/web/src/app/portal/page.tsx:503 | /portal | article |  | {"className":"\"learner-row learner-session-row\""} |
| apps/web/src/app/portal/page.tsx:503 | /portal | a |  | {"className":"\"learner-join-link\"","href":"{x.meetingLink}","target":"\"_blank\"","rel":"\"noreferrer\"","aria-label":"{'${actionLabel} for ${x.batchName}'}"} |
| apps/web/src/app/portal/page.tsx:512 | /portal | button | Previous | {"type":"\"button\"","aria-label":"\"Previous month\"","onClick":"{() => setMonth(new Date(year, monthIndex - 1, 1))}"} |
| apps/web/src/app/portal/page.tsx:512 | /portal | button | Next | {"type":"\"button\"","aria-label":"\"Next month\"","onClick":"{() => setMonth(new Date(year, monthIndex + 1, 1))}"} |
| apps/web/src/app/portal/page.tsx:516 | /portal | a |  | {"className":"\"learner-calendar-event learner-calendar-join\"","href":"{session.meetingLink}","target":"\"_blank\"","rel":"\"noreferrer\"","aria-label":"{'Open class ${session.batchName}'}"} |
| apps/web/src/app/portal/page.tsx:521 | /portal | article |  | {"className":"\"learner-row learner-resource-row\""} |
| apps/web/src/app/portal/page.tsx:521 | /portal | a | Open / download | {"href":"{x.url.startsWith(\"/\") ? '${apiUrl}${x.url}' : x.url}","target":"\"_blank\"","rel":"\"noreferrer\""} |
| apps/web/src/app/portal/page.tsx:525 | /portal | article |  | {"className":"\"learner-history-row\""} |
| apps/web/src/app/portal/page.tsx:525 | /portal | button |  | {"type":"\"button\"","onClick":"{() => setExpanded(value => !value)}","aria-expanded":"{expanded}"} |
| apps/web/src/app/portal/page.tsx:530 | /portal | button | Clear | {"type":"\"button\"","onClick":"{() => { setFrom(\"\"); setTo(\"\"); }}"} |
| apps/web/src/app/portal/page.tsx:535 | /portal | article |  | {"className":"\"learner-cycle-row\""} |
| apps/web/src/app/portal/page.tsx:535 | /portal | button |  | {"type":"\"button\"","className":"\"learner-cycle-toggle\"","onClick":"{() => setExpanded(value => !value)}","aria-expanded":"{expanded}"} |
| apps/web/src/app/portal/page.tsx:544 | /portal | article |  | {"className":"\"learner-row learner-download-row\""} |
| apps/web/src/app/portal/page.tsx:544 | /portal | button |  | {"type":"\"button\"","onClick":"{() => void downloadPortalFile(path, filename).catch(() => setMessage(\"Download could not be prepared.\"))}"} |
| apps/web/src/app/portal/page.tsx:549 | /portal | button | Clear | {"type":"\"button\"","onClick":"{() => { setCycle(\"\"); setStatus(\"All\"); }}"} |
| apps/web/src/app/portal/page.tsx:580 | /portal | article |  | {"className":"\"learner-assignment\""} |
| apps/web/src/app/portal/page.tsx:592 | /portal | button | Submit | {} |
| apps/web/src/app/portal/page.tsx:750 | /portal | button | Save | {} |
| apps/web/src/app/portal-accounts/page.tsx:105 | /portal-accounts | StandardSelectField |  | {"name":"\"portal-role\"","value":"{role}","onChange":"{(nextRole) => { setRole(nextRole); setId(\"\"); }}","placeholder":"\"Account type\"","options":"{[ { value: \"Student\", label: \"Student\" }, { value: \"Guardian\", label: \"Parent\" }, { value: \"Teacher\", label: \"Teacher\" }, ]}"} |
| apps/web/src/app/portal-accounts/page.tsx:119 | /portal-accounts | StandardSelectField |  | {"name":"\"portal-person\"","value":"{id}","onChange":"{setId}","placeholder":"{'Select ${role === \"Guardian\" ? \"parent\" : role.toLowerCase()}'}","options":"{choices.map((x) => ({ value: x.id, label: '${x.firstName} ${x.lastName}', }))}"} |
| apps/web/src/app/portal-accounts/page.tsx:159 | /portal-accounts | button | Create portal account | {"className":"\"enterprise-action-button portal-access-action\""} |
| apps/web/src/app/practice-logs/page.tsx:2 | /practice-logs | button | Save log | {"className":"\"rounded bg-cyan-400 p-2 font-semibold text-slate-950\""} |
| apps/web/src/app/practice-logs/page.tsx:2 | /practice-logs | article |  | {"key":"{x.id}","className":"\"rounded bg-slate-900 p-4\""} |
| apps/web/src/app/practice-logs/page.tsx:2 | /practice-logs | button | Review | {"onClick":"{()=>onSave(v)}","className":"\"rounded bg-cyan-400 px-3 text-sm font-semibold text-slate-950\""} |
| apps/web/src/app/register/page.tsx:84 | /register | button |  | {"disabled":"{busy}","className":"\"mt-6 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60\""} |
| apps/web/src/app/reports/page.tsx:191 | /reports | StandardInteractiveTile |  | {"key":"{label}","label":"{label}","value":"{value}","detail":"\"View details\"","onClick":"{() => setDetail(key)}","className":"\"workspace-reports-kpi\""} |
| apps/web/src/app/reports/page.tsx:192 | /reports | article |  | {"className":"\"workspace-reports-kpi\""} |
| apps/web/src/app/reports/page.tsx:194 | /reports | StandardDetailModal |  | {"eyebrow":"\"Workspace reports\"","title":"\"Active students\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/reports/page.tsx:195 | /reports | StandardDetailModal |  | {"eyebrow":"\"Workspace reports\"","title":"\"Teachers\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/reports/page.tsx:196 | /reports | StandardDetailModal |  | {"eyebrow":"\"Workspace reports\"","title":"\"Scheduled classes\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/reports/page.tsx:197 | /reports | StandardDetailModal |  | {"eyebrow":"\"Workspace reports\"","title":"\"Attendance by student\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/reports/page.tsx:198 | /reports | StandardDetailModal |  | {"eyebrow":"\"Workspace reports\"","title":"\"Collected fee payments\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/reports/page.tsx:199 | /reports | StandardDetailModal |  | {"eyebrow":"\"Workspace reports\"","title":"\"Outstanding fees and salary\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/reports/page.tsx:200 | /reports | StandardDetailModal |  | {"eyebrow":"\"Workspace reports\"","title":"\"Expenses\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/reports/page.tsx:205 | /reports | button | Download CSV | {"onClick":"{() => downloadCsv(\"academydesk-batch-occupancy.csv\", [ [\"Batch\", \"Active enrolments\", \"Capacity\"], ...batchRows, ]) }","className":"\"workspace-reports-secondary-action\""} |
| apps/web/src/app/reports/page.tsx:220 | /reports | ul |  | {"className":"\"workspace-reports-list\""} |
| apps/web/src/app/reports/page.tsx:242 | /reports | button | Download CSV | {"onClick":"{() => downloadCsv(\"academydesk-invoices.csv\", [ [\"Invoice\", \"Student\", \"Amount\", \"Due date\"], ...invoices.map((invoice) => { const student = students.find( (item) => item.id === invoice.studentId, ); return [ invoice.invoiceNumber, student ? '${student.firstName} ${student.lastName}' : \"Unknown\", String(invoice.totalAmount), invoice.dueDate, ]; }), ]) }","className":"\"workspace-reports-secondary-action\""} |
| apps/web/src/app/reports/page.tsx:280 | /reports | ExportCard |  | {"title":"\"Students\"","detail":"\"Student contact register\"","onClick":"{() => downloadCsv(\"academydesk-students.csv\", [ [\"First name\", \"Last name\"], ...students.map((student) => [ student.firstName, student.lastName, ]), ]) }"} |
| apps/web/src/app/reports/page.tsx:293 | /reports | ExportCard |  | {"title":"\"Batches\"","detail":"\"Capacity and active enrolments\"","onClick":"{() => downloadCsv(\"academydesk-batches.csv\", [ [\"Batch\", \"Active enrolments\", \"Capacity\"], ...batchRows, ]) }"} |
| apps/web/src/app/reports/page.tsx:303 | /reports | ExportCard |  | {"title":"\"Invoices\"","detail":"\"Issued fees and due dates\"","onClick":"{() => downloadCsv(\"academydesk-invoices.csv\", [ [\"Invoice\", \"Student\", \"Amount\", \"Due date\"], ...invoices.map((invoice) => { const student = students.find( (item) => item.id === invoice.studentId, ); return [ invoice.invoiceNumber, student ? '${student.firstName} ${student.lastName}' : \"Unknown\", String(invoice.totalAmount), invoice.dueDate, ]; }), ]) }"} |
| apps/web/src/app/reports/page.tsx:325 | /reports | ExportCard |  | {"title":"\"Expenses\"","detail":"\"Expense ledger totals\"","onClick":"{() => downloadCsv(\"academydesk-expenses.csv\", [ [\"Expense amount\"], ...expenses.map((expense) => [String(expense.amount)]), ]) }"} |
| apps/web/src/app/reports/page.tsx:350 | /reports | article |  | {"className":"\"workspace-reports-export-card\""} |
| apps/web/src/app/reports/page.tsx:353 | /reports | button | Download CSV → | {"onClick":"{onClick}","className":"\"workspace-reports-secondary-action\""} |
| apps/web/src/app/reports/page.tsx:363 | /reports | article |  | {"key":"{'${title}-${index}'}"} |
| apps/web/src/app/resources/page.tsx:170 | /resources | StandardSelectField |  | {"name":"\"type\"","value":"{type}","onChange":"{setType}","placeholder":"\"Select type\"","options":"{resourceTypes.map((value) => ({ value, label: value === \"SheetMusic\" ? \"Sheet music\" : value, }))}"} |
| apps/web/src/app/resources/page.tsx:183 | /resources | StandardSelectField |  | {"name":"\"course\"","value":"{course}","onChange":"{setCourse}","placeholder":"\"All subjects\"","options":"{courses.map((value) => ({ value: value.id, label: '${value.name}${value.subjectArea ? ' · ${value.subjectArea}' : \"\"}', }))}"} |
| apps/web/src/app/resources/page.tsx:196 | /resources | StandardSelectField |  | {"name":"\"batch\"","value":"{batch}","onChange":"{setBatch}","placeholder":"\"All students in the subject\"","options":"{batches.map((value) => ({ value: value.id, label: value.name, }))}"} |
| apps/web/src/app/resources/page.tsx:207 | /resources | button | Publish resource | {"className":"\"enterprise-action-button\""} |
| apps/web/src/app/resources/page.tsx:224 | /resources | ul |  | {"className":"\"resource-list\""} |
| apps/web/src/app/resources/page.tsx:228 | /resources | a |  | {"href":"{resource.url}","target":"\"_blank\"","rel":"\"noreferrer\""} |
| apps/web/src/app/sales-campaigns/page.tsx:150 | /sales-campaigns | StandardSelectField |  | {"name":"\"channel\"","value":"{channel}","onChange":"{setChannel}","placeholder":"\"Channel\"","options":"{channels.map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/sales-campaigns/page.tsx:158 | /sales-campaigns | StandardDateField |  | {"name":"\"start-date\"","label":"\"Start date\"","value":"{startDate}","onChange":"{setStartDate}","required":"true"} |
| apps/web/src/app/sales-campaigns/page.tsx:180 | /sales-campaigns | StandardSelectField |  | {"name":"\"status\"","value":"{status}","onChange":"{setStatus}","placeholder":"\"Status\"","options":"{statuses.map((value) => ({ value, label: value }))}"} |
| apps/web/src/app/sales-campaigns/page.tsx:189 | /sales-campaigns | button | Create campaign | {"className":"\"enterprise-action-button campaigns-create-button\"","disabled":"{!academy}"} |
| apps/web/src/app/sales-campaigns/page.tsx:208 | /sales-campaigns | ul |  | {} |
| apps/web/src/app/sales-campaigns/page.tsx:224 | /sales-campaigns | StandardSelectField |  | {"name":"{'campaign-status-${campaign.id}'}","value":"{campaign.status}","onChange":"{(nextStatus) => void update(campaign, nextStatus) }","placeholder":"\"Status\"","options":"{statuses.map((value) => ({ value, label: value, }))}"} |
| apps/web/src/app/sales-marketing/page.tsx:107 | /sales-marketing | Link |  | {"key":"{lead.id}","href":"\"/leads\""} |
| apps/web/src/app/sales-marketing/page.tsx:120 | /sales-marketing | Link |  | {"key":"{source}","href":"\"/leads\""} |
| apps/web/src/app/sales-marketing/page.tsx:154 | /sales-marketing | StandardInteractiveTile |  | {"key":"{String(name)}","label":"{name}","value":"{value}","detail":"{detail}","onClick":"{() => setTileDetail(name)}","className":"\"sales-tile\""} |
| apps/web/src/app/sales-marketing/page.tsx:170 | /sales-marketing | Link | View sources | {"href":"\"/sales-marketing?view=sources\""} |
| apps/web/src/app/sales-marketing/page.tsx:217 | /sales-marketing | StandardInteractiveTile |  | {"key":"{String(name)}","label":"{String(name)}","value":"{String(value)}","detail":"{String(detail)}","onClick":"{() => setTileDetail(String(name))}","className":"\"sales-tile\""} |
| apps/web/src/app/sales-marketing/page.tsx:233 | /sales-marketing | Link | Open trial bookings | {"href":"\"/trial-bookings\""} |
| apps/web/src/app/sales-marketing/page.tsx:259 | /sales-marketing | StandardDetailModal |  | {"eyebrow":"\"Sales & marketing\"","title":"{tileDetail}","onClose":"{() => setTileDetail(null)}"} |
| apps/web/src/app/sales-marketing/page.tsx:298 | /sales-marketing | Link | Open details | {"href":"{tileDetail === \"Leads\" \|\| tileDetail === \"Total leads\" ? \"/leads\" : tileDetail === \"Trial bookings\" ? \"/trial-bookings\" : \"/sales-marketing?view=conversion\"}","className":"\"enterprise-action-button enterprise-action-button-secondary\"","onClick":"{() => setTileDetail(null)}"} |
| apps/web/src/app/schedule/page.tsx:275 | /schedule | button | Schedule class | {"disabled":"{!academy \|\| batches.length === 0}","className":"\"mt-5 w-full rounded-lg bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 hover:bg-cyan-300 disabled:opacity-60\""} |
| apps/web/src/app/schedule/page.tsx:287 | /schedule | ul |  | {"className":"\"mt-5 space-y-3\""} |
| apps/web/src/app/staff/page.tsx:194 | /staff | StandardSelectField |  | {"name":"\"staff-role\"","value":"{role}","onChange":"{setRole}","placeholder":"\"Access role\"","options":"{staffRoles.map((value) => ({ value, label: roleLabel(value), }))}"} |
| apps/web/src/app/staff/page.tsx:204 | /staff | button | Create staff account | {"disabled":"{!academy}","className":"\"enterprise-action-button staff-create-button\""} |
| apps/web/src/app/staff/page.tsx:225 | /staff | ul |  | {} |
| apps/web/src/app/staff/page.tsx:235 | /staff | StandardSelectField |  | {"name":"{'staff-role-${person.id}'}","value":"{person.roles[0] ?? \"Operations\"}","onChange":"{(nextRole) => void updateRole(person, nextRole) }","placeholder":"\"Access role\"","options":"{staffRoles.map((value) => ({ value, label: roleLabel(value), }))}","disabled":"{ !person.isActive \|\| updatingStaffId === person.id }"} |
| apps/web/src/app/staff/page.tsx:254 | /staff | button | Offboard | {"onClick":"{() => void offboard(person)}","className":"\"staff-offboard-button\""} |
| apps/web/src/app/student-fees/page.tsx:76 | /student-fees | StandardSelectField |  | {"name":"\"fee-student\"","value":"{studentId}","onChange":"{setStudentId}","placeholder":"\"Select student\"","options":"{students.map((student) => ({ value: student.id, label: '${student.firstName} ${student.lastName}${student.isActive ? \"\" : \" (Inactive)\"}' }))}"} |
| apps/web/src/app/student-fees/page.tsx:93 | /student-fees | button |  | {"disabled":"{savingAdmission}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/student-fees/page.tsx:99 | /student-fees | StandardDateField |  | {"name":"\"admissionDueDateDisplay\"","label":"\"Due date\"","value":"{admissionDueDate}","onChange":"{setAdmissionDueDate}"} |
| apps/web/src/app/student-onboarding/page.tsx:103 | /student-onboarding | StandardSelectField |  | {"name":"\"gender\"","value":"{gender}","onChange":"{setGender}","placeholder":"\"Gender (optional)\"","options":"{[{ value: \"Female\", label: \"Female\" }, { value: \"Male\", label: \"Male\" }, { value: \"Non-binary\", label: \"Non-binary\" }, { value: \"Prefer not to say\", label: \"Prefer not to say\" }]}"} |
| apps/web/src/app/student-onboarding/page.tsx:104 | /student-onboarding | StandardDateField |  | {"required":"true","name":"\"dateOfBirth\"","label":"\"Date of birth (DOB)\"","value":"{dob}","onChange":"{setDob}"} |
| apps/web/src/app/student-onboarding/page.tsx:105 | /student-onboarding | StandardDateField |  | {"name":"\"admissionDate\"","label":"\"Admission date\"","value":"{admissionDate}","onChange":"{setAdmissionDate}"} |
| apps/web/src/app/student-onboarding/page.tsx:211 | /student-onboarding | button |  | {"disabled":"{saving \|\| !academy}","className":"\"student-onboarding-submit\""} |
| apps/web/src/app/student-overview/page.tsx:121 | /student-overview | Link | Add student | {"href":"\"/student-onboarding\"","className":"\"enterprise-action-button\""} |
| apps/web/src/app/student-overview/page.tsx:122 | /student-overview | Link | Open Student 360 | {"href":"\"/student-management\"","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/student-overview/page.tsx:129 | /student-overview | StandardInteractiveTile |  | {"label":"\"Total students\"","value":"{summary.totalStudents.toLocaleString(\"en-IN\")}","detail":"{'${summary.activeStudents} active · ${summary.inactiveStudents} inactive'}","onClick":"{() => setDetail(\"students\")}","className":"\"student-overview-kpi\""} |
| apps/web/src/app/student-overview/page.tsx:130 | /student-overview | StandardInteractiveTile |  | {"label":"\"Total fees\"","value":"{money(summary.totalSubjectFees)}","detail":"{'${summary.activeSubjectFeeArrangements} active subject fees'}","onClick":"{() => setDetail(\"fees\")}","className":"\"student-overview-kpi\""} |
| apps/web/src/app/student-overview/page.tsx:131 | /student-overview | StandardInteractiveTile |  | {"label":"\"Total admission fees\"","value":"{money(summary.totalAdmissionFees)}","detail":"\"View admission fee by student\"","onClick":"{() => setDetail(\"admission\")}","className":"\"student-overview-kpi\""} |
| apps/web/src/app/student-overview/page.tsx:132 | /student-overview | StandardInteractiveTile |  | {"label":"\"Outstanding fees\"","value":"{money(summary.outstandingFees)}","detail":"{'${money(summary.overdueFees)} overdue'}","onClick":"{() => setDetail(\"outstanding\")}","className":"{'student-overview-kpi ${summary.overdueFees > 0 ? \"student-overview-kpi-warning\" : \"\"}'}"} |
| apps/web/src/app/student-overview/page.tsx:134 | /student-overview | StandardDetailModal |  | {"eyebrow":"\"Student overview\"","title":"\"Students\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/student-overview/page.tsx:135 | /student-overview | StandardDetailModal |  | {"eyebrow":"\"Student overview\"","title":"\"Active student fees\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/student-overview/page.tsx:136 | /student-overview | StandardDetailModal |  | {"eyebrow":"\"Student overview\"","title":"\"Admission fees\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/student-overview/page.tsx:137 | /student-overview | StandardDetailModal |  | {"eyebrow":"\"Student overview\"","title":"\"Outstanding student fees\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/student-overview/page.tsx:150 | /student-overview | article |  | {"className":"\"student-overview-insight\""} |
| apps/web/src/app/student-overview/page.tsx:150 | /student-overview | ul |  | {} |
| apps/web/src/app/student-overview/page.tsx:152 | /student-overview | article |  | {"key":"{'${title}-${index}'}"} |
| apps/web/src/app/student-profile/page.tsx:158 | /student-profile | StandardSelectField |  | {"name":"\"student-record\"","value":"{studentId}","onChange":"{(value) => void select(value)}","placeholder":"\"Select student\"","options":"{students.map((item) => ({ value: item.id, label: '${item.firstName} ${item.lastName}' }))}"} |
| apps/web/src/app/student-profile/page.tsx:166 | /student-profile | Link | Enrol student | {"href":"{'/enrollments?studentId=${studentId}'}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/student-profile/page.tsx:172 | /student-profile | Link | Student management | {"href":"\"/students\"","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/student-profile/page.tsx:182 | /student-profile | StandardInteractiveTile |  | {"className":"\"student-summary-tile\"","label":"\"Enrolments\"","value":"{count(profile.enrollments)}","detail":"{profile.enrollments?.map((item) => item.courseName).filter((value, index, values) => values.indexOf(value) === index).join(\" · \") \|\| \"No enrolled subjects\"}","onClick":"{() => setDetail(\"enrolments\")}"} |
| apps/web/src/app/student-profile/page.tsx:183 | /student-profile | StandardInteractiveTile |  | {"className":"\"student-summary-tile\"","label":"\"Attendance\"","value":"{attendanceCount(profile.attendance)}","detail":"{formatAttendance(profile.currentMonthAttendance) + \" this month\"}","onClick":"{() => setDetail(\"attendance\")}"} |
| apps/web/src/app/student-profile/page.tsx:184 | /student-profile | StandardInteractiveTile |  | {"className":"\"student-summary-tile\"","label":"\"Fees\"","value":"{'₹${outstanding.toLocaleString(\"en-IN\")}'}","detail":"{'₹${upcoming.toLocaleString(\"en-IN\")} upcoming · ₹${overdue.toLocaleString(\"en-IN\")} overdue'}","onClick":"{() => setDetail(\"fees\")}"} |
| apps/web/src/app/student-profile/page.tsx:185 | /student-profile | StandardInteractiveTile |  | {"className":"\"student-summary-tile\"","label":"\"Family\"","value":"{count(profile.guardians)}","detail":"\"View parent details\"","onClick":"{() => setDetail(\"family\")}"} |
| apps/web/src/app/student-profile/page.tsx:206 | /student-profile | Link | Review assignments and submissions | {"href":"\"/assignments\"","className":"\"enterprise-action-button enterprise-action-button-secondary mt-4\""} |
| apps/web/src/app/student-profile/page.tsx:219 | /student-profile | Link | Contact preferences | {"href":"\"/communication-preferences\"","className":"\"enterprise-action-button enterprise-action-button-secondary mt-4\""} |
| apps/web/src/app/student-profile/page.tsx:228 | /student-profile | StandardDetailModal |  | {"title":"\"Enrolled subjects\"","eyebrow":"\"Student 360\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/student-profile/page.tsx:229 | /student-profile | StandardDetailModal |  | {"title":"\"Attendance\"","eyebrow":"\"Student 360\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/student-profile/page.tsx:230 | /student-profile | StandardDetailModal |  | {"title":"\"Fee payments\"","eyebrow":"\"Student 360\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/student-profile/page.tsx:231 | /student-profile | StandardDetailModal |  | {"title":"\"Family\"","eyebrow":"\"Student 360\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/students/page.tsx:125 | /students | button | Active | {"onClick":"{() => setStatus(\"Active\")}","aria-selected":"{status === \"Active\"}"} |
| apps/web/src/app/students/page.tsx:131 | /students | button | Inactive | {"onClick":"{() => setStatus(\"Inactive\")}","aria-selected":"{status === \"Inactive\"}"} |
| apps/web/src/app/students/page.tsx:164 | /students | table |  | {"className":"\"w-full min-w-[720px] text-left text-sm\""} |
| apps/web/src/app/students/page.tsx:204 | /students | Link | Open record | {"className":"\"text-cyan-300\"","href":"{'/student-profile?studentId=${student.id}'}"} |
| apps/web/src/app/students/page.tsx:210 | /students | button |  | {"disabled":"{saving}","onClick":"{() => void toggle(student)}","className":"\"text-slate-400 hover:text-slate-100\""} |
| apps/web/src/app/students/page.tsx:235 | /students | button | Assign batch | {"disabled":"{saving \|\| !assignmentStudentId \|\| !assignmentBatchId}","onClick":"{() => void assignBatch()}","className":"\"student-management-assign-button\""} |
| apps/web/src/app/students/page.tsx:244 | /students | StandardSelectField |  | {"name":"\"assignment-student\"","value":"{assignmentStudentId}","onChange":"{setAssignmentStudentId}","placeholder":"\"Select student\"","options":"{students.filter((student) => student.isActive).map((student) => ({ value: student.id, label: '${student.firstName} ${student.lastName}' }))}"} |
| apps/web/src/app/students/page.tsx:251 | /students | StandardSelectField |  | {"name":"\"assignment-batch\"","value":"{assignmentBatchId}","onChange":"{setAssignmentBatchId}","placeholder":"\"Select class or batch\"","options":"{batches.filter((batch) => batch.isActive && batch.enrollmentStatus === \"Open\").map((batch) => ({ value: batch.id, label: batch.name }))}"} |
| apps/web/src/app/submission-review/page.tsx:87 | /submission-review | ul |  | {} |
| apps/web/src/app/submission-review/page.tsx:101 | /submission-review | button | Review | {"type":"\"button\"","onClick":"{() => void review(submission.id)}"} |
| apps/web/src/app/teacher/page.tsx:121 | /teacher | a |  | {"href":"\"/teacher\"","className":"\"enterprise-brand\""} |
| apps/web/src/app/teacher/page.tsx:125 | /teacher | nav |  | {"className":"\"enterprise-nav-section teacher-portal-nav\"","aria-label":"\"Teacher workspace\""} |
| apps/web/src/app/teacher/page.tsx:128 | /teacher | button |  | {"key":"{k}","data-active":"{t === k}","onClick":"{() => { if (k === \"classroom\") { setSid(\"\"); setRoster([]); setAttendance([]); } setT(k); }}"} |
| apps/web/src/app/teacher/page.tsx:190 | /teacher | button |  | {"className":"\"teacher-timetable-row\"","key":"{x.id}","onClick":"{() => { setSid(x.id); void loadRoster(x.id); setT(\"classroom\"); }}"} |
| apps/web/src/app/teacher/page.tsx:217 | /teacher | nav |  | {"className":"\"learner-bottom-nav\""} |
| apps/web/src/app/teacher/page.tsx:219 | /teacher | button |  | {"key":"{k}","data-active":"{t === k}","onClick":"{() => setT(k)}"} |
| apps/web/src/app/teacher/page.tsx:229 | /teacher | ul |  | {} |
| apps/web/src/app/teacher/page.tsx:258 | /teacher | button |  | {"type":"\"button\"","className":"\"enterprise-profile-trigger\"","onClick":"{() => { setMessage(\"\"); setOpen((value) => !value); }}","aria-label":"\"Open profile menu\"","aria-expanded":"{open}"} |
| apps/web/src/app/teacher/page.tsx:261 | /teacher | button | Edit profile picture | {"type":"\"button\"","className":"\"enterprise-profile-menu-action\"","onClick":"{() => inputRef.current?.click()}"} |
| apps/web/src/app/teacher/page.tsx:261 | /teacher | button | Sign out | {"type":"\"button\"","className":"\"enterprise-profile-menu-action\"","onClick":"{signOut}"} |
| apps/web/src/app/teacher/page.tsx:266 | /teacher | article |  | {"className":"\"learner-kpi\""} |
| apps/web/src/app/teacher/page.tsx:275 | /teacher | article |  | {"className":"\"learner-row\""} |
| apps/web/src/app/teacher/page.tsx:317 | /teacher | button | ‹ | {"type":"\"button\"","aria-label":"\"Previous month\"","onClick":"{() => setMonth((current) => new Date(current.getFullYear(), current.getMonth() - 1, 1))}"} |
| apps/web/src/app/teacher/page.tsx:317 | /teacher | button | Today | {"type":"\"button\"","onClick":"{() => { const today = new Date(); setMonth(new Date(today.getFullYear(), today.getMonth(), 1)); setSelectedDay(indiaDateKey(today)); }}"} |
| apps/web/src/app/teacher/page.tsx:317 | /teacher | button | › | {"type":"\"button\"","aria-label":"\"Next month\"","onClick":"{() => setMonth((current) => new Date(current.getFullYear(), current.getMonth() + 1, 1))}"} |
| apps/web/src/app/teacher/page.tsx:322 | /teacher | button |  | {"type":"\"button\"","key":"{key}","className":"\"teacher-calendar-day\"","data-selected":"{selectedDay === key}","data-holiday":"{Boolean(holiday)}","onClick":"{() => setSelectedDay(key)}"} |
| apps/web/src/app/teacher/page.tsx:322 | /teacher | span |  | {"key":"{session.id}","onClick":"{(event) => { event.stopPropagation(); onOpen(session.id); }}"} |
| apps/web/src/app/teacher/page.tsx:324 | /teacher | button |  | {"type":"\"button\"","key":"{session.id}","onClick":"{() => onOpen(session.id)}"} |
| apps/web/src/app/teacher/page.tsx:339 | /teacher | AttendanceStatusMenu |  | {"value":"{teacherDraft}","disabled":"{busy}","label":"\"Teacher attendance\"","onChange":"{setTeacherDraft}"} |
| apps/web/src/app/teacher/page.tsx:343 | /teacher | article |  | {"key":"{student.id}"} |
| apps/web/src/app/teacher/page.tsx:343 | /teacher | AttendanceStatusMenu |  | {"value":"{value}","disabled":"{busy}","label":"{'Attendance for ${student.firstName} ${student.lastName}'}","onChange":"{(status) => setDraft((current) => ({ ...current, [student.id]: status }))}"} |
| apps/web/src/app/teacher/page.tsx:345 | /teacher | button |  | {"type":"\"button\"","className":"\"enterprise-action-button\"","disabled":"{busy \|\| !sessionId \|\| !attendanceReady}","onClick":"{() => void onSubmit(teacherDraft, roster.map((student) => ({ studentId: student.id, status: draft[student.id] ?? \"\" })))}"} |
| apps/web/src/app/teacher/page.tsx:358 | /teacher | button |  | {"type":"\"button\"","aria-label":"{label}","aria-haspopup":"\"listbox\"","aria-expanded":"{open}","disabled":"{disabled}","onClick":"{() => setOpen((current) => !current)}"} |
| apps/web/src/app/teacher/page.tsx:358 | /teacher | button |  | {"type":"\"button\"","role":"\"option\"","aria-selected":"{value === status}","key":"{status}","data-active":"{value === status}","onClick":"{() => { onChange(status); setOpen(false); }}"} |
| apps/web/src/app/teacher/page.tsx:375 | /teacher | button |  | {"type":"\"button\"","aria-label":"{label}","aria-haspopup":"\"listbox\"","aria-expanded":"{open}","onClick":"{() => setOpen((currentOpen) => !currentOpen)}"} |
| apps/web/src/app/teacher/page.tsx:376 | /teacher | button |  | {"type":"\"button\"","role":"\"option\"","aria-selected":"{option.value === value}","data-active":"{option.value === value}","key":"{option.value}","onClick":"{() => { onChange(option.value); setOpen(false); }}"} |
| apps/web/src/app/teacher/page.tsx:405 | /teacher | button |  | {"type":"\"button\"","className":"\"enterprise-action-button\"","onClick":"{() => void startOnlineClass()}"} |
| apps/web/src/app/teacher/page.tsx:405 | /teacher | button | Start class | {"type":"\"button\"","className":"\"enterprise-action-button\"","onClick":"{() => void updateStatus(\"InProgress\")}"} |
| apps/web/src/app/teacher/page.tsx:405 | /teacher | button | Complete class | {"type":"\"button\"","className":"\"enterprise-action-button enterprise-action-button-secondary\"","onClick":"{() => void updateStatus(\"Completed\")}"} |
| apps/web/src/app/teacher/page.tsx:463 | /teacher | button | Save note | {} |
| apps/web/src/app/teacher/page.tsx:464 | /teacher | button | Remove | {"type":"\"button\"","className":"\"teacher-clear-file\"","onClick":"{() => stageFile(null)}"} |
| apps/web/src/app/teacher/page.tsx:464 | /teacher | button | Upload confirmed file | {"disabled":"{!pendingFile}"} |
| apps/web/src/app/teacher/page.tsx:464 | /teacher | button | 🎙 | {"type":"\"button\"","className":"\"teacher-mic-button\"","aria-label":"\"Start audio recording\"","title":"\"Start audio recording\"","onClick":"{() => void toggleRecording()}"} |
| apps/web/src/app/teacher/page.tsx:464 | /teacher | button |  | {"type":"\"button\"","className":"\"enterprise-action-button enterprise-action-button-secondary\"","onClick":"{toggleRecordingPause}"} |
| apps/web/src/app/teacher/page.tsx:464 | /teacher | button | Stop | {"type":"\"button\"","className":"\"teacher-stop-button\"","onClick":"{() => void toggleRecording()}"} |
| apps/web/src/app/teacher/page.tsx:466 | /teacher | button | Clear | {"type":"\"button\"","onClick":"{() => { setFromDate(\"\"); setToDate(\"\"); }}"} |
| apps/web/src/app/teacher/page.tsx:466 | /teacher | article |  | {"key":"{item.id}"} |
| apps/web/src/app/teacher/page.tsx:466 | /teacher | a |  | {"href":"{'${apiUrl}${item.url}'}","target":"\"_blank\"","rel":"\"noreferrer\""} |
| apps/web/src/app/teacher/page.tsx:466 | /teacher | article |  | {"key":"{item.id}"} |
| apps/web/src/app/teacher/page.tsx:481 | /teacher | button | All months | {"type":"\"button\"","onClick":"{() => setPayslipMonth(\"\")}"} |
| apps/web/src/app/teacher/page.tsx:481 | /teacher | article |  | {"key":"{item.id}"} |
| apps/web/src/app/teacher/page.tsx:481 | /teacher | button | View | {"type":"\"button\"","onClick":"{() => setViewing(item)}"} |
| apps/web/src/app/teacher/page.tsx:481 | /teacher | button | Download PDF | {"type":"\"button\"","onClick":"{() => download(item)}"} |
| apps/web/src/app/teacher/page.tsx:481 | /teacher | button | Available after approval | {"type":"\"button\"","disabled":"true"} |
| apps/web/src/app/teacher/page.tsx:481 | /teacher | article |  | {} |
| apps/web/src/app/teacher/page.tsx:481 | /teacher | button | × | {"type":"\"button\"","aria-label":"\"Close payslip\"","onClick":"{() => setViewing(null)}"} |
| apps/web/src/app/teacher/page.tsx:481 | /teacher | button | Download PDF | {"type":"\"button\"","onClick":"{() => download(viewing)}"} |
| apps/web/src/app/teacher/page.tsx:494 | /teacher | article |  | {} |
| apps/web/src/app/teacher/page.tsx:494 | /teacher | article |  | {} |
| apps/web/src/app/teacher/page.tsx:494 | /teacher | article |  | {"data-pending":"{pending.length > 0}"} |
| apps/web/src/app/teacher/page.tsx:494 | /teacher | article |  | {"key":"{item.id}"} |
| apps/web/src/app/teacher/page.tsx:502 | /teacher | article |  | {"key":"{batch.batchId}"} |
| apps/web/src/app/teacher/page.tsx:577 | /teacher | button | Assign homework | {} |
| apps/web/src/app/teacher/page.tsx:588 | /teacher | button | Save lesson plan | {} |
| apps/web/src/app/teacher/page.tsx:608 | /teacher | button | Create assessment | {} |
| apps/web/src/app/teacher/page.tsx:615 | /teacher | article |  | {"className":"\"learner-row\"","key":"{log.id}"} |
| apps/web/src/app/teacher/page.tsx:619 | /teacher | button |  | {"disabled":"{reviewing === log.id}","onClick":"{() => void reviewPractice(log)}"} |
| apps/web/src/app/teacher/page.tsx:659 | /teacher | button | Update profile picture | {"type":"\"button\"","onClick":"{() => imageInput.current?.click()}"} |
| apps/web/src/app/teacher/page.tsx:671 | /teacher | button | Save profile | {} |
| apps/web/src/app/teacher/page.tsx:682 | /teacher | button | Send leave request | {} |
| apps/web/src/app/teacher/page.tsx:685 | /teacher | article |  | {"key":"{request.id}"} |
| apps/web/src/app/teacher-onboarding/page.tsx:105 | /teacher-onboarding | button | Add subject | {"type":"\"button\"","className":"\"teacher-inline-button\"","onClick":"{() => setSubjects((current) => [...current, { subject: \"\", certification: \"\" }])}"} |
| apps/web/src/app/teacher-onboarding/page.tsx:109 | /teacher-onboarding | button | Remove | {"type":"\"button\"","className":"\"teacher-remove-button\"","onClick":"{() => setSubjects((current) => current.filter((_, subjectIndex) => subjectIndex !== index))}"} |
| apps/web/src/app/teacher-onboarding/page.tsx:112 | /teacher-onboarding | StandardSelectField |  | {"name":"\"employmentType\"","value":"{employmentType}","onChange":"{setEmploymentType}","placeholder":"\"Employment type\"","options":"{[ { value: \"Full-time\", label: \"Full-time\" }, { value: \"Part-time\", label: \"Part-time\" }, { value: \"Contract\", label: \"Contract\" }, ]}"} |
| apps/web/src/app/teacher-onboarding/page.tsx:123 | /teacher-onboarding | StandardDateField |  | {"name":"\"dateOfBirth\"","label":"\"Date of birth (DOB)\"","value":"{dateOfBirth}","onChange":"{setDateOfBirth}"} |
| apps/web/src/app/teacher-onboarding/page.tsx:124 | /teacher-onboarding | StandardDateField |  | {"name":"\"joiningDate\"","label":"\"Joining date\"","value":"{joiningDate}","onChange":"{setJoiningDate}"} |
| apps/web/src/app/teacher-onboarding/page.tsx:135 | /teacher-onboarding | button |  | {"className":"\"teacher-onboarding-submit\"","disabled":"{saving \|\| !academy}"} |
| apps/web/src/app/teacher-overview/page.tsx:64 | /teacher-overview | Link | Add teacher | {"href":"\"/teacher-onboarding\"","className":"\"enterprise-action-button\""} |
| apps/web/src/app/teacher-overview/page.tsx:66 | /teacher-overview | StandardInteractiveTile |  | {"label":"\"Total teachers\"","value":"{teachers.length}","detail":"{'${active} active · ${teachers.length - active} inactive'}","onClick":"{() => setDetail(\"teachers\")}","className":"\"teacher-overview-kpi\""} |
| apps/web/src/app/teacher-overview/page.tsx:66 | /teacher-overview | StandardInteractiveTile |  | {"label":"\"Payment details due\"","value":"{pendingPayroll.length}","detail":"\"View teacher payments due\"","onClick":"{() => setDetail(\"due\")}","className":"\"teacher-overview-kpi\""} |
| apps/web/src/app/teacher-overview/page.tsx:66 | /teacher-overview | StandardInteractiveTile |  | {"label":"\"Teaching team\"","value":"{active}","detail":"\"View teachers and subjects\"","onClick":"{() => setDetail(\"team\")}","className":"\"teacher-overview-kpi\""} |
| apps/web/src/app/teacher-overview/page.tsx:66 | /teacher-overview | StandardInteractiveTile |  | {"label":"\"Next pay cycle\"","value":"\"Upcoming\"","detail":"\"View upcoming teacher payments\"","onClick":"{() => setDetail(\"upcoming\")}","className":"\"teacher-overview-kpi\""} |
| apps/web/src/app/teacher-overview/page.tsx:67 | /teacher-overview | StandardDetailModal |  | {"eyebrow":"\"Teacher overview\"","title":"\"Teachers\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teacher-overview/page.tsx:68 | /teacher-overview | StandardDetailModal |  | {"eyebrow":"\"Teacher overview\"","title":"\"Teacher payments due\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teacher-overview/page.tsx:69 | /teacher-overview | StandardDetailModal |  | {"eyebrow":"\"Teacher overview\"","title":"\"Teaching team\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teacher-overview/page.tsx:70 | /teacher-overview | StandardDetailModal |  | {"eyebrow":"\"Teacher overview\"","title":"\"Upcoming teacher payments\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teacher-overview/page.tsx:73 | /teacher-overview | article |  | {"className":"\"teacher-overview-insight\""} |
| apps/web/src/app/teacher-overview/page.tsx:74 | /teacher-overview | article |  | {"key":"{'${title}-${index}'}"} |
| apps/web/src/app/teacher-payments/page.tsx:37 | /teacher-payments | StandardSelectField |  | {"name":"\"payment-teacher\"","value":"{teacherId}","onChange":"{(value) => void select(value)}","placeholder":"\"Select teacher\"","options":"{teachers.map((teacher) => ({ value: teacher.id, label: '${teacher.firstName} ${teacher.lastName}${teacher.isActive ? \"\" : \" (Inactive)\"}' }))}"} |
| apps/web/src/app/teacher-payments/page.tsx:47 | /teacher-payments | button |  | {"disabled":"{saving}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/teacher-payments/page.tsx:49 | /teacher-payments | StandardSelectField |  | {"name":"\"payment-model\"","value":"{form.model ?? \"Monthly\"}","onChange":"{(value) => set(\"model\", value)}","placeholder":"\"Payment model\"","options":"{[{ value: \"Monthly\", label: \"Monthly salary\" }, { value: \"Hourly\", label: \"Hourly rates\" }]}"} |
| apps/web/src/app/teacher-payments/page.tsx:51 | /teacher-payments | StandardDateField |  | {"name":"\"effective-from\"","label":"\"Effective from\"","value":"{form.effectiveFrom ?? \"\"}","onChange":"{(value) => set(\"effectiveFrom\", value)}"} |
| apps/web/src/app/teacher-profile/page.tsx:163 | /teacher-profile | StandardSelectField |  | {"name":"\"teacher-record\"","value":"{teacherId}","onChange":"{(value) => void select(value)}","placeholder":"\"Select teacher\"","options":"{teachers.map((teacher) => ({ value: teacher.id, label: '${teacher.firstName} ${teacher.lastName}' }))}"} |
| apps/web/src/app/teacher-profile/page.tsx:179 | /teacher-profile | StandardInteractiveTile |  | {"label":"\"Assigned batches\"","value":"{count(profile.batches)}","detail":"\"View batch and cycle details\"","onClick":"{() => setDetail(\"batches\")}","className":"\"teacher-profile-summary-tile\""} |
| apps/web/src/app/teacher-profile/page.tsx:180 | /teacher-profile | StandardInteractiveTile |  | {"label":"\"Class sessions\"","value":"{count(profile.classes)}","detail":"\"View completed and pending sessions\"","onClick":"{() => setDetail(\"sessions\")}","className":"\"teacher-profile-summary-tile\""} |
| apps/web/src/app/teacher-profile/page.tsx:181 | /teacher-profile | article |  | {"className":"\"teacher-profile-summary-tile\""} |
| apps/web/src/app/teacher-profile/page.tsx:187 | /teacher-profile | StandardInteractiveTile |  | {"label":"\"Students assigned\"","value":"{count(profile.students)}","detail":"\"View students and their subjects\"","onClick":"{() => setDetail(\"students\")}","className":"\"teacher-profile-summary-tile\""} |
| apps/web/src/app/teacher-profile/page.tsx:192 | /teacher-profile | article |  | {} |
| apps/web/src/app/teacher-profile/page.tsx:193 | /teacher-profile | article |  | {} |
| apps/web/src/app/teacher-profile/page.tsx:194 | /teacher-profile | article |  | {} |
| apps/web/src/app/teacher-profile/page.tsx:200 | /teacher-profile | button |  | {"onClick":"{() => void save()}","disabled":"{saving}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/teacher-profile/page.tsx:221 | /teacher-profile | StandardSelectField |  | {"name":"\"teacher-employment-type\"","value":"{form.employmentType ?? \"\"}","onChange":"{(value) => set(\"employmentType\", value)}","placeholder":"\"Employment type\"","options":"{[{ value: \"Full-time\", label: \"Full-time\" }, { value: \"Part-time\", label: \"Part-time\" }, { value: \"Contract\", label: \"Contract\" }, { value: \"Visiting faculty\", label: \"Visiting faculty\" }]}"} |
| apps/web/src/app/teacher-profile/page.tsx:228 | /teacher-profile | StandardDateField |  | {"name":"\"teacher-date-of-birth\"","label":"\"Date of birth (DOB)\"","value":"{form.dateOfBirth ?? \"\"}","onChange":"{(value) => set(\"dateOfBirth\", value)}"} |
| apps/web/src/app/teacher-profile/page.tsx:229 | /teacher-profile | StandardDateField |  | {"name":"\"teacher-joining-date\"","label":"\"Joining date\"","value":"{form.joiningDate ?? \"\"}","onChange":"{(value) => set(\"joiningDate\", value)}"} |
| apps/web/src/app/teacher-profile/page.tsx:280 | /teacher-profile | StandardDetailModal |  | {"eyebrow":"\"Teacher 360\"","title":"\"Assigned batches\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teacher-profile/page.tsx:281 | /teacher-profile | article |  | {"key":"{batch.id}"} |
| apps/web/src/app/teacher-profile/page.tsx:283 | /teacher-profile | StandardDetailModal |  | {"eyebrow":"\"Teacher 360\"","title":"\"Class sessions\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teacher-profile/page.tsx:284 | /teacher-profile | article |  | {"key":"{session.id}"} |
| apps/web/src/app/teacher-profile/page.tsx:286 | /teacher-profile | StandardDetailModal |  | {"eyebrow":"\"Teacher 360\"","title":"\"Students assigned\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teacher-profile/page.tsx:287 | /teacher-profile | article |  | {"key":"{'${student.id}-${student.batchName}'}"} |
| apps/web/src/app/teachers/page.tsx:202 | /teachers | StandardInteractiveTile |  | {"label":"\"Active teachers\"","value":"{activeTeachers.length}","detail":"\"View active teacher names\"","onClick":"{() => setDetail(\"active\")}","className":"\"teacher-management-kpi\""} |
| apps/web/src/app/teachers/page.tsx:203 | /teachers | StandardInteractiveTile |  | {"label":"\"Due this cycle\"","value":"{pendingPayroll.length}","detail":"\"View teacher payments due\"","onClick":"{() => setDetail(\"due\")}","className":"\"teacher-management-kpi\""} |
| apps/web/src/app/teachers/page.tsx:204 | /teachers | StandardInteractiveTile |  | {"label":"\"Next payment cycle\"","value":"{new Intl.DateTimeFormat(\"en-IN\", { day: \"2-digit\", month: \"short\", timeZone: \"Asia/Kolkata\" }).format(nextPayCycle)}","detail":"\"View upcoming teacher payments\"","onClick":"{() => setDetail(\"upcoming\")}","className":"\"teacher-management-kpi\""} |
| apps/web/src/app/teachers/page.tsx:205 | /teachers | article |  | {"className":"\"teacher-management-assignment-tile\""} |
| apps/web/src/app/teachers/page.tsx:208 | /teachers | button | Save | {"disabled":"{saving \|\| !assignmentTeacherId \|\| !assignmentBatchId}","onClick":"{() => void assignBatch()}","className":"\"teacher-assignment-confirm\""} |
| apps/web/src/app/teachers/page.tsx:217 | /teachers | StandardSelectField |  | {"name":"\"assignment-teacher\"","value":"{assignmentTeacherId}","onChange":"{setAssignmentTeacherId}","placeholder":"\"Select teacher\"","options":"{activeTeachers.map((teacher) => ({ value: teacher.id, label: '${teacher.firstName} ${teacher.lastName}' }))}"} |
| apps/web/src/app/teachers/page.tsx:226 | /teachers | StandardSelectField |  | {"name":"\"assignment-batch\"","value":"{assignmentBatchId}","onChange":"{setAssignmentBatchId}","placeholder":"\"Select class or batch\"","options":"{batches.filter((batch) => batch.isActive).map((batch) => ({ value: batch.id, label: batch.name }))}"} |
| apps/web/src/app/teachers/page.tsx:236 | /teachers | StandardDetailModal |  | {"eyebrow":"\"Teacher management\"","title":"\"Active teachers\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teachers/page.tsx:237 | /teachers | StandardDetailModal |  | {"eyebrow":"\"Teacher management\"","title":"\"Teacher payments due\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teachers/page.tsx:238 | /teachers | StandardDetailModal |  | {"eyebrow":"\"Teacher management\"","title":"\"Upcoming teacher payments\"","onClose":"{() => setDetail(null)}"} |
| apps/web/src/app/teachers/page.tsx:244 | /teachers | ul |  | {"className":"\"teacher-directory-list\""} |
| apps/web/src/app/teachers/page.tsx:280 | /teachers | StandardSelectField |  | {"name":"\"teacher-branch\"","value":"{editBranchId}","onChange":"{setEditBranchId}","placeholder":"\"No branch assigned\"","options":"{branches.map((branch) => ({ value: branch.id, label: branch.name }))}"} |
| apps/web/src/app/teachers/page.tsx:289 | /teachers | Link | Open record | {"href":"{'/teacher-profile?teacherId=${teacher.id}'}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/teachers/page.tsx:295 | /teachers | button |  | {"onClick":"{() => void saveTeacher(teacher)}","disabled":"{savingId === teacher.id}","className":"\"enterprise-action-button\""} |
| apps/web/src/app/teachers/page.tsx:302 | /teachers | button | Cancel | {"onClick":"{() => setEditingId(null)}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/teachers/page.tsx:337 | /teachers | Link | Open Teacher 360 | {"href":"{'/teacher-profile?teacherId=${teacher.id}'}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/teachers/page.tsx:343 | /teachers | button | Edit | {"onClick":"{() => beginEdit(teacher)}","className":"\"enterprise-action-button enterprise-action-button-secondary\""} |
| apps/web/src/app/teachers/page.tsx:349 | /teachers | button |  | {"onClick":"{() => void toggleActive(teacher)}","disabled":"{savingId === teacher.id}","className":"\"teacher-status-action\""} |
| apps/web/src/app/teachers/page.tsx:367 | /teachers | article |  | {"key":"{'${title}-${index}'}"} |
| apps/web/src/app/trial-bookings/page.tsx:147 | /trial-bookings | StandardSelectField |  | {"name":"\"trial-lead\"","value":"{leadId}","onChange":"{setLeadId}","placeholder":"\"Select lead\"","options":"{leads.map((lead) => ({ value: lead.id, label: '${lead.fullName}${lead.programInterest ? ' · ${lead.programInterest}' : \"\"}', }))}"} |
| apps/web/src/app/trial-bookings/page.tsx:157 | /trial-bookings | StandardSelectField |  | {"name":"\"trial-teacher\"","value":"{teacherId}","onChange":"{setTeacherId}","placeholder":"\"No teacher assigned\"","options":"{teachers.map((teacher) => ({ value: teacher.id, label: '${teacher.firstName} ${teacher.lastName}', }))}"} |
| apps/web/src/app/trial-bookings/page.tsx:168 | /trial-bookings | StandardDateField |  | {"name":"\"trial-date\"","label":"\"Trial date\"","value":"{scheduledDate}","onChange":"{setScheduledDate}","required":"true"} |
| apps/web/src/app/trial-bookings/page.tsx:175 | /trial-bookings | StandardTimeField |  | {"name":"\"trial-time\"","label":"\"Start time\"","value":"{scheduledTime}","onChange":"{setScheduledTime}"} |
| apps/web/src/app/trial-bookings/page.tsx:190 | /trial-bookings | button | Book trial class | {"className":"\"enterprise-action-button trials-book-button\"","disabled":"{!academy}"} |
| apps/web/src/app/trial-bookings/page.tsx:209 | /trial-bookings | ul |  | {} |
| apps/web/src/app/trial-bookings/page.tsx:224 | /trial-bookings | StandardSelectField |  | {"name":"{'trial-status-${trial.id}'}","value":"{trial.status}","onChange":"{(status) => void update(trial, status)}","placeholder":"\"Status\"","options":"{trialStatuses.map((value) => ({ value, label: statusLabel(value), }))}"} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | article |  | {} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | article |  | {} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | article |  | {} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | StandardSelectField |  | {"name":"\"type\"","value":"{type}","onChange":"{setType}","placeholder":"\"Choose area\"","options":"{[\"Compliance\", \"Admissions\", \"Finance\", \"Academic\", \"People\", \"Operations\"].map(value => ({ value, label: value }))}"} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | StandardSelectField |  | {"name":"\"priority\"","value":"{priority}","onChange":"{setPriority}","placeholder":"\"Choose priority\"","options":"{[\"Low\", \"Normal\", \"High\", \"Critical\"].map(value => ({ value, label: value }))}"} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | StandardSelectField |  | {"name":"\"assignedUserId\"","value":"{assignedUserId}","onChange":"{setAssignedUserId}","placeholder":"\"Unassigned\"","options":"{staff.map(person => ({ value: person.id, label: '${person.displayName} · ${person.roles.join(\", \")}' }))}"} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | StandardDateField |  | {"name":"\"dueDate\"","value":"{dueDate}","onChange":"{setDueDate}","label":"\"Due date\""} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | StandardTimeField |  | {"name":"\"dueTime\"","value":"{dueTime}","onChange":"{setDueTime}","label":"\"Due time\""} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | button | Add to queue | {"className":"\"enterprise-action-button work-queue-wide\""} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | StandardSelectField |  | {"name":"\"workQueueFilter\"","value":"{filter}","onChange":"{value => { setFilter(value); void load(value); }}","placeholder":"\"Filter queue\"","options":"{filters.map(value => ({ value, label: value === \"InProgress\" ? \"In progress\" : value }))}"} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | ul |  | {"className":"\"work-queue-list\""} |
| apps/web/src/app/work-queue/page.tsx:16 | /work-queue | StandardSelectField |  | {"name":"{'status-${item.id}'}","value":"{item.status}","onChange":"{value => void move(item, value)}","placeholder":"\"Set status\"","options":"{[{ value: \"Open\", label: \"Open\" }, { value: \"InProgress\", label: \"In progress\" }, { value: \"Completed\", label: \"Completed\" }, { value: \"Cancelled\", label: \"Cancelled\" }]}"} |
| apps/web/src/components/design-system/interactive.tsx:10 |  | button |  | {"type":"\"button\"","onClick":"{onClick}","className":"{'standard-interactive-tile ${className}'.trim()}"} |
| apps/web/src/components/design-system/interactive.tsx:15 |  | article |  | {"onMouseDown":"{(event) => event.stopPropagation()}"} |
| apps/web/src/components/design-system/interactive.tsx:15 |  | button | × | {"type":"\"button\"","onClick":"{onClose}","aria-label":"\"Close details\""} |
| apps/web/src/components/design-system/portal-standard.tsx:17 |  | button |  | {"type":"\"button\"","className":"{'${style} ${className}'.trim()}"} |
| apps/web/src/components/design-system/portal-standard.tsx:21 |  | article |  | {"className":"\"learner-kpi\""} |
| apps/web/src/components/enterprise-shell.tsx:328 |  | Link |  | {"href":"{href}","data-active":"{isNavigationActive(href)}"} |
| apps/web/src/components/enterprise-shell.tsx:329 |  | button |  | {"type":"\"button\"","className":"\"enterprise-locked-link\"","onClick":"{() => setUpgradeModule(routeModule(href))}","aria-label":"{'${label} requires an upgrade'}"} |
| apps/web/src/components/enterprise-shell.tsx:394 |  | Link |  | {"href":"\"/dashboard\"","className":"\"enterprise-brand\""} |
| apps/web/src/components/enterprise-shell.tsx:398 |  | nav |  | {"className":"\"enterprise-nav-section\"","aria-label":"\"AcademyDesk modules\""} |
| apps/web/src/components/enterprise-shell.tsx:404 |  | details |  | {"key":"{group.label}","className":"\"enterprise-module-group\"","open":"{(group.links ?? group.sections?.flatMap((section) => section.links) ?? []).some(([, href]) => isNavigationActive(href))}"} |
| apps/web/src/components/enterprise-shell.tsx:409 |  | summary |  | {} |
| apps/web/src/components/enterprise-shell.tsx:422 |  | nav |  | {"className":"\"enterprise-nav-section enterprise-administration-section\"","aria-label":"\"Administration\""} |
| apps/web/src/components/enterprise-shell.tsx:427 |  | details |  | {"className":"\"enterprise-module-group enterprise-administration-group\"","open":"{activeAdministrationNavigation.some(([, href]) => isNavigationActive(href))}"} |
| apps/web/src/components/enterprise-shell.tsx:431 |  | summary |  | {} |
| apps/web/src/components/enterprise-shell.tsx:450 |  | button | ⌕ | {"type":"\"button\"","className":"\"enterprise-icon-button\"","aria-label":"\"Search AcademyDesk\"","onClick":"{() => setSearchOpen((open) => !open)}"} |
| apps/web/src/components/enterprise-shell.tsx:458 |  | Link | ♧ | {"href":"\"/communications\"","className":"\"enterprise-icon-button\"","aria-label":"\"Open communications\""} |
| apps/web/src/components/enterprise-shell.tsx:464 |  | button | ♧ | {"type":"\"button\"","className":"\"enterprise-icon-button enterprise-icon-button-locked\"","aria-label":"\"Communications requires an upgrade\"","onClick":"{() => setUpgradeModule(\"Engagement\")}"} |
| apps/web/src/components/enterprise-shell.tsx:473 |  | button | ♢ | {"type":"\"button\"","className":"\"enterprise-icon-button\"","aria-label":"\"Open notifications\"","aria-expanded":"{notificationsOpen}","onClick":"{() => setNotificationsOpen(open => !open)}"} |
| apps/web/src/components/enterprise-shell.tsx:474 |  | article |  | {"key":"{item.id}"} |
| apps/web/src/components/enterprise-shell.tsx:478 |  | button |  | {"type":"\"button\"","className":"\"enterprise-profile-trigger\"","aria-label":"\"Open account menu\"","aria-expanded":"{profileOpen}","onClick":"{() => { setProfileMessage(\"\"); setProfileOpen((open) => !open); }}"} |
| apps/web/src/components/enterprise-shell.tsx:497 |  | button | Edit profile picture | {"type":"\"button\"","className":"\"enterprise-profile-menu-action\"","onClick":"{() => profileImageInputRef.current?.click()}"} |
| apps/web/src/components/enterprise-shell.tsx:505 |  | button | Sign out | {"type":"\"button\"","className":"\"enterprise-profile-menu-action\"","onClick":"{signOut}"} |
| apps/web/src/components/enterprise-shell.tsx:528 |  | Link |  | {"key":"{href}","href":"{href}","onClick":"{() => setSearchOpen(false)}"} |
| apps/web/src/components/enterprise-shell.tsx:530 |  | button |  | {"key":"{href}","type":"\"button\"","onClick":"{() => { setSearchOpen(false); setUpgradeModule(routeModule(href)); }}"} |
| apps/web/src/components/enterprise-shell.tsx:536 |  | nav |  | {"ref":"{mobileWorkspaceNavRef}","className":"\"enterprise-mobile-workspace-nav\"","aria-label":"\"Academy workspace navigation\"","onClick":"{(event) => { const target = event.target as HTMLElement; if (!target.closest(\"a, button\")) return; const menu = mobileWorkspaceNavRef.current?.querySelector(\"details\"); if (menu) menu.open = false; }}"} |
| apps/web/src/components/enterprise-shell.tsx:547 |  | details |  | {} |
| apps/web/src/components/enterprise-shell.tsx:548 |  | summary |  | {} |
| apps/web/src/components/enterprise-shell.tsx:575 |  | button |  | {"type":"\"button\"","className":"\"enterprise-locked-content-overlay\"","onClick":"{() => setUpgradeModule(routeModule(pathname))}","aria-label":"\"Upgrade to use this feature\""} |
| apps/web/src/components/enterprise-shell.tsx:577 |  | button | Keep browsing | {"type":"\"button\"","className":"\"enterprise-action-button enterprise-action-button-secondary\"","onClick":"{() => setUpgradeModule(undefined)}"} |
| apps/web/src/components/enterprise-shell.tsx:577 |  | Link | View subscription | {"href":"\"/admin/control\"","className":"\"enterprise-action-button\"","onClick":"{() => setUpgradeModule(undefined)}"} |
| apps/web/src/components/meeting-provider-settings.tsx:49 |  | StandardSelectField |  | {"name":"\"meeting-provider\"","value":"{value.provider}","onChange":"{(provider) => onChange({ ...value, provider })}","placeholder":"\"Meeting provider\"","options":"{[ { value: \"GoogleWorkspace\", label: \"Google Workspace · Meet\" }, { value: \"Zoom\", label: \"Zoom\" }, { value: \"Microsoft365\", label: \"Microsoft 365 · Teams\" }, ]}"} |
| apps/web/src/components/meeting-provider-settings.tsx:63 |  | StandardSelectField |  | {"name":"\"meeting-status\"","value":"{value.status}","onChange":"{(status) => onChange({ ...value, status })}","placeholder":"\"Connection status\"","options":"{[ { value: \"NotConfigured\", label: \"Not configured\" }, { value: \"Configured\", label: \"Ready to connect\" }, { value: \"Disabled\", label: \"Disabled\" }, ]}"} |
| apps/web/src/components/meeting-provider-settings.tsx:108 |  | button | Save meeting provider | {"type":"\"button\"","disabled":"{disabled}","onClick":"{onSave}","className":"\"mt-5 rounded-lg bg-cyan-400 px-5 py-2.5 font-semibold text-slate-950 disabled:opacity-60\""} |
| apps/web/src/components/standard-date-field.tsx:44 |  | button |  | {"type":"\"button\"","className":"\"standard-date-trigger\"","aria-expanded":"{open}","aria-haspopup":"\"dialog\"","onClick":"{() => { if (!open && !value && month.getFullYear() === 2000) setMonth(new Date()); setOpen(current => !current); }}"} |
| apps/web/src/components/standard-date-field.tsx:48 |  | button | ‹ | {"type":"\"button\"","aria-label":"\"Previous month\"","onClick":"{() => setMonth(new Date(month.getFullYear(), month.getMonth() - 1, 1))}"} |
| apps/web/src/components/standard-date-field.tsx:48 |  | button | › | {"type":"\"button\"","aria-label":"\"Next month\"","onClick":"{() => setMonth(new Date(month.getFullYear(), month.getMonth() + 1, 1))}"} |
| apps/web/src/components/standard-date-field.tsx:50 |  | button |  | {"key":"{day.toISOString()}","type":"\"button\"","data-current":"{isCurrentMonth}","data-selected":"{isSelected}","data-today":"{isToday}","onClick":"{() => { onChange(isoDate(day)); setOpen(false); }}"} |
| apps/web/src/components/standard-date-field.tsx:51 |  | button | Clear | {"type":"\"button\"","onClick":"{() => { onChange(\"\"); setOpen(false); }}","disabled":"{required}"} |
| apps/web/src/components/standard-date-field.tsx:51 |  | button | Today | {"type":"\"button\"","onClick":"{() => { const today = new Date(); onChange(isoDate(today)); setMonth(today); setOpen(false); }}"} |
| apps/web/src/components/standard-select-field.tsx:74 |  | button |  | {"type":"\"button\"","disabled":"{disabled}","className":"\"standard-select-trigger\"","aria-expanded":"{open}","aria-haspopup":"\"listbox\"","onClick":"{() => setOpen((current) => !current)}"} |
| apps/web/src/components/standard-select-field.tsx:88 |  | button |  | {"key":"{option.value \|\| \"__placeholder\"}","type":"\"button\"","role":"\"option\"","aria-selected":"{value === option.value}","style":"{{ minHeight: 36, height: 36, maxHeight: 36, padding: \"8px 10px\", lineHeight: 1.2 }}","onClick":"{() => { onChange(option.value); setOpen(false); }}"} |
| apps/web/src/components/standard-time-field.tsx:101 |  | button |  | {"type":"\"button\"","className":"\"standard-time-trigger\"","aria-expanded":"{open}","aria-haspopup":"\"dialog\"","onClick":"{() => setOpen((current) => !current)}"} |
| apps/web/src/components/standard-time-field.tsx:124 |  | button |  | {"key":"{option}","type":"\"button\"","aria-pressed":"{currentHour === option}","onClick":"{() => setTime(option, minute, isPm)}"} |
| apps/web/src/components/standard-time-field.tsx:140 |  | button |  | {"key":"{option}","type":"\"button\"","aria-pressed":"{minute === option}","onClick":"{() => setTime(currentHour, option, isPm)}"} |
| apps/web/src/components/standard-time-field.tsx:158 |  | button |  | {"key":"{String(period)}","type":"\"button\"","aria-pressed":"{isPm === periodIsPm}","onClick":"{() => setTime(currentHour, minute, Boolean(periodIsPm)) }"} |
| apps/web/src/components/student-admin-profile.tsx:94 |  | button |  | {"onClick":"{() => void save()}","disabled":"{saving}","className":"\"enterprise-action-button shrink-0 whitespace-nowrap\""} |
| apps/web/src/components/student-admin-profile.tsx:116 |  | StandardSelectField |  | {"name":"\"student-gender\"","value":"{form.gender}","onChange":"{(value) => set(\"gender\", value)}","placeholder":"\"Gender (optional)\"","options":"{[ { value: \"Female\", label: \"Female\" }, { value: \"Male\", label: \"Male\" }, { value: \"Non-binary\", label: \"Non-binary\" }, { value: \"Prefer not to say\", label: \"Prefer not to say\" }, ]}"} |
| apps/web/src/components/student-admin-profile.tsx:128 |  | StandardDateField |  | {"name":"\"dateOfBirthDisplay\"","label":"\"Date of birth\"","value":"{form.dateOfBirth}","onChange":"{(value) => set(\"dateOfBirth\", value)}"} |
| apps/web/src/components/student-admin-profile.tsx:129 |  | StandardDateField |  | {"name":"\"admissionDateDisplay\"","label":"\"Admission date\"","value":"{form.admissionDate}","onChange":"{(value) => set(\"admissionDate\", value)}"} |
| apps/web/src/components/student-fee-arrangements.tsx:65 |  | ul |  | {"className":"\"mt-4 divide-y divide-slate-800\""} |
| apps/web/src/components/student-fee-arrangements.tsx:104 |  | StandardSelectField |  | {"name":"\"fee-frequency\"","value":"{frequency}","onChange":"{setFrequency}","placeholder":"\"Billing frequency\"","options":"{[ { value: \"Monthly\", label: \"Monthly\" }, { value: \"Quarterly\", label: \"Quarterly\" }, { value: \"HalfYearly\", label: \"Half-yearly\" }, { value: \"Annual\", label: \"Annual\" }, ]}"} |
| apps/web/src/components/student-fee-arrangements.tsx:117 |  | button | Add | {"className":"\"enterprise-action-button student-fee-add-button\""} |
| apps/web/src/components/theme-toggle.tsx:14 |  | button | Light | {"type":"\"button\"","title":"\"Use light theme\"","aria-label":"\"Use light theme\"","aria-pressed":"{hydrated ? theme === \"light\" : undefined}","onClick":"{() => setTheme(\"light\")}"} |
| apps/web/src/components/theme-toggle.tsx:23 |  | button | Dark | {"type":"\"button\"","title":"\"Use dark theme\"","aria-label":"\"Use dark theme\"","aria-pressed":"{hydrated ? theme === \"dark\" : undefined}","onClick":"{() => setTheme(\"dark\")}"} |
| apps/web/src/components/workspace-nav.tsx:36 |  | Link |  | {"href":"\"/dashboard\"","className":"\"mr-2 flex items-center gap-2 font-semibold tracking-tight\"","data-active":"{pathname === \"/dashboard\"}"} |
| apps/web/src/components/workspace-nav.tsx:42 |  | nav |  | {"className":"\"flex flex-1 flex-wrap items-center gap-x-4 gap-y-2\"","aria-label":"\"Workspace navigation\""} |
| apps/web/src/components/workspace-nav.tsx:44 |  | details |  | {"key":"{group.label}","className":"\"enterprise-nav-group\""} |
| apps/web/src/components/workspace-nav.tsx:45 |  | summary |  | {"className":"\"text-sm font-medium\"","data-active":"{group.links.some(([, href]) => pathname === href)}"} |
| apps/web/src/components/workspace-nav.tsx:47 |  | Link |  | {"key":"{href}","href":"{href}","data-active":"{pathname === href}","aria-current":"{pathname === href ? \"page\" : undefined}"} |
| apps/web/src/components/workspace-nav.tsx:55 |  | Link | Activity | {"href":"\"/activity\"","className":"\"hidden text-sm font-medium md:inline\""} |
| apps/web/src/components/workspace-nav.tsx:56 |  | button | Sign out | {"type":"\"button\"","onClick":"{signOut}","className":"\"enterprise-signout text-sm font-medium\""} |
