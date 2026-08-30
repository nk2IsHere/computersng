namespace Computers.Mailer;

public interface IMailerWorld {
    void Notify(string text, bool error);

    void QueueMail(string mailId);

    // Drops the cached Data/mail asset so newly registered letters become visible.
    void InvalidateMailData();
}

public record Letter(string MailId, string Text, string Title);

public interface ILetterSource {
    IReadOnlyList<Letter> Letters { get; }
}
