# API Design

This document outlines the design conventions and standards for the AuditAI REST API.

## 1. REST Conventions

We follow standard RESTful principles for our API design.

*   **Resources**: The API is organized around resources (e.g., `controls`, `findings`).
*   **HTTP Verbs**: We use standard HTTP verbs to perform actions on resources:
    *   `GET`: Retrieve resources.
    *   `POST`: Create a new resource.
    *   `PUT` / `PATCH`: Update an existing resource.
    *   `DELETE`: Delete a resource.
*   **Statelessness**: Each request from a client must contain all the information needed to understand and process the request.

## 2. Endpoint Naming Rules

*   **Use nouns, not verbs**: Endpoints should refer to resources.
    *   Good: `/api/controls/{id}`
    *   Bad: `/api/getControlById/{id}`
*   **Use plural nouns**:
    *   Good: `/api/controls`
    *   Bad: `/api/control`
*   **Use kebab-case for paths**: If a resource name has multiple words, separate them with hyphens (though we will try to stick to single-word resources).
*   **Versioning**: The API version is included in the path: `/api/v1/...`

Current implemented endpoints are intentionally unversioned during the early build-out phase:

*   `POST /api/auth/login`
*   `POST /api/controls`
*   `GET /api/controls/{id}`
*   `GET /api/controls`
*   `PUT /api/controls/{id}`
*   `PATCH /api/controls/{id}/deactivate`
*   `POST /api/evidence`
*   `GET /api/evidence/{id}`
*   `GET /api/evidence`
*   `PATCH /api/evidence/{id}/accept`
*   `PATCH /api/evidence/{id}/reject`
*   `POST /api/audit-findings`
*   `GET /api/audit-findings/{id}`
*   `GET /api/audit-findings`
*   `PUT /api/audit-findings/{id}`
*   `PATCH /api/audit-findings/{id}/status`
*   `POST /api/action-plans`
*   `GET /api/action-plans/{id}`
*   `GET /api/action-plans`
*   `PUT /api/action-plans/{id}`
*   `PATCH /api/action-plans/{id}/status`
*   `GET /api/audit-logs/{id}`
*   `GET /api/audit-logs`

Audit logs are read-only through the public API. There are no `POST`, `PUT`, `PATCH`, or `DELETE` audit-log endpoints.

## 3. HTTP Status Code Rules

We use standard HTTP status codes to indicate the outcome of a request.

*   **2xx - Success**:
    *   `200 OK`: The request was successful.
    *   `201 Created`: A new resource was successfully created.
    *   `204 No Content`: The request was successful, but there is no data to return (e.g., after a `DELETE` operation).
*   **4xx - Client Errors**:
    *   `400 Bad Request`: The request was malformed (e.g., invalid JSON, validation error).
    *   `401 Unauthorized`: The request requires authentication, but the user is not authenticated.
    *   `403 Forbidden`: The user is authenticated but does not have permission to perform the action.
    *   `404 Not Found`: The requested resource does not exist.
*   **5xx - Server Errors**:
    *   `500 Internal Server Error`: An unexpected error occurred on the server.

## 4. Pagination, Filtering, and Sorting

For any endpoint that returns a list of resources, we will support:

*   **Pagination**: Using query parameters like `pageNumber` and `pageSize`.
*   **Filtering**: Based on specific fields (e.g., `/api/findings?status=Open`).
*   **Sorting**: Using a `sortBy` parameter (e.g., `/api/action-plans?sortBy=dueDate_desc`).

The response for a paginated list will include pagination metadata (total count, page size, current page, etc.).

## 5. Error Response Format

When an error occurs, the API will return a consistent error response based on the `ProblemDetails` standard.

**Example 400 Bad Request (Validation Error):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "RejectionReason": [
      "Rejection reason is required when the evidence status is 'Rejected'."
    ]
  }
}
```

**Example 500 Internal Server Error:**

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An unexpected error occurred.",
  "status": 500,
  "detail": "An internal server error has occurred. Please try again later."
}
```

## 6. DTO Naming Conventions

*   **Request DTOs**: Suffix with `Request` (e.g., `CreateControlRequest`).
*   **Response DTOs**: Suffix with `Response` (e.g., `ControlResponse`).
*   **Use clear, descriptive names**: The DTO name should clearly indicate its purpose.

