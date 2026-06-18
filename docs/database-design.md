# Database Design

This document defines the initial PostgreSQL relational design for AuditAI based on the current domain model in `AuditAI.Domain`. The domain model drives the invariants and workflow rules; the database model supports those rules with clear keys, constraints, and query-oriented indexes.

## 1. Entity/Table List

Core tables:

* `organizations`
* `departments`
* `users`
* `controls`
* `evidence`
* `audit_findings`
* `action_plans`
* `audit_logs`

Roadmap-only future AI tables:

* `ai_suggestions`
* `ai_suggestion_feedback`
* `ai_analysis_runs`

## 2. Main Columns for Each Table

### `organizations`

* `id` UUID
* `name` text
* `created_at` timestamptz
* `updated_at` timestamptz

### `departments`

* `id` UUID
* `organization_id` UUID
* `name` text
* `created_at` timestamptz
* `updated_at` timestamptz

### `users`

* `id` UUID
* `organization_id` UUID
* `department_id` UUID nullable
* `full_name` text
* `email` text
* `role` text
* `created_at` timestamptz
* `updated_at` timestamptz

### `controls`

* `id` UUID
* `organization_id` UUID
* `department_id` UUID nullable
* `code` text
* `title` text
* `description` text nullable
* `status` text
* `frequency` text
* `created_at` timestamptz
* `updated_at` timestamptz

### `evidence`

* `id` UUID
* `control_id` UUID
* `submitted_by_user_id` UUID
* `reviewed_by_user_id` UUID nullable
* `file_name` text
* `storage_reference` text
* `status` text
* `rejection_reason` text nullable
* `created_at` timestamptz
* `updated_at` timestamptz
* `reviewed_at` timestamptz nullable

### `audit_findings`

* `id` UUID
* `control_id` UUID
* `created_by_user_id` UUID
* `title` text
* `description` text
* `severity` text
* `status` text
* `created_at` timestamptz
* `updated_at` timestamptz
* `resolved_at` timestamptz nullable

### `action_plans`

* `id` UUID
* `audit_finding_id` UUID
* `assigned_to_user_id` UUID
* `title` text
* `description` text
* `due_date` timestamptz
* `status` text
* `created_at` timestamptz
* `updated_at` timestamptz

### `audit_logs`

* `id` UUID
* `organization_id` UUID
* `user_id` UUID nullable
* `action` text
* `entity_name` text
* `entity_id` UUID
* `metadata` jsonb nullable
* `timestamp` timestamptz

## 3. Primary Keys

All core tables use `id UUID PRIMARY KEY`.

Rationale:

* UUIDs are appropriate for a multi-tenant system.
* They avoid tenant data leakage through sequential ids.
* They simplify future integrations and distributed workflows.

## 4. Foreign Keys

* `departments.organization_id -> organizations.id`
* `users.organization_id -> organizations.id`
* `users.department_id -> departments.id`
* `controls.organization_id -> organizations.id`
* `controls.department_id -> departments.id`
* `evidence.control_id -> controls.id`
* `evidence.submitted_by_user_id -> users.id`
* `evidence.reviewed_by_user_id -> users.id`
* `audit_findings.control_id -> controls.id`
* `audit_findings.created_by_user_id -> users.id`
* `action_plans.audit_finding_id -> audit_findings.id`
* `action_plans.assigned_to_user_id -> users.id`
* `audit_logs.organization_id -> organizations.id`
* `audit_logs.user_id -> users.id`

## 5. Required Fields

Required non-null fields by table:

* `organizations`: `id`, `name`, `created_at`, `updated_at`
* `departments`: `id`, `organization_id`, `name`, `created_at`, `updated_at`
* `users`: `id`, `organization_id`, `full_name`, `email`, `role`, `created_at`, `updated_at`
* `controls`: `id`, `organization_id`, `code`, `title`, `status`, `frequency`, `created_at`, `updated_at`
* `evidence`: `id`, `control_id`, `submitted_by_user_id`, `file_name`, `storage_reference`, `status`, `created_at`, `updated_at`
* `audit_findings`: `id`, `control_id`, `created_by_user_id`, `title`, `description`, `severity`, `status`, `created_at`, `updated_at`
* `action_plans`: `id`, `audit_finding_id`, `assigned_to_user_id`, `title`, `description`, `due_date`, `status`, `created_at`, `updated_at`
* `audit_logs`: `id`, `organization_id`, `action`, `entity_name`, `entity_id`, `timestamp`

