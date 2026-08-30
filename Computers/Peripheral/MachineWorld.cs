using Computers.Peripheral.Domain;
using Computers.Router.Domain;

namespace Computers.Peripheral;

public interface IMachineWorld {
    IMachineLocation? LocationOf(Placement placement);
}

public interface IMachineLocation {
    IGroupWorld GroupWorld { get; }

    IMachineGroupOps Ops(MachineGroup group, Action invalidateGroup);

    IReadOnlySet<(int X, int Y)> ReadyMachines(IEnumerable<(int X, int Y)> machines);

    MachineSnapshot? Snapshot(int x, int y);
}
