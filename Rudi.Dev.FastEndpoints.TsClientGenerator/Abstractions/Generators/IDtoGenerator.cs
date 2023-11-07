using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Abstractions;

/// <summary>
/// Generates the TypeScript code for DTOs.
/// </summary>
/// <remarks>Not currently overridable, but future extension point?</remarks>
public interface IDtoGenerator
{
    void SetLogger(ILogger logger);
    void SetOptions(TsClientGeneratorOptions options);
    
    string AddDtoType(Type type);
    //void AddDtoTypes(IReadOnlyCollection<Type> types);

    void AddAssembly(Assembly assembly);
    
    Task<string> GenerateDtoContentAsync();
}