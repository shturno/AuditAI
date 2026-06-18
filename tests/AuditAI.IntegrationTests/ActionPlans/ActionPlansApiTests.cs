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
    public async Task Should_ReturnBadRequest_When_AuditFindingDoesNotExist()
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

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("AuditFindingId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_AssignedUserDoesNotExist()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding for missing user", AuditFindingSeverity.High);

        var response = await _fixture.Client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = finding.Id,
            AssignedToUserId = Guid.NewGuid(),
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
    public async Task Should_ReturnBadRequest_When_AssignedUserBelongsToAnotherOrganization()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding for mismatched user", AuditFindingSeverity.High);

        var response = await _fixture.Client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = finding.Id,
            AssignedToUserId = TestData.OtherUserId,
            Title = "Plan C",
            Description = "Plan description",
            DueDate = TestData.SeedTimestamp.AddDays(10)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("AssignedToUserId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnCreated_When_RequestIsValid()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding create action plan", AuditFindingSeverity.High);

        var response = await CreateActionPlanAsync(finding.Id, "Plan D");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ActionPlanResponse>();
        Assert.NotNull(created);
        Assert.Equal(ActionPlanStatus.Open, created.Status);
    }

    [Fact]
    public async Task Should_ReturnActionPlanById_AfterValidCreate()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding get action plan", AuditFindingSeverity.High);
        var createResponse = await CreateActionPlanAsync(finding.Id, "Plan E");
        var created = await createResponse.Content.ReadFromJsonAsync<ActionPlanResponse>();

        var response = await _fixture.Client.GetAsync($"/api/action-plans/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actionPlan = await response.Content.ReadFromJsonAsync<ActionPlanResponse>();
        Assert.NotNull(actionPlan);
        Assert.Equal("Plan E", actionPlan.Title);
    }

    [Fact]
    public async Task Should_ReturnPaginatedList_IncludingCreatedActionPlan()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding list action plans", AuditFindingSeverity.High);
        await CreateActionPlanAsync(finding.Id, "Plan F");
        await CreateActionPlanAsync(finding.Id, "Plan G");

        var response = await _fixture.Client.GetAsync($"/api/action-plans?pageNumber=1&pageSize=10&auditFindingId={finding.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<ActionPlanListItemResponse>>();
        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 2);
        Assert.Contains(page.Items, item => item.Title == "Plan F");
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdatingMissingActionPlan()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PutAsJsonAsync(
            $"/api/action-plans/{Guid.NewGuid()}",
            new UpdateActionPlanRequest
            {
                AssignedToUserId = TestData.UserId,
                Title = "Updated plan",
                Description = "Updated description",
                DueDate = TestData.SeedTimestamp.AddDays(12)
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_UpdateIsValid()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding update action plan", AuditFindingSeverity.High);
        var createResponse = await CreateActionPlanAsync(finding.Id, "Plan H");
        var created = await createResponse.Content.ReadFromJsonAsync<ActionPlanResponse>();

        var response = await _fixture.Client.PutAsJsonAsync(
            $"/api/action-plans/{created!.Id}",
            new UpdateActionPlanRequest
            {
                AssignedToUserId = TestData.UserId,
                Title = "Updated plan H",
                Description = "Updated description for plan.",
                DueDate = TestData.SeedTimestamp.AddDays(14)
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ActionPlanResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated plan H", updated.Title);
    }

    [Fact]
    public async Task Should_ReturnOk_For_ValidStatusTransition()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding status action plan", AuditFindingSeverity.High);
        var createResponse = await CreateActionPlanAsync(finding.Id, "Plan I");
        var created = await createResponse.Content.ReadFromJsonAsync<ActionPlanResponse>();

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/action-plans/{created!.Id}/status",
            new ChangeActionPlanStatusRequest
            {
                Status = ActionPlanStatus.InProgress
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ActionPlanResponse>();
        Assert.NotNull(updated);
        Assert.Equal(ActionPlanStatus.InProgress, updated.Status);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_For_InvalidStatusTransition()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding invalid action plan status", AuditFindingSeverity.High);
        var createResponse = await CreateActionPlanAsync(finding.Id, "Plan J");
        var created = await createResponse.Content.ReadFromJsonAsync<ActionPlanResponse>();

        await _fixture.Client.PatchAsJsonAsync(
            $"/api/action-plans/{created!.Id}/status",
            new ChangeActionPlanStatusRequest
            {
                Status = ActionPlanStatus.Completed
            });

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/action-plans/{created.Id}/status",
            new ChangeActionPlanStatusRequest
            {
                Status = ActionPlanStatus.InProgress
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("Status", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ActionPlanIsMissing()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync($"/api/action-plans/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

    private async Task<HttpResponseMessage> CreateActionPlanAsync(Guid auditFindingId, string title)
    {
        return await _fixture.Client.PostAsJsonAsync("/api/action-plans", new CreateActionPlanRequest
        {
            AuditFindingId = auditFindingId,
            AssignedToUserId = TestData.UserId,
            Title = title,
            Description = $"Detailed description for {title}.",
            DueDate = TestData.SeedTimestamp.AddDays(10)
        });
    }
}
