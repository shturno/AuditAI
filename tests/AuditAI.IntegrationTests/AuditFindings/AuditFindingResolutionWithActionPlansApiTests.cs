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
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var finding = await CreateFindingAsync(client, "Critical finding open");
        await MoveFindingToInProgressAsync(client, finding.Id);
        await CreateActionPlanAsync(client, finding.Id, "Open plan");

        var response = await ChangeFindingStatusAsync(client, finding.Id, AuditFindingStatus.Resolved);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("Status", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ResolveCriticalFinding_When_AllActionPlansAreCompletedOrCancelled()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var finding = await CreateFindingAsync(client, "Critical finding resolved");
        await MoveFindingToInProgressAsync(client, finding.Id);
        var completedPlan = await CreateActionPlanAsync(client, finding.Id, "Completed plan");
        var cancelledPlan = await CreateActionPlanAsync(client, finding.Id, "Cancelled plan");
        await ChangeActionPlanStatusAsync(client, completedPlan.Id, ActionPlanStatus.Completed);
        await ChangeActionPlanStatusAsync(client, cancelledPlan.Id, ActionPlanStatus.Cancelled);

        var response = await ChangeFindingStatusAsync(client, finding.Id, AuditFindingStatus.Resolved);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resolved = await response.Content.ReadFromJsonAsync<AuditFindingResponse>();
        Assert.NotNull(resolved);
        Assert.Equal(AuditFindingStatus.Resolved, resolved.Status);
    }

    private static async Task<AuditFindingResponse> CreateFindingAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = TestData.ControlId,
            Title = title,
            Description = $"Detailed description for {title}.",
            Severity = AuditFindingSeverity.Critical
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuditFindingResponse>())!;
    }

    private static async Task<ActionPlanResponse> CreateActionPlanAsync(HttpClient client, Guid auditFindingId, string title)
    {
        var response = await client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
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

    private static async Task MoveFindingToInProgressAsync(HttpClient client, Guid findingId)
    {
        var response = await ChangeFindingStatusAsync(client, findingId, AuditFindingStatus.InProgress);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> ChangeFindingStatusAsync(HttpClient client, Guid findingId, AuditFindingStatus status)
    {
        return await client.PatchAsJsonAsync(
            $"/api/audit-findings/{findingId}/status",
            new ChangeAuditFindingStatusRequest
            {
                Status = status
            });
    }

    private static async Task ChangeActionPlanStatusAsync(HttpClient client, Guid actionPlanId, ActionPlanStatus status)
    {
        var response = await client.PatchAsJsonAsync(
            $"/api/action-plans/{actionPlanId}/status",
            new ChangeActionPlanStatusRequest
            {
                Status = status
            });

        response.EnsureSuccessStatusCode();
    }
}
