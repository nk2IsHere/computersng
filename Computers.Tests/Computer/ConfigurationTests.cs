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
    public void EngineSchedulerKeysParse() {
        var yaml = string.Join("\n",
            "engine:",
            "  shouldResetScriptOnFatalError: true",
            "  schedulerWorkers: 2",
            "  maxStatementsPerSlice: 5000"
        );
        var configuration = yaml.DeserializeConfiguration<Configuration>();
        Assert.Equal(2, configuration.Engine.SchedulerWorkers);
        Assert.Equal(5000, configuration.Engine.MaxStatementsPerSlice);
    }

    [Fact]
    public void EngineSchedulerKeysHaveSpecDefaults() {
        var yaml = string.Join("\n",
            "engine:",
            "  shouldResetScriptOnFatalError: true"
        );
        var configuration = yaml.DeserializeConfiguration<Configuration>();
        Assert.Equal(0, configuration.Engine.SchedulerWorkers);
        Assert.Equal(2_000_000, configuration.Engine.MaxStatementsPerSlice);
    }

    [Fact]
    public void PeripheralKeysParse() {
        var yaml = string.Join("\n",
            "peripheral:",
            "  maxGroupSize: 9",
            "  maxCommandsPerTick: 3",
            "  maxSubscribersPerPeripheral: 2"
        );
        var configuration = yaml.DeserializeConfiguration<Configuration>();
        Assert.Equal(9, configuration.Peripheral.MaxGroupSize);
        Assert.Equal(3, configuration.Peripheral.MaxCommandsPerTick);
        Assert.Equal(2, configuration.Peripheral.MaxSubscribersPerPeripheral);
    }

    [Fact]
    public void PeripheralKeysHaveSpecDefaultsWhenSectionOmitted() {
        var yaml = string.Join("\n",
            "engine:",
            "  shouldResetScriptOnFatalError: true"
        );
        var configuration = yaml.DeserializeConfiguration<Configuration>();
        Assert.NotNull(configuration.Peripheral);
        Assert.Equal(256, configuration.Peripheral.MaxGroupSize);
        Assert.Equal(16, configuration.Peripheral.MaxCommandsPerTick);
        Assert.Equal(16, configuration.Peripheral.MaxSubscribersPerPeripheral);
    }

    [Fact]
    public void RenderMaxFpsDefaultsTo60WhenOmitted() {
        var yaml = string.Join("\n",
            "render:",
            "  canvasWidth: 452",
            "  canvasHeight: 256",
            "  fontDefaultScale: 1"
        );
        var configuration = yaml.DeserializeConfiguration<Configuration>();
        Assert.Equal(60, configuration.Render.MaxFps);
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
