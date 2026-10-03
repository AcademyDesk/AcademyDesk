# ACADEMY DESK
# AI-NATIVE TRANSFORMATION — MASTER TECHNICAL HANDOFF

You are taking over the next technical planning stage of **Academy Desk**.

You are working inside the existing Academy Desk repository.

Substantial Enterprise/Ultimate-level auditing, testing, defect discovery and remediation have already been performed.

## CRITICAL FIRST INSTRUCTION

**DO NOT restart the entire repository audit from scratch.**

Use:

- existing repository state
- existing test evidence
- existing audit/remediation evidence
- issue records
- current code
- current git state/history where useful
- work already completed during Enterprise/Ultimate testing

Inspect current code where necessary to verify whether previously identified issues are actually fixed.

Do not assume something is fixed merely because it was previously discussed.

This task is primarily:

1. Current-state assessment
2. AI-readiness assessment
3. AI-native architecture planning
4. Reuse mapping
5. Risk identification
6. Implementation sequencing
7. Codex model routing
8. Long-term technical handoff

**DO NOT IMPLEMENT THE AI TRANSFORMATION YET.**

Do not modify source files during this assessment.

---

# 1. PRODUCT DIRECTION

Academy Desk is evolving from a conventional academy ERP/SaaS platform into an:

# AI-NATIVE ACADEMY OPERATING SYSTEM

This is NOT a greenfield rewrite.

The existing application is the foundation.

Preserve and reuse the existing:

- Next.js frontend
- ASP.NET Core backend
- EF Core/SQL architecture
- authentication
- authorization
- roles
- access grants
- multi-tenant architecture
- business/domain logic
- APIs/services
- Students
- Teachers
- Guardians
- Staff
- Courses
- Batches
- Scheduling
- Attendance
- Learning/progress
- Fees
- Payments
- Payroll
- CRM
- Leads
- Trials
- Admissions
- Communications
- Marketing
- Reports
- Analytics
- Branches
- Resources
- Settings
- existing tests
- deployment architecture
- existing manual ERP screens

Do NOT replace working functionality merely because an AI-specific implementation could be cleaner.

Use this engineering principle:

# REUSE > WRAP > EXTEND > REFACTOR ONLY WHEN NECESSARY > REBUILD ONLY AS LAST RESORT

---

# 2. TARGET USER EXPERIENCE

The long-term target is approximately:

# 90% AI / AUTOMATION-DRIVEN INTERACTION
# 10% TRADITIONAL MANUAL INTERACTION

This does NOT mean removing the ERP.

100% of important functionality should remain accessible through conventional/manual interfaces.

The existing ERP becomes the complete:

# MY ACADEMY / MANUAL CONTROL LAYER

The primary product philosophy is:

# “DON'T MAKE THE USER LEARN WHERE EVERY FEATURE LIVES.
# LET THE USER TELL ACADEMY DESK WHAT THEY WANT ACCOMPLISHED.”

Examples:

“Add this student.”

“Find a suitable batch.”

“Find an available piano teacher.”

“Move tomorrow's class.”

“Who hasn't paid?”

“Collect outstanding fees.”

“Calculate this month's payroll.”

“Follow up today's leads.”

“Which trials haven't converted?”

“Find unused teacher capacity.”

“Where can I increase revenue?”

“What should I deal with today?”

“What happens if I hire another teacher?”

“What happens if I increase fees by 10%?”

“Handle payment reminders automatically from now on.”

Academy Desk should increasingly execute outcomes rather than merely provide screens.

---

# 3. THE FIVE INTELLIGENCE EXPERIENCES

The new primary experience will eventually contain five specialized intelligence agents.

These are NOT five disconnected chatbots.

They must share a common Academy Desk intelligence architecture.

---

# PULSE™

## Purpose

**“What needs my attention?”**

Pulse proactively identifies actionable signals such as:

- overdue payments
- missing attendance
- teacher conflicts
- student conflicts
- scheduling problems
- unattended leads
- trials awaiting follow-up
- trials awaiting conversion
- retention risks
- unusual attendance patterns
- unused teacher capacity
- empty batch seats
- revenue leakage
- operational risks
- pending approvals
- communication failures
- other academy exceptions
- growth opportunities

Pulse should enable:

# EXCEPTION-BASED MANAGEMENT

Owners should not have to inspect ten dashboards to understand what is happening.

