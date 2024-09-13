using Computers.Game;
using Jint;
using Jint.Runtime.Modules;
using StardewModdingAPI;

namespace Computers.Computer.Domain;

internal class ComputerModuleLoader : ModuleLoader {

    private readonly IMonitor _monitor;
    private readonly List<IRedundantLoader> _libraryLoaders;
    
    public ComputerModuleLoader(IMonitor monitor, List<IRedundantLoader> libraryLoaders) {
        _monitor = monitor;
        _libraryLoaders = libraryLoaders;
    }

    public override ResolvedSpecifier Resolve(string? referencingModuleLocation, ModuleRequest moduleRequest) {
        _monitor.Log($"Resolving module: module location is {referencingModuleLocation} request is {moduleRequest}");
        
        var specifier = moduleRequest.Specifier;
        if (string.IsNullOrEmpty(specifier)) {
            throw new InvalidOperationException($"Invalid Module Specifier for module request: {moduleRequest}");
        }

        var resolvedUri = new Uri("/");
        if (Uri.TryCreate(specifier, UriKind.Absolute, out var uri)) {
            resolvedUri = uri;
        } else if(IsRelative(specifier)) {
            var baseUri = BuildBaseUri(referencingModuleLocation);
            resolvedUri = new Uri(baseUri, specifier);
        }
        
        return new ResolvedSpecifier(
            moduleRequest,
            resolvedUri.AbsoluteUri,
            resolvedUri,
            SpecifierType.RelativeOrAbsolute
        );
    }

    protected override string LoadModuleContents(Engine engine, ResolvedSpecifier resolved) {
        _monitor.Log($"Loading module: {resolved}");
        if (resolved.Uri is null) {
            throw new InvalidOperationException($"Invalid Module Specifier for module request: {resolved}");
        }
        
        var fileName = Uri.UnescapeDataString(resolved.Uri.AbsolutePath);
        foreach (var libraryModule in _libraryLoaders.Select(loader => TryLoadModuleUsing(loader, fileName)).OfType<string>()) {
            return libraryModule;
        }
        
        throw new InvalidOperationException($"Module {fileName} not found.");
    }
    
    private static string? TryLoadModuleUsing(IRedundantLoader loader, string fileName) {
        if (loader.Exists(fileName)) {
            return loader.Load<string>(fileName);
        }
        
        var fileNameWithExtension = fileName + ".js";
        return loader.Exists(fileNameWithExtension) 
            ? loader.Load<string>(fileNameWithExtension)
            : null;
    }

    private static bool IsRelative(string specifier) {
        return specifier.StartsWith('.') || specifier.StartsWith('/');
    }
    
    private static Uri BuildBaseUri(string? referencingModuleLocation) {
        if (referencingModuleLocation is null) {
            return new Uri("/");
        }
        
        return Uri.TryCreate(referencingModuleLocation, UriKind.Absolute, out var referencingLocation) 
            ? referencingLocation
            : new Uri("/");
    }
}