## 7. Swagger/OpenAPI

*   Every endpoint must be documented using Swagger/OpenAPI.
*   Documentation should include a summary, description, and clear information about parameters and responses.
*   DTO properties should have example values to make the API easier to understand.

## 8. Authentication Notes

Current authentication behavior:

*   `POST /api/auth/login` returns an access token and token expiration on valid credentials.
*   Invalid login credentials return `401 Unauthorized`.
*   Malformed login requests still return `400 Bad Request`.
*   Controls endpoints currently require a bearer token.
*   Evidence endpoints currently require a bearer token.
*   Controls organization scope comes from JWT `org_id`, even if request or query payloads still contain organization fields for compatibility.
*   Evidence organization scope comes from JWT `org_id`.
*   Audit logs for Controls and Evidence now use the authenticated actor.
*   AuditFindings endpoints currently require a bearer token.
*   ActionPlans endpoints currently require a bearer token.
*   AuditLogs read endpoints currently require a bearer token.
*   AuditFindings and ActionPlans organization scope comes from JWT `org_id`.
*   Audit logs for AuditFindings and ActionPlans now use the authenticated actor.
*   AuditLogs read scope also comes from JWT `org_id`; request-supplied organization filters cannot escape the authenticated organization.
*   `403 Forbidden` is returned when the user is authenticated but lacks the required role for a protected use case.
*   Current RBAC matrix:
    *   Controls list/get: `Admin`, `Auditor`, `Reviewer`
    *   Controls create/update/deactivate: `Admin`, `Auditor`
    *   Evidence list/get: `Admin`, `Auditor`, `Reviewer`
    *   Evidence create: `Admin`, `Auditor`
    *   Evidence accept/reject: `Admin`, `Reviewer`
    *   AuditFindings list/get: `Admin`, `Auditor`, `Reviewer`
    *   AuditFindings create/update/change-status: `Admin`, `Auditor`
    *   ActionPlans list/get: `Admin`, `Auditor`, `Reviewer`
    *   ActionPlans create/update/change-status: `Admin`, `Auditor`
    *   AuditLogs list/get: `Admin`, `Auditor`
*   No refresh token, cookie auth, public registration, or password reset flow exists yet.

## 9. API Behavior for Future AI Endpoints

When AI-related endpoints are added, they will follow specific conventions:

*   **Clear Identification**: Endpoints that return AI-generated content will be clearly marked, for example, `/api/v1/evidence/{id}/ai-summary`.
*   **Advisory Nature**: The response body will clearly indicate that the content is AI-generated and should be reviewed by a human.
*   **Traceability ID**: The response may include a traceability ID that links the AI output to a log entry.

**Example AI Summary Response:**

```json
{
  "evidenceId": "uuid-goes-here",
  "summary": "This document appears to be a SOC 2 Type 2 report...",
  "generatedBy": "AI-Summarization-Service-v1",
  "confidenceScore": 0.85,
  "disclaimer": "This summary is AI-generated and should be verified by a human reviewer."
}
```

## 10. Dashboard Summary Endpoint

*   **Endpoint**: `GET /api/dashboard/summary`
*   **Access**: Read-only, available to `Admin`, `Auditor`, `Reviewer`
*   **Authentication**: JWT required; tenant scoping enforced via JWT `OrganizationId`
*   **Authorization**: No RBAC filtering beyond organization scoping
*   **Query Parameters**:
    *   `recentLimit` (int, default 5, max 20): Number of recent audit logs to return
    *   `includeRecentActivity` (bool, default true): Whether to include recent activity in response
*   **Response**: Aggregate counts and status breakdowns for Controls, Evidence, AuditFindings, and ActionPlans, plus recent activity
*   **Behavior**:
    *   Returns 401 if not authenticated
    *   Returns 403 if organization is not set or user role is not authorized
    *   Returns 200 with zero counts if organization has no data
    *   Does not accept OrganizationId from query/body (tenant scoping is enforced via JWT)
    *   Does not create, update, or delete any data
    *   Does not include AI insights or cross-organization analytics