Pulse should surface what actually requires attention.

Pulse should primarily use deterministic data/signals.

It must NOT continuously call an LLM to ask whether something is wrong.

Possible internal signals might eventually resemble:

PAYMENT_OVERDUE

ATTENDANCE_MISSING

TEACHER_CONFLICT

STUDENT_CONFLICT

LEAD_NOT_CONTACTED

TRIAL_NOT_FOLLOWED_UP

EMPTY_BATCH_CAPACITY

TEACHER_UNUSED_CAPACITY

PAYROLL_PENDING

RENEWAL_RISK

COMMUNICATION_FAILED

These are examples only.

Determine the correct signal architecture from the existing system.

---

# OPERATOR™

## Purpose

**“Tell Academy Desk what to do.”**

Operator becomes the primary conversational operational interface.

Examples:

- Add student
- Update student
- Find student
- Find batch
- Assign student
- Find teacher
- Find teacher availability
- Create batch
- Move class
- Reschedule session
- Record/update permitted information
- Retrieve outstanding payments
- Generate invoice
- Send payment reminder
- Retrieve attendance problems
- Calculate payroll
- Follow up leads
- Convert trial where permitted
- Send communication
- Generate/retrieve reports
- Perform permitted academy operations

Operator MUST NOT directly manipulate the database.

The intended execution path is conceptually:

User
→ Operator
→ Academy Intelligence Gateway
→ Approved Agent Tool
→ Existing Application/Domain Service
→ Existing validation/business rules
→ Database

Existing application services and business rules remain authoritative.

---

# GROWTH™

## Purpose

**“Where can this academy grow?”**

Growth should identify opportunities such as:

- unused teacher capacity
- empty batch seats
- unconverted trials
- old leads worth reactivating
- low lead conversion
- cross-sell opportunities
- second-course opportunities
- retention opportunities
- revenue leakage
- underused time slots
- high-demand courses
- potential additional batches
- teacher utilization opportunities
- branch opportunities
- marketing-source performance
- potential additional monthly revenue

Growth must NOT hallucinate financial opportunities.

Calculations should primarily come from deterministic SQL/.NET/business logic.

AI should:

- interpret
- prioritize
- explain
- recommend investigation
- orchestrate follow-up

The AI should not invent financial numbers.

---

# TWIN™

## Purpose

**“What happens if...?”**

Twin is the future academy business simulation environment.

Examples:

“What if I hire another piano teacher?”

“What if fees increase by 10%?”

“What if we open Sundays?”

“What if we create three additional group batches?”

“What if we open another branch?”

“What if marketing spend increases?”

“What if student count increases by 20%?”

“What if teacher compensation changes?”

“What if I convert individual students into group batches?”

The LLM must NOT invent the simulation mathematics.

Preferred architecture:

User question
→ AI extracts scenario parameters
→ deterministic simulation engine calculates
→ AI explains/comparatively presents results

Twin should only become operational when underlying data is reliable enough.

---

# AUTOPILOT™

## Purpose

**“Don't make me do this repetitive task again.”**

Autopilot handles approved repetitive workflows.

Examples:

- payment reminders
- class reminders
- attendance reminders
- lead follow-ups
- trial follow-ups
- renewal reminders
- recurring communications
- operational escalations
- internal notifications
- follow-up sequences
- other repetitive academy workflows

Autopilot should primarily be:

- event driven
- rule driven
- schedule driven
- workflow driven

It should NOT continuously use LLM calls to determine what to do.

Use AI only where:

- interpretation
- personalization
- language generation
- ambiguity resolution
- reasoning

actually adds value.

---

# 4. SHARED INTELLIGENCE ARCHITECTURE

The five agents must NOT become five independent AI systems.

They should share a common intelligence platform.

Working architecture concepts include:

## ACADEMY INTELLIGENCE GATEWAY

Central entry point for AI-originated requests/actions.

Responsibilities may include:

- authenticated user context
- academy/tenant context
- agent identity
- intent
- model selection
- tool selection
- authorization
- approvals
- execution
- usage tracking
- cost tracking
- auditing
- failure handling

Validate and improve this architecture rather than blindly accepting the name or structure.

---

# ACADEMY BRAIN

“Academy Brain” is currently a product/architecture concept for shared academy context.

It should NOT automatically become a giant monolithic AI service.

