using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Dashboard.Interfaces;
using AuditAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuditAI.Infrastructure.Persistence.Repositories;

internal sealed class DashboardSummaryRepository : IDashboardSummaryRepository
{
    private readonly AuditAIDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DashboardSummaryRepository(
        AuditAIDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<DashboardSummaryData>> GetSummaryAsync(
        Guid organizationId,
        int recentLimit,
        bool includeRecentActivity,
        CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow.UtcDateTime;
        var dueSoonAfter = now.AddDays(7);

        var (totalControls, activeControls, inactiveControls) = await GetControlsSummaryAsync(organizationId, cancellationToken);
        var (totalEvidence, pendingEvidence, acceptedEvidence, rejectedEvidence) = await GetEvidenceSummaryAsync(organizationId, cancellationToken);
        var findings = await GetFindingsSummaryAsync(organizationId, cancellationToken);
        var actionPlans = await GetActionPlansSummaryAsync(organizationId, now, dueSoonAfter, cancellationToken);
        var recentActivity = includeRecentActivity
            ? await GetRecentActivityAsync(organizationId, recentLimit, cancellationToken)
            : Array.Empty<AuditLogEntry>();

        var data = new DashboardSummaryData(
            TotalControls: totalControls,
            ActiveControls: activeControls,
            InactiveControls: inactiveControls,
            TotalEvidence: totalEvidence,
            PendingEvidence: pendingEvidence,
            AcceptedEvidence: acceptedEvidence,
            RejectedEvidence: rejectedEvidence,
            TotalFindings: findings.Total,
            OpenFindings: findings.Open,
            InProgressFindings: findings.InProgress,
            ResolvedFindings: findings.Resolved,
            CancelledFindings: findings.Cancelled,
            LowFindings: findings.Low,
            MediumFindings: findings.Medium,
            HighFindings: findings.High,
            CriticalFindings: findings.Critical,
            UnresolvedCriticalFindings: findings.UnresolvedCritical,
            TotalActionPlans: actionPlans.Total,
            OpenActionPlans: actionPlans.Open,
            InProgressActionPlans: actionPlans.InProgress,
            CompletedActionPlans: actionPlans.Completed,
            OverdueStatusActionPlans: actionPlans.OverdueStatus,
            CancelledActionPlans: actionPlans.Cancelled,
            OverdueActionPlans: actionPlans.Overdue,
            DueSoonActionPlans: actionPlans.DueSoon,
            RecentActivity: recentActivity);

        return Result<DashboardSummaryData>.Success(data);
    }

    private async Task<(int total, int active, int inactive)> GetControlsSummaryAsync(
        Guid organizationId, CancellationToken cancellationToken)
    {
        var breakdown = await _dbContext.Controls
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId)
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dict = breakdown.ToDictionary(r => r.Status, r => r.Count);
        return (dict.Values.Sum(), dict.GetValueOrDefault(ControlStatus.Active, 0), dict.GetValueOrDefault(ControlStatus.Inactive, 0));
    }

