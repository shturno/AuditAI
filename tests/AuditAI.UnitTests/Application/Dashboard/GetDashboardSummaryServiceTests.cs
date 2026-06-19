using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Dashboard.Contracts;
using AuditAI.Application.Dashboard.Interfaces;
using AuditAI.Application.Dashboard.Services;
using AuditAI.Application.Dashboard.Validators;
using AuditAI.Domain.Enums;
using FluentValidation;
using Moq;

namespace AuditAI.UnitTests.Application.Dashboard;

public sealed class GetDashboardSummaryServiceTests
{
    private readonly Mock<IDashboardSummaryRepository> _repositoryMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly IValidator<DashboardQueryParameters> _validator;
    private readonly GetDashboardSummaryService _service;

    public GetDashboardSummaryServiceTests()
    {
        _repositoryMock = new Mock<IDashboardSummaryRepository>();
        _currentUserMock = new Mock<ICurrentUser>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _validator = new DashboardQueryParametersValidator();
        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(DateTimeOffset.UtcNow);
        _service = new GetDashboardSummaryService(
            _repositoryMock.Object,
            _currentUserMock.Object,
            _dateTimeProviderMock.Object,
            _validator);
    }

    [Fact]
    public async Task Should_FailDashboard_When_CurrentUserIsNotAuthenticated()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);

        var result = await _service.ExecuteAsync(new DashboardQueryParameters());

        Assert.False(result.IsSuccess);
        Assert.True(result.IsUnauthorized);
    }

    [Fact]
    public async Task Should_FailDashboard_When_CurrentUserHasNoOrganizationId()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.OrganizationId).Returns((Guid?)null);
        _currentUserMock.Setup(u => u.Role).Returns(UserRole.Admin);

        var result = await _service.ExecuteAsync(new DashboardQueryParameters());

        Assert.False(result.IsSuccess);
        Assert.True(result.IsUnauthorized);
    }

    [Fact]
    public async Task Should_AllowDashboard_When_CurrentUserIsAdmin()
    {
        var organizationId = Guid.NewGuid();
        SetupAuthorizedUser(organizationId, UserRole.Admin);
        SetupSummaryData();

        var result = await _service.ExecuteAsync(new DashboardQueryParameters());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Should_AllowDashboard_When_CurrentUserIsAuditor()
    {
        var organizationId = Guid.NewGuid();
        SetupAuthorizedUser(organizationId, UserRole.Auditor);
        SetupSummaryData();

        var result = await _service.ExecuteAsync(new DashboardQueryParameters());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_AllowDashboard_When_CurrentUserIsReviewer()
    {
        var organizationId = Guid.NewGuid();
        SetupAuthorizedUser(organizationId, UserRole.Reviewer);
        SetupSummaryData();

        var result = await _service.ExecuteAsync(new DashboardQueryParameters());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Should_FailDashboard_When_CurrentUserRoleIsUnsupported()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.OrganizationId).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.Role).Returns((UserRole?)null);

        var result = await _service.ExecuteAsync(new DashboardQueryParameters());

        Assert.False(result.IsSuccess);
        Assert.True(result.IsForbidden);
    }

    [Fact]
    public async Task Should_CallRepositoryWith_CurrentUserOrganizationId()
    {
        var organizationId = Guid.NewGuid();
        SetupAuthorizedUser(organizationId, UserRole.Admin);
        SetupSummaryData();

        var result = await _service.ExecuteAsync(new DashboardQueryParameters());

        Assert.True(result.IsSuccess);
        _repositoryMock.Verify(
            r => r.GetSummaryAsync(organizationId, 5, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_FailDashboard_When_RecentLimitExceedsMaximum()
    {
        SetupAuthorizedUser(Guid.NewGuid(), UserRole.Admin);

        var result = await _service.ExecuteAsync(new DashboardQueryParameters
        {
            RecentLimit = 21
        });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationFailure);
    }

    [Fact]
    public async Task Should_RespectIncludeRecentActivityFalse_When_RequestDisablesRecentActivity()
    {
        var organizationId = Guid.NewGuid();
        SetupAuthorizedUser(organizationId, UserRole.Admin);
        _repositoryMock
            .Setup(r => r.GetSummaryAsync(organizationId, 5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuditAI.Application.Common.Results.Result<DashboardSummaryData>.Success(CreateSummaryData(Array.Empty<AuditLogEntry>())));

        var result = await _service.ExecuteAsync(new DashboardQueryParameters
        {
            IncludeRecentActivity = false
        });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.RecentActivity);
        _repositoryMock.Verify(
            r => r.GetSummaryAsync(organizationId, 5, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_SetGeneratedAtUtc_FromDateTimeProvider()
    {
        var expectedUtc = new DateTimeOffset(2026, 06, 19, 12, 0, 0, TimeSpan.Zero);
        SetupAuthorizedUser(Guid.NewGuid(), UserRole.Admin);
        SetupSummaryData();
        _dateTimeProviderMock.Setup(d => d.UtcNow).Returns(expectedUtc);

        var result = await _service.ExecuteAsync(new DashboardQueryParameters());

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedUtc, result.Value!.GeneratedAtUtc);
    }

    private void SetupAuthorizedUser(Guid organizationId, UserRole role)
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.OrganizationId).Returns(organizationId);
        _currentUserMock.Setup(u => u.Role).Returns(role);
    }

    private void SetupSummaryData()
    {
        _repositoryMock
            .Setup(r => r.GetSummaryAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuditAI.Application.Common.Results.Result<DashboardSummaryData>.Success(CreateSummaryData(Array.Empty<AuditLogEntry>())));
    }

    private static DashboardSummaryData CreateSummaryData(IReadOnlyList<AuditLogEntry> recentActivity)
    {
        return new DashboardSummaryData(
            TotalControls: 2,
            ActiveControls: 1,
            InactiveControls: 1,
            TotalEvidence: 3,
            PendingEvidence: 1,
            AcceptedEvidence: 1,
            RejectedEvidence: 1,
            TotalFindings: 4,
            OpenFindings: 1,
            InProgressFindings: 1,
            ResolvedFindings: 1,
            CancelledFindings: 1,
            LowFindings: 1,
            MediumFindings: 1,
            HighFindings: 1,
            CriticalFindings: 1,
            UnresolvedCriticalFindings: 1,
            TotalActionPlans: 5,
            OpenActionPlans: 1,
            InProgressActionPlans: 1,
            CompletedActionPlans: 1,
            OverdueStatusActionPlans: 1,
            CancelledActionPlans: 1,
            OverdueActionPlans: 2,
            DueSoonActionPlans: 1,
            RecentActivity: recentActivity);
    }
}
