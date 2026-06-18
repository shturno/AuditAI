# Contributing to AuditAI

First off, thank you for considering contributing to AuditAI! As a portfolio project, the main goal is to demonstrate best practices, so your contributions are valuable.

## How Can I Contribute?

*   **Reporting Bugs**: If you find a bug, please open an issue and provide as much detail as possible.
*   **Suggesting Enhancements**: If you have an idea for a new feature or an improvement, open an issue to discuss it.
*   **Pull Requests**: We welcome pull requests, but please follow the guidelines below.

## Git Workflow

We follow a feature-branch workflow with specific naming conventions to keep the repository history clean and understandable.

### Branch Naming Convention

Please name your branches using the following convention:

*   `feature/<short-description>`: For new features (e.g., `feature/add-evidence-review-workflow`).
*   `fix/<short-description>`: For bug fixes (e.g., `fix/validate-action-plan-due-date`).
*   `docs/<short-description>`: For documentation changes (e.g., `docs/update-architecture-overview`).
*   `refactor/<short-description>`: For code refactoring (e.g., `refactor/simplify-control-service`).
*   `test/<short-description>`: For adding or improving tests (e.g., `test/add-finding-resolution-tests`).
*   `chore/<short-description>`: For maintenance tasks (e.g., `chore/update-dependencies`).

### Commit Convention

We use a conventional commit message format to make the commit history more readable.

*   **Format**: `<type>: <description>`
*   **Examples**:
    *   `feat: add audit finding severity rules`
    *   `fix: validate action plan due date`
    *   `docs: add responsible AI notes`
    *   `test: add finding resolution tests`
    *   `refactor: simplify control service`
    *   `chore: update dependencies`

### Commit Rules

*   **Small, Atomic Commits**: Each commit should represent a single logical change.
*   **Clear Messages**: Write clear and concise commit messages in English.
*   **No Secrets**: Never commit secrets, API keys, or other sensitive information.
*   **Tests Pass**: Ensure that all tests pass before pushing your changes.

## Pull Request Process

1.  Ensure any install or build dependencies are removed before the end of the layer when doing a build.
2.  Update the README.md with details of changes to the interface, this includes new environment variables, exposed ports, useful file locations, and container parameters.
3.  Increase the version numbers in any examples and the README.md to the new version that this Pull Request would represent. The versioning scheme we use is [SemVer](http://semver.org/).
4.  You may merge the Pull Request in once you have the sign-off of two other developers, or if you do not have permission to do that, you may request the second reviewer to merge it for you.

### Pull Request Checklist

Before submitting a pull request, please ensure you have completed the following:

*   [ ] The build passes (`dotnet build`).
*   [ ] All tests pass (`dotnet test`).
*   [ ] Documentation has been updated if necessary.
*   [ ] No secrets or sensitive information are included.
*   [ ] The changes are related to the PR's purpose.
*   [ ] Business rules have been respected.
*   [ ] Security implications have been considered.
*   [ ] AI implications have been considered (if applicable).

Thank you for your contribution!
