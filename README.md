# AuditAI API

AuditAI is a backend API for internal audit and compliance workflows, designed to manage controls, evidence, audit findings, action plans, and future AI-assisted audit analysis.

## Why This Project Exists

This project serves as a public portfolio piece for a Junior .NET Backend Developer. It aims to demonstrate practical, real-world backend engineering skills, including:

*   **Business-Oriented Design**: Modeling a realistic business domain (internal audit).
*   **Clean Architecture**: Building a maintainable and scalable solution with clear separation of concerns.
*   **Security Awareness**: Implementing fundamental security practices like authentication, authorization, and secret management.
*   **Testing Discipline**: Ensuring code quality and correctness through unit and integration tests.
*   **Professional Documentation**: Creating clear and comprehensive documentation for developers and stakeholders.
*   **Modern .NET Practices**: Utilizing the latest .NET features and best practices.
*   **Responsible AI Readiness**: Designing a system that is prepared for future AI integration without compromising core principles.

**Disclaimer**: This is a portfolio project, not production-ready software. It is designed to showcase technical skills in a realistic business context.

## The Business Problem

Internal audit and compliance teams in many organizations rely on a mix of spreadsheets, documents, and disparate systems to manage their workflows. This leads to several challenges:

*   **Inefficiency**: Manual tracking of controls, evidence, and findings is time-consuming and error-prone.
*   **Lack of Visibility**: It's difficult to get a real-time overview of the organization's compliance posture.
*   **Poor Traceability**: Connecting audit findings to specific controls and evidence is challenging.
*   **Scalability Issues**: As the organization grows, the complexity of managing audits increases exponentially.
*   **AI Adoption Gap**: Existing systems are often not designed to leverage modern AI capabilities for analysis and assistance.

AuditAI provides a centralized, API-first backend to address these problems, creating a single source of truth for audit and compliance data and laying the groundwork for future AI-powered insights.

## Features

*   **Core Audit Management**: Manage organizations, departments, users, controls, evidence, findings, and action plans.
*   **Role-Based Access Control (RBAC)**: Secure access to resources based on user roles (Admin, Auditor, Reviewer).
*   **JWT Authentication**: Secure API endpoints using JSON Web Tokens.
*   **Validation**: Enforce business rules with server-side validation.
*   **Audit Logging**: Track sensitive actions for accountability and traceability.
*   **Dashboard Summary**: Provide an aggregated view of key audit metrics.
*   **Docker Support**: Run the entire application stack locally with a single command.

## Future AI Vision

The long-term vision for AuditAI is to enhance the audit process with AI-assisted capabilities. The current architecture is designed to be extended with AI features in a responsible and modular way.

Key principles of our AI strategy:

*   **Human-in-the-Loop**: AI provides suggestions and analysis; humans make the final decisions.
*   **Provider-Agnostic**: The system is not tied to any specific AI vendor (e.g., OpenAI, Azure, Gemini).
*   **Data Privacy**: Sensitive audit data is handled with care, and AI integrations are designed to respect privacy.
*   **Traceability**: All AI-generated outputs are clearly marked and traceable.

Future AI modules may include:

*   Evidence Summarization
*   Risk Severity Suggestions
*   Compliance Gap Detection
*   Semantic Search over Audit Evidence

## Tech Stack

*   **.NET 8 LTS**
*   **C#**
*   **ASP.NET Core Web API**
*   **PostgreSQL**
*   **Entity Framework Core**
*   **Docker Compose**
*   **JWT Authentication**
*   **xUnit** (for testing)
*   **FluentValidation**
*   **Serilog**
*   **Swagger/OpenAPI**
*   **GitHub Actions**

## Architecture Overview

AuditAI is built using **Clean Architecture** principles, which creates a clear separation of concerns and a dependency rule that points inwards.

*   **Domain**: Contains the core business logic, entities, and rules. It has no dependencies on other layers.
*   **Application**: Orchestrates the use cases of the application. It defines interfaces that are implemented by the Infrastructure layer.
*   **Infrastructure**: Provides implementations for the interfaces defined in the Application layer, such as databases, external services, and logging.
*   **API**: The entry point to the system, responsible for handling HTTP requests, authentication, and presenting data to the client.

This structure ensures that the core business logic is independent of technical details, making the system easier to maintain, test, and evolve.

### Folder Structure

```
/src
    /AuditAI.Api
    /AuditAI.Application
    /AuditAI.Domain
    /AuditAI.Infrastructure
/tests
    /AuditAI.UnitTests
    /AuditAI.IntegrationTests
/docs
    /architecture.md
    /business-rules.md
    /...
```

## Business Rules Summary

*   Users must be authenticated to access protected resources.
*   Roles (Admin, Auditor, Reviewer) have specific permissions.
*   Evidence must be linked to a Control, and Findings must be linked to Evidence.
*   Entities have defined status workflows (e.g., Evidence can be `Pending`, `Accepted`, `Rejected`).
*   Critical findings cannot be resolved if they have open action plans.
*   All sensitive actions are logged.

For a complete list, see [docs/business-rules.md](docs/business-rules.md).

## Security Considerations

*   **Authentication**: Handled via JWT.
*   **Authorization**: Based on user roles and policies.
*   **Secret Management**: Uses `user-secrets` for local development and environment variables in production. **No secrets are ever committed to the repository.**
*   **Data Handling**: The system is designed to handle sensitive audit data, with logging and access controls in place.
*   **Dependency Management**: Dependencies are regularly scanned for vulnerabilities.

For more details, see [SECURITY.md](SECURITY.md).

## How to Run Locally

### Prerequisites

*   [.NET 8 SDK](https://dotnet.microsoft.com/download)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Clone the Repository

```bash
git clone https://github.com/shturno/AuditAI.git
cd AuditAI
```

### 2. Configure Environment Variables

Create a `.env` file in the root directory by copying the example:

```bash
cp .env.example .env
```

Update the `.env` file with your local settings, especially the `DB_PASSWORD`.

For local development secrets like the JWT key, use the .NET Secret Manager:

```bash
dotnet user-secrets init --project src/AuditAI.Api
dotnet user-secrets set "Jwt:Key" "YourSuperSecretKeyThatIsLongAndSecure"
dotnet user-secrets set "Jwt:Issuer" "AuditAI"
dotnet user-secrets set "Jwt:Audience" "AuditAI.Users"
```

### 3. Run with Docker Compose

This is the recommended way to run the application locally. It will start the API and a PostgreSQL database.

```bash
docker-compose up --build
```

The API will be available at `http://localhost:5000`.

### 4. Database Setup

The database is managed by EF Core migrations. When the application starts for the first time, it will automatically apply any pending migrations to the database.

## How to Run Tests

Run all tests from the root directory:

```bash
dotnet test
```

## API Documentation

Once the application is running, you can access the Swagger UI for interactive API documentation at:

`http://localhost:5000/swagger`

## Roadmap

See [docs/roadmap.md](docs/roadmap.md) for the planned development phases.

## AI Roadmap

See [docs/ai-roadmap.md](docs/ai-roadmap.md) for the vision and strategy for integrating AI features.

## What This Project Demonstrates

This project is designed to showcase the following skills and qualities:

*   **Strong Backend Fundamentals**: Proficiency in C#, .NET, and ASP.NET Core.
*   **Architectural Thinking**: Ability to design and implement a clean, maintainable architecture.
*   **Business Acumen**: Understanding and modeling real-world business processes.
*   **Attention to Detail**: Focus on documentation, testing, and code quality.
*   **Forward-Looking Mindset**: Designing for future requirements, such as AI integration.
*   **Professionalism**: A commitment to best practices in software development.
