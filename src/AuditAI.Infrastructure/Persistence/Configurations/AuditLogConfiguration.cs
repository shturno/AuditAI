using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditAI.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.EntityName)
            .HasColumnName("entity_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnName("entity_id")
            .IsRequired();

        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(x => x.Timestamp)
            .HasColumnName("timestamp")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.Timestamp })
            .HasDatabaseName("ix_audit_logs_organization_id_timestamp");

        builder.HasIndex(x => new { x.EntityName, x.EntityId, x.Timestamp })
            .HasDatabaseName("ix_audit_logs_entity_name_entity_id_timestamp");

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_audit_logs_action",
                "action IN ('UserLoggedIn', 'UserLoggedOut', 'ControlCreated', 'ControlUpdated', 'ControlDeleted', 'EvidenceSubmitted', 'EvidenceAccepted', 'EvidenceRejected', 'AuditFindingCreated', 'AuditFindingResolved', 'ActionPlanCreated', 'ActionPlanCompleted', 'UserRoleChanged', 'ControlDeactivated', 'AuditFindingUpdated', 'AuditFindingStatusChanged', 'ActionPlanUpdated', 'ActionPlanStatusChanged')");
        });

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
