using StardewModdingAPI;

namespace Computers.Tests.TestDoubles;

public class TestMonitor : IMonitor {
    public bool IsVerbose => false;
    public void Log(string message, LogLevel level = LogLevel.Trace) { }
    public void LogOnce(string message, LogLevel level = LogLevel.Trace) { }
    public void VerboseLog(string message) { }
    public void VerboseLog(ref StardewModdingAPI.Framework.Logging.VerboseLogStringHandler message) { }
}
