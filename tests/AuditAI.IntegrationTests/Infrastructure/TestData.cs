namespace AuditAI.IntegrationTests.Infrastructure;

internal static class TestData
{
    public static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid DepartmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid OtherOrganizationId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid OtherDepartmentId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static readonly Guid UserId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public static readonly Guid OtherUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    public const string UserEmail = "submitter@auditai.test";

    public const string UserPassword = "P@ssword123!";

    public const string OtherUserEmail = "other@auditai.test";

    public const string OtherUserPassword = "OtherPassword123!";

    public static readonly Guid ControlId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    public static readonly Guid OtherControlId = Guid.Parse("88888888-8888-8888-8888-888888888888");

    public static readonly DateTimeOffset SeedTimestamp = new(2026, 06, 18, 12, 0, 0, TimeSpan.Zero);
}
