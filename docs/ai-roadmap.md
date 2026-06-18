# AI Roadmap

This document outlines the vision, principles, and phased roadmap for integrating Artificial Intelligence (AI) features into the AuditAI project.

## 1. Why AI for AuditAI?

The field of internal audit is rich with opportunities for AI to provide value. By handling repetitive, data-intensive tasks, AI can free up human auditors to focus on high-level analysis, critical thinking, and strategic risk management.

Our vision is to use AI as an **assistant**, not a replacement, for the audit professional.

## 2. Guiding Principles

Our AI integration strategy is guided by the following principles:

*   **Human-in-the-Loop is Non-Negotiable**: AI will provide suggestions, summaries, and analysis, but a human user will always be responsible for the final decision. The system will be designed to make this clear.
*   **Responsible and Ethical**: We will be mindful of the ethical implications of using AI in a compliance context. AI outputs will be treated as advisory, and we will avoid biases where possible.
*   **Provider-Agnostic Design**: The core application will not be tied to a specific AI vendor (e.g., OpenAI, Azure, Gemini, Claude). We will use interfaces to abstract away the implementation details, allowing us to switch providers if needed.
*   **Data Privacy and Security First**: We recognize that audit data is sensitive. We will not send sensitive data to external AI providers without a clear policy, explicit configuration, and robust security measures.

## 3. What AI Will (and Will Not) Do

*   **AI Will**:
    *   Summarize large documents of evidence.
    *   Suggest a potential risk severity for an audit finding based on its description.
    *   Classify findings into categories.
    *   Enable semantic search over the evidence database.
    *   Help draft action plans.

*   **AI Will Not**:
    *   Automatically approve or reject evidence.
    *   Automatically resolve or close an audit finding.
    *   Make any final, binding decision on behalf of a user.
    *   Delete any data.

## 4. Provider-Agnostic Design

To avoid vendor lock-in, we will use interfaces in the `Application` layer to define the *capabilities* we need from an AI service.

**Possible Interface Examples:**

```csharp
// In AuditAI.Application/Interfaces

public interface IEvidenceSummarizationService
{
    Task<string> GetSummaryAsync(string evidenceText, CancellationToken cancellationToken);
}

public interface IRiskSuggestionService
{
    Task<SuggestedRisk> GetRiskSuggestionAsync(string findingDescription, CancellationToken cancellationToken);
}

public interface IAuditFindingClassifier
{
    Task<string> ClassifyFindingAsync(string findingDescription, CancellationToken cancellationToken);
}
```

The concrete implementations of these interfaces, which will call the actual AI provider APIs, will reside in the `Infrastructure` layer. This keeps our core application clean and flexible.

## 5. Data Privacy and Security

*   **API Keys**: All AI provider API keys will be treated as critical secrets and will never be committed to the repository.
*   **Prompt Injection**: We will implement measures to mitigate the risk of prompt injection attacks.
*   **Data in Prompts**: We will be careful about the data we include in prompts sent to external services, and we will explore techniques like data anonymization where appropriate.
*   **Logging**: We will log the usage of AI features for traceability but will avoid logging the full, sensitive payloads of prompts and responses unless necessary for debugging under secure conditions.

## 6. Phased Roadmap

We will introduce AI features in a phased approach, building on the solid foundation of the core backend.

*   **Phase 1: Backend Foundation (Current)**: Build the core, non-AI backend for managing audit workflows.
*   **Phase 2: Audit Workflow**: Implement the full CRUD functionality for all core entities.
*   **Phase 3: Test Coverage and Documentation**: Achieve high test coverage and complete all documentation.
*   **Phase 4: AI Extension Interfaces**: Define the AI service interfaces (like the examples above) in the `Application` layer. At this stage, we might create a "dummy" implementation in the `Infrastructure` layer that returns hardcoded data, allowing us to build the UI and application logic without a real AI backend.
*   **Phase 5: Optional Provider Integration**: Implement a real AI service client in the `Infrastructure` layer for a specific provider (e.g., using the Azure OpenAI SDK). This will be behind a feature flag or configuration setting.
*   **Phase 6: Semantic Search or Summarization**: Implement the first major AI feature, such as evidence summarization or semantic search, using the infrastructure built in the previous phases.
