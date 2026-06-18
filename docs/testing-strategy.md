# Testing Strategy

This document outlines the testing strategy for the AuditAI project, emphasizing a Test-Driven Development (TDD) approach where practical.

## 1. TDD Approach

Our goal is to let tests drive development, especially for business-critical logic.

*   **Red-Green-Refactor**: For each new piece of functionality, we aim to:
    1.  **Red**: Write a failing test that captures the requirement.
    2.  **Green**: Write the simplest possible code to make the test pass.
    3.  **Refactor**: Improve the code's design while keeping the test passing.
*   **Focus on Business Rules**: TDD is most valuable when applied to the core business rules in the `Domain` and `Application` layers.

## 2. What Must Be Tested First

*   **Domain Logic**: All business rules, validations, and state transitions within the domain entities must be covered by unit tests.
*   **Application Use Cases**: The primary success and failure paths of each application service should be tested.

## 3. Unit Test Rules

*   **Scope**: Unit tests focus on a single unit of code (e.g., a method or a class) in isolation.
*   **Location**: `tests/AuditAI.UnitTests`
*   **Dependencies**: All external dependencies (like repositories or other services) must be mocked or stubbed.
*   **Speed**: Unit tests must be fast.
*   **What to Test**:
    *   Domain entity behavior.
    *   Application service logic.
    *   Validators.
    *   Application result handling for validation and not-found flows.

## 4. Integration Test Rules

*   **Scope**: Integration tests verify that multiple components of the system work together correctly.
*   **Location**: `tests/AuditAI.IntegrationTests`
*   **What to Test**:
    *   **API Endpoints**: Test the full flow from an HTTP request to the database and back.
    *   **Database Interactions**: Verify that the EF Core repository implementations work as expected.
*   **Environment**: Integration tests will run against a real (but temporary or containerized) database to ensure accuracy.

## 5. Test Naming Convention

We use a clear, descriptive naming convention for our tests:

`Should_ExpectedBehavior_When_StateUnderTest`

**Examples**:

*   `Should_CreateAuditFinding_When_ValidDataIsProvided`
*   `Should_ThrowException_When_DueDateIsInThePast`
*   `Should_RejectEvidence_When_RejectionReasonIsMissing`

## 6. Arrange, Act, Assert (AAA) Convention

Every test method should be structured with three distinct parts:

*   **Arrange**: Set up the test. Initialize objects, mock dependencies, and prepare the input data.
*   **Act**: Execute the method being tested.
*   **Assert**: Verify the outcome. Check the return value, assert that a specific exception was thrown, or verify that a mock was called.

## 7. What Should Not Be Tested

*   **Third-Party Libraries**: We don't test the behavior of external libraries like EF Core or FluentValidation themselves. We test that we are *using* them correctly.
*   **Simple Properties**: We don't write tests for simple C# properties (getters and setters).
*   **Implementation Details**: Tests should focus on the public behavior of a unit, not its private implementation details.

## 8. Minimum Expectations Before Merging

*   Every new feature or bug fix must be accompanied by relevant tests.
*   All existing tests must pass.
*   The overall test coverage should not decrease.

## 9. Future AI Testing Strategy

Testing AI-integrated features requires a specific approach, as we cannot rely on the deterministic output of an AI model.

*   **Mock AI Services**: In our unit and integration tests, we will **always** mock the AI service interfaces (e.g., `IEvidenceSummarizationService`). The mock will return a predictable, hardcoded response.
*   **Test the System's Reaction**: Our tests will focus on how our application *behaves* when it receives a response from the AI service.
    *   Does it handle a successful response correctly?
    *   Does it handle an error or a null response gracefully?
    *   Does it enforce safety boundaries (e.g., not allowing an AI suggestion to automatically resolve a finding)?
*   **Do Not Test the Model Itself**: We will not write tests that try to validate the quality or correctness of the AI model's output. That is the responsibility of the model provider. Our responsibility is to test the integration.