Conditional requirement:

* `evidence.rejection_reason` is required when `evidence.status = 'Rejected'`.

## 6. Unique Constraints

Recommended unique constraints:

* `organizations`: none beyond primary key
* `departments`: unique `(organization_id, lower(name))`
* `users`: unique `(organization_id, lower(email))`
* `controls`: unique `(organization_id, code)`
* `audit_logs`: none beyond primary key

Not recommended:

* No uniqueness on evidence file name, because the same filename can be submitted many times.
* No uniqueness on finding titles or action plan titles, because those are business descriptions, not identifiers.

## 7. Suggested Indexes

Indexes for expected demo and operational queries:

* `controls (organization_id, status, created_at desc)`
* `controls (organization_id, department_id)`
* `evidence (control_id, status, created_at desc)`
* `evidence (submitted_by_user_id, created_at desc)`
* `audit_findings (control_id, status, created_at desc)`
* `audit_findings (severity, status, created_at desc)`
* `action_plans (assigned_to_user_id, due_date)`
* `action_plans (audit_finding_id, status)`
* `action_plans (status, due_date)`
* `audit_logs (organization_id, timestamp desc)`
* `audit_logs (entity_name, entity_id, timestamp desc)`
* `users (organization_id, role)`
* `departments (organization_id, name)`

If case-insensitive lookups are frequent and `citext` is not used:

* functional index on `lower(users.email)`
* functional index on `lower(departments.name)`

## 8. Delete Behavior Recommendations

Recommended delete behavior:

* `organizations -> departments`: `RESTRICT`
* `organizations -> users`: `RESTRICT`
* `organizations -> controls`: `RESTRICT`
* `organizations -> audit_logs`: `RESTRICT`
* `departments -> users`: `SET NULL`
* `departments -> controls`: `SET NULL`
* `controls -> evidence`: `RESTRICT`
* `controls -> audit_findings`: `RESTRICT`
* `audit_findings -> action_plans`: `RESTRICT`
* `users -> evidence.submitted_by_user_id`: `RESTRICT`
* `users -> evidence.reviewed_by_user_id`: `SET NULL`
* `users -> audit_findings.created_by_user_id`: `RESTRICT`
* `users -> action_plans.assigned_to_user_id`: `RESTRICT`
* `users -> audit_logs.user_id`: `SET NULL`

Rationale:

* Audit history should not disappear because a parent row is deleted.
* Controls, findings, evidence, and action plans are core audit records and should be preserved.
* Optional references such as reviewer and department can be nullified without corrupting the record.

## 9. Status Enum Storage Strategy

Recommendation: store enums as strings in PostgreSQL columns, using application-controlled values.

Examples:

* `users.role`: `Admin`, `Auditor`, `Reviewer`
* `evidence.status`: `Pending`, `Accepted`, `Rejected`
* `audit_findings.severity`: `Low`, `Medium`, `High`, `Critical`
* `audit_findings.status`: `Open`, `InProgress`, `Resolved`, `Cancelled`
* `action_plans.status`: `Open`, `InProgress`, `Completed`, `Overdue`, `Cancelled`
* `controls.status`: `Active`, `Inactive`
* `controls.frequency`: `Monthly`, `Quarterly`, `Yearly`
* `audit_logs.action`: `EvidenceAccepted`, `AuditFindingResolved`, and similar

Why strings:

* Better readability in ad hoc SQL and demos.
* Lower risk of semantic drift if enum numeric ordering changes.
* Easier operational debugging.

Tradeoff:

* Strings consume more storage than integers, but the clarity is worth it for this stage.

## 10. Audit Logging Strategy

`audit_logs` is designed as an append-only table.

Recommended strategy:

* Every sensitive business action inserts a new row.
* No updates in normal operation.
* No hard deletes except rare operational cleanup with explicit approval.
* `metadata` should be `jsonb` for flexible contextual details such as old/new status, source, comment, or AI-assisted flag in the future.

Actions that should be logged:

* user login and logout
* control create, update, delete
* evidence submit, accept, reject
* audit finding create, resolve
* action plan create, complete
* user role change

