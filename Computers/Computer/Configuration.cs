// ReSharper disable ClassNeverInstantiated.Global
namespace Computers.Computer;

public class Configuration {
    public ResourceConfiguration Resource { get; set; } = null!;

    public UiConfiguration Ui { get; set; } = null!;
    
    public RenderConfiguration Render { get; set; } = null!;
    
    public EngineConfiguration Engine { get; set; } = null!;
    
    public NetworkConfiguration Network { get; set; } = null!;
}

public class ResourceConfiguration {
    public string CoreLibraryPath { get; set; } = null!;
    
    public string EntryPointModule { get; set; } = null!;
    
    public string AssetsPath { get; set; } = null!;
    
    public string FontDefinitionPath { get; set; } = null!;
    
    public string FontTexturePath { get; set; } = null!;
}

public class UiConfiguration {
    public int WindowWidth { get; set; }
    
    public int WindowHeight { get; set; }
}

public class RenderConfiguration {
    public int CanvasWidth { get; set; }
    
    public int CanvasHeight { get; set; }
    
    public float FontDefaultScale { get; set; }
}

public class EngineConfiguration {
    public bool ShouldResetScriptOnFatalError { get; set; }
}

public enum NetworkMode {
    AllowAll,
    BlockAll,
    AllowSome,
    BlockSome
}

public class NetworkConfiguration {
    public NetworkMode Mode { get; set; }
    
    public List<string> BlockedAddresses { get; set; } = null!;
    
    public List<string> AllowedAddresses { get; set; } = null!;
}