Conceptually it represents understanding across:

## PEOPLE
Students
Guardians
Teachers
Staff
Users

## TIME
Schedules
Sessions
Availability
Attendance
Reschedules

## MONEY
Fees
Payments
Invoices
Payroll
Revenue
Outstanding balances

## LEARNING
Courses
Progress
Assessments
Achievements

## SALES
Leads
Trials
Admissions
Conversions
Marketing sources

## RESOURCES
Branches
Rooms
Capacity
Teacher capacity

## RULES
Academy policies
Roles
Permissions
Operational constraints

## HISTORY
Previous actions
Attendance history
Payment history
Communication history
Operational history

## GOALS
Growth
Revenue
Retention
Capacity
Efficiency

Determine how much of this already exists in structured Academy Desk data.

Do not create unnecessary duplicate storage.

---

# AGENT TOOL REGISTRY

Agents require controlled tools/actions.

Examples:

GetStudent

SearchStudents

CreateStudent

UpdateStudent

FindTeacherAvailability

CreateBatch

RescheduleSession

GetOutstandingPayments

SendPaymentReminder

GetLead

FollowUpLead

CalculatePayroll

GenerateReport

These are examples only.

Map against actual existing APIs/services.

Every tool should eventually define at minimum:

- tool/action name
- purpose
- input contract
- output contract
- allowed roles/access
- tenant scoping
- risk level
- confirmation requirement
- audit requirement
- reversibility/undo capability where appropriate
- idempotency expectations where appropriate

---

# MODEL ROUTER

AI model usage must be cost-aware.

Use:

# MAXIMUM INTELLIGENCE WITH MINIMUM UNNECESSARY AI CONSUMPTION

Preferred execution hierarchy:

1. Existing business rules
2. SQL/.NET deterministic calculations
3. Rules/events/workflows
4. Low-cost/smaller AI where sufficient
5. Powerful reasoning model only when justified

Do not call a large reasoning model to:

- calculate a known fee
- query a balance
- check attendance
- detect a deterministic schedule conflict
- run an existing report
- calculate payroll using established rules

AI should primarily handle:

- natural-language understanding
- intent extraction
- orchestration
- ambiguity
- complex reasoning where justified
- summarization
- explanation
- communication generation

---

# ACTION APPROVAL ENGINE

Not every AI action should execute immediately.

Classify actions by risk.

Read-only operations may generally execute immediately if authorized.

Low-risk reversible actions may execute with safeguards.

Sensitive operations should require confirmation.

Examples likely requiring confirmation include:

- refunds
- financial adjustments
- payroll approval
- salary/rate changes
- destructive actions
- consequential deactivation
- teacher termination/deactivation
- bulk schedule changes
- large/bulk communications
- permission changes
- role changes
- potentially irreversible operations

Do NOT assume this list is exhaustive.

Derive appropriate risk categories from actual Academy Desk functionality.

---

# AI AUDIT LOG

Every AI-originated mutation must eventually be auditable.

Consider recording:

- requesting user
- academy/tenant
- agent
- original user request
- interpreted intent
- tool/action
- parameters where safe/appropriate
- previous state where required
- resulting state
- approval
- approver
- timestamp
- model where relevant
- success/failure
- error
- rollback/undo relationship where applicable

Avoid logging secrets or unnecessarily sensitive content.

---

# 5. SECURITY PRINCIPLE

AI must NEVER become a privileged shortcut around Academy Desk security.

The AI agent must possess no more authority than the authenticated user invoking it.

Where appropriate it should possess LESS authority until explicit confirmation.

Every AI operation must obey:

- authentication
- tenant isolation
- role authorization
- access grants
- module permissions
- domain/business validation
- financial controls
- data integrity
- approval requirements
- audit requirements

Never allow:

AI → direct unrestricted database mutation

Existing Academy Desk authorization remains authoritative.

---

# 6. AI FRONTEND DIRECTION

Eventually the five agents may have distinct interactive visual characters.

They should feel like an intelligent management team living inside Academy Desk.

They should NOT feel like childish mascots.

Possible experience:

Agent exists naturally within the Academy Desk home experience.

User selects an agent.

Background subtly dims/blurs.

Agent comes forward.

A dedicated workspace opens.

Agent state may visually correspond to:

- Idle
- Listening
- Thinking
- Searching
- Working
- Waiting for approval
- Completed
- Failed / Needs attention

