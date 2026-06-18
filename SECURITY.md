# Security Policy

This document outlines the security policy for the AuditAI project.

## Supported Versions

As a portfolio project, only the latest version on the `main` branch is actively maintained. We encourage users to always use the most recent commit.

| Version | Supported          |
| ------- | ------------------ |
| latest  | :white_check_mark: |

## Reporting a Vulnerability

We take security seriously. If you discover a security vulnerability, please report it to us privately.

**DO NOT open a public GitHub issue for security vulnerabilities.**

Please send an email to `[Your Email Address]` with the following information:

*   A description of the vulnerability.
*   Steps to reproduce the vulnerability.
*   Any proof-of-concept code.
*   The potential impact of the vulnerability.

We will acknowledge your report within 48 hours and will work with you to understand and resolve the issue.

## Secret Handling Policy

*   **No Secrets in Code**: This repository must **never** contain secrets, API keys, or sensitive credentials.
*   **Configuration**: Local development secrets are managed using the .NET `user-secrets` tool. Production secrets must be managed through environment variables or a secure secret management service (e.g., Azure Key Vault, AWS Secrets Manager).
*   **.env.example**: The `.env.example` file provides a template for required environment variables but contains no sensitive values.

## JWT and Security Considerations

*   **JWT Keys**: The JWT signing key is a critical secret and must be strong and kept confidential.
*   **Token Expiration**: JWTs have a defined expiration time to limit the window of opportunity for replay attacks.
*   **HTTPS**: In a production environment, the API must only be accessible over HTTPS to prevent token interception.
*   **Logging**: JWTs and other sensitive authentication details are not logged.

## Dependency Update Policy

Dependencies are kept up-to-date to incorporate the latest security patches. We use tools to scan for known vulnerabilities in our dependencies and update them as needed.

## Local Development Security Notes

*   The `docker-compose.yml` file is configured for local development and is not hardened for production use.
*   The default database password in `.env.example` is for local convenience only. Use a strong, unique password for any non-local deployment.

## No Real Personal/Company Data

This project is for demonstration purposes only. Do not use real personal information, company data, or sensitive audit findings in your local or forked versions of this project.

## Responsible Disclosure

We are committed to responsible disclosure. We will not take legal action against individuals who discover and report vulnerabilities in good faith and in accordance with this policy. We ask that you do not publicly disclose the vulnerability until we have had a reasonable amount of time to address it.

## Future AI Security Considerations

As we plan for AI integration, we are mindful of the unique security challenges it presents.

*   **Prompt Injection**: We are aware of the risk of prompt injection attacks, where a malicious user could craft input to manipulate the AI model's behavior. Future AI features will include input sanitization and context-aware filtering to mitigate this risk.
*   **Data Privacy with AI Providers**: Sensitive audit evidence must not be sent to external AI providers without explicit configuration, clear documentation, and user consent. We will explore options like on-premise models or providers with strong data privacy guarantees.
*   **AI API Key Security**: Any API keys for AI services will be treated as critical secrets and managed accordingly, using environment variables or a secret manager. They will never be committed to the repository.
*   **Logging AI Interactions**: Logs of AI prompts and responses will be designed to avoid storing sensitive data, or will use appropriate data masking techniques.
