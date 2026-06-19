namespace AuditAI.Application.Evidence.Contracts;

public sealed class CreateEvidenceRequest
{
    public Guid ControlId { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string StorageReference { get; init; } = string.Empty;
}
