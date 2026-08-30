using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.Speaker.Domain.Wire;

public abstract record SpeakerRequest;

public record SpeakerPingRequest : SpeakerRequest;

public record PlayRequest(string Cue, int? Pitch) : SpeakerRequest;
