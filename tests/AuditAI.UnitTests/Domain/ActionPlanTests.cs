using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using AuditAI.Domain.Exceptions;

namespace AuditAI.UnitTests.Domain;

public sealed class ActionPlanTests
{
    [Fact]
    public void Should_NotAllowActionPlanDueDateBeforeCreationDate()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var dueDate = createdAt.AddDays(-1);

        Assert.Throws<DomainRuleViolationException>(() =>
            ActionPlan.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Fix control gap",
                "Implement remediation steps.",
                dueDate,
                createdAt));
    }

    [Fact]
    public void Should_CreateActionPlan_When_DueDateIsValid()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var dueDate = createdAt.AddDays(10);

        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fix control gap",
            "Implement remediation steps.",
            dueDate,
            createdAt);

        Assert.Equal(ActionPlanStatus.Open, actionPlan.Status);
        Assert.Equal(dueDate, actionPlan.DueDate);
    }

    [Fact]
    public void Should_NotAllowMoveToInProgress_When_ActionPlanIsNotOpen()
    {
        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fix control gap",
            "Implement remediation steps.",
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow);

        actionPlan.Complete(DateTimeOffset.UtcNow.AddDays(1));

        Assert.Throws<DomainRuleViolationException>(() => actionPlan.MarkInProgress(DateTimeOffset.UtcNow.AddDays(2)));
    }

    [Fact]
    public void Should_NotAllowCancel_When_ActionPlanIsCompleted()
    {
        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fix control gap",
            "Implement remediation steps.",
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow);

        actionPlan.Complete(DateTimeOffset.UtcNow.AddDays(1));

        Assert.Throws<DomainRuleViolationException>(() => actionPlan.Cancel(DateTimeOffset.UtcNow.AddDays(2)));
    }

    [Fact]
    public void Should_NotAllowMarkOverdue_When_ActionPlanIsCancelled()
    {
        var actionPlan = ActionPlan.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fix control gap",
            "Implement remediation steps.",
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow);

        actionPlan.Cancel(DateTimeOffset.UtcNow.AddDays(1));

        Assert.Throws<DomainRuleViolationException>(() => actionPlan.MarkOverdue(DateTimeOffset.UtcNow.AddDays(2)));
    }
}
