using Computers.Core;

namespace Computers.Peripheral;

// A factory for one peripheral kind. ItemId names the big craftable the kind is
// placed as, so placement can find the right factory through a context lookup
// and new kinds plug in by registering a factory entry only.
public interface IPeripheralFactory : IStatefulDataContextEntryFactory {
    Id ItemId { get; }
}
