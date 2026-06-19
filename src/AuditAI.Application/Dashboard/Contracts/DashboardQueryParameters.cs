namespace AuditAI.Application.Dashboard.Contracts;

public sealed class DashboardQueryParameters
{
    public int RecentLimit { get; init; } = 5;

    public bool IncludeRecentActivity { get; init; } = true;
}
