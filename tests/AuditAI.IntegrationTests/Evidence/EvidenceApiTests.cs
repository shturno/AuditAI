using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using AuditAI.Infrastructure.Persistence;
using AuditAI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuditAI.IntegrationTests.Evidence;

[Collection(IntegrationTestCollection.Name)]
public sealed class EvidenceApiTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public EvidenceApiTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_CreatingEvidence_WithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/evidence", new CreateEvidenceRequest
        {
            ControlId = TestData.ControlId,
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_ListingEvidence_WithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/evidence?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnCreated_When_RequestIsValid()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await CreateValidEvidenceAsync(client, TestData.ControlId, "evidence/report-a.pdf");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<EvidenceResponse>();
        Assert.NotNull(created);
        Assert.Equal(TestData.ControlId, created.ControlId);
        Assert.Equal(TestData.UserId, created.SubmittedByUserId);
        Assert.Equal(EvidenceStatus.Pending, created.Status);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ControlBelongsToAnotherOrganization()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await CreateValidEvidenceAsync(client, TestData.OtherControlId, "evidence/report-foreign.pdf");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnEvidenceById_AfterValidCreate()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var createResponse = await CreateValidEvidenceAsync(client, TestData.ControlId, "evidence/report-b.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await client.GetAsync($"/api/evidence/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var evidence = await response.Content.ReadFromJsonAsync<EvidenceResponse>();
        Assert.NotNull(evidence);
        Assert.Equal(created.Id, evidence.Id);
        Assert.Equal("report.pdf", evidence.FileName);
    }

    [Fact]
    public async Task Should_ListOnlyEvidence_FromAuthenticatedOrganization()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        using var otherClient = await _fixture.CreateAuthenticatedClientAsync(TestData.OtherUserEmail, TestData.OtherUserPassword);

        await CreateValidEvidenceAsync(client, TestData.ControlId, "evidence/report-c.pdf");
        var otherCreateResponse = await CreateValidEvidenceAsync(otherClient, TestData.OtherControlId, "evidence/report-d.pdf");
        var otherCreated = await otherCreateResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await client.GetAsync($"/api/evidence?pageNumber=1&pageSize=10&submittedByUserId={TestData.OtherUserId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<EvidenceListItemResponse>>();
        Assert.NotNull(page);
        Assert.DoesNotContain(page.Items, item => item.Id == otherCreated!.Id);
        Assert.All(page.Items, item => Assert.Equal(TestData.UserId, item.SubmittedByUserId));
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_GettingEvidenceFromAnotherOrganization()
    {
        await _fixture.ResetDatabaseAsync();
        using var otherClient = await _fixture.CreateAuthenticatedClientAsync(TestData.OtherUserEmail, TestData.OtherUserPassword);
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var otherCreateResponse = await CreateValidEvidenceAsync(otherClient, TestData.OtherControlId, "evidence/report-other.pdf");
        var otherCreated = await otherCreateResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await client.GetAsync($"/api/evidence/{otherCreated!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_AcceptEvidence_UsingAuthenticatedUserAsReviewer()
    {
        await _fixture.ResetDatabaseAsync();
        using var auditorClient = await _fixture.CreateAuthenticatedClientAsync();
        using var reviewerClient = await _fixture.CreateAuthenticatedClientAsync(TestData.ReviewerUserEmail, TestData.ReviewerUserPassword);
        var createResponse = await CreateValidEvidenceAsync(auditorClient, TestData.ControlId, "evidence/report-e.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await reviewerClient.PatchAsJsonAsync(
            $"/api/evidence/{created!.Id}/accept",
            new ReviewEvidenceRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var accepted = await response.Content.ReadFromJsonAsync<EvidenceResponse>();
        Assert.NotNull(accepted);
        Assert.Equal(EvidenceStatus.Accepted, accepted.Status);
        Assert.Equal(TestData.ReviewerUserId, accepted.ReviewedByUserId);
    }

    [Fact]
    public async Task Should_ReturnForbidden_When_AuditorAcceptsEvidence()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();
        var createResponse = await CreateValidEvidenceAsync(client, TestData.ControlId, "evidence/report-e-auditor.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await client.PatchAsJsonAsync(
            $"/api/evidence/{created!.Id}/accept",
            new ReviewEvidenceRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_RejectionReasonIsMissing()
    {
        await _fixture.ResetDatabaseAsync();
        using var auditorClient = await _fixture.CreateAuthenticatedClientAsync();
        using var reviewerClient = await _fixture.CreateAuthenticatedClientAsync(TestData.ReviewerUserEmail, TestData.ReviewerUserPassword);
        var createResponse = await CreateValidEvidenceAsync(auditorClient, TestData.ControlId, "evidence/report-f.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await reviewerClient.PatchAsJsonAsync(
            $"/api/evidence/{created!.Id}/reject",
            new ReviewEvidenceRequest
            {
                RejectionReason = string.Empty
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("RejectionReason", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_CreateAuditLogWithAuthenticatedActor_When_RejectingEvidence()
    {
        await _fixture.ResetDatabaseAsync();
        using var auditorClient = await _fixture.CreateAuthenticatedClientAsync();
        using var reviewerClient = await _fixture.CreateAuthenticatedClientAsync(TestData.ReviewerUserEmail, TestData.ReviewerUserPassword);
        var createResponse = await CreateValidEvidenceAsync(auditorClient, TestData.ControlId, "evidence/report-g.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await reviewerClient.PatchAsJsonAsync(
            $"/api/evidence/{created!.Id}/reject",
            new ReviewEvidenceRequest
            {
                RejectionReason = "Missing approval."
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _fixture.Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditAIDbContext>();
        var auditLog = await dbContext.AuditLogs
            .AsNoTracking()
            .SingleAsync(log =>
                log.EntityName == nameof(Evidence) &&
                log.EntityId == created.Id &&
                log.Action == AuditLogAction.EvidenceRejected);

        Assert.Equal(TestData.ReviewerUserId, auditLog.UserId);
        Assert.Equal(TestData.OrganizationId, auditLog.OrganizationId);
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_AuditFindingsAreCalledWithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/audit-findings?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_EvidenceIsMissing()
    {
        await _fixture.ResetDatabaseAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/evidence/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Task<HttpResponseMessage> CreateValidEvidenceAsync(HttpClient client, Guid controlId, string storageReference)
    {
        return client.PostAsJsonAsync("/api/evidence", new CreateEvidenceRequest
        {
            ControlId = controlId,
            FileName = "report.pdf",
            StorageReference = storageReference
        });
    }
}
