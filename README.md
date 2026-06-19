# AuditAI

Clean Architecture backend for audit and compliance workflows.

## Overview

AuditAI is a .NET 8 backend API that models internal audit and compliance workflows. It demonstrates backend development skills through a domain-specific application built with Clean Architecture principles.

The project implements a vertical slice of audit operations: organizations manage controls, submit evidence, identify audit findings, create action plans, and maintain an audit trail. All operations are tenant-scoped, JWT-protected, and RBAC-enforced.

This is a portfolio project focused on backend architecture, domain modeling, and testing practices—not a production-ready audit platform.

## Feature Highlights

- **JWT Authentication**: Custom token-based auth with password hashing
- **RBAC Authorization**: Role-based access control (Admin, Auditor, Reviewer)
- **Tenant Scoping**: Organization-based multi-tenancy
- **Audit Workflow**: Controls → Evidence → Audit Findings → Action Plans
- **Audit Logs**: Append-only audit trail with authenticated actor tracking
- **Dashboard Summary**: Read-only aggregate statistics endpoint
- **PostgreSQL Persistence**: Full-text search and relational data
- **EF Core Migrations**: Version-controlled database schema
- **Comprehensive Testing**: Unit tests and integration tests with PostgreSQL Testcontainers
- **Clean Architecture**: Clear layer boundaries with dependency inversion

## Architecture

AuditAI follows Clean Architecture principles with four main layers:

```
┌─────────────────────────────────────────────────────────────┐
│                         API Layer                            │
│  (Controllers, HTTP handling, JWT middleware, DI config)    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                      Application Layer                       │
│  (Use cases, DTOs, validators, interfaces, Result pattern)  │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                         Domain Layer                         │
│  (Entities, enums, business rules, domain exceptions)       │
└─────────────────────────────────────────────────────────────┘
                              ↑
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure Layer                      │
│  (EF Core, repositories, migrations, JWT, PostgreSQL)       │
└─────────────────────────────────────────────────────────────┘
```

**Dependency Rule**: Dependencies flow inward. The Domain layer has no external dependencies. The Application layer depends on Domain. The Infrastructure layer depends on Application. The API layer depends on both Application and Infrastructure.

## Domain Workflow

```
Organization
    ↓
Department
    ↓
User
    ↓
Control (with status: Proposed, Active, Inactive)
    ↓
Evidence (with status: Pending, Accepted, Rejected)
    ↓
Audit Finding (with severity: Low, Medium, High, Critical)
    ↓
Action Plan (with status: Open, InProgress, Completed, Overdue, Cancelled)
    ↓
Audit Log (append-only, actor-tracked)
    ↓
Dashboard Summary (read-only aggregates)
```

## Main Business Rules

- **Evidence Lifecycle**: Evidence starts in `Pending` status. Rejected evidence requires a rejection reason. Evidence can only be reviewed once.
- **Finding Severity**: Audit findings have severity levels (Low, Medium, High, Critical) and status transitions (Open → InProgress → Resolved/Cancelled).
- **Critical Findings**: Critical findings cannot be resolved while blocking action plans exist.
- **Action Plan Status**: Action plans have status transitions (Open → InProgress → Completed/Overdue/Cancelled).
- **Audit Logs**: Append-only audit trail. Created by Application services after successful sensitive operations. No public write/update/delete endpoints.
- **Tenant Boundary**: All data is scoped to the authenticated user's organization. No cross-organization data leakage.

## Authentication and Authorization

### Authentication

- Custom JWT login endpoint: `POST /api/auth/login`
- Password hashing using ASP.NET Core `PasswordHasher`
- No ASP.NET Identity, no public registration, no refresh tokens
- JWT claims include: `sub` (user ID), `email`, `role`, `org_id`, optional `department_id`
- JWT expiration configurable via environment variables

### Authorization

**Roles**:
- `Admin`: Full access to all resources
- `Auditor`: Read and write access to controls, evidence, findings, action plans
- `Reviewer`: Read access to all resources, can accept/reject evidence

