using Computers.Computer;
using Computers.Core;
using Computers.Peripheral.Domain;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Wire;
using Computers.Speaker.Domain.Wire;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;

namespace Computers.Speaker.Domain;

public class SpeakerStatefulDataContextEntry : PeripheralEntity<SpeakerStatefulDataContextEntry> {

    private readonly ISpeakerWorld _speakerWorld;

    public SpeakerStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        ISpeakerWorld speakerWorld
    ) : base(factoryId, id, monitor, configuration, registry, routers) {
        _speakerWorld = speakerWorld;
    }

    protected override Reply? ProcessRequest(string sourceAddress, JObject request) {
        if (!WireRequests.TryReadCid(request, out var cid)) {
            return null;
        }

        try {
            return Reply.Success(cid, Dispatch(SpeakerRequestParser.ParseBody(request)));
        } catch (PeripheralRequestException exception) {
            return Reply.Failure(cid, exception.Message);
        }
    }

    private object? Dispatch(SpeakerRequest request) {
        switch (request) {
            case SpeakerPingRequest:
                return new PingResult("speaker");

            case PlayRequest play: {
                var placement = Registry.PlacementOf(Id)
                    ?? throw new PeripheralRequestException("speaker has no world position");
                
                return !_speakerWorld.Play(placement, play.Cue, play.Pitch) 
                    ? throw new PeripheralRequestException($"unknown cue '{play.Cue}'") 
                    : null;
            }

            default:
                throw new PeripheralRequestException($"unknown command '{request.GetType().Name}'");
        }
    }
}
