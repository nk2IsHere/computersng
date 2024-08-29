namespace Computers.Computer.Boundary;

public record Configuration(
    int WindowWidth,
    int WindowHeight,
    int CanvasWidth,
    int CanvasHeight,
    string EntryPointPath,
    string FontDefinitionPath,
    string FontTexturePath
);