**RBAC Matrix**:

| Resource | Read | Write | Review |
|----------|------|-------|--------|
| Controls | Admin, Auditor, Reviewer | Admin, Auditor | — |
| Evidence | Admin, Auditor, Reviewer | Admin, Auditor | Admin, Reviewer |
| Audit Findings | Admin, Auditor, Reviewer | Admin, Auditor | — |
| Action Plans | Admin, Auditor, Reviewer | Admin, Auditor | — |
| Audit Logs | Admin, Auditor | — | — |
| Dashboard | Admin, Auditor, Reviewer | — | — |

### Tenant Scoping

- All queries are scoped to `CurrentUser.OrganizationId` from JWT
- No organization ID from query/body accepted
- Cross-tenant data is never included

## API Overview

### Authentication

- `POST /api/auth/login` - Authenticate user and receive JWT token

### Controls

- `GET /api/controls` - List controls with pagination
- `GET /api/controls/{id}` - Get control by ID
- `POST /api/controls` - Create control
- `PUT /api/controls/{id}` - Update control
- `DELETE /api/controls/{id}` - Deactivate control

### Evidence

- `GET /api/evidence` - List evidence with pagination
- `GET /api/evidence/{id}` - Get evidence by ID
- `POST /api/evidence` - Submit evidence for a control
- `PUT /api/evidence/{id}/accept` - Accept evidence
- `PUT /api/evidence/{id}/reject` - Reject evidence with reason

### Audit Findings

- `GET /api/audit-findings` - List findings with pagination
- `GET /api/audit-findings/{id}` - Get finding by ID
- `POST /api/audit-findings` - Create finding
- `PUT /api/audit-findings/{id}` - Update finding
- `PUT /api/audit-findings/{id}/status` - Change finding status

### Action Plans

- `GET /api/action-plans` - List action plans with pagination
- `GET /api/action-plans/{id}` - Get action plan by ID
- `POST /api/action-plans` - Create action plan
- `PUT /api/action-plans/{id}` - Update action plan
- `PUT /api/action-plans/{id}/status` - Change action plan status

### Audit Logs

- `GET /api/audit-logs` - List audit logs with pagination
- `GET /api/audit-logs/{id}` - Get audit log by ID

### Dashboard

- `GET /api/dashboard/summary` - Get dashboard summary (read-only aggregates)

## Dashboard Summary

The dashboard summary endpoint provides read-only aggregate statistics for the authenticated user's organization:

- **Controls**: Total, Active, Inactive counts
- **Evidence**: Total, Pending, Accepted, Rejected counts
- **Audit Findings**: Total, status breakdown, severity breakdown, unresolved critical count
- **Action Plans**: Total, status breakdown, overdue count, due-soon count
- **Recent Activity**: Last N audit logs (configurable, default 5, max 20)

**Query Parameters**:
- `recentLimit` (int, default 5, max 20): Number of recent audit logs to return
- `includeRecentActivity` (bool, default true): Whether to include recent activity

**Behavior**:
- JWT protected
- Available to Admin, Auditor, Reviewer
- Tenant-scoped by authenticated user's organization
- Returns 401 if not authenticated
- Returns 403 if organization is not set or role is not authorized

## Testing

### Test Strategy

- **Unit Tests**: Test Application layer behavior, validators, and domain logic in isolation
- **Integration Tests**: Test full HTTP request/response cycles with PostgreSQL Testcontainers

### Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test --filter "FullyQualifiedName~UnitTests"

# Run only integration tests
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### Test Coverage

Tests cover:
- Authentication and authorization (JWT, RBAC)
- Tenant scoping across all resources
- Audit log creation and querying
- Domain business rules and status transitions
- Input validation
- Error handling

## Local Development Setup

### Prerequisites

- .NET 8 SDK
- Docker
- Docker Compose

