using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuditLogActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_audit_logs_action",
                table: "audit_logs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_audit_logs_action",
                table: "audit_logs",
                sql: "action IN ('UserLoggedIn', 'UserLoggedOut', 'ControlCreated', 'ControlUpdated', 'ControlDeleted', 'EvidenceSubmitted', 'EvidenceAccepted', 'EvidenceRejected', 'AuditFindingCreated', 'AuditFindingResolved', 'ActionPlanCreated', 'ActionPlanCompleted', 'UserRoleChanged', 'ControlDeactivated', 'AuditFindingUpdated', 'AuditFindingStatusChanged', 'ActionPlanUpdated', 'ActionPlanStatusChanged')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_audit_logs_action",
                table: "audit_logs");

            migrationBuilder.AddCheckConstraint(
                name: "ck_audit_logs_action",
                table: "audit_logs",
                sql: "action IN ('UserLoggedIn', 'UserLoggedOut', 'ControlCreated', 'ControlUpdated', 'ControlDeleted', 'EvidenceSubmitted', 'EvidenceAccepted', 'EvidenceRejected', 'AuditFindingCreated', 'AuditFindingResolved', 'ActionPlanCreated', 'ActionPlanCompleted', 'UserRoleChanged')");
        }
    }
}
