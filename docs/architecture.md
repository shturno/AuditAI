# AuditAI Architecture

This document provides an overview of the architecture of the AuditAI backend system. The project is built using **Clean Architecture** principles to create a system that is maintainable, testable, and scalable.

## 1. Clean Architecture Boundaries

The solution is divided into four main projects, each representing a layer in the architecture:

*   `AuditAI.Domain`
*   `AuditAI.Application`
*   `AuditAI.Infrastructure`
*   `AuditAI.Api`

These layers create clear boundaries and enforce a one-way dependency flow.

## 2. The Dependency Rule

The core principle of Clean Architecture is the **Dependency Rule**: *source code dependencies can only point inwards*.

```
Api -> Application -> Domain
Infrastructure -> Application
```

*   **Domain** is the innermost layer and has no dependencies on any other layer.
*   **Application** depends on Domain.
*   **Infrastructure** depends on Application (by implementing its interfaces).
*   **Api** depends on Application and Infrastructure.

This ensures that the core business logic (Domain) is completely independent of any technical implementation details.

## 3. Layer Responsibilities

### `AuditAI.Domain`

*   **Purpose**: Contains the core business logic and data of the application.
*   **Contents**:
    *   **Entities**: The core business objects (e.g., `Control`, `AuditFinding`).
    *   **Value Objects**: Immutable objects that represent a descriptive aspect of the domain (e.g., `Money`, `Address`).
    *   **Enums**: Controlled vocabularies for the domain (e.g., `AuditFindingSeverity`).
    *   **Domain Rules**: Logic that is central to the business, often encapsulated within entities.
    *   **Domain Exceptions**: Custom exceptions that represent a violation of a business rule.
*   **Key Rule**: This layer must not have any dependencies on infrastructure concerns like databases, networks, or specific frameworks. It's pure C# code representing the business.

### `AuditAI.Application`

*   **Purpose**: Orchestrates the use cases of the application. It defines what the application can do.
*   **Contents**:
    *   **Use Cases/Application Services**: Classes that orchestrate the flow of data from the API to the Domain and back.
    *   **DTOs (Data Transfer Objects)**: Simple objects used to transfer data between layers, especially between the API and Application.
    *   **Validators**: Rules for validating input data (e.g., using FluentValidation).
    *   **Interfaces**: Abstractions for infrastructure concerns (e.g., `IUserRepository`, `IEmailService`, `IAiAnalysisService`). These interfaces are implemented by the Infrastructure layer.
*   **Key Rule**: This layer defines *what* the application needs from the outside world (via interfaces) but not *how* it's implemented.

#### Application Feature Structure

We prefer a feature-oriented structure inside `AuditAI.Application` when a slice has enough behavior to justify it.

Example:

*   `Controls/Contracts`
*   `Controls/Validators`
*   `Controls/Interfaces`
*   `Controls/Services`

This keeps each vertical slice explicit without introducing a full CQRS/MediatR stack too early.

### `AuditAI.Infrastructure`

*   **Purpose**: Provides the technical implementation for the interfaces defined in the Application layer.
*   **Contents**:
    *   **Persistence**: EF Core `DbContext`, repository implementations, and database migrations.
    *   **External Services**: Clients for interacting with external APIs (e.g., sending emails, payment processing).
    *   **Authentication/Authorization**: Helpers for handling JWTs and user identity.
    *   **Logging**: Implementation of logging providers (e.g., Serilog).
*   **Key Rule**: This is the "dirty" layer where all the implementation details live. It depends on the Application layer to know which interfaces to implement.

Planned auth-related implementations belong here:

* password hashing
* JWT generation
* auth-oriented user lookups
* current-user support consumed from API context

### `AuditAI.Api`

*   **Purpose**: The entry point for external clients (e.g., a web browser, a mobile app). It handles HTTP-related concerns.
*   **Contents**:
    *   **Controllers/Endpoints**: Handle HTTP requests, call Application services, and return HTTP responses.
    *   **Middleware**: Custom middleware for concerns like error handling and request logging.
    *   **Dependency Injection**: Configuration for the IoC container.
    *   **Authentication/Authorization Setup**: Configuration for JWT bearer authentication.
*   **Key Rule**: Controllers should be "thin." Their job is to translate HTTP requests into calls to the Application layer and then translate the results back into HTTP responses. They should not contain business logic.

## 4. Interaction Flow Example

A typical request flows through the layers like this:

1.  An HTTP request hits a **Controller** in the `Api` layer.
2.  The Controller validates the request and maps it to a **DTO**.
3.  The Controller calls a method on an **Application Service** in the `Application` layer, passing the DTO.
4.  The Application Service uses a **Repository Interface** to fetch a **Domain Entity**.
5.  The Application Service executes business logic on the Domain Entity.
6.  The Application Service uses the Repository Interface to save the updated Entity. The implementation of this interface is in the `Infrastructure` layer, which uses **EF Core** to talk to the database.
7.  The Application Service maps the result to another DTO and returns it to the Controller.
8.  The Controller returns an HTTP response to the client.

## 5. Avoiding Anemic Overengineering

While Clean Architecture is powerful, it's important to avoid overengineering, especially in a project of this scale.

*   **Keep it Practical**: Not every class needs an interface. Not every piece of logic needs to be a complex service.
*   **Rich Domain Model**: Strive for a "rich" domain model where business logic lives inside the entities themselves, rather than an "anemic" model where entities are just bags of properties.
*   **Start Simple**: It's easier to add complexity later than to remove it. Start with a simple structure and refactor as the application grows.

## 6. Future AI Integration

The Clean Architecture model is ideal for integrating AI features responsibly.

*   **AI Interfaces in Application**: The `Application` layer will define interfaces for AI services, such as `IEvidenceSummarizationService` or `IRiskSuggestionService`.
*   **AI Implementations in Infrastructure**: The actual implementations of these interfaces, which will call external AI providers (like Azure OpenAI or Gemini), will live in the `Infrastructure` layer.
*   **No Vendor Lock-In**: Because the core business logic in the `Domain` and `Application` layers only depends on the interface, we can easily swap out one AI provider for another without changing the core of our application. The Domain layer remains completely unaware of any AI provider.

This approach ensures that our core audit workflow is not corrupted by AI-specific code and that we maintain control and flexibility over our AI strategy.