    private async Task<(int total, int pending, int accepted, int rejected)> GetEvidenceSummaryAsync(
        Guid organizationId, CancellationToken cancellationToken)
    {
        var breakdown = await (
            from e in _dbContext.Evidence.AsNoTracking()
            join c in _dbContext.Controls.AsNoTracking() on e.ControlId equals c.Id
            where c.OrganizationId == organizationId
            group e by e.Status into g
            select new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dict = breakdown.ToDictionary(r => r.Status, r => r.Count);
        return (dict.Values.Sum(), dict.GetValueOrDefault(EvidenceStatus.Pending, 0), dict.GetValueOrDefault(EvidenceStatus.Accepted, 0), dict.GetValueOrDefault(EvidenceStatus.Rejected, 0));
    }

    private async Task<(int Total, int Open, int InProgress, int Resolved, int Cancelled, int Low, int Medium, int High, int Critical, int UnresolvedCritical)> GetFindingsSummaryAsync(
        Guid organizationId, CancellationToken cancellationToken)
    {
        var statusBreakdown = await (
            from f in _dbContext.AuditFindings.AsNoTracking()
            join c in _dbContext.Controls.AsNoTracking() on f.ControlId equals c.Id
            where c.OrganizationId == organizationId
            group f by f.Status into g
            select new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var severityBreakdown = await (
            from f in _dbContext.AuditFindings.AsNoTracking()
            join c in _dbContext.Controls.AsNoTracking() on f.ControlId equals c.Id
            where c.OrganizationId == organizationId
            group f by f.Severity into g
            select new { Severity = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var statusDict = statusBreakdown.ToDictionary(r => r.Status, r => r.Count);
        var severityDict = severityBreakdown.ToDictionary(r => r.Severity, r => r.Count);

        var unresolvedCritical = await (
            from f in _dbContext.AuditFindings.AsNoTracking()
            join c in _dbContext.Controls.AsNoTracking() on f.ControlId equals c.Id
            where c.OrganizationId == organizationId
                && f.Severity == AuditFindingSeverity.Critical
                && f.Status != AuditFindingStatus.Resolved
                && f.Status != AuditFindingStatus.Cancelled
            select f.Id)
            .CountAsync(cancellationToken);

        return (
            Total: statusDict.Values.Sum(),
            Open: statusDict.GetValueOrDefault(AuditFindingStatus.Open, 0),
            InProgress: statusDict.GetValueOrDefault(AuditFindingStatus.InProgress, 0),
            Resolved: statusDict.GetValueOrDefault(AuditFindingStatus.Resolved, 0),
            Cancelled: statusDict.GetValueOrDefault(AuditFindingStatus.Cancelled, 0),
            Low: severityDict.GetValueOrDefault(AuditFindingSeverity.Low, 0),
            Medium: severityDict.GetValueOrDefault(AuditFindingSeverity.Medium, 0),
            High: severityDict.GetValueOrDefault(AuditFindingSeverity.High, 0),
            Critical: severityDict.GetValueOrDefault(AuditFindingSeverity.Critical, 0),
            UnresolvedCritical: unresolvedCritical);
    }

    private async Task<(int Total, int Open, int InProgress, int Completed, int OverdueStatus, int Cancelled, int Overdue, int DueSoon)> GetActionPlansSummaryAsync(
        Guid organizationId, DateTime now, DateTime dueSoonAfter, CancellationToken cancellationToken)
    {
        var statusBreakdown = await (
            from ap in _dbContext.ActionPlans.AsNoTracking()
            join af in _dbContext.AuditFindings.AsNoTracking() on ap.AuditFindingId equals af.Id
            join c in _dbContext.Controls.AsNoTracking() on af.ControlId equals c.Id
            where c.OrganizationId == organizationId
            group ap by ap.Status into g
            select new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dict = statusBreakdown.ToDictionary(r => r.Status, r => r.Count);

        var overdue = await (
            from ap in _dbContext.ActionPlans.AsNoTracking()
            join af in _dbContext.AuditFindings.AsNoTracking() on ap.AuditFindingId equals af.Id
            join c in _dbContext.Controls.AsNoTracking() on af.ControlId equals c.Id
            where c.OrganizationId == organizationId
                 && ap.Status != ActionPlanStatus.Completed
                 && ap.Status != ActionPlanStatus.Cancelled
                 && ap.DueDate < now
            select ap.Id)
            .CountAsync(cancellationToken);

        var dueSoon = await (
            from ap in _dbContext.ActionPlans.AsNoTracking()
            join af in _dbContext.AuditFindings.AsNoTracking() on ap.AuditFindingId equals af.Id
            join c in _dbContext.Controls.AsNoTracking() on af.ControlId equals c.Id
            where c.OrganizationId == organizationId
                 && ap.Status != ActionPlanStatus.Completed
                 && ap.Status != ActionPlanStatus.Cancelled
                 && ap.DueDate >= now
                 && ap.DueDate <= dueSoonAfter
            select ap.Id)
            .CountAsync(cancellationToken);

        return (
            Total: dict.Values.Sum(),
            Open: dict.GetValueOrDefault(ActionPlanStatus.Open, 0),
            InProgress: dict.GetValueOrDefault(ActionPlanStatus.InProgress, 0),
            Completed: dict.GetValueOrDefault(ActionPlanStatus.Completed, 0),
            OverdueStatus: dict.GetValueOrDefault(ActionPlanStatus.Overdue, 0),
            Cancelled: dict.GetValueOrDefault(ActionPlanStatus.Cancelled, 0),
            Overdue: overdue,
            DueSoon: dueSoon);
    }

    private async Task<IReadOnlyList<AuditLogEntry>> GetRecentActivityAsync(
        Guid organizationId, int limit, CancellationToken cancellationToken)
    {
        var logs = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return logs.Select(a => new AuditLogEntry(
            Id: a.Id,
            Action: a.Action.ToString(),
            EntityName: a.EntityName,
            EntityId: a.EntityId,
            UserId: a.UserId,
            Timestamp: a.Timestamp)).ToList();
    }
}