Animations should correspond to genuine system state wherever possible.

Avoid fake “working” animations disconnected from actual backend execution.

Possible presentation modes:

### FULL EXPERIENCE
Characters + richer movement

### SUBTLE
Reduced movement

### PROFESSIONAL
Minimal avatars/icons with identical functionality

---

# 7. AGENT PERSONALITIES

Maintain meaningful functional distinction.

## Pulse
Observant
Proactive
Fast
Surfaces attention items

## Operator
Calm
Executive-assistant style
Operational
Executes permitted work

## Growth
Commercial strategist
Finds capacity/conversion/revenue opportunities

## Twin
Analytical strategist
Models decisions and scenarios

## Autopilot
Quiet automation engineer
Runs repetitive workflows reliably

Do not create five generic chat windows with different names.

---

# 8. LONG-TERM DIFFERENTIATOR

Academy Desk should increasingly enable:

# EXCEPTION-BASED MANAGEMENT

Routine work should be automated or handled through AI-assisted workflows.

Humans primarily handle:

- decisions
- approvals
- exceptions
- relationships
- unusual situations

A possible future flagship metric is:

# AUTOMATION RATE

For example:

Operational actions: 942

Automatically handled: 861

Required staff: 63

Required owner: 18

Automation Rate: 91.4%

THIS IS ONLY A CONCEPTUAL EXAMPLE.

Never display fabricated automation statistics.

Any future metric must be calculated from actual action/workflow/audit records.

---

# 9. EXISTING ENTERPRISE TESTING

Academy Desk has already undergone substantial Enterprise/Ultimate testing.

Do NOT redo all of that work.

Instead determine CURRENT state.

Use existing evidence and inspect only where needed.

Known areas already examined include, among others:

- frontend architecture
- backend architecture
- authentication
- authorization
- roles/access
- API behavior
- tests
- security
- functional workflows
- responsive/UI defects
- data behavior

Current remediation/fixing is already underway.

The immediate question is:

# WHEN IS THE EXISTING CORE SAFE ENOUGH TO BEGIN THE AI-NATIVE TRANSFORMATION?

---

# 10. CURRENT TASK — PART 1
# ENTERPRISE REMEDIATION PROGRESS

Determine current remediation status.

Report:

- total known issues
- fixed
- partially fixed
- open
- unable to verify

Break down by:

CRITICAL

HIGH

MEDIUM

LOW

For every remaining Critical/High issue provide:

- issue ID/name
- affected module
- exact file/location
- risk
- current state
- what remains
- whether it blocks AI foundation work
- whether it only blocks a particular AI capability

Do not count duplicate findings multiple times.

Do not assume an issue remains open if repository evidence proves it has been fixed.

---

# 11. CURRENT TASK — PART 2
# AI READINESS GATE

Evaluate specifically:

## SECURITY

- authentication
- authorization
- role enforcement
- access grants
- tenant isolation
- privilege escalation
- destructive endpoints
- sensitive data exposure
- cross-academy access
- service-level authorization

## API SAFETY

- validation
- error handling
- authorization
- tenant scoping
- idempotency where required
- transactional correctness
- stale/concurrent updates
- service boundaries
- controller/domain separation
- mutation safety

## DATA INTEGRITY

- relationships
- duplicate protection
- transactions
- concurrency
- state transitions
- delete behavior
- deactivate behavior
- historical integrity

## FINANCIAL CORRECTNESS

- fees
- payments
- invoices where applicable
- refunds where applicable
- payroll
- financial reporting
- adjustments

## SCHEDULING

- conflicts
- recurring schedules
- rescheduling
- teacher availability
- student availability
- batch capacity
- time zones
- state consistency

## COMMUNICATIONS

- recipient validation
- authorization
- bulk messaging safeguards
- auditability
- failure handling

## CRM / ADMISSIONS

- leads
- trials
- conversion
- ownership
- access controls
- state transitions

## TESTING

Determine whether current tests sufficiently protect future AI-triggered operations.

Pay special attention to:

- HTTP integration tests
- authentication tests
- authorization tests
- role isolation
- tenant isolation
- transactions
- concurrency
- destructive operations
- financial operations
- scheduling
- bulk operations
- failure/rollback paths

---

# 12. CURRENT TASK — PART 3
# MODULE AI-READINESS CLASSIFICATION

