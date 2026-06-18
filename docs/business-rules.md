# Business Rules

This document outlines the core business rules, entities, and workflows for the AuditAI system.

## 1. Business Glossary

*   **Control**: A specific policy, procedure, or activity designed to mitigate a risk.
*   **Evidence**: A document, record, or file submitted to demonstrate that a control is operating effectively.
*   **Audit Finding**: A conclusion reached by an auditor that a control is not effective or that there is a compliance gap.
*   **Action Plan**: A set of steps to be taken to remediate an audit finding.
*   **Audit Log**: An immutable record of significant actions taken within the system.

## 2. Entity Descriptions

*   **User**: Represents a person who can log in to the system. Has an assigned role.
*   **Organization**: The top-level entity. A company or a major division.
*   **Department**: A business unit within an organization.
*   **Control**: The central entity for an audit. Belongs to a Department.
*   **Evidence**: Linked to a single Control. Contains metadata and a reference to the stored file.
*   **AuditFinding**: Linked to a single Control. Describes a problem or gap.
*   **ActionPlan**: Linked to a single AuditFinding. Details the remediation steps.
*   **AuditLog**: Records who did what, and when.

## 3. Status Workflows

### Evidence Status

*   `Pending`: The initial status when evidence is submitted.
*   `Accepted`: A Reviewer has approved the evidence.
*   `Rejected`: A Reviewer has rejected the evidence.

### Audit Finding Severity

*   `Low`: Minor issue with limited impact.
*   `Medium`: Moderate issue that requires attention.
*   `High`: Serious issue that could have a significant impact.
*   `Critical`: A critical failure that requires immediate attention.

### Audit Finding Status

*   `Open`: The initial status when a finding is created.
*   `InProgress`: The finding is being investigated or remediated.
*   `Resolved`: The finding has been addressed and is considered closed.
*   `Cancelled`: The finding was deemed invalid or is no longer relevant.

### Action Plan Status

*   `Open`: The initial status when an action plan is created.
*   `InProgress`: The action plan is being worked on.
*   `Completed`: All steps in the action plan have been finished.
*   `Overdue`: The action plan has passed its due date.
*   `Cancelled`: The action plan is no longer necessary.

## 4. Role Permissions

*   **Admin**:
    *   Manage organizations, departments, and users.
    *   Configure global settings.
*   **Auditor**:
    *   Create and update controls.
    *   Submit evidence.
    *   Create audit findings and action plans.
*   **Reviewer**:
    *   Review submitted evidence.
    *   Update the status of evidence (`Accepted` or `Rejected`).

## 5. Validation Rules

*   An authenticated user is required for all protected actions.
*   Evidence must belong to a valid, existing Control.
*   An AuditFinding must be linked to a Control.
*   An ActionPlan must be linked to an AuditFinding.
*   An ActionPlan's due date cannot be in the past.
*   When a Reviewer rejects evidence, a rejection reason is mandatory.
*   A `Critical` finding cannot be marked as `Resolved` if it has any `Open` or `InProgress` action plans.

## 6. Audit Logging Rules

The following actions must generate an `AuditLog` entry:

*   User login/logout.
*   Creation, update, or deletion of a Control.
*   Submission of Evidence.
*   Acceptance or rejection of Evidence.
*   Creation or resolution of an AuditFinding.
*   Creation or completion of an ActionPlan.
*   Changes to user roles or permissions.

## 7. Dashboard Aggregation Rules

The main dashboard will provide a summary of:

*   Total number of controls.
*   Number of evidence items with a `Pending` status.
*   Number of `Open` audit findings.
*   Number of `Critical` audit findings.
*   Number of `Overdue` action plans.

## 8. AI-Assisted Workflow Principles

The future integration of AI will follow these core principles:

*   **Advisory, Not Authoritative**: AI will provide suggestions, summaries, and analysis. It will **never** have the authority to make final decisions.
*   **Human-in-the-Loop**: A human user (e.g., an Auditor or Reviewer) must always be the one to approve, reject, resolve, or delete any audit data.
*   **Traceability**: When an AI-assisted action is taken (e.g., accepting an AI's suggested risk severity), the system will log that the action was based on an AI suggestion.
*   **No Automatic State Changes**: AI modules will not be allowed to automatically change the status of critical entities like `AuditFinding` or `ActionPlan`.

## 9. Out-of-Scope Features (for now)

*   Real-time notifications.
*   File storage and management (we will store metadata, but the initial implementation may not include file uploads).
*   Direct integration with third-party systems.
*   Any concrete AI model implementation.
