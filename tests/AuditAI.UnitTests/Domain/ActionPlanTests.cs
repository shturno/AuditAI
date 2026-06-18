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
}