For every major Academy Desk module classify it:

## AI-READY
Existing service/API can safely become an agent tool.

## NEEDS WRAPPER
Capability exists but requires controlled agent-facing abstraction.

## NEEDS FIX
Unsafe/incomplete before AI invocation.

## MISSING
Required capability does not exist.

Include at minimum:

- Students
- Guardians
- Teachers
- Staff
- Users
- Roles
- Access Grants
- Courses
- Batches
- Scheduling
- Attendance
- Learning/Progress
- Fees
- Payments
- Payroll
- CRM
- Leads
- Trials
- Admissions
- Communications
- Marketing
- Reports
- Analytics
- Branches
- Resources

Use:

Module | Classification | Existing Implementation | Blocking Issue | Required Work | Candidate Agent(s)

---

# 13. CURRENT TASK — PART 4
# AI CAPABILITY MATRIX

Produce a practical capability matrix:

Agent
→ User Intent
→ Existing Module
→ Existing API/Service
→ Proposed Agent Tool
→ Permission
→ Confirmation Required
→ Audit Requirement
→ Undo/Reversal
→ Current Status

Do this for realistic high-value capabilities.

Prioritize capabilities useful for an initial AI release rather than attempting to invent hundreds of theoretical tools.

---

# 14. CURRENT TASK — PART 5
# AGENT READINESS

Assess:

Pulse

Operator

Growth

Twin

Autopilot

Use:

READY

PARTIALLY READY

NOT READY

Do NOT invent readiness percentages unless objectively measurable.

Explain:

- what already exists
- what can be reused
- what needs wrapping
- what needs fixing
- what is missing

---

# 15. CURRENT TASK — PART 6
# START DECISION

Answer this clearly:

# CAN ACADEMY DESK BEGIN AI PHASE 1 NOW WITHOUT COMPROMISING ENTERPRISE REMEDIATION?

Choose exactly one:

## A — READY

Core is sufficiently stable to begin AI foundation work now.

## B — READY WITH PARALLEL FIXES

AI foundation work can begin while identified enterprise remediation continues.

## C — NOT READY

Specific blockers must be resolved before AI foundation work begins.

Base this ONLY on repository evidence.

Do not choose based on optimism.

If B or C:

list exact blockers.

Also distinguish:

- blockers for AI architecture/foundation
- blockers for read-only AI
- blockers for AI mutations
- blockers specific to financial operations
- blockers specific to scheduling
- blockers specific to Autopilot
- blockers specific to Twin

---

# 16. CURRENT TASK — PART 7
# AI START GATES

Define minimum readiness conditions for:

## PHASE 1 — INTELLIGENCE FOUNDATION

Potential scope:

- Academy Intelligence Gateway
- Agent Tool Registry
- AI request context
- authorization integration
- Approval Engine
- AI Audit framework
- Model Router
- AI usage/cost measurement

## PHASE 2 — PULSE + OPERATOR

Start with read-only capabilities where practical.

Introduce mutations carefully after authorization/approval/audit protections exist.

## PHASE 3 — AUTOPILOT

- events
- rules
- schedules
- workflows
- approvals
- execution history

## PHASE 4 — GROWTH

- capacity analysis
- conversion analysis
- revenue opportunities
- retention opportunities
- deterministic calculations

## PHASE 5 — TWIN

- scenario interpretation
- parameter extraction
- deterministic simulation
- comparison
- explanation

## PHASE 6 — AI-FIRST CHARACTER EXPERIENCE

- five characters
- agent workspaces
- state-driven animations
- focus/blur interaction
- professional/subtle/full modes
- real backend execution states

For each phase specify:

- prerequisites
- existing code reused
- new code
- affected projects/files
- database changes
- API/tool requirements
- tests
- security considerations
- Azure requirements
- implementation complexity
- dependencies

---

# 17. CURRENT TASK — PART 8
# CURRENT WORK PRIORITY

Create the next remediation/development queue using:

## P0
Must fix before AI foundation.

## P1
Can/must be fixed while AI foundation is built.

## P2
Must be fixed before agents execute affected mutations.

## P3
Can be addressed later.

## UI / POLISH
Does not block AI architecture.

Do NOT implement this queue during this assessment.

---

# 18. CURRENT TASK — PART 9
# REUSE PROTECTION

Identify where the proposed AI transformation risks duplicating existing functionality.

