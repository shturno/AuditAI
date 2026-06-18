namespace AuditAI.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<PostgreSqlContainerFixture>
{
    public const string Name = "ControlsApiIntegration";
}
