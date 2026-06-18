using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditAI.Infrastructure.Persistence.Configurations;

internal sealed class AuditFindingConfiguration : IEntityTypeConfiguration<AuditFinding>
{
    public void Configure(EntityTypeBuilder<AuditFinding> builder)
    {
        builder.ToTable("audit_findings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ControlId)
            .HasColumnName("control_id")
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.Severity)
            .HasColumnName("severity")
            .HasConversion<string>()
            .HasMaxLength(32)
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

        builder.Property(x => x.ResolvedAt)
            .HasColumnName("resolved_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.ControlId, x.Status, x.CreatedAt })
            .HasDatabaseName("ix_audit_findings_control_id_status_created_at");

        builder.HasIndex(x => new { x.Severity, x.Status, x.CreatedAt })
            .HasDatabaseName("ix_audit_findings_severity_status_created_at");

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_audit_findings_severity",
                $"severity IN ('{nameof(AuditFindingSeverity.Low)}', '{nameof(AuditFindingSeverity.Medium)}', '{nameof(AuditFindingSeverity.High)}', '{nameof(AuditFindingSeverity.Critical)}')");
            tableBuilder.HasCheckConstraint(
                "ck_audit_findings_status",
                $"status IN ('{nameof(AuditFindingStatus.Open)}', '{nameof(AuditFindingStatus.InProgress)}', '{nameof(AuditFindingStatus.Resolved)}', '{nameof(AuditFindingStatus.Cancelled)}')");
        });

        builder.Navigation(x => x.ActionPlans)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne<Control>()
            .WithMany()
            .HasForeignKey(x => x.ControlId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