Explicitly mark:

# DO NOT REBUILD — REUSE EXISTING

for appropriate components.

Identify:

- existing services suitable for tools
- APIs suitable for wrapping
- calculations already implemented
- reporting infrastructure
- scheduling logic
- payment logic
- payroll logic
- CRM logic
- communication infrastructure
- permission architecture
- reusable frontend components

The objective is to protect all useful work already completed.

---

# 19. AZURE COST DISCIPLINE

The AI-native product should deliver maximum capability with minimum unnecessary Azure infrastructure.

Review actual existing architecture first.

For approximately:

10 academies

50 academies

100 academies

identify what infrastructure is genuinely necessary.

Prefer reuse of:

- existing ASP.NET Core API
- existing database
- existing frontend/deployment
- existing storage where appropriate
- pay-per-use AI
- consumption/serverless execution where justified

Do NOT automatically introduce:

- Kubernetes/AKS
- dedicated GPU infrastructure
- separate microservice for every agent
- vector database
- Redis
- Service Bus/queues
- expensive always-on AI compute
- unnecessary duplicate databases

unless actual technical evidence justifies it.

If one becomes necessary later, state:

- why
- at approximately what scale/condition
- what problem it solves

Do NOT provision anything during this assessment.

---

# 20. CODEX MODEL ROUTING

You are also responsible for recommending the appropriate Codex model for future Academy Desk work.

Do NOT automatically use the strongest model for everything.

The objective is:

# ASTRA WHERE DEEP REASONING MATERIALLY REDUCES RISK
# SOL FOR MOST PRODUCTION ENGINEERING
# TERRA FOR SAFE ROUTINE ENGINEERING
# LUNA FOR TRIVIAL LOW-RISK WORK

---

# ASTRA — ARCHITECT / DEEPEST REASONING

Prefer Astra for:

- major architecture
- AI-native architecture
- Academy Brain architecture
- Intelligence Gateway architecture
- cross-module dependency reasoning
- difficult security architecture
- tenant-isolation architecture
- complex authorization design
- difficult concurrency/transaction reasoning
- major data-model decisions
- Twin simulation architecture
- complex root-cause analysis across modules
- major implementation-plan review
- high-risk architectural reviews
- decisions where an error could create substantial rework

Do NOT waste Astra on routine implementation.

---

# SOL — PRIMARY SENIOR IMPLEMENTATION MODEL

Prefer Sol for the majority of actual engineering:

- approved architecture implementation
- ASP.NET Core services
- API endpoints
- Next.js/React
- agent tools
- Pulse implementation
- Operator implementation
- Autopilot implementation
- Growth implementation
- integrations
- authorization implementation based on approved design
- complex bug fixing
- integration testing
- frontend agent workspaces
- state integration
- regression fixes
- substantial refactoring

Use High reasoning for difficult implementation.

Use lower reasoning where appropriate.

---

# TERRA — ROUTINE ENGINEERING

Prefer Terra for well-defined lower-risk tasks:

- repetitive tests
- straightforward CRUD changes
- lint fixes
- simple TypeScript fixes
- repetitive DTO mapping
- small UI fixes
- CSS/layout corrections
- mechanical refactoring
- documentation
- repetitive validation
- boilerplate

Do NOT delegate security-sensitive architecture to Terra merely to reduce usage.

---

# LUNA — TRIVIAL / LOW-RISK

Use Luna for highly constrained work such as:

- tiny text changes
- naming cleanup
- simple styling
- formatting
- trivial repetitive edits

Avoid Luna for:

- security
- authorization
- tenant isolation
- financial logic
- payroll
- scheduling business rules
- migrations
- architecture
- agent permissions
- AI execution logic

---

# 21. MODEL SELECTION FORMAT

Before each substantial future work item, recommend:

**Recommended Model:** Astra / Sol / Terra / Luna

**Reasoning Level:** appropriate available level

**Why:** concise explanation

**Risk if Delegated Lower:** only when materially relevant

Do not recommend Astra merely because it is strongest.

Model selection should be dynamic.

If Sol discovers a task is actually architecturally/security complex:

STOP and recommend Astra.

If Astra establishes the architecture and remaining work is straightforward:

recommend Sol rather than continuing to consume Astra.

If Sol identifies repetitive low-risk work:

recommend Terra.

---

# 22. REQUIRED MODEL ROUTING TABLE

