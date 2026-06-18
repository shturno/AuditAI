# Security Notes

This document provides additional notes on the security posture of the AuditAI project. For the formal security policy, please see `SECURITY.md`.

## 1. Authentication (JWT)

*   **Mechanism**: We use JSON Web Tokens (JWT) for authenticating users.
*   **Flow**:
    1.  A user sends their credentials to an `/auth/login` endpoint.
    2.  The server validates the credentials and, if successful, returns a signed JWT.
    3.  The client stores this token and includes it in the `Authorization` header for all subsequent requests to protected endpoints (e.g., `Authorization: Bearer <token>`).
*   **Key Management**: The JWT signing key is a critical secret. It is managed via `user-secrets` in local development and must be stored securely (e.g., in Azure Key Vault or as an environment variable) in production.

## 2. Authorization (Role-Based)

*   **Mechanism**: We use role-based authorization to control access to resources.
*   **Roles**:
    *   `Admin`
    *   `Auditor`
    *   `Reviewer`
*   **Implementation**: In ASP.NET Core, this is implemented using the `[Authorize]` attribute, often with a specific role (e.g., `[Authorize(Roles = "Admin")]`). We may also use policy-based authorization for more complex rules.

## 3. Secret Management

*   **Rule #1**: No secrets are ever committed to the Git repository.
*   **Local Development**: We use the .NET Secret Manager (`dotnet user-secrets`). This stores secrets in a file on the local machine, outside of the project folder.
*   **Production**: Secrets must be provided through environment variables or a dedicated secret management service. The `docker-compose.yml` file is configured to read secrets from a `.env` file for local convenience, but this is not a secure production practice.

## 4. CORS (Cross-Origin Resource Sharing)

*   **Policy**: By default, the API should have a restrictive CORS policy.
*   **Configuration**: In a production environment, the CORS policy should be configured to only allow requests from known, trusted origins (e.g., the domain of the frontend application). A wildcard `*` policy is not safe for production.

## 5. HTTPS

*   **Requirement**: In any real deployment, the API must be served over HTTPS. This encrypts the traffic between the client and the server, protecting sensitive data (including JWTs) from being intercepted.
*   **Local Development**: For simplicity, the default project template may start with HTTP, but production readiness requires HTTPS.

## 6. Input Validation

*   **Importance**: All data coming from a client is untrusted. We must validate all input to prevent common vulnerabilities like injection attacks and to ensure data integrity.
*   **Implementation**: We use FluentValidation to define clear, declarative validation rules for our DTOs.

## 7. Error Handling

*   **No Sensitive Information**: The API should never expose internal exception details, stack traces, or other sensitive information in its error responses. We use a global error handling middleware to catch unhandled exceptions and return a generic `500 Internal Server Error` response.

## 8. Future AI Security

*   **Prompt Injection**: We will need to be vigilant about sanitizing any user-provided input that is passed to an AI model to prevent prompt injection.
*   **Data Leakage**: We must ensure that our AI integration does not inadvertently leak sensitive audit data to external providers or in its own logs.
*   **Model Security**: If we were to host our own models, we would need to secure them against unauthorized access and potential attacks.