### Setup Steps

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd auditai
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Start PostgreSQL via Docker Compose**
   ```bash
   docker compose up -d
   ```

4. **Run migrations**
   ```bash
   dotnet tool restore
   dotnet tool run dotnet-ef database update --project src/AuditAI.Infrastructure --startup-project src/AuditAI.Api
   ```

5. **Build the solution**
   ```bash
   dotnet build
   ```

6. **Run the API**
   ```bash
   dotnet run --project src/AuditAI.Api
   ```

7. **Run tests**
   ```bash
   dotnet test
   ```

## Configuration

Configure the application using environment variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=localhost;Port=5432;Database=auditai;Username=postgres;Password=postgres` |
| `JwtSettings__Issuer` | JWT token issuer | `AuditAI` |
| `JwtSettings__Audience` | JWT token audience | `https://auditai.local` |
| `JwtSettings__Secret` | JWT signing key (minimum 32 characters) | `your-super-secret-jwt-key-at-least-32-chars` |
| `JwtSettings__ExpirationMinutes` | JWT token expiration in minutes | `60` |

**Important**: Never commit real secrets. Use `.env.example` as a template and add `.env` to `.gitignore`.

## Database and Migrations

- **Database**: PostgreSQL
- **ORM**: EF Core with Npgsql provider
- **Migrations**: Located in `src/AuditAI.Infrastructure/Persistence/Configurations/`

### Adding a Migration

```bash
dotnet ef migrations add <MigrationName> --project src/AuditAI.Infrastructure --startup-project src/AuditAI.Api
```

### Updating the Database

```bash
dotnet ef database update --project src/AuditAI.Infrastructure --startup-project src/AuditAI.Api
```

### Reverting a Migration

```bash
dotnet ef database update <PreviousMigrationName> --project src/AuditAI.Infrastructure --startup-project src/AuditAI.Api
```

## Design Choices

### No Framework Magic

- **No MediatR**: Direct service calls instead of CQRS/MediatR
- **No AutoMapper**: Manual mapping with records and DTOs
- **No Generic Repository**: Repository interfaces are specific to each entity
- **No ASP.NET Identity**: Custom JWT authentication with existing User entity

### Result Pattern

Expected failures use the `Result<T>` pattern instead of exceptions:
- `Result.Success(value)` - Operation succeeded
- `Result<T>.Unauthorized(message)` - Authentication required
- `Result<T>.Forbidden(message)` - Authorization required
- `Result<T>.NotFound(message)` - Resource not found
- `Result<T>.ValidationFailure(errors)` - Validation errors

### Testcontainers

Integration tests use PostgreSQL Testcontainers for realistic database behavior without manual setup. Tests spin up a temporary PostgreSQL container, run migrations, execute test scenarios, and clean up automatically.

### Clean Architecture

Controllers stay thin. Domain entities contain business logic. Application services orchestrate use cases. Infrastructure implements persistence and external services. This separation makes the codebase maintainable and testable.

## Current Limitations

This is a portfolio project with intentional limitations:

- **No frontend**: API-only implementation
- **No public registration**: Users must be created manually
- **No refresh tokens**: Single-use JWT tokens
- **No user management**: No endpoints to create/update/delete users
- **No AI features**: AI integration is planned but not implemented
- **No deployment pipeline**: No CI/CD configuration
- **No advanced reporting**: Basic dashboard summary only

## Future Work

Potential enhancements (not currently implemented):

- Refresh tokens and token refresh endpoint
- User management endpoints (CRUD for users)
- Password reset flow
- Multi-factor authentication (MFA)
- OAuth/OIDC integration
- AI-assisted evidence summarization
- AI-suggested action plans based on findings
- Notification/reminder workflows for overdue action plans
- Advanced reporting and export functionality
- Bulk operations for controls and evidence
- File upload handling for evidence attachments

## Repository Status

This is a portfolio project demonstrating backend development skills. It is actively developed as a learning exercise and not production-ready without further hardening.

## License

MIT License - see LICENSE file for details