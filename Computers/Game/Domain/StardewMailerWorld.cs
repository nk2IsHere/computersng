using Computers.Core;
using Computers.Mailer;
using StardewModdingAPI;
using StardewValley;

namespace Computers.Game.Domain;

public class StardewMailerWorld : IMailerWorld {
    private readonly IModHelper _helper;

    public StardewMailerWorld(IModHelper helper) {
        _helper = helper;
    }

    public void Notify(string text, bool error) {
        Game1.addHUDMessage(error ? HUDMessage.ForCornerTextbox(text) : new HUDMessage(text, HUDMessage.newQuest_type));
    }

    public void QueueMail(string mailId) {
        Game1.addMailForTomorrow(mailId);
    }

    public void InvalidateMailData() {
        _helper.GameContent.InvalidateCache("Data/mail");
    }
}

public class MailPatcherService : IPatcherService {
    private readonly ContextLookup<ILetterSource> _mailers;

    public MailPatcherService(ContextLookup<ILetterSource> mailers) {
        _mailers = mailers;
    }

    public bool CanPatch(Type assetType, IAssetName assetName) {
        return assetType == typeof(Dictionary<string, string>)
            && assetName.IsEquivalentTo("Data/mail");
    }

    public void Patch(IAssetData asset) {
        var assetData = asset.GetData<Dictionary<string, string>>();
        foreach (var mailer in _mailers.Get()) {
            foreach (var letter in mailer.Value.Letters) {
                // The vanilla mail format ends the body with the letter title after [#].
                assetData[letter.MailId] = $"{letter.Text}[#]{letter.Title}";
            }
        }
    }
}
