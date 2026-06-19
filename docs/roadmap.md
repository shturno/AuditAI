# Project Roadmap

This document outlines the planned development phases for the AuditAI project.

## Phase 1: Foundation and Skeleton (In Progress)

*   **Goal**: Set up the complete repository structure, documentation, and project skeleton.
*   **Tasks**:
    *   [x] Create solution and project structure (`Api`, `Application`, `Domain`, `Infrastructure`, `Tests`).
    *   [x] Create root documentation files (`README.md`, `SECURITY.md`, `CONTRIBUTING.md`, etc.).
    *   [x] Create detailed documentation in the `/docs` folder (`architecture.md`, `ai-roadmap.md`, etc.).
    *   [x] Set up `.editorconfig`, `Directory.Build.props`, and `.gitignore`.
    *   [x] Create `docker-compose.yml` for PostgreSQL and the API.
    *   [x] Set up initial GitHub Actions workflow for CI.
    *   [ ] Add a minimal health check endpoint to verify the API boots.

## Phase 2: Core Domain and Persistence

*   **Goal**: Define the core business entities and set up the database.
*   **Tasks**:
    *   [ ] Define all `Domain` entities (`User`, `Control`, `Evidence`, etc.).
    *   [ ] Define `Domain` enums (`EvidenceStatus`, `AuditFindingSeverity`, etc.).
    *   [ ] Implement the EF Core `DbContext` in the `Infrastructure` layer.
    *   [ ] Configure entity relationships and constraints.
    *   [ ] Create the initial database migration.
    *   [ ] Add seed data for local development.

## Phase 3: Application Logic and API Endpoints

*   **Goal**: Implement the core use cases and expose them via the API.
*   **Tasks**:
    *   [ ] Create `Application` services for each core entity.
    *   [ ] Define DTOs and validation rules (using FluentValidation).
    *   [ ] Implement repository interfaces in the `Infrastructure` layer.
    *   [ ] Create API controllers and endpoints for all CRUD operations.
    *   [ ] Integrate Swagger for API documentation.

## Phase 4: Authentication and Authorization

*   **Goal**: Secure the API.
*   **Tasks**:
    *   [ ] Implement minimal `password_hash` support on users.
    *   [ ] Implement JWT generation and validation.
    *   [ ] Create `/auth/login` endpoint.
    *   [ ] Add `ICurrentUser` support for actor resolution.
    *   [ ] Replace request-provided actor ids with authenticated actor where appropriate.
    *   [ ] Add `[Authorize]` attributes to protected endpoints.
    *   [ ] Implement role-based and organization-bound authorization policies.

## Phase 5: Testing and Quality

*   **Goal**: Ensure the application is robust and reliable.
*   **Tasks**:
    *   [ ] Write unit tests for all `Domain` and `Application` logic.
    *   [ ] Write integration tests for critical API endpoints.
    *   [ ] Set up code coverage reporting.
    *   [ ] Run `dotnet format` to ensure code style consistency.

## Phase 6: Advanced Features and AI Readiness

*   **Goal**: Add advanced business logic and prepare for AI integration.
*   **Tasks**:
    *   [x] Implement the dashboard summary endpoint.
    *   [ ] Implement audit logging for sensitive actions.
    *   [ ] Define the AI service interfaces in the `Application` layer (e.g., `IEvidenceSummarizationService`).
    *   [ ] Create mock implementations of the AI interfaces for testing.

## Phase 7: Polish and Final Review

*   **Goal**: Finalize the project for portfolio presentation.
*   **Tasks**:
    *   [ ] Review and polish all documentation.
    *   [ ] Ensure the `README.md` is comprehensive and easy to follow.
    *   [ ] Clean up any remaining code smells or warnings.
    *   [ ] Verify that the local development experience is smooth.
