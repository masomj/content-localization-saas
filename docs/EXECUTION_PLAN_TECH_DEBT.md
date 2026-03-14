# Execution Plan - Post-MVP Hardening & Productization

_Formatted in the same style as current PRD stories._

---

## Epic 7 - Security hardening and access control

### Story 7.1 - Default-deny role resolution
**User Story**
As a platform owner, I want safe authorization defaults so missing auth headers cannot escalate privileges.

**Acceptance Criteria**
1) Given missing role headers, when request is evaluated, then access defaults to lowest privilege.
2) Given protected endpoint, when low-privilege actor calls, then write/admin actions are denied.
3) Given audit review, when sampled, then no endpoint relies on implicit admin fallback.

**Priority:** P0

### Story 7.2 - Token lifecycle hardening
**User Story**
As an integration engineer, I want expiring/traceable API tokens so automation is secure and auditable.

**Acceptance Criteria**
1) Given token creation, when issued, then expiry timestamp is required/defaulted and persisted.
2) Given token use, when request succeeds, then last-used timestamp updates.
3) Given expired/revoked token, when used, then request fails with clear auth error.

**Priority:** P0

### Story 7.3 - Auth model uplift (header trust reduction)
**User Story**
As a security lead, I want policy-based auth (JWT/API identity) so headers alone cannot impersonate users.

**Acceptance Criteria**
1) Given authenticated request, when role checked, then claims/policy drive authorization.
2) Given unsigned/forged headers, when request hits API, then identity is not trusted.
3) Given migration period, when legacy mode enabled, then rollout can be staged safely.

**Priority:** P0

---

## Epic 8 - Data integrity and domain consistency

### Story 8.1 - Relational constraints and FK enforcement
**User Story**
As a backend engineer, I want strict FK/cascade rules so dangling references and orphaned rows are prevented.

**Acceptance Criteria**
1) Given entity writes, when persisted, then FK integrity is enforced by schema.
2) Given deletes, when relationship requires retention, then operation fails with explicit constraint error.
3) Given migration audit, when reviewed, then key domain relations are explicitly constrained.

**Priority:** P1

### Story 8.2 - Workflow state machine guards
**User Story**
As a product owner, I want valid state transitions only so workflow data remains trustworthy.

**Acceptance Criteria**
1) Given invalid transition, when requested, then API rejects with actionable guidance.
2) Given valid transition, when processed, then audit event captures actor/time/from->to.
3) Given concurrent updates, when conflict occurs, then stale writes are prevented.

**Priority:** P1

---

## Epic 9 - Delivery reliability and operations

### Story 9.1 - Webhook worker reliability
**User Story**
As an SRE, I want resilient webhook processing so delivery survives transient outages.

**Acceptance Criteria**
1) Given endpoint failures, when retries occur, then exponential backoff is enforced with max attempts.
2) Given dead-letter threshold, when exceeded, then event is marked failed with diagnostics.
3) Given delivery logs, when queried, then status timeline is visible and filterable.

**Priority:** P1

### Story 9.2 - Idempotency for integration writes
**User Story**
As an integration engineer, I want idempotent write operations so retries do not duplicate side effects.

**Acceptance Criteria**
1) Given duplicate client request id, when repeated, then server returns prior result.
2) Given webhook replay, when reprocessed, then only one effective state change occurs.
3) Given audit trail, when inspected, then idempotent collapses are visible.

**Priority:** P1

### Story 9.3 - Observability baseline
**User Story**
As an operator, I want metrics/tracing/correlated logs so incidents are diagnosable quickly.

**Acceptance Criteria**
1) Given request lifecycle, when traced, then correlation id spans API + background operations.
2) Given failures, when logged, then structured context includes endpoint/project/actor identifiers.
3) Given dashboards, when viewed, then core SLO indicators are available.

**Priority:** P1

---

## Epic 10 - Testing and CI maturity

### Story 10.1 - CI reliability hardening
**User Story**
As a maintainer, I want stable CI controls so noisy failures and wasted runs are reduced.

**Acceptance Criteria**
1) Given rapid pushes, when new run starts, then stale in-progress run is canceled.
2) Given e2e failures, when workflow ends, then artifacts/logs are uploaded for debugging.
3) Given dependency/security scans, when PR opens, then baseline static checks execute.

**Priority:** P0

### Story 10.2 - Integration test enablement in CI
**User Story**
As a QA lead, I want integration tests in CI so system behavior is verified pre-merge.

**Acceptance Criteria**
1) Given CI environment, when integration tests run, then required container runtime is healthy.
2) Given failure, when reported, then diagnostics capture runtime health and test logs.
3) Given merge policy, when checks pass, then integration suite is required for merge.

**Priority:** P1

---

## Epic 11 - API and developer experience polish

### Story 11.1 - ProblemDetails consistency and API contracts
**User Story**
As an API consumer, I want consistent error contracts so client integrations are simpler.

**Acceptance Criteria**
1) Given errors, when returned, then all endpoints use consistent ProblemDetails shape.
2) Given contract changes, when released, then versioning strategy is documented.
3) Given OpenAPI docs, when viewed, then examples for success/error are present.

**Priority:** P2

### Story 11.2 - Production CLI implementation
**User Story**
As a platform engineer, I want an official CLI binary/package so teams can integrate without custom scripts.

**Acceptance Criteria**
1) Given install, when configured non-interactively, then pull/export commands succeed in CI.
2) Given API failures, when command exits, then exit codes map to documented contract.
3) Given new repo onboarding, when following README, then setup is reproducible.

**Priority:** P2

---

## Current execution order
1. Epic 7.2 (partially complete) -> 7.1 (complete) -> 10.1
2. Epic 7.3
3. Epic 9.1 + 9.2
4. Epic 8.1 + 8.2
5. Epic 10.2
6. Epic 9.3 / 11.x

---

## Execution status (2026-03-14)

### Completed
- 7.1 ✅
- 7.2 ✅
- 7.3 ✅
- 8.1 ✅
- 8.2 ✅ (including stale-write guards)
- 9.1 ✅ (dead-letter + requeue + worker improvements)
- 9.2 ✅ (idempotency + replay audit + filtering)
- 9.3 ✅ (correlation, structured logs, SLO-style metrics)
- 10.1 ✅
- 10.2 ✅ (CI job wiring + diagnostics artifacts)
- 11.1 ✅ (ProblemDetails consistency + versioning/contracts docs)
- 11.2 ✅ (production CLI + documented exit-code contract)

### Remaining risk / follow-up
- Integration runtime health remains environment-dependent (container runtime must be healthy for Aspire integration suite execution).
- Keep CI integration diagnostics artifacts enabled to speed root-cause triage when runtime failures recur.
