using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Domain.Enums;
using AuditAI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

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
    public async Task Should_ReturnBadRequest_When_ControlDoesNotExist()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/evidence", new CreateEvidenceRequest
        {
            ControlId = Guid.NewGuid(),
            SubmittedByUserId = TestData.UserId,
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("ControlId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_SubmitterUserDoesNotExist()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.PostAsJsonAsync("/api/evidence", new CreateEvidenceRequest
        {
            ControlId = TestData.ControlId,
            SubmittedByUserId = Guid.NewGuid(),
            FileName = "report.pdf",
            StorageReference = "evidence/report.pdf"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("SubmittedByUserId", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnCreated_When_RequestIsValid()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await CreateValidEvidenceAsync("evidence/report-a.pdf");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<EvidenceResponse>();
        Assert.NotNull(created);
        Assert.Equal(TestData.ControlId, created.ControlId);
        Assert.Equal(TestData.UserId, created.SubmittedByUserId);
        Assert.Equal(EvidenceStatus.Pending, created.Status);
    }

    [Fact]
    public async Task Should_ReturnEvidenceById_AfterValidCreate()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidEvidenceAsync("evidence/report-b.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await _fixture.Client.GetAsync($"/api/evidence/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var evidence = await response.Content.ReadFromJsonAsync<EvidenceResponse>();
        Assert.NotNull(evidence);
        Assert.Equal(created.Id, evidence.Id);
        Assert.Equal("report.pdf", evidence.FileName);
    }

    [Fact]
    public async Task Should_ReturnPaginatedList_IncludingCreatedEvidence()
    {
        await _fixture.ResetDatabaseAsync();
        await CreateValidEvidenceAsync("evidence/report-c.pdf");
        await CreateValidEvidenceAsync("evidence/report-d.pdf");

        var response = await _fixture.Client.GetAsync($"/api/evidence?pageNumber=1&pageSize=10&controlId={TestData.ControlId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<EvidenceListItemResponse>>();
        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 2);
        Assert.Contains(page.Items, item => item.FileName == "report.pdf");
    }

    [Fact]
    public async Task Should_AcceptEvidence_AndPersistAcceptedStatus()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidEvidenceAsync("evidence/report-e.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/evidence/{created!.Id}/accept",
            new ReviewEvidenceRequest
            {
                ReviewerUserId = TestData.UserId
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var accepted = await response.Content.ReadFromJsonAsync<EvidenceResponse>();
        Assert.NotNull(accepted);
        Assert.Equal(EvidenceStatus.Accepted, accepted.Status);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_RejectionReasonIsMissing()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidEvidenceAsync("evidence/report-f.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/evidence/{created!.Id}/reject",
            new ReviewEvidenceRequest
            {
                ReviewerUserId = TestData.UserId,
                RejectionReason = string.Empty
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("RejectionReason", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_RejectEvidence_AndPersistRejectedStatus()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidEvidenceAsync("evidence/report-g.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/evidence/{created!.Id}/reject",
            new ReviewEvidenceRequest
            {
                ReviewerUserId = TestData.UserId,
                RejectionReason = "Missing approval."
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rejected = await response.Content.ReadFromJsonAsync<EvidenceResponse>();
        Assert.NotNull(rejected);
        Assert.Equal(EvidenceStatus.Rejected, rejected.Status);
        Assert.Equal("Missing approval.", rejected.RejectionReason);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_AcceptingAlreadyRejectedEvidence()
    {
        await _fixture.ResetDatabaseAsync();
        var createResponse = await CreateValidEvidenceAsync("evidence/report-h.pdf");
        var created = await createResponse.Content.ReadFromJsonAsync<EvidenceResponse>();

        await _fixture.Client.PatchAsJsonAsync(
            $"/api/evidence/{created!.Id}/reject",
            new ReviewEvidenceRequest
            {
                ReviewerUserId = TestData.UserId,
                RejectionReason = "Missing approval."
            });

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/evidence/{created.Id}/accept",
            new ReviewEvidenceRequest
            {
                ReviewerUserId = TestData.UserId
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(body);
        Assert.Contains("Status", body.Errors.Keys);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_EvidenceIsMissing()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync($"/api/evidence/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> CreateValidEvidenceAsync(string storageReference)
    {
        return await _fixture.Client.PostAsJsonAsync("/api/evidence", new CreateEvidenceRequest
        {
            ControlId = TestData.ControlId,
            SubmittedByUserId = TestData.UserId,
            FileName = "report.pdf",
            StorageReference = storageReference
        });
    }
}