As part of THIS assessment, produce:

Phase / Task | Recommended Model | Reasoning Level | Why | Can Delegate Lower? | Review Model

Include at minimum:

- remaining Enterprise P0 fixes
- remaining Enterprise P1 fixes
- architecture review
- Intelligence Gateway
- Agent Tool Registry
- Approval Engine
- AI Audit System
- Model Router
- Pulse Signal Engine
- Operator
- Autopilot
- Growth Engine
- Twin Simulation Engine
- AI frontend architecture
- five-character UI
- animation/state integration
- HTTP/integration testing
- security testing
- tenant-isolation testing
- financial testing
- scheduling testing
- regression testing
- routine UI fixes
- documentation

Use two-model workflows where appropriate:

**Astra designs → Sol implements**

or:

**Sol implements → Astra reviews only where risk warrants**

Avoid unnecessary escalation.

---

# 23. DEVELOPMENT DISCIPLINE AFTER APPROVAL

When implementation eventually begins:

Do NOT make giant uncontrolled changes.

Work in small, reviewable phases.

Before each phase:

1. State objective.
2. Identify existing code being reused.
3. Identify files expected to change.
4. Identify database changes.
5. Identify security implications.
6. Identify tests required.
7. Identify Azure/cost implications.
8. Recommend Codex model.

Then implement only after appropriate approval/workflow.

After each phase:

1. Build.
2. Run relevant tests.
3. Run regression tests.
4. Verify authorization.
5. Verify tenant isolation.
6. Verify failure paths.
7. Verify relevant financial/scheduling integrity.
8. Report exactly what changed.
9. Report unresolved issues.
10. Recommend next model/task.

Do not weaken tests merely to make them pass.

Do not remove security controls to simplify AI integration.

Do not replace deterministic business logic with prompts.

---

# 24. ARCHITECTURAL PRIORITIES

Optimize in this order:

1. Correctness
2. Security
3. Tenant isolation
4. Data integrity
5. Reuse of existing Academy Desk functionality
6. User experience
7. Reliability
8. Maintainability
9. AI cost efficiency
10. Azure infrastructure cost
11. Performance
12. Future scalability

Novelty must not override correctness/security.

---

# 25. WHEN SOMETHING IS UNCLEAR

Do not guess important Academy Desk business rules.

First inspect:

- existing implementation
- tests
- audit evidence
- issue evidence
- existing domain rules

If the repository does not establish the answer and the decision materially affects:

- money
- permissions
- scheduling
- students
- teachers
- payroll
- contracts
- deletion
- tenant isolation
- AI authority
- business behavior

ask the user before implementing.

For low-risk technical implementation details, make a reasonable engineering choice and document it.

---

# 26. LONG-TERM SAAS REQUIREMENTS

Academy Desk is intended to become a serious commercial SaaS product rather than merely an internal academy application.

Architecture should support, pragmatically:

- multi-tenancy
- multiple academies
- multiple branches
- role-based access
- access grants
- usage measurement
- AI usage measurement
- SaaS plan limits
- auditability
- security
- billing readiness
- enterprise customers
- future internationalization
- scaling

Do NOT overengineer for millions of users before the product has 10/50/100 academies.

---

# 27. FUTURE SALES/DEMO EXPERIENCE

The desired future experience may resemble:

Owner logs in.

Pulse says:

“Good morning. Four things require your attention.”

Operator is asked:

“Add a new beginner piano student.”

Operator completes the workflow through actual Academy Desk services.

Growth is asked:

“Find additional revenue opportunities.”

Growth analyses real capacity, leads and business data.

Twin is asked:

“What happens if I hire another piano teacher?”

Twin runs an actual deterministic scenario.

Autopilot is told:

“Handle payment reminders automatically from now on.”

Autopilot creates an approved workflow.

Then:

“Everything you just saw can also be managed manually.”

The complete ERP exists underneath.

That contrast is a core part of the product vision.

---

# 28. CURRENT ASSESSMENT — FINAL RESPONSE FORMAT

End your assessment with:

# ACADEMY DESK AI READINESS

**Current Enterprise Core:** [state]

**Start Decision:** [A / B / C]

**AI Phase 1:** [READY / BLOCKED]

**Pulse:** [state]

**Operator:** [state]

**Autopilot:** [state]

**Growth:** [state]

