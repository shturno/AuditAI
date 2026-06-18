using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using AuditAI.Domain.Exceptions;

namespace AuditAI.UnitTests.Domain;

public sealed class AuditFindingTests
{
    [Fact]
    public void Should_NotResolveCriticalFinding_When_OpenActionPlansExist()
    {
        var finding = AuditFinding.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Critical control failure",
            "Segregation of duties is broken.",
            AuditFindingSeverity.Critical,
            DateTimeOffset.UtcNow);

        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            finding.Id,
            Guid.NewGuid(),
            "Restore approval separation",
            "Split responsibilities between approvers.",
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow);

        finding.AddActionPlan(actionPlan);
        finding.MarkInProgress(DateTimeOffset.UtcNow);

        Assert.Throws<DomainRuleViolationException>(() => finding.Resolve(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Should_CreateAuditFinding_When_ControlIsProvided()
    {
        var controlId = Guid.NewGuid();

        var finding = AuditFinding.Create(
            Guid.NewGuid(),
            controlId,
            Guid.NewGuid(),
            "Missing evidence trail",
            "The control lacks periodic evidence.",
            AuditFindingSeverity.Medium,
            DateTimeOffset.UtcNow);

        Assert.Equal(controlId, finding.ControlId);
        Assert.Equal(AuditFindingStatus.Open, finding.Status);
    }

    [Fact]
    public void Should_ResolveCriticalFinding_When_AllActionPlansAreClosed()
    {
        var now = DateTimeOffset.UtcNow;
        var finding = AuditFinding.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Critical control failure",
            "Segregation of duties is broken.",
            AuditFindingSeverity.Critical,
            now);

        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            finding.Id,
            Guid.NewGuid(),
            "Restore approval separation",
            "Split responsibilities between approvers.",
            now.AddDays(7),
            now);

        actionPlan.Complete(now.AddDays(2));
        finding.AddActionPlan(actionPlan);
        finding.MarkInProgress(now.AddDays(1));

        finding.Resolve(now.AddDays(3));

        Assert.Equal(AuditFindingStatus.Resolved, finding.Status);
        Assert.Equal(now.AddDays(3), finding.ResolvedAt);
    }

    [Fact]
    public void Should_NotAllowResolve_When_FindingIsNotInProgress()
    {
        var finding = AuditFinding.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Missing evidence trail",
            "The control lacks periodic evidence.",
            AuditFindingSeverity.Medium,
            DateTimeOffset.UtcNow);

        Assert.Throws<DomainRuleViolationException>(() => finding.Resolve(DateTimeOffset.UtcNow));
    }
}
