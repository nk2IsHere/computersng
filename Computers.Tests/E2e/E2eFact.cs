using Xunit;

namespace Computers.Tests.E2e;

// E2e tests run only where the game is installed and the developer opted in.
public sealed class E2eFactAttribute : FactAttribute {
    public E2eFactAttribute() {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPUTERS_E2E"))) {
            Skip = "COMPUTERS_E2E is not set";
        }
    }
}