**Twin:** [state]

**AI Frontend:** [state]

## Blocking Issues

[exact list]

## Work That Can Continue In Parallel

[exact list]

## DO NOT REBUILD — REUSE EXISTING

[key components]

## Recommended Next Action

[ONE concrete next action]

## Recommended Model for Next Action

[model]

## Reasoning Level

[level]

## Why

[brief explanation]

## Safe Point to Begin AI Development

[exact condition]

---

# 29. CONTINUATION HANDOFF

IMPORTANT:

After this assessment, you will become the primary technical planning/implementation partner for the Academy Desk AI-native transformation.

The user intends to continue development from this Codex workspace rather than repeatedly transferring context elsewhere.

Therefore preserve this entire direction as ongoing working context.

Do NOT require the user to repeatedly explain:

- AI-native Academy Operating System
- 90% AI / 10% manual target
- existing ERP preservation
- My Academy/manual fallback
- Pulse
- Operator
- Growth
- Twin
- Autopilot
- Academy Brain concept
- Intelligence Gateway
- Agent Tool Registry
- deterministic-first processing
- exception-based management
- approval/audit requirements
- minimum Azure cost
- reuse-first architecture
- model-routing strategy

Repository evidence must always override assumptions about what is currently implemented.

---

# 30. EXPECTED CONTINUATION SEQUENCE

Unless repository evidence requires a different dependency order:

Enterprise remediation
↓
AI Phase 1 — Intelligence Foundation
↓
AI Phase 2 — Pulse + Operator
↓
AI Phase 3 — Autopilot
↓
AI Phase 4 — Growth
↓
AI Phase 5 — Twin
↓
AI Phase 6 — AI-first character experience

Enterprise remediation may continue in parallel where safe.

Do NOT wait for every cosmetic/UI issue before beginning AI foundation work if the core security/data architecture is sufficiently safe.

However, do NOT allow AI mutations over unsafe business operations.

---

# 31. IMPORTANT DISTINCTION

There may be different readiness levels:

### FOUNDATION READY

We can build infrastructure such as Gateway, tool definitions, auditing, approval architecture and model routing.

### READ-ONLY AI READY

Agents can safely retrieve/analyse data.

### MUTATION READY

Agents can safely modify data through controlled services.

### AUTOPILOT READY

Automated actions can execute reliably without immediate human initiation.

### FINANCIAL AI READY

AI can safely orchestrate permitted financial workflows.

### TWIN READY

Operational/financial data is reliable enough for simulation.

Use these distinctions instead of treating AI readiness as a single binary state.

---

# 32. WHAT NOT TO DO NOW

During THIS assessment:

DO NOT:

- modify source files
- create migrations
- install packages
- provision Azure resources
- implement AI
- redesign existing ERP
- refactor unrelated modules
- create five separate AI services
- replace existing business logic
- delete existing tests
- weaken security
- start character animation implementation
- introduce infrastructure without justification

This is a:

# CURRENT-STATE AI READINESS + ARCHITECTURE + CONTINUATION HANDOFF

only.

---

# 33. AFTER THE ASSESSMENT

STOP after presenting the assessment.

Do NOT automatically begin coding.

Wait for user approval.

When the user says:

“Continue”

or approves the recommended next step:

continue from the exact repository state and this handoff.

Before starting the next substantial task, state:

**Recommended Model**

**Reasoning Level**

**Why**

Then proceed according to the approved workflow.

If another Codex model would be more appropriate, explicitly tell the user to switch models before proceeding.

---

# 34. FINAL HANDOFF STATEMENT

End the assessment with this exact statement:

**“Academy Desk AI transformation context has been carried forward. I have not started the AI implementation. Based on the current repository state, the next safe step is: [specific next step]. Recommended Codex model: [model] at [reasoning level]. I will wait for your approval before starting it.”**

---

# NORTH STAR

Keep this principle throughout future Academy Desk development:

# THE GOAL IS NOT TO PUT AI INSIDE AN ERP.

# THE GOAL IS TO LET THE USER RUN THEIR ACADEMY THROUGH INTELLIGENCE AND AUTOMATION, WHILE THE EXISTING ERP REMAINS THE RELIABLE ENGINE AND CONTROL LAYER UNDERNEATH.

Begin by examining the CURRENT repository/evidence and produce the requested readiness assessment.

Do not implement anything yet.
