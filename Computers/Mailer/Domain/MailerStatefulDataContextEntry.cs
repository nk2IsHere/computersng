using Computers.Computer;
using Computers.Core;
using Computers.Mailer.Domain.Wire;
using Computers.Peripheral.Domain;
using Computers.Router;
using Computers.Router.Domain;
using Computers.Router.Domain.Wire;
using Newtonsoft.Json.Linq;
using StardewModdingAPI;
using Context = Computers.Core.Context;

namespace Computers.Mailer.Domain;

public class MailerStatefulDataContextEntry : PeripheralEntity<MailerStatefulDataContextEntry>, ILetterSource {

    private readonly IMailerWorld _mailerWorld;

    // Persisted so queued letters survive save and load, since the Data/mail patch reads
    // them back from here.
    private readonly List<Letter> _letters = new();
    private int _nextLetterNumber = 1;

    public MailerStatefulDataContextEntry(
        Id factoryId,
        Id id,
        IMonitor monitor,
        Configuration configuration,
        NetworkRegistry registry,
        ContextLookup<IRouterPort> routers,
        IMailerWorld mailerWorld
    ) : base(factoryId, id, monitor, configuration, registry, routers) {
        _mailerWorld = mailerWorld;
    }

    public IReadOnlyList<Letter> Letters => _letters;

    public override void Restore(Context context, ContextEntryState state) {
        _nextLetterNumber = Convert.ToInt32(state.GetOrDefault<object>("NextLetterNumber", 1));
        _letters.Clear();
        _letters.AddRange(state.GetOrDefault("Letters", new List<Letter>()));
    }

    public override ContextEntryState Store(Context context) {
        var state = base.Store(context);
        state.Set("NextLetterNumber", _nextLetterNumber);
        state.Set("Letters", new List<Letter>(_letters));
        return state;
    }

    protected override Reply? ProcessRequest(string sourceAddress, JObject request) {
        if (!WireRequests.TryReadCid(request, out var cid)) {
            return null;
        }

        try {
            return Reply.Success(cid, Dispatch(MailerRequestParser.ParseBody(request)));
        } catch (PeripheralRequestException exception) {
            return Reply.Failure(cid, exception.Message);
        }
    }

    private object? Dispatch(MailerRequest request) {
        switch (request) {
            case MailerPingRequest:
                return new PingResult("mailer");

            case NotifyRequest notify:
                _mailerWorld.Notify(notify.Text, notify.Error);
                return null;

            case MailRequest mail: {
                var mailId = $"{Id.Last}_{_nextLetterNumber++}";
                _letters.Add(new Letter(mailId, mail.Text, mail.Title ?? "Letter from the LAN"));
                _mailerWorld.InvalidateMailData();
                _mailerWorld.QueueMail(mailId);
                return new MailResult(mailId);
            }

            default:
                throw new PeripheralRequestException($"unknown command '{request.GetType().Name}'");
        }
    }
}
