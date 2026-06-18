# Git Workflow

This document outlines the Git workflow and conventions for the AuditAI project. A consistent workflow is essential for maintaining a clean and understandable project history.

## 1. Branching Strategy

We use a simple feature-branch workflow.

*   **`main` branch**: This is the primary branch and represents the current state of the project. Direct commits to `main` are not allowed.
*   **Feature branches**: All new work (features, fixes, documentation, etc.) must be done on a separate branch.

### Branch Naming Convention

To keep our branches organized, please use the following naming convention:

*   `feature/<short-description>`: For new features.
    *   *Example*: `feature/add-evidence-review-workflow`
*   `fix/<short-description>`: For bug fixes.
    *   *Example*: `fix/validate-action-plan-due-date`
*   `docs/<short-description>`: For changes to documentation.
    *   *Example*: `docs/update-architecture-overview`
*   `refactor/<short-description>`: For code refactoring without changing behavior.
    *   *Example*: `refactor/simplify-control-service`
*   `test/<short-description>`: For adding or improving tests.
    *   *Example*: `test/add-finding-resolution-tests`
*   `chore/<short-description>`: For maintenance tasks that don't fit elsewhere (e.g., updating dependencies).
    *   *Example*: `chore/update-dotnet-sdk`

## 2. Commit Convention

We follow the **Conventional Commits** specification. This makes the commit history more readable and allows for potential automation in the future.

### Format

The commit message should be structured as follows:

```
<type>: <description>

[optional body]

[optional footer]
```

### Commit Types

*   `feat`: A new feature.
*   `fix`: A bug fix.
*   `docs`: Documentation only changes.
*   `style`: Changes that do not affect the meaning of the code (white-space, formatting, etc).
*   `refactor`: A code change that neither fixes a bug nor adds a feature.
*   `perf`: A code change that improves performance.
*   `test`: Adding missing tests or correcting existing tests.
*   `chore`: Changes to the build process or auxiliary tools and libraries.

### Examples

*   `feat: add audit finding severity rules`
*   `fix: prevent negative values in control budget`
*   `docs: add responsible AI notes to README`
*   `test: cover all outcomes of evidence rejection`
*   `refactor: extract validation logic into separate service`
*   `chore: update Serilog package to latest version`

### Rules for Commits

*   **Keep them small and atomic**: One commit should represent one logical change.
*   **Write in English**: All commit messages must be in English.
*   **No secrets**: Never, ever commit secrets, API keys, or other sensitive data.
*   **Tests must pass**: Do not commit changes that break the build or fail tests.

## 3. Pull Request (PR) Process

1.  **Create a branch**: Create a new branch from `main` following the naming convention.
2.  **Do your work**: Make your changes, following the commit conventions.
3.  **Push your branch**: Push your branch to the remote repository.
4.  **Open a Pull Request**: Open a PR from your feature branch to the `main` branch.
5.  **Fill out the checklist**: The PR template will have a checklist. Please ensure you have completed all the items.
6.  **Code Review**: At least one other developer must review and approve the PR.
7.  **Merge**: Once approved, the PR can be merged into `main`. Prefer "Squash and merge" to keep the `main` branch history clean.

### Pull Request Checklist

*   Build passes (`dotnet build`).
*   Tests pass (`dotnet test`).
*   Documentation updated if needed.
*   No secrets committed.
*   Changes are relevant to the PR's goal.
*   Business rules are respected.
*   Security and AI implications have been considered.
