using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Authorization;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using AuditAI.Application.Dashboard.Contracts;
using AuditAI.Application.Dashboard.Interfaces;
using FluentValidation;

namespace AuditAI.Application.Dashboard.Services;

public sealed class GetDashboardSummaryService
{
    private readonly IDashboardSummaryRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<DashboardQueryParameters> _validator;

    public GetDashboardSummaryService(
        IDashboardSummaryRepository repository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<DashboardQueryParameters> validator)
    {
        _repository = repository;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<DashboardSummaryResponse>> ExecuteAsync(
        DashboardQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return Result<DashboardSummaryResponse>.Unauthorized("User is not authenticated.");
        }

        if (!_currentUser.OrganizationId.HasValue)
        {
            return Result<DashboardSummaryResponse>.Unauthorized("An authenticated user context is required.");
        }

        if (!RoleAuthorization.CanReadDashboard(_currentUser))
        {
            return Result<DashboardSummaryResponse>.Forbidden(RoleAuthorization.DashboardReadForbiddenMessage);
        }

        var validationResult = await _validator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<DashboardSummaryResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var organizationId = _currentUser.OrganizationId.Value;
        var summaryResult = await _repository.GetSummaryAsync(
            organizationId,
            queryParameters.RecentLimit,
            queryParameters.IncludeRecentActivity,
            cancellationToken);

        var data = summaryResult.Value!;
        var response = new DashboardSummaryResponse(
            Controls: new ControlSummaryResponse(
                Total: data.TotalControls,
                Active: data.ActiveControls,
                Inactive: data.InactiveControls),
            Evidence: new EvidenceSummaryResponse(
                Total: data.TotalEvidence,
                Pending: data.PendingEvidence,
                Accepted: data.AcceptedEvidence,
                Rejected: data.RejectedEvidence),
            Findings: new AuditFindingSummaryResponse(
                Total: data.TotalFindings,
                Open: data.OpenFindings,
                InProgress: data.InProgressFindings,
                Resolved: data.ResolvedFindings,
                Cancelled: data.CancelledFindings,
                Low: data.LowFindings,
                Medium: data.MediumFindings,
                High: data.HighFindings,
                Critical: data.CriticalFindings,
                UnresolvedCritical: data.UnresolvedCriticalFindings),
            ActionPlans: new ActionPlanSummaryResponse(
                Total: data.TotalActionPlans,
                Open: data.OpenActionPlans,
                InProgress: data.InProgressActionPlans,
                Completed: data.CompletedActionPlans,
                Overdue: data.OverdueStatusActionPlans,
                Cancelled: data.CancelledActionPlans,
                OverdueCount: data.OverdueActionPlans,
                DueSoonCount: data.DueSoonActionPlans),
            RecentActivity: data.RecentActivity.Select(a => new RecentAuditLogItemResponse(
                Id: a.Id,
                Action: a.Action,
                EntityName: a.EntityName,
                EntityId: a.EntityId,
                UserId: a.UserId,
                Timestamp: a.Timestamp)).ToList(),
            GeneratedAtUtc: _dateTimeProvider.UtcNow);

        return Result<DashboardSummaryResponse>.Success(response);
    }
}
