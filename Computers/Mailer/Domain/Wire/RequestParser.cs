using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.Mailer.Domain.Wire;

public static class MailerRequestParser {
    public const int MaxTextLength = 1024;

    public static MailerRequest ParseBody(JObject payload) {
        var cmd = payload["cmd"]?.Value<string>();
        switch (cmd) {
            case "ping":
                return new MailerPingRequest();

            case "notify":
                return new NotifyRequest(
                    ReadText(payload),
                    payload["error"]?.Value<bool>() ?? false
                );

            case "mail":
                return new MailRequest(
                    ReadText(payload),
                    payload["title"]?.Value<string>()
                );

            default:
                throw new PeripheralRequestException($"unknown command '{cmd}'");
        }
    }

    private static string ReadText(JObject payload) {
        var text = payload["text"]?.Value<string>();
        if (string.IsNullOrWhiteSpace(text)) {
            throw new PeripheralRequestException("text is required");
        }
        if (text.Length > MaxTextLength) {
            throw new PeripheralRequestException($"text must be at most {MaxTextLength} characters");
        }

        return text;
    }
}
