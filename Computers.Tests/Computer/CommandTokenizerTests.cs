using Computers.Computer.Domain;
using Computers.Game;
using Computers.Tests.TestDoubles;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Xunit;

namespace Computers.Tests.Computer;

/// <summary>
/// Drives the real /Core/Commands tokenizer in a bare Jint engine.
/// Bracket-aware tokenization: {…}/[…] tokens are preserved verbatim (quotes, spaces,
/// pipes included) so JSON command arguments survive; behavior outside brackets is
/// unchanged (quotes delimit tokens with spaces and are stripped, | splits pipes).
/// </summary>
public class CommandTokenizerTests {
    private static string[] Tokenize(string input) {
        var loader = new FileSystemLibraryLoader();
        var engine = new Engine(options => {
            options.Strict();
            options.EnableModules(new ComputerModuleLoader(new TestMonitor(), new List<IRedundantLoader> { loader }));
            options.ExperimentalFeatures = ExperimentalFeature.All;
        });

        engine.SetValue("__commands", engine.Modules.Import("/Core/Commands"));
        engine.SetValue("__input", input);
        var result = engine.Evaluate("__commands.ParseTokensForInput(__input)");
        return result.AsArray().Select(value => value.AsString()).ToArray();
    }

    [Fact]
    public void SplitsOnSpaces() {
        Assert.Equal(new[] { ".cd", "/a" }, Tokenize(".cd /a"));
    }

    [Fact]
    public void QuotedTokensKeepSpacesAndDropQuotes() {
        Assert.Equal(new[] { ".write", "f.txt", "Hello, World!" }, Tokenize(".write f.txt \"Hello, World!\""));
    }

    [Fact]
    public void PipeSplitsCommands() {
        Assert.Equal(new[] { ".ls", "|", ".echo" }, Tokenize(".ls | .echo"));
    }

    [Fact]
    public void EscapedQuoteInsideStringSurvives() {
        Assert.Equal(new[] { "a\"b" }, Tokenize("\"a\\\"b\""));
    }

    [Fact]
    public void BraceTokenIsPreservedVerbatim() {
        Assert.Equal(
            new[] { ".peripheral", "ab12", "insert", "{\"machine\":{\"x\":2,\"y\":7},\"itemId\":\"378\"}" },
            Tokenize(".peripheral ab12 insert {\"machine\":{\"x\":2,\"y\":7},\"itemId\":\"378\"}"));
    }

    [Fact]
    public void BraceTokenMayContainSpacesAndPipes() {
        Assert.Equal(
            new[] { ".x", "{\"a b\":\"c|d\", \"n\": 1}" },
            Tokenize(".x {\"a b\":\"c|d\", \"n\": 1}"));
    }

    [Fact]
    public void BraceInsideQuotedValueDoesNotCloseTheToken() {
        Assert.Equal(
            new[] { ".x", "{\"t\":\"a } b\"}" },
            Tokenize(".x {\"t\":\"a } b\"}"));
    }

    [Fact]
    public void BracketArrayIsOneToken() {
        Assert.Equal(
            new[] { ".x", "[1, 2, 3]" },
            Tokenize(".x [1, 2, 3]"));
    }

    [Fact]
    public void RelaxedObjectLiteralIsOneToken() {
        Assert.Equal(
            new[] { ".peripheral", "ab12", "collect", "{machine:all}" },
            Tokenize(".peripheral ab12 collect {machine:all}"));
    }
}
