using Computers.Computer;
using Computers.Core;
using Computers.Peripheral;
using Computers.Router;
using Computers.Router.Domain;
using StardewModdingAPI;

namespace Computers.Mailer.Domain;

public class MailerStatefulDataContextEntryFactory : IPeripheralFactory {

    private readonly Id _basePeripheralId;

    private readonly IMonitor _monitor;
    private readonly Configuration _configuration;
    private readonly NetworkRegistry _registry;
    private readonly ContextLookup<IRouterPort> _routers;
    private readonly IMailerWorld _mailerWorld;

    public MailerStatefulDataContextEntryFactory(
        Id id,
        Id basePeripheralId,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        IMailerWorld mailerWorld
    ) {
        FactoryId = id;
        _basePeripheralId = basePeripheralId;
        _monitor = monitor;
        _configuration = configuration;
        _registry = registry;
        _routers = routers;
        _mailerWorld = mailerWorld;
    }

    public Id FactoryId { get; }

    public Id ItemId => _basePeripheralId;

    public IContextEntry ProduceValue() {
        return ProduceValue(ContextEntryState.Empty);
    }

    public IContextEntry ProduceValue(ContextEntryState state) {
        var id = state.Id ?? _basePeripheralId / Id.Random();
        return new MailerStatefulDataContextEntry(
            FactoryId,
            id,
            _monitor,
            _configuration,
            _registry,
            _routers,
            _mailerWorld
        );
    }
}
