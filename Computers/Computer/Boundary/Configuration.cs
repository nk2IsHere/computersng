namespace Computers.Computer.Boundary;

public record Configuration(
    int WindowWidth,
    int WindowHeight,
    int CanvasWidth,
    int CanvasHeight,
    string EntryPointPath,
    string EntryPointName,
    string FontDefinitionPath,
    string FontTexturePath,
    float FontDefaultScale,
    bool ShouldResetScriptOnFatalError
);
