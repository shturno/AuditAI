using System.Net;
using System.Net.Http.Json;
using AuditAI.Application.Dashboard.Contracts;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using AuditAI.Infrastructure.Persistence;
using AuditAI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AuditEvidence = AuditAI.Domain.Entities.Evidence;

namespace AuditAI.IntegrationTests.Dashboard;

[Collection(IntegrationTestCollection.Name)]
public sealed class DashboardApiTests
{
    private readonly PostgreSqlContainerFixture _fixture;

    public DashboardApiTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_ReturnUnauthorized_When_GettingDashboardSummary_WithoutToken()
    {
        await _fixture.ResetDatabaseAsync();

        var response = await _fixture.Client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_AdminGetsDashboardSummary()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedDashboardDataAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync(TestData.AdminUserEmail, TestData.AdminUserPassword);

        var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_AuditorGetsDashboardSummary()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedDashboardDataAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync(TestData.UserEmail, TestData.UserPassword);

        var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnOk_When_ReviewerGetsDashboardSummary()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedDashboardDataAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync(TestData.ReviewerUserEmail, TestData.ReviewerUserPassword);

        var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnScopedDashboardCounts_When_GettingDashboardSummary()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedDashboardDataAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync(TestData.AdminUserEmail, TestData.AdminUserPassword);

        var response = await client.GetAsync("/api/dashboard/summary");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<DashboardSummaryResponse>>();
        var summary = Assert.IsType<DashboardSummaryResponse>(body?.Value);

        Assert.Equal(2, summary.Controls.Total);
        Assert.Equal(1, summary.Controls.Active);
        Assert.Equal(1, summary.Controls.Inactive);

        Assert.Equal(3, summary.Evidence.Total);
        Assert.Equal(1, summary.Evidence.Pending);
        Assert.Equal(1, summary.Evidence.Accepted);
        Assert.Equal(1, summary.Evidence.Rejected);

        Assert.Equal(4, summary.Findings.Total);
        Assert.Equal(1, summary.Findings.Open);
        Assert.Equal(1, summary.Findings.InProgress);
        Assert.Equal(1, summary.Findings.Resolved);
        Assert.Equal(1, summary.Findings.Cancelled);
        Assert.Equal(1, summary.Findings.Low);
        Assert.Equal(1, summary.Findings.Medium);
        Assert.Equal(1, summary.Findings.High);
        Assert.Equal(1, summary.Findings.Critical);
        Assert.Equal(1, summary.Findings.UnresolvedCritical);

        Assert.Equal(6, summary.ActionPlans.Total);
        Assert.Equal(2, summary.ActionPlans.Open);
        Assert.Equal(1, summary.ActionPlans.InProgress);
        Assert.Equal(1, summary.ActionPlans.Completed);
        Assert.Equal(1, summary.ActionPlans.Overdue);
        Assert.Equal(1, summary.ActionPlans.Cancelled);
        Assert.Equal(2, summary.ActionPlans.OverdueCount);
        Assert.Equal(1, summary.ActionPlans.DueSoonCount);
    }

    [Fact]
    public async Task Should_NotIncludeOtherOrganizationData_When_GettingDashboardSummary()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedDashboardDataAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync(TestData.AdminUserEmail, TestData.AdminUserPassword);

        var response = await client.GetAsync("/api/dashboard/summary");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<DashboardSummaryResponse>>();
        var summary = Assert.IsType<DashboardSummaryResponse>(body?.Value);

