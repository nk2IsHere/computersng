using Xunit;

namespace Computers.Tests.E2e;

[CollectionDefinition("E2E", DisableParallelization = true)]
public class E2eCollection : ICollectionFixture<GameFixture> {
}
