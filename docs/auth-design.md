# Authentication And Authorization Design

This document defines the recommended authentication, authorization, and actor-resolution plan for AuditAI before implementation.

The goal is to add security in a way that fits the current Clean Architecture, keeps the code understandable, and avoids introducing a large identity system too early.

## 1. Recommendation

Recommended approach: custom JWT authentication using the existing `User` entity.

Why this is the best fit now:

* The project already has a `User` entity and `users` table.
* The current roles are simple: `Admin`, `Auditor`, `Reviewer`.
* This is a portfolio backend, so clarity matters more than enterprise-level identity surface area.
* A custom JWT flow is easy to understand, test, and explain in a code review.
* It keeps the Domain model clean because auth mechanics stay in Application/Infrastructure/API.

Not recommended right now:

* ASP.NET Core Identity
  * It is a valid production option, but it adds many concepts, tables, flows, and framework conventions that the project does not need yet.
  * It would make the codebase heavier before the project even has basic login and tenant checks.
* External providers or OAuth
  * Not needed for the current internal-system scope.
  * Adds complexity without solving a current business problem.

## 2. Current User Model Review

Current `User` entity already has:

* `Email`
* `FullName`
* `Role`
* `OrganizationId`
* `DepartmentId`

Current `User` entity does not have:

* `PasswordHash`
* `IsActive`
* `LastLoginAt`
* security stamp or token version

Minimum schema change needed later:

* add `password_hash` text/varchar not null

Recommended near-future but still minimal optional fields:

* `is_active` boolean not null default `true`

`is_active` is not strictly required for the first auth step, but it is a useful minimal control for disabling access without deleting users.

Expected migration later:

* add `password_hash`
* optionally add `is_active`

No auth schema change is needed now because this document is design-only.

## 3. Clean Architecture Placement

### Domain

Keep Domain free from auth infrastructure.

Domain should only keep:

* `User` identity and role as business data
* no JWT logic
* no password hashing
* no claims logic
* no HTTP identity assumptions

### Application

Application should define small interfaces:

* `IUserAuthRepository`
* `IPasswordHasher`
* `IJwtTokenGenerator`
* `ICurrentUser`

Recommended responsibilities:

* `IUserAuthRepository`
  * get user by email
  * get user by id
  * later persist login-related updates if needed
* `IPasswordHasher`
  * hash password
  * verify password against hash
* `IJwtTokenGenerator`
  * build signed access token from authenticated user
* `ICurrentUser`
  * expose `UserId`, `OrganizationId`, `Role`, and authentication status for current request

### Infrastructure

Infrastructure should implement:

* EF Core repository for auth user lookup
* password hashing implementation
* JWT token generation

### API

API should provide:

* JWT bearer setup later
* HTTP-based `ICurrentUser` implementation reading claims from `HttpContext.User`

## 4. Planned Use Cases

Recommended minimal auth use cases:

* `LoginService`
* `GetCurrentUserService` optional, only if the frontend needs a `/me` style endpoint later

Do not add public self-registration now.

Reason:

* AuditAI is an internal audit/compliance system.
* Public registration does not fit the business context well.
* A better model is admin-created users later, or seeded bootstrap admin user for local/demo environments.

Possible later use cases:

* `CreateUserByAdminService`
* `ChangePasswordService`
* `DeactivateUserService`

But those are not needed for the first auth implementation step.

## 5. JWT Claims Plan

Recommended claims:

* `sub`: user id
* `email`: user email
* `role`: user role
* `org_id`: organization id
* `department_id`: optional, if present

Rules:

* do not include `password_hash`
* do not include secrets
* do not include permission lists yet
* do not include mutable profile data beyond what is useful for authorization and request context

Recommended token behavior later:

* short-lived access token
* no refresh tokens yet

## 6. Authorization Plan

Authorization should be layered, not pushed into Domain entities.

### Role plan

Future role direction:

