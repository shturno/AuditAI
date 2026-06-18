using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.ActionPlans.Contracts;
using AuditAI.Application.AuditFindings.Contracts;
using AuditAI.Application.AuditLogs.Contracts;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Evidence.Contracts;
using AuditAI.Domain.Enums;
using AuditAI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AuditAI.IntegrationTests.AuditLogs;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuditLogsApiTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public AuditLogsApiTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_PersistControlCreatedAuditLog_When_ControlIsCreated()
    {
        await _fixture.ResetDatabaseAsync();
        var control = await CreateControlAsync("CTRL-AUD-001");

        var page = await GetAuditLogsAsync($"action={AuditLogAction.ControlCreated}&entityName=Control&entityId={control.Id}");

        Assert.Contains(page.Items, item => item.EntityId == control.Id && item.Action == AuditLogAction.ControlCreated);
    }

    [Fact]
    public async Task Should_PersistEvidenceRejectedAuditLog_When_EvidenceIsRejected()
    {
        await _fixture.ResetDatabaseAsync();
        var evidence = await CreateEvidenceAsync("evidence/audit-log-reject.pdf");

        var rejectResponse = await _fixture.Client.PatchAsJsonAsync(
            $"/api/evidence/{evidence.Id}/reject",
            new ReviewEvidenceRequest
            {
                ReviewerUserId = TestData.UserId,
                RejectionReason = "Missing approval."
            });

        rejectResponse.EnsureSuccessStatusCode();

        var log = await GetSingleAuditLogAsync($"action={AuditLogAction.EvidenceRejected}&entityName=Evidence&entityId={evidence.Id}");

        Assert.Equal(AuditLogAction.EvidenceRejected, log.Action);
        Assert.DoesNotContain("token", log.Metadata ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_PersistAuditFindingStatusChangedAuditLog_When_StatusChanges()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding audit log status", AuditFindingSeverity.High);

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/audit-findings/{finding.Id}/status",
            new ChangeAuditFindingStatusRequest
            {
                Status = AuditFindingStatus.InProgress
            });

        response.EnsureSuccessStatusCode();

        var log = await GetSingleAuditLogAsync($"action={AuditLogAction.AuditFindingStatusChanged}&entityName=AuditFinding&entityId={finding.Id}");

        Assert.Equal(AuditLogAction.AuditFindingStatusChanged, log.Action);
    }

    [Fact]
    public async Task Should_PersistActionPlanStatusChangedAuditLog_When_StatusChanges()
    {
        await _fixture.ResetDatabaseAsync();
        var finding = await CreateFindingAsync("Finding for action plan audit log", AuditFindingSeverity.High);
        var actionPlan = await CreateActionPlanAsync(finding.Id, "Plan audit log");

        var response = await _fixture.Client.PatchAsJsonAsync(
            $"/api/action-plans/{actionPlan.Id}/status",
            new ChangeActionPlanStatusRequest
            {
                Status = ActionPlanStatus.InProgress
            });

        response.EnsureSuccessStatusCode();

        var log = await GetSingleAuditLogAsync($"action={AuditLogAction.ActionPlanStatusChanged}&entityName=ActionPlan&entityId={actionPlan.Id}");

        Assert.Equal(AuditLogAction.ActionPlanStatusChanged, log.Action);
    }

    [Fact]
    public async Task Should_ReturnPaginatedAuditLogs()
    {
        await _fixture.ResetDatabaseAsync();
        await CreateControlAsync("CTRL-AUD-002");
        await CreateControlAsync("CTRL-AUD-003");

        var response = await _fixture.Client.GetAsync($"/api/audit-logs?pageNumber=1&pageSize=10&organizationId={TestData.OrganizationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogListItemResponse>>();
        Assert.NotNull(page);
        Assert.True(page.TotalCount >= 2);
    }

    [Fact]
    public async Task Should_ReturnAuditLogById_When_LogExists()
    {
        await _fixture.ResetDatabaseAsync();
        var control = await CreateControlAsync("CTRL-AUD-004");
        var log = await GetSingleAuditLogAsync($"action={AuditLogAction.ControlCreated}&entityName=Control&entityId={control.Id}");

        var response = await _fixture.Client.GetAsync($"/api/audit-logs/{log.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuditLogResponse>();
        Assert.NotNull(body);
        Assert.Equal(log.Id, body.Id);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_AuditLogIsMissing()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync($"/api/audit-logs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Should_NotExposeSecretsInAuditLogMetadata()
    {
        await _fixture.ResetDatabaseAsync();
        var evidence = await CreateEvidenceAsync("evidence/audit-log-safe.pdf");

        var rejectResponse = await _fixture.Client.PatchAsJsonAsync(
            $"/api/evidence/{evidence.Id}/reject",
            new ReviewEvidenceRequest
            {
                ReviewerUserId = TestData.UserId,
                RejectionReason = "Missing approval."
            });

        rejectResponse.EnsureSuccessStatusCode();
        var log = await GetSingleAuditLogAsync($"action={AuditLogAction.EvidenceRejected}&entityName=Evidence&entityId={evidence.Id}");
        var metadata = log.Metadata ?? string.Empty;

        Assert.DoesNotContain("password", metadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", metadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("jwt", metadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connectionString", metadata, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_OrderAuditLogsByTimestampDescending_ByDefault()
    {
        await _fixture.ResetDatabaseAsync();
        var first = await CreateControlAsync("CTRL-AUD-005");
        var second = await CreateControlAsync("CTRL-AUD-006");

        var page = await GetAuditLogsAsync($"organizationId={TestData.OrganizationId}&action={AuditLogAction.ControlCreated}");

        var firstIndex = page.Items.ToList().FindIndex(item => item.EntityId == first.Id);
        var secondIndex = page.Items.ToList().FindIndex(item => item.EntityId == second.Id);

        Assert.NotEqual(-1, firstIndex);
        Assert.NotEqual(-1, secondIndex);
        Assert.True(secondIndex < firstIndex);
    }

    private async Task<ControlResponse> CreateControlAsync(string code)
    {
        using var client = await _fixture.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/controls", new CreateControlRequest
        {
            OrganizationId = TestData.OrganizationId,
            DepartmentId = TestData.DepartmentId,
            Code = code,
            Category = "Access Management",
            Title = $"Control {code}",
            Description = $"Detailed quarterly access review for {code}.",
            Frequency = ControlFrequency.Quarterly
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ControlResponse>())!;
    }

    private async Task<EvidenceResponse> CreateEvidenceAsync(string storageReference)
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/evidence", new CreateEvidenceRequest
        {
            ControlId = TestData.ControlId,
            SubmittedByUserId = TestData.UserId,
            FileName = "report.pdf",
            StorageReference = storageReference
        });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EvidenceResponse>())!;
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

    private async Task<PagedResult<AuditLogListItemResponse>> GetAuditLogsAsync(string query)
    {
        var response = await _fixture.Client.GetAsync($"/api/audit-logs?pageNumber=1&pageSize=20&{query}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<AuditLogListItemResponse>>())!;
    }

    private async Task<AuditLogResponse> GetSingleAuditLogAsync(string query)
    {
        var page = await GetAuditLogsAsync(query);
        var item = Assert.Single(page.Items);

        var response = await _fixture.Client.GetAsync($"/api/audit-logs/{item.Id}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuditLogResponse>())!;
    }
}
