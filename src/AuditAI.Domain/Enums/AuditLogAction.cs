namespace AuditAI.Domain.Enums;

public enum AuditLogAction
{
    UserLoggedIn = 1,
    UserLoggedOut = 2,
    ControlCreated = 3,
    ControlUpdated = 4,
    ControlDeleted = 5,
    EvidenceSubmitted = 6,
    EvidenceAccepted = 7,
    EvidenceRejected = 8,
    AuditFindingCreated = 9,
    AuditFindingResolved = 10,
    ActionPlanCreated = 11,
    ActionPlanCompleted = 12,
    UserRoleChanged = 13,
    ControlDeactivated = 14,
    AuditFindingUpdated = 15,
    AuditFindingStatusChanged = 16,
    ActionPlanUpdated = 17,
    ActionPlanStatusChanged = 18
}
