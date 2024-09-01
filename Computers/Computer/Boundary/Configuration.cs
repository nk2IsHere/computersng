namespace Computers.Computer.Boundary;

public record Configuration(
    int WindowWidth,
    int WindowHeight,
    int CanvasWidth,
    int CanvasHeight,
    string CoreLibraryPath,
    string EntryPointModule,
    string AssetsPath,
    string FontDefinitionPath,
    string FontTexturePath,
    float FontDefaultScale,
    bool ShouldResetScriptOnFatalError
);
