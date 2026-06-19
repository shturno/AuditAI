using AuditAI.Application.Common.Results;
using AuditAI.Domain.Enums;

namespace AuditAI.Application.Dashboard.Interfaces;

public interface IDashboardSummaryRepository
{
    Task<Result<DashboardSummaryData>> GetSummaryAsync(
        Guid organizationId,
        int recentLimit,
        bool includeRecentActivity,
        CancellationToken cancellationToken);
}

public sealed record DashboardSummaryData(
    int TotalControls,
    int ActiveControls,
    int InactiveControls,
    int TotalEvidence,
    int PendingEvidence,
    int AcceptedEvidence,
    int RejectedEvidence,
    int TotalFindings,
    int OpenFindings,
    int InProgressFindings,
    int ResolvedFindings,
    int CancelledFindings,
    int LowFindings,
    int MediumFindings,
    int HighFindings,
    int CriticalFindings,
    int UnresolvedCriticalFindings,
    int TotalActionPlans,
    int OpenActionPlans,
    int InProgressActionPlans,
    int CompletedActionPlans,
    int OverdueStatusActionPlans,
    int CancelledActionPlans,
    int OverdueActionPlans,
    int DueSoonActionPlans,
    IReadOnlyList<AuditLogEntry> RecentActivity);

public sealed record AuditLogEntry(
    Guid Id,
    string Action,
    string EntityName,
    Guid EntityId,
    Guid? UserId,
    DateTimeOffset Timestamp);
