using Computers.MachineController.Domain;
using Computers.MachineController.Domain.Wire;
using Computers.Router.Domain;

namespace Computers.MachineController;

public interface IMachineWorld {
    IMachineLocation? LocationOf(Placement placement);
}

public interface IMachineLocation {
    IGroupWorld GroupWorld { get; }

    IMachineGroupOps Ops(MachineGroup group, Action invalidateGroup);

    IReadOnlySet<(int X, int Y)> ReadyMachines(IEnumerable<(int X, int Y)> machines);

    MachineMemberSnapshot? Snapshot(int x, int y);
}
