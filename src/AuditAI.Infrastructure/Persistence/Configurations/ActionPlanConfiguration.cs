using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditAI.Infrastructure.Persistence.Configurations;

internal sealed class ActionPlanConfiguration : IEntityTypeConfiguration<ActionPlan>
{
    public void Configure(EntityTypeBuilder<ActionPlan> builder)
    {
        builder.ToTable("action_plans");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.AuditFindingId)
            .HasColumnName("audit_finding_id")
            .IsRequired();

        builder.Property(x => x.AssignedToUserId)
            .HasColumnName("assigned_to_user_id")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.DueDate)
            .HasColumnName("due_date")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(x => new { x.AssignedToUserId, x.DueDate })
            .HasDatabaseName("ix_action_plans_assigned_to_user_id_due_date");

        builder.HasIndex(x => new { x.AuditFindingId, x.Status })
            .HasDatabaseName("ix_action_plans_audit_finding_id_status");

        builder.HasIndex(x => new { x.Status, x.DueDate })
            .HasDatabaseName("ix_action_plans_status_due_date");

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_action_plans_status",
                $"status IN ('{nameof(ActionPlanStatus.Open)}', '{nameof(ActionPlanStatus.InProgress)}', '{nameof(ActionPlanStatus.Completed)}', '{nameof(ActionPlanStatus.Overdue)}', '{nameof(ActionPlanStatus.Cancelled)}')");
            tableBuilder.HasCheckConstraint(
                "ck_action_plans_due_date",
                "due_date >= created_at");
        });

        builder.HasOne<AuditFinding>()
            .WithMany(x => x.ActionPlans)
            .HasForeignKey(x => x.AuditFindingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
