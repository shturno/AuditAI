using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.Common.Pagination;
using AuditAI.Domain.Enums;
using AuditAI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.IntegrationTests.AuditFindings;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuditFindingsApiTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public AuditFindingsApiTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_ControlDoesNotExist()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = Guid.NewGuid(),
            CreatedByUserId = TestData.UserId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("ControlId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CreatorDoesNotExist()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = TestData.ControlId,
            CreatedByUserId = Guid.NewGuid(),
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("CreatedByUserId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_CreatorBelongsToAnotherOrganization()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = TestData.ControlId,
            CreatedByUserId = TestData.OtherUserId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("CreatedByUserId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnCreated_When_RequestIsValid()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await CreateValidFindingAsync("Finding A");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AuditFindingResponse>();
        Assert.NotNull(created);
        Assert.Equal(AuditFindingStatus.Open, created.Status);
    }

    [Fact]
    public async Task Should_ReturnFindingById_AfterValidCreate()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidFindingAsync("Finding B");
        var created = await createResponse.Content.ReadFromJsonAsync<AuditFindingResponse>();

        var response = await _fixture.Client.GetAsync($"/api/audit-findings/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var finding = await response.Content.ReadFromJsonAsync<AuditFindingResponse>();
        Assert.NotNull(finding);
        Assert.Equal("Finding B", finding.Title);
    }

    [Fact]
    public async Task Should_ReturnPaginatedList_IncludingCreatedFinding()
    {
        await _fixture.ResetDatabaseAsync();
        await CreateValidFindingAsync("Finding C");
        await CreateValidFindingAsync("Finding D");

        var response = await _fixture.Client.GetAsync($"/api/audit-findings?pageNumber=1&pageSize=10&controlId={TestData.ControlId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AuditFindingListItemResponse>>();
        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 2);
        Assert.Contains(page.Items, item => item.Title == "Finding C");
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdatingMissingFinding()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PutAsJsonAsync(
            $"/api/audit-findings/{Guid.NewGuid()}",
            new UpdateAuditFindingRequest
            {
                Title = "Updated title",
                Description = "Updated description for finding.",
                Severity = AuditFindingSeverity.Medium
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_UpdateIsValid()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidFindingAsync("Finding E");
        var created = await createResponse.Content.ReadFromJsonAsync<AuditFindingResponse>();

        var response = await _fixture.Client.PutAsJsonAsync(
            $"/api/audit-findings/{created!.Id}",
            new UpdateAuditFindingRequest
            {
                Title = "Updated finding",
                Description = "Updated description for finding.",
                Severity = AuditFindingSeverity.Critical
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AuditFindingResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated finding", updated.Title);
        Assert.Equal(AuditFindingSeverity.Critical, updated.Severity);
    }

    [Fact]
    public async Task Should_ReturnOk_For_ValidStatusTransition()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidFindingAsync("Finding F");
        var created = await createResponse.Content.ReadFromJsonAsync<AuditFindingResponse>();

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/audit-findings/{created!.Id}/status",
            new ChangeAuditFindingStatusRequest
            {
                Status = AuditFindingStatus.InProgress
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<AuditFindingResponse>();
        Assert.NotNull(updated);
        Assert.Equal(AuditFindingStatus.InProgress, updated.Status);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_For_InvalidStatusTransition()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidFindingAsync("Finding G");
        var created = await createResponse.Content.ReadFromJsonAsync<AuditFindingResponse>();

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/audit-findings/{created!.Id}/status",
            new ChangeAuditFindingStatusRequest
            {
                Status = AuditFindingStatus.Resolved
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("Status", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_FindingIsMissing()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync($"/api/audit-findings/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> CreateValidFindingAsync(string title)
    {
        return await _fixture.Client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = TestData.ControlId,
            CreatedByUserId = TestData.UserId,
            Title = title,
            Description = $"Detailed description for {title}.",
            Severity = AuditFindingSeverity.High
        });
    }
}