## 11. Multi-Tenant / Organization Boundary Rules

AuditAI should enforce a strict organization boundary.

Rules:

* Every `user`, `department`, `control`, and `audit_log` belongs directly to one organization.
* `evidence` and `audit_findings` belong indirectly to an organization through `control`.
* `action_plans` belong indirectly through `audit_finding -> control`.
* Cross-organization references must never be allowed.
* A user may only submit evidence, create findings, or be assigned action plans inside the same organization as the target aggregate.

Implementation note:

* Several cross-table organization consistency rules are easier to enforce in the Application layer than as pure database constraints.

## 12. Rules That Should Be Enforced in Domain

These belong in `AuditAI.Domain` because they are core invariants of the model:

* Evidence must have a control id.
* Audit finding must have a control id.
* Action plan must have an audit finding id.
* Rejecting evidence requires a rejection reason.
* Evidence can only be reviewed from `Pending`.
* Action plan due date cannot be earlier than creation date.
* A critical finding cannot be resolved while it has any open, in-progress, or overdue action plans.
* Core descriptive fields such as names, titles, descriptions, codes, and storage references cannot be blank.

## 13. Rules That Should Be Enforced in Application

These depend on identity, current time, orchestration, or cross-aggregate checks:

* Authenticated user is required for protected actions.
* Only reviewers may accept or reject evidence.
* Only authorized roles may create controls, findings, or action plans.
* A user and target record must belong to the same organization.
* Action plan due date should not be in the past relative to the current clock.
* Reviewer identity must be different from invalid or deleted users.
* Audit log rows should be generated for all sensitive actions.
* Any future AI suggestion must remain advisory only and must never directly approve, reject, resolve, or delete audit data.

## 14. Rules That Should Be Enforced by Database Constraints

Recommended database constraints:

* primary keys on all tables
* foreign keys for all relationships
* not-null constraints for required columns
* unique `(organization_id, lower(email))` on users
* unique `(organization_id, lower(name))` on departments
* unique `(organization_id, code)` on controls
* check constraints limiting enum text values to known literals
* check constraint requiring `rejection_reason` when evidence status is `Rejected`
* check constraint ensuring `due_date >= created_at` on action plans

Possible but deferred:

* advanced tenant-consistency triggers for indirect organization checks
* append-only enforcement on `audit_logs` through triggers or restricted permissions

## 15. Future AI-Related Tables Roadmap

These are roadmap items only and should not be implemented yet.

### `ai_suggestions`

Purpose:

* store AI-generated recommendations such as proposed severity, evidence summary, or remediation draft

Potential columns:

* `id` UUID
* `organization_id` UUID
* `target_entity_name` text
* `target_entity_id` UUID
* `suggestion_type` text
* `suggested_payload` jsonb
* `model_name` text
* `created_at` timestamptz
* `accepted_by_user_id` UUID nullable
* `accepted_at` timestamptz nullable

Constraint direction:

* acceptance of a suggestion must not itself mutate audit records automatically

### `ai_suggestion_feedback`

Purpose:

* capture whether a human accepted, rejected, or ignored a suggestion

Potential columns:

* `id` UUID
* `ai_suggestion_id` UUID
* `user_id` UUID
* `feedback_type` text
* `comment` text nullable
* `created_at` timestamptz

### `ai_analysis_runs`

Purpose:

* track traceable AI analysis executions for explainability and cost monitoring

Potential columns:

* `id` UUID
* `organization_id` UUID
* `analysis_type` text
* `target_entity_name` text
* `target_entity_id` UUID
* `status` text
* `prompt_version` text
* `model_name` text
* `started_at` timestamptz
* `completed_at` timestamptz nullable

## Additional Design Decisions

### Timestamp Strategy

* Use `timestamptz` for all timestamps.
* Use `created_at` and `updated_at` on mutable business tables.
* Use `timestamp` only for `audit_logs` naming consistency in the audit trail, but the type is still `timestamptz`.

### Soft Delete Strategy

Recommendation: do not use soft delete initially.

Why:

* Soft delete adds query complexity early.
* Audit records should usually be preserved by restrictive deletes rather than hidden.
* If deletion workflows become important later, introduce archive or lifecycle semantics deliberately instead of spreading `is_deleted` across all tables.