* `Admin`
  * manage users, organizations, departments
  * view and mutate all audit records inside the organization
* `Auditor`
  * create and update controls
  * submit evidence
  * create and update findings
  * create and update action plans
* `Reviewer`
  * review evidence
  * view findings and action plans
  * limited mutation focused on review flows

### Tenant boundary

Tenant boundary rule:

* authenticated users should only access data inside their own `OrganizationId`

Where this should live later:

* request authentication in API
* use-case enforcement in Application
* optional policy helpers in API for role checks

Where this should not live:

* Domain entities
* controllers as ad hoc `if` statements

## 7. AuditLog Actor Resolution Plan

Current state:

* some logs use request-provided user ids
* some logs store `null`

Planned future behavior:

* `AuditLog.UserId` should come from `ICurrentUser.UserId`
* actor should not be trusted from request body once auth exists

Important distinction:

* actor identity is authentication data
* entity-specific user fields can still remain business data

Examples:

* `ActionPlan.AssignedToUserId` remains business data
* `Evidence.SubmittedByUserId` likely stops coming from request and becomes current actor
* `Evidence.ReviewerUserId` likely stops coming from request and becomes current actor
* `AuditFinding.CreatedByUserId` likely stops coming from request and becomes current actor

This separation matters:

* the actor is who performed the command
* business data may still reference another user for assignment or ownership

## 8. Existing Request Contracts Likely To Change Later

These contracts will likely need refactor once auth exists:

* `CreateEvidenceRequest.SubmittedByUserId`
  * likely removed from request
  * derive from `ICurrentUser`
* `ReviewEvidenceRequest.ReviewerUserId`
  * likely removed from request
  * derive from `ICurrentUser`
* `CreateAuditFindingRequest.CreatedByUserId`
  * likely removed from request
  * derive from `ICurrentUser`

Likely unchanged:

* `CreateControlRequest`
  * no actor field now, which is fine
* `CreateActionPlanRequest.AssignedToUserId`
  * keep as business field
* `UpdateActionPlanRequest.AssignedToUserId`
  * keep as business field

## 9. Security Plan

When implementation starts later, follow these rules:

* hash passwords with a strong adaptive hasher
* never store plaintext passwords
* keep JWT secret in environment or secret store only
* do not commit secrets to `appsettings`
* set token expiration
* do not log tokens
* do not include secrets in claims
* serve authenticated APIs over HTTPS in real deployment
* do not fake current-user resolution in production code

Recommended hashing implementation later:

* use a standard .NET password hashing approach with a dedicated abstraction

Recommended token behavior later:

* one access token
* no refresh token flow yet
* keep the first implementation small and reviewable

## 10. Test Plan

Future authentication tests should include:

* login succeeds with correct credentials
* login fails with wrong password
* login fails for missing user
* password verification works against stored hash
* protected endpoint returns `401` without token
* protected endpoint returns `403` when role is insufficient
* organization boundary prevents cross-tenant access
* `AuditLog.UserId` uses current authenticated user
* JWT claims do not expose sensitive fields

Recommended test layering:

* unit tests for `LoginService`
* unit tests for password hashing abstraction contract
* integration tests for API auth middleware and protected endpoints
* integration tests for tenant-boundary enforcement

## 11. Suggested Implementation Sequence

Recommended order for the later implementation work:

1. Add `password_hash` to user model and database.
2. Add `IUserAuthRepository`, `IPasswordHasher`, `IJwtTokenGenerator`, and `ICurrentUser`.
3. Implement `LoginService`.
4. Add JWT setup in API and Infrastructure.
5. Add a minimal `/auth/login` endpoint.
6. Introduce `ICurrentUser` in business services and stop trusting actor ids from request contracts.
7. Protect selected endpoints.
8. Add role and organization enforcement incrementally.

This order minimizes disruption and lets the team validate the auth foundation before RBAC and tenant authorization spread across all slices.
