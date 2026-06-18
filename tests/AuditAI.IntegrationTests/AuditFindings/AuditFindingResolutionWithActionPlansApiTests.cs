using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Domain.Enums;
using AuditAI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.IntegrationTests.AuditFindings;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuditFindingResolutionWithActionPlansApiTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public AuditFindingResolutionWithActionPlansApiTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_NotResolveCriticalFinding_When_OpenActionPlanExists()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Critical finding open", AuditFindingSeverity.Critical);
        await MoveFindingToInProgressAsync(finding.Id);
        await CreateActionPlanAsync(finding.Id, "Open plan");

        var response = await ChangeFindingStatusAsync(finding.Id, AuditFindingStatus.Resolved);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("Status", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_NotResolveCriticalFinding_When_InProgressActionPlanExists()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Critical finding in progress", AuditFindingSeverity.Critical);
        await MoveFindingToInProgressAsync(finding.Id);
        var actionPlan = await CreateActionPlanAsync(finding.Id, "In progress plan");
        await ChangeActionPlanStatusAsync(actionPlan.Id, ActionPlanStatus.InProgress);

        var response = await ChangeFindingStatusAsync(finding.Id, AuditFindingStatus.Resolved);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_NotResolveCriticalFinding_When_OverdueActionPlanExists()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Critical finding overdue", AuditFindingSeverity.Critical);
        await MoveFindingToInProgressAsync(finding.Id);
        var actionPlan = await CreateActionPlanAsync(finding.Id, "Overdue plan");
        await ChangeActionPlanStatusAsync(actionPlan.Id, ActionPlanStatus.Overdue);

        var response = await ChangeFindingStatusAsync(finding.Id, AuditFindingStatus.Resolved);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_ResolveCriticalFinding_When_AllActionPlansAreCompletedOrCancelled()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Critical finding resolved", AuditFindingSeverity.Critical);
        await MoveFindingToInProgressAsync(finding.Id);
        var completedPlan = await CreateActionPlanAsync(finding.Id, "Completed plan");
        var cancelledPlan = await CreateActionPlanAsync(finding.Id, "Cancelled plan");
        await ChangeActionPlanStatusAsync(completedPlan.Id, ActionPlanStatus.Completed);
        await ChangeActionPlanStatusAsync(cancelledPlan.Id, ActionPlanStatus.Cancelled);

        var response = await ChangeFindingStatusAsync(finding.Id, AuditFindingStatus.Resolved);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resolved = await response.Content.ReadFromJsonAsync<AuditFindingResponse>();
        Assert.NotNull(resolved);
        Assert.Equal(AuditFindingStatus.Resolved, resolved.Status);
    }

    [Fact]
    public async Task Should_ResolveNonCriticalFinding_EvenWhen_ActionPlanExists()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("High finding", AuditFindingSeverity.High);
        await MoveFindingToInProgressAsync(finding.Id);
        await CreateActionPlanAsync(finding.Id, "Open plan for non critical");

        var response = await ChangeFindingStatusAsync(finding.Id, AuditFindingStatus.Resolved);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<AuditFindingResponse> CreateFindingAsync(string title, AuditFindingSeverity severity)
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = TestData.ControlId,
            CreatedByUserId = TestData.UserId,
            Title = title,
            Description = $"Detailed description for {title}.",
            Severity = severity
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuditFindingResponse>())!;
    }

    private async Task<ActionPlanResponse> CreateActionPlanAsync(Guid auditFindingId, string title)
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = auditFindingId,
            AssignedToUserId = TestData.UserId,
            Title = title,
            Description = $"Detailed description for {title}.",
            DueDate = TestData.SeedTimestamp.AddDays(10)
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ActionPlanResponse>())!;
    }

    private async Task MoveFindingToInProgressAsync(Guid findingId)
    {
        var response = await ChangeFindingStatusAsync(findingId, AuditFindingStatus.InProgress);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> ChangeFindingStatusAsync(Guid findingId, AuditFindingStatus status)
    {
        return await _fixture.Client.PatchAsJsonAsync(
            $"/api/audit-findings/{findingId}/status",
            new ChangeAuditFindingStatusRequest
            {
                Status = status
            });
    }

    private async Task ChangeActionPlanStatusAsync(Guid actionPlanId, ActionPlanStatus status)
    {
        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/action-plans/{actionPlanId}/status",
            new ChangeActionPlanStatusRequest
            {
                Status = status
            });

        response.EnsureSuccessStatusCode();
    }
}
