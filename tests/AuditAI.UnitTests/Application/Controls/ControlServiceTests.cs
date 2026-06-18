using AuditAI.Application.Common.Abstractions;
using AuditAI.Application.Common.Pagination;
using AuditAI.Application.Controls.Contracts;
using AuditAI.Application.Controls.Interfaces;
using AuditAI.Application.Controls.Services;
using AuditAI.Application.Controls.Validators;
using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;

namespace AuditAI.UnitTests.Application.Controls;

public sealed class ControlServiceTests
{
    [Fact]
    public async Task Should_ReturnNotFound_When_ControlDoesNotExist()
    {
        var repository = new FakeControlRepository();
        var service = new UpdateControlService(repository, new FakeDateTimeProvider(), new UpdateControlRequestValidator());

        var result = await service.ExecuteAsync(
            Guid.NewGuid(),
            new UpdateControlRequest
            {
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Quarterly access review",
                Description = "Detailed quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task Should_CreateControl_When_RequestIsValid()
    {
        var repository = new FakeControlRepository();
        var service = new CreateControlService(repository, new FakeDateTimeProvider(), new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(
            new CreateControlRequest
            {
                OrganizationId = Guid.NewGuid(),
                DepartmentId = Guid.NewGuid(),
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Quarterly access review",
                Description = "Detailed quarterly access review.",
                Frequency = ControlFrequency.Quarterly
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(repository.StoredControls);
        Assert.Equal("Access Management", repository.StoredControls[0].Category);
    }

    [Fact]
    public async Task Should_NotBypassDomainInvariants_When_RequestIsInvalid()
    {
        var repository = new FakeControlRepository();
        var service = new CreateControlService(repository, new FakeDateTimeProvider(), new CreateControlRequestValidator());

        var result = await service.ExecuteAsync(
            new CreateControlRequest
            {
                OrganizationId = Guid.NewGuid(),
                Code = "CTRL-001",
                Category = "Access Management",
                Title = "Valid title",
                Description = string.Empty,
                Frequency = ControlFrequency.Monthly
            });

        Assert.False(result.IsSuccess);
        Assert.True(result.IsValidationFailure);
        Assert.Empty(repository.StoredControls);
    }

    private sealed class FakeControlRepository : IControlRepository
    {
        public List<Control> StoredControls { get; } = [];

        public Task AddAsync(Control control, CancellationToken cancellationToken)
        {
            StoredControls.Add(control);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsWithCodeAsync(
            Guid organizationId,
            string code,
            Guid? excludedControlId,
            CancellationToken cancellationToken)
        {
            var exists = StoredControls.Any(control =>
                control.OrganizationId == organizationId &&
                control.Code == code &&
                (!excludedControlId.HasValue || control.Id != excludedControlId.Value));

            return Task.FromResult(exists);
        }

        public Task<Control?> GetByIdAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredControls.SingleOrDefault(control => control.Id == controlId));
        }

        public Task<Control?> GetByIdForUpdateAsync(Guid controlId, CancellationToken cancellationToken)
        {
            return Task.FromResult(StoredControls.SingleOrDefault(control => control.Id == controlId));
        }

        public Task<PagedResult<Control>> ListAsync(ControlQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<Control>(StoredControls, StoredControls.Count, 1, 20));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 06, 18, 15, 0, 0, TimeSpan.Zero);
    }
}
