namespace RentalPipeline.IntegrationTests.Infrastructure;

/// <summary>
/// Shares a single <see cref="RentalPipelineApiFactory"/> (and therefore a single Testcontainers
/// PostgreSQL instance) across every integration test class. Starting a fresh container per test
/// class would be prohibitively slow; sharing one instead means all tests in this collection run
/// sequentially against the same database (xUnit never parallelizes tests within one collection),
/// so each test must use its own randomly-generated data rather than assuming a pristine database.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<RentalPipelineApiFactory>
{
    public const string Name = "Integration Tests";
}
