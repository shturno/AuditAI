using AuditAI.Application.Controls.Contracts;
using AuditAI.Domain.Entities;

namespace AuditAI.Application.Controls.Mappers;

internal static class ControlResponseMapper
{
    public static ControlResponse ToResponse(Control control)
    {
        return new ControlResponse(
            control.Id,
            control.OrganizationId,
            control.DepartmentId,
            control.Code,
            control.Category,
            control.Title,
            control.Description,
            control.Status,
            control.Frequency,
            control.CreatedAt,
            control.UpdatedAt);
    }

    public static ControlListItemResponse ToListItemResponse(Control control)
    {
        return new ControlListItemResponse(
            control.Id,
            control.OrganizationId,
            control.DepartmentId,
            control.Code,
            control.Category,
            control.Title,
            control.Status,
            control.Frequency,
            control.CreatedAt,
            control.UpdatedAt);
    }
}
