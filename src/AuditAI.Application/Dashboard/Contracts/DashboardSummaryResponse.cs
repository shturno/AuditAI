using AuditAI.Domain.Enums;

namespace AuditAI.Application.Dashboard.Contracts;

public sealed record DashboardSummaryResponse(
    ControlSummaryResponse Controls,
    EvidenceSummaryResponse Evidence,
    AuditFindingSummaryResponse Findings,
    ActionPlanSummaryResponse ActionPlans,
    IReadOnlyList<RecentAuditLogItemResponse> RecentActivity,
    DateTimeOffset GeneratedAtUtc);

public sealed record ControlSummaryResponse(
    int Total,
    int Active,
    int Inactive);

public sealed record EvidenceSummaryResponse(
    int Total,
    int Pending,
    int Accepted,
    int Rejected);

public sealed record AuditFindingSummaryResponse(
    int Total,
    int Open,
    int InProgress,
    int Resolved,
    int Cancelled,
    int Low,
    int Medium,
    int High,
    int Critical,
    int UnresolvedCritical);

public sealed record ActionPlanSummaryResponse(
    int Total,
    int Open,
    int InProgress,
    int Completed,
    int Overdue,
    int Cancelled,
    int OverdueCount,
    int DueSoonCount);

public sealed record RecentAuditLogItemResponse(
    Guid Id,
    string Action,
    string EntityName,
    Guid EntityId,
    Guid? UserId,
    DateTimeOffset Timestamp);
