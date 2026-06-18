namespace AuditAI.IntegrationTests.Infrastructure;

internal static class TestData
{
    public static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid DepartmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid OtherOrganizationId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid OtherDepartmentId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static readonly DateTimeOffset SeedTimestamp = new(2026, 06, 18, 12, 0, 0, TimeSpan.Zero);
}
