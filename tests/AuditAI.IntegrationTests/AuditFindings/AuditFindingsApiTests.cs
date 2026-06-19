using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.AuditFindings.Contracts;
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
    public async Task Should_ReturnUnauthorized_When_CreateIsCalledWithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = TestData.ControlId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_ListIsCalledWithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/audit-findings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_CreateFinding_And_UseAuthenticatedUserAsCreator()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = TestData.ControlId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AuditFindingResponse>();
        Assert.NotNull(created);
        Assert.Equal(TestData.UserId, created.CreatedByUserId);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_ReviewerCreatesFinding()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync(TestData.ReviewerUserEmail, TestData.ReviewerUserPassword);

        var response = await client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = TestData.ControlId,
            Title = "Reviewer should not create findings",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CreatingFindingForOtherOrganizationControl()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/audit-findings", new CreateAuditFindingRequest
        {
            ControlId = TestData.OtherControlId,
            Title = "Missing approval",
            Description = "This control has no approval evidence.",
            Severity = AuditFindingSeverity.High
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_NotReturnOtherOrganizationFindings_InList()
    {
        await _fixture.ResetDatabaseAsync();

        using var client = await _fixture.CreateAuthenticatedClientAsync();
        using var otherClient = await _fixture.CreateAuthenticatedClientAsync(TestData.OtherUserEmail, TestData.OtherUserPassword);

        var first = await CreateFindingAsync(client, "Organization finding", TestData.ControlId);
        var other = await CreateFindingAsync(otherClient, "Other organization finding", TestData.OtherControlId);

        var response = await client.GetAsync("/api/audit-findings?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<AuditAI.Application.Common.Pagination.PagedResult<AuditFindingListItemResponse>>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Id == first.Id);
        Assert.DoesNotContain(page.Items, item => item.Id == other.Id);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_GettingFindingFromAnotherOrganization()
    {
        await _fixture.ResetDatabaseAsync();

        using var client = await _fixture.CreateAuthenticatedClientAsync();
        using var otherClient = await _fixture.CreateAuthenticatedClientAsync(TestData.OtherUserEmail, TestData.OtherUserPassword);

        var otherFinding = await CreateFindingAsync(otherClient, "Other organization finding", TestData.OtherControlId);

        var response = await client.GetAsync($"/api/audit-findings/{otherFinding.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_AllowValidUpdateAndStatusTransition()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var created = await CreateFindingAsync(client, "Finding update", TestData.ControlId);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/audit-findings/{created.Id}",
            new UpdateAuditFindingRequest
            {
                Title = "Updated finding",
                Description = "Updated description for finding.",
                Severity = AuditFindingSeverity.Critical
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var statusResponse = await client.PatchAsJsonAsync(
            $"/api/audit-findings/{created.Id}/status",
            new ChangeAuditFindingStatusRequest
            {
                Status = AuditFindingStatus.InProgress
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
}
