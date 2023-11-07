using Microsoft.Extensions.Logging;
using Rudi.Dev.FastEndpoints.TsClientGenerator.Internal;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Abstractions;

/// <summary>
/// Generates the TypeScript code for the API Client.
/// </summary>
/// <remarks>Not currently overridable, but future extension point?</remarks>
public interface IApiClientGenerator
{
    Task<string> GenerateApiClientAsync(IReadOnlyCollection<EndpointMethodDefinition> endpoints);
    
    void SetLogger(ILogger logger);
    void SetOptions(TsClientGeneratorOptions options);
}