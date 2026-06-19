using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.Common.Pagination;
using AuditAI.Domain.Enums;
using AuditAI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.IntegrationTests.ActionPlans;

[Collection(IntegrationTestCollection.Name)]
public sealed class ActionPlansApiTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public ActionPlansApiTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_CreateIsCalledWithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = Guid.NewGuid(),
            AssignedToUserId = TestData.UserId,
            Title = "Plan A",
            Description = "Plan description",
            DueDate = TestData.SeedTimestamp.AddDays(10)
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_ListIsCalledWithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/action-plans");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_CreateActionPlan_When_RequestIsValid()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var finding = await CreateFindingAsync(client, "Finding create action plan", TestData.ControlId);

        var response = await client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = finding.Id,
            AssignedToUserId = TestData.UserId,
            Title = "Plan D",
            Description = "Plan description",
            DueDate = TestData.SeedTimestamp.AddDays(10)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ActionPlanResponse>();
        Assert.NotNull(created);
        Assert.Equal(ActionPlanStatus.Open, created.Status);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_ReviewerCreatesActionPlan()
    {
        await _fixture.ResetDatabaseAsync();
        using var auditorClient = await _fixture.CreateAuthenticatedClientAsync();
        using var reviewerClient = await _fixture.CreateAuthenticatedClientAsync(TestData.ReviewerUserEmail, TestData.ReviewerUserPassword);
        var finding = await CreateFindingAsync(auditorClient, "Finding for reviewer action plan", TestData.ControlId);

        var response = await reviewerClient.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = finding.Id,
            AssignedToUserId = TestData.UserId,
            Title = "Reviewer should not create action plan",
            Description = "Plan description",
            DueDate = TestData.SeedTimestamp.AddDays(10)
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CreatingForOtherOrganizationFinding()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        using var otherClient = await _fixture.CreateAuthenticatedClientAsync(TestData.OtherUserEmail, TestData.OtherUserPassword);
        var otherFinding = await CreateFindingAsync(otherClient, "Other org finding", TestData.OtherControlId);

        var response = await client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = otherFinding.Id,
            AssignedToUserId = TestData.UserId,
            Title = "Plan C",
            Description = "Plan description",
            DueDate = TestData.SeedTimestamp.AddDays(10)
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_AssignedUserBelongsToAnotherOrganization()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var finding = await CreateFindingAsync(client, "Finding for mismatched user", TestData.ControlId);

        var response = await client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = finding.Id,
            AssignedToUserId = TestData.OtherUserId,
            Title = "Plan B",
            Description = "Plan description",
            DueDate = TestData.SeedTimestamp.AddDays(10)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("AssignedToUserId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_NotReturnOtherOrganizationActionPlans_InList()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        using var otherClient = await _fixture.CreateAuthenticatedClientAsync(TestData.OtherUserEmail, TestData.OtherUserPassword);

        var first = await CreateActionPlanAsync(client, await CreateFindingAsync(client, "Finding list action plans", TestData.ControlId), "Plan F", TestData.UserId);
        var other = await CreateActionPlanAsync(otherClient, await CreateFindingAsync(otherClient, "Other org finding", TestData.OtherControlId), "Plan G", TestData.OtherUserId);

        var response = await client.GetAsync("/api/action-plans?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ActionPlanListItemResponse>>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Id == first.Id);
        Assert.DoesNotContain(page.Items, item => item.Id == other.Id);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_GettingActionPlanFromAnotherOrganization()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        using var otherClient = await _fixture.CreateAuthenticatedClientAsync(TestData.OtherUserEmail, TestData.OtherUserPassword);
        var otherFinding = await CreateFindingAsync(otherClient, "Other org finding", TestData.OtherControlId);
        var otherActionPlan = await CreateActionPlanAsync(otherClient, otherFinding, "Plan H", TestData.OtherUserId);

        var response = await client.GetAsync($"/api/action-plans/{otherActionPlan.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_AllowValidUpdateAndStatusTransition()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var finding = await CreateFindingAsync(client, "Finding update action plan", TestData.ControlId);
        var created = await CreateActionPlanAsync(client, finding, "Plan I", TestData.UserId);

        var response = await client.PutAsJsonAsync(
            $"/api/action-plans/{created.Id}",
            new UpdateActionPlanRequest
            {
                AssignedToUserId = TestData.UserId,
                Title = "Updated plan",
                Description = "Updated description",
                DueDate = TestData.SeedTimestamp.AddDays(12)
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var statusResponse = await client.PatchAsJsonAsync(
            $"/api/action-plans/{created.Id}/status",
            new ChangeActionPlanStatusRequest
            {
                Status = ActionPlanStatus.InProgress
            });

        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
    }

    private static async Task<AuditFindingResponse> CreateFindingAsync(HttpClient client, string title, Guid controlId)
    {
        var response = await client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = controlId,
            Title = title,
            Description = $"Detailed description for {title}.",
            Severity = AuditFindingSeverity.High
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuditFindingResponse>())!;
    }

    private static async Task<ActionPlanResponse> CreateActionPlanAsync(HttpClient client, AuditFindingResponse finding, string title, Guid assignedToUserId)
    {
        var response = await client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = finding.Id,
            AssignedToUserId = assignedToUserId,
            Title = title,
            Description = $"Detailed description for {title}.",
            DueDate = TestData.SeedTimestamp.AddDays(10)
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ActionPlanResponse>())!;
    }
}