        Assert.Equal(2, summary.Controls.Total);
        Assert.Equal(3, summary.Evidence.Total);
        Assert.Equal(4, summary.Findings.Total);
        Assert.Equal(6, summary.ActionPlans.Total);
        Assert.DoesNotContain(summary.RecentActivity, item => item.EntityId == TestData.OtherControlId);
    }

    [Fact]
    public async Task Should_OrderRecentActivityByTimestampDescending_When_GettingDashboardSummary()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedDashboardDataAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync(TestData.AdminUserEmail, TestData.AdminUserPassword);

        var response = await client.GetAsync("/api/dashboard/summary?recentLimit=3");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<DashboardSummaryResponse>>();
        var summary = Assert.IsType<DashboardSummaryResponse>(body?.Value);

        Assert.Equal(3, summary.RecentActivity.Count);
        Assert.Equal(
            summary.RecentActivity.OrderByDescending(item => item.Timestamp).Select(item => item.Id),
            summary.RecentActivity.Select(item => item.Id));
    }

    [Fact]
    public async Task Should_ReturnEmptyRecentActivity_When_IncludeRecentActivityIsFalse()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedDashboardDataAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync(TestData.AdminUserEmail, TestData.AdminUserPassword);

        var response = await client.GetAsync("/api/dashboard/summary?includeRecentActivity=false");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ApiEnvelope<DashboardSummaryResponse>>();
        var summary = Assert.IsType<DashboardSummaryResponse>(body?.Value);

        Assert.Empty(summary.RecentActivity);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_RecentLimitExceedsMaximum()
    {
        await _fixture.ResetDatabaseAsync();
        await SeedDashboardDataAsync();
        using var client = await _fixture.CreateAuthenticatedClientAsync(TestData.AdminUserEmail, TestData.AdminUserPassword);

        var response = await client.GetAsync("/api/dashboard/summary?recentLimit=21");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task SeedDashboardDataAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditAIDbContext>();
        var createdAt = TestData.SeedTimestamp;

        var secondControl = Control.Create(
            Guid.Parse("10111111-1111-1111-1111-111111111111"),
            TestData.OrganizationId,
            TestData.DepartmentId,
            "CTRL-DASH-002",
            "Dashboard",
            "Inactive dashboard control",
            "Dashboard test control",
            ControlFrequency.Monthly,
            createdAt);
        secondControl.Deactivate(createdAt.AddMinutes(1));

        var otherSecondControl = Control.Create(
            Guid.Parse("20222222-2222-2222-2222-222222222222"),
            TestData.OtherOrganizationId,
            TestData.OtherDepartmentId,
            "CTRL-OTHER-002",
            "Other",
            "Other org dashboard control",
            "Other org control",
            ControlFrequency.Yearly,
            createdAt);

        var pendingEvidence = AuditEvidence.Create(Guid.Parse("30111111-1111-1111-1111-111111111111"), TestData.ControlId, TestData.UserId, "pending.pdf", "evidence/pending.pdf", createdAt);
        var acceptedEvidence = AuditEvidence.Create(Guid.Parse("30222222-2222-2222-2222-222222222222"), secondControl.Id, TestData.UserId, "accepted.pdf", "evidence/accepted.pdf", createdAt);
        acceptedEvidence.Accept(TestData.ReviewerUserId, createdAt.AddHours(1));
        var rejectedEvidence = AuditEvidence.Create(Guid.Parse("30333333-3333-3333-3333-333333333333"), secondControl.Id, TestData.UserId, "rejected.pdf", "evidence/rejected.pdf", createdAt);
        rejectedEvidence.Reject(TestData.ReviewerUserId, "Missing evidence.", createdAt.AddHours(2));
        var otherEvidence = AuditEvidence.Create(Guid.Parse("30444444-4444-4444-4444-444444444444"), TestData.OtherControlId, TestData.OtherUserId, "other.pdf", "evidence/other.pdf", createdAt);

        var openFinding = AuditFinding.Create(Guid.Parse("40111111-1111-1111-1111-111111111111"), TestData.ControlId, TestData.UserId, "Open finding", "Open finding description", AuditFindingSeverity.Low, createdAt);
        var inProgressCriticalFinding = AuditFinding.Create(Guid.Parse("40222222-2222-2222-2222-222222222222"), TestData.ControlId, TestData.UserId, "Critical finding", "Critical finding description", AuditFindingSeverity.Critical, createdAt);
        inProgressCriticalFinding.MarkInProgress(createdAt.AddHours(1));
        var resolvedFinding = AuditFinding.Create(Guid.Parse("40333333-3333-3333-3333-333333333333"), secondControl.Id, TestData.UserId, "Resolved finding", "Resolved finding description", AuditFindingSeverity.High, createdAt);
        resolvedFinding.MarkInProgress(createdAt.AddHours(1));
        resolvedFinding.Resolve(createdAt.AddHours(2));
        var cancelledFinding = AuditFinding.Create(Guid.Parse("40444444-4444-4444-4444-444444444444"), secondControl.Id, TestData.UserId, "Cancelled finding", "Cancelled finding description", AuditFindingSeverity.Medium, createdAt);
        cancelledFinding.Cancel(createdAt.AddHours(1));
        var otherFinding = AuditFinding.Create(Guid.Parse("40555555-5555-5555-5555-555555555555"), TestData.OtherControlId, TestData.OtherUserId, "Other org finding", "Other org finding description", AuditFindingSeverity.Critical, createdAt);

        var openActionPlan = ActionPlan.Create(Guid.Parse("50111111-1111-1111-1111-111111111111"), openFinding.Id, TestData.UserId, "Open plan", "Open plan description", createdAt.AddDays(10), createdAt);
        var overdueOpenActionPlan = ActionPlan.Create(
            Guid.Parse("50222222-2222-2222-2222-222222222222"),
            openFinding.Id,
            TestData.UserId,
            "Overdue open plan",
            "Overdue open plan description",
            createdAt.AddDays(-1),
            createdAt.AddDays(-2));
        var inProgressActionPlan = ActionPlan.Create(Guid.Parse("50333333-3333-3333-3333-333333333333"), inProgressCriticalFinding.Id, TestData.UserId, "In progress plan", "In progress plan description", createdAt.AddDays(3), createdAt);
        inProgressActionPlan.MarkInProgress(createdAt.AddHours(3));
        var completedActionPlan = ActionPlan.Create(Guid.Parse("50444444-4444-4444-4444-444444444444"), resolvedFinding.Id, TestData.UserId, "Completed plan", "Completed plan description", createdAt.AddDays(5), createdAt);
        completedActionPlan.Complete(createdAt.AddHours(3));
        var overdueStatusActionPlan = ActionPlan.Create(Guid.Parse("50555555-5555-5555-5555-555555555555"), cancelledFinding.Id, TestData.UserId, "Overdue status plan", "Overdue status plan description", createdAt, createdAt);
        overdueStatusActionPlan.MarkOverdue(createdAt.AddDays(1));
        var cancelledActionPlan = ActionPlan.Create(Guid.Parse("50666666-6666-6666-6666-666666666666"), cancelledFinding.Id, TestData.UserId, "Cancelled plan", "Cancelled plan description", createdAt.AddDays(4), createdAt);
        cancelledActionPlan.Cancel(createdAt.AddHours(3));
        var otherActionPlan = ActionPlan.Create(
            Guid.Parse("50777777-7777-7777-7777-777777777777"),
            otherFinding.Id,
            TestData.OtherUserId,
            "Other org plan",
            "Other org plan description",
            createdAt.AddDays(-1),
            createdAt.AddDays(-2));

        var orgLogOldest = AuditLog.Create(Guid.Parse("60111111-1111-1111-1111-111111111111"), TestData.OrganizationId, TestData.UserId, AuditLogAction.ControlCreated, "Control", TestData.ControlId, "{\"source\":\"dashboard-test\"}", createdAt.AddMinutes(1));
        var orgLogMiddle = AuditLog.Create(Guid.Parse("60222222-2222-2222-2222-222222222222"), TestData.OrganizationId, TestData.AdminUserId, AuditLogAction.EvidenceAccepted, "Evidence", acceptedEvidence.Id, "{\"source\":\"dashboard-test\"}", createdAt.AddMinutes(2));
        var orgLogLatest = AuditLog.Create(Guid.Parse("60333333-3333-3333-3333-333333333333"), TestData.OrganizationId, TestData.ReviewerUserId, AuditLogAction.ActionPlanStatusChanged, "ActionPlan", overdueStatusActionPlan.Id, "{\"source\":\"dashboard-test\"}", createdAt.AddMinutes(3));
        var otherOrgLog = AuditLog.Create(Guid.Parse("60444444-4444-4444-4444-444444444444"), TestData.OtherOrganizationId, TestData.OtherUserId, AuditLogAction.ControlCreated, "Control", TestData.OtherControlId, "{\"source\":\"other-org\"}", createdAt.AddMinutes(4));

        context.Controls.AddRange(secondControl, otherSecondControl);
        context.Evidence.AddRange(pendingEvidence, acceptedEvidence, rejectedEvidence, otherEvidence);
        context.AuditFindings.AddRange(openFinding, inProgressCriticalFinding, resolvedFinding, cancelledFinding, otherFinding);
        context.ActionPlans.AddRange(openActionPlan, overdueOpenActionPlan, inProgressActionPlan, completedActionPlan, overdueStatusActionPlan, cancelledActionPlan, otherActionPlan);
        context.AuditLogs.AddRange(orgLogOldest, orgLogMiddle, orgLogLatest, otherOrgLog);

        await context.SaveChangesAsync();
    }

    private sealed class ApiEnvelope<T>
    {
        public bool IsSuccess { get; init; }

        public T? Value { get; init; }
    }
}
