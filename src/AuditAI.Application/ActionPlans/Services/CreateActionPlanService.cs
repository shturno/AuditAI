using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.ActionPlans.Interfaces;
using AuditAI.Application.ActionPlans.Mappers;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.AuditLogs.Interfaces;
using AuditAI.Application.AuditLogs.Services;
using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Results;
using AuditAI.Application.Common.Validation;
using FluentValidation;
using AuditActionPlan = AuditAI.Domain.Entities.ActionPlan;

namespace AuditAI.Application.ActionPlans.Services;

public sealed class CreateActionPlanService
{
    private readonly IActionPlanRepository _actionPlanRepository;
    private readonly IAuditFindingLookup _auditFindingLookup;
    private readonly IUserLookup _userLookup;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateActionPlanRequest> _validator;

    public CreateActionPlanService(
        IActionPlanRepository actionPlanRepository,
        IAuditFindingLookup auditFindingLookup,
        IUserLookup userLookup,
        IAuditLogWriter auditLogWriter,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateActionPlanRequest> validator)
    {
        _actionPlanRepository = actionPlanRepository;
        _auditFindingLookup = auditFindingLookup;
        _userLookup = userLookup;
        _auditLogWriter = auditLogWriter;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
    }

    public async Task<Result<ActionPlanResponse>> ExecuteAsync(
        CreateActionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ActionPlansCurrentUserContext.TryGetActor(_currentUser, out var userId, out var organizationId))
        {
            return Result<ActionPlanResponse>.Unauthorized(ActionPlansCurrentUserContext.UnauthorizedMessage);
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result<ActionPlanResponse>.ValidationFailure(validationResult.ToValidationErrors());
        }

        var findingOrganizationId = await _auditFindingLookup.GetFindingOrganizationIdAsync(request.AuditFindingId, cancellationToken);
        if (!findingOrganizationId.HasValue || findingOrganizationId.Value != organizationId)
        {
            return Result<ActionPlanResponse>.NotFound("Audit finding was not found.");
        }

        var assignedUserOrganizationId = await _userLookup.GetUserOrganizationIdAsync(request.AssignedToUserId, cancellationToken);
        if (!assignedUserOrganizationId.HasValue)
        {
            return Result<ActionPlanResponse>.ValidationFailure(
            [
                new ValidationError("AssignedToUserId", "Assigned user does not exist.")
            ]);
        }

        if (assignedUserOrganizationId.Value != organizationId)
        {
            return Result<ActionPlanResponse>.ValidationFailure(
            [
                new ValidationError("AssignedToUserId", "Assigned user must belong to the same organization as the action plan.")
            ]);
        }

        var actionPlan = AuditActionPlan.Create(
            Guid.NewGuid(),
            request.AuditFindingId,
            request.AssignedToUserId,
            request.Title,
            request.Description,
            request.DueDate,
            _dateTimeProvider.UtcNow);

        await _actionPlanRepository.AddAsync(actionPlan, cancellationToken);
        await _auditLogWriter.WriteAsync(
            new AuditLogWriteEntry(
                organizationId,
                userId,
                AuditAI.Domain.Enums.AuditLogAction.ActionPlanCreated,
                nameof(AuditAI.Domain.Entities.ActionPlan),
                actionPlan.Id,
                AuditLogMetadata.Build(
                    ("status", actionPlan.Status.ToString()),
                    ("assignedToUserId", actionPlan.AssignedToUserId),
                    ("dueDate", actionPlan.DueDate))),
            cancellationToken);
        await _actionPlanRepository.SaveChangesAsync(cancellationToken);

        return Result<ActionPlanResponse>.Success(ActionPlanResponseMapper.ToResponse(actionPlan));
    }
}
