using Computers.Peripheral.Domain;
using Newtonsoft.Json.Linq;

namespace Computers.Mailer.Domain.Wire;

public record MailResult(string MailId);

public abstract record MailerRequest;

public record MailerPingRequest : MailerRequest;

public record NotifyRequest(string Text, bool Error) : MailerRequest;

public record MailRequest(string Text, string? Title) : MailerRequest;
