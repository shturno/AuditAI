using AuditAI.Domain.Entities;
using AuditAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuditAI.Infrastructure.Persistence.Configurations;

internal sealed class EvidenceConfiguration : IEntityTypeConfiguration<Evidence>
{
    public void Configure(EntityTypeBuilder<Evidence> builder)
    {
        builder.ToTable("evidence");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ControlId)
            .HasColumnName("control_id")
            .IsRequired();

        builder.Property(x => x.SubmittedByUserId)
            .HasColumnName("submitted_by_user_id")
            .IsRequired();

        builder.Property(x => x.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id");

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StorageReference)
            .HasColumnName("storage_reference")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(x => x.ReviewedAt)
            .HasColumnName("reviewed_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(x => new { x.ControlId, x.Status, x.CreatedAt })
            .HasDatabaseName("ix_evidence_control_id_status_created_at");

        builder.HasIndex(x => new { x.SubmittedByUserId, x.CreatedAt })
            .HasDatabaseName("ix_evidence_submitted_by_user_id_created_at");

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_evidence_status",
                $"status IN ('{nameof(EvidenceStatus.Pending)}', '{nameof(EvidenceStatus.Accepted)}', '{nameof(EvidenceStatus.Rejected)}')");
            tableBuilder.HasCheckConstraint(
                "ck_evidence_rejection_reason_required",
                $"status <> '{nameof(EvidenceStatus.Rejected)}' OR rejection_reason IS NOT NULL");
        });

        builder.HasOne<Control>()
            .WithMany()
            .HasForeignKey(x => x.ControlId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
