using Computers.Computer;
using Computers.Core;
using Xunit;

namespace Computers.Tests.Computer;

public class ConfigurationTests {
    [Fact]
    public void NetworkSectionParsesLanKeys() {
        var yaml = string.Join("\n",
            "network:",
            "  mode: AllowAll",
            "  blockedAddresses: []",
            "  allowedAddresses: []",
            "  coverageRadius: 7",
            "  linkRadius: 20",
            "  messageTtl: 5",
            "  routerQueueLimit: 8",
            "  maxPayloadBytes: 1024"
        );
        var configuration = yaml.DeserializeConfiguration<Configuration>();
        Assert.Equal(7, configuration.Network.CoverageRadius);
        Assert.Equal(20, configuration.Network.LinkRadius);
        Assert.Equal(5, configuration.Network.MessageTtl);
        Assert.Equal(8, configuration.Network.RouterQueueLimit);
        Assert.Equal(1024, configuration.Network.MaxPayloadBytes);
    }

    [Fact]
    public void LanKeysHaveSpecDefaultsWhenOmitted() {
        var yaml = string.Join("\n",
            "network:",
            "  mode: AllowAll",
            "  blockedAddresses: []",
            "  allowedAddresses: []"
        );
        var configuration = yaml.DeserializeConfiguration<Configuration>();
        Assert.Equal(10, configuration.Network.CoverageRadius);
        Assert.Equal(16, configuration.Network.LinkRadius);
        Assert.Equal(16, configuration.Network.MessageTtl);
        Assert.Equal(256, configuration.Network.RouterQueueLimit);
        Assert.Equal(65536, configuration.Network.MaxPayloadBytes);
    }
}
