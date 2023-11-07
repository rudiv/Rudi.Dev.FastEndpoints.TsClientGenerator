using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Rudi.Dev.FastEndpoints.TsClientGenerator.Abstractions;
using Rudi.Dev.FastEndpoints.TsClientGenerator.Internal;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator;

public class TsClientGenerator
{
    private readonly EndpointDefinition[] endpoints;
    private readonly ILogger logger;
    private readonly TsClientGeneratorOptions options;

    private readonly IReadOnlyCollection<EndpointMethodDefinition> endpointMethodDefinitions;

    public TsClientGenerator(EndpointDefinition[] endpoints, ILogger logger, TsClientGeneratorOptions options)
    {
        this.endpoints = endpoints;
        this.logger = logger;
        this.options = options;

        var endpointMethodDefinitions = new List<EndpointMethodDefinition>();
        foreach (var ep in endpoints)
        {
            endpointMethodDefinitions.AddRange(EndpointMethodDefinition.GenerateMethodDefinitions(ep));
        }
        
        // Detect duplicates
        var duplicateEndpoints = endpointMethodDefinitions.GroupBy(m => m.Name).Any(m => m.Count() > 1);
        if (duplicateEndpoints)
        {
            throw new InvalidOperationException("Duplicate Endpoint Names found, please name your endpoints appropriately.");
        }

        this.endpointMethodDefinitions = endpointMethodDefinitions;
    }

    public async Task<string> GenerateAndReturn(IReadOnlyCollection<Assembly>? additionalTypeGenAssemblies = null, IReadOnlyCollection<string>? tags = null)
    {
        var endpoints = GetFilteredEndpointMethodDefinitions(tags);
        return await GenerateInternal<FluidApiClientGenerator, TypeGenDtoGenerator>(endpoints, additionalTypeGenAssemblies);
    }
    
    public async Task GenerateAndWriteToFile(string path, IReadOnlyCollection<Assembly>? additionalTypeGenAssemblies = null, IReadOnlyCollection<string>? tags = null)
    {
        var endpoints = GetFilteredEndpointMethodDefinitions(tags);
        var apiClient = await GenerateInternal<FluidApiClientGenerator, TypeGenDtoGenerator>(endpoints, additionalTypeGenAssemblies);
        
        await File.WriteAllTextAsync(path, apiClient);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected IReadOnlyCollection<EndpointMethodDefinition> GetFilteredEndpointMethodDefinitions(IReadOnlyCollection<string>? tags = null)
    {
        if (tags != null)
        {
            return endpointMethodDefinitions.Where(e => e.Tags.Any(tags.Contains)).ToList();
        }

        return endpointMethodDefinitions;
    }

    protected async Task<string> GenerateInternal<TApiClientGenerator, TDtoGenerator>(IReadOnlyCollection<EndpointMethodDefinition> endpoints, IReadOnlyCollection<Assembly>? additionalTypeGenAssemblies = null)
        where TApiClientGenerator: IApiClientGenerator, new()
        where TDtoGenerator: IDtoGenerator, new()
    {
        var dtoGenerator = new TDtoGenerator();
        dtoGenerator.SetLogger(logger);
        dtoGenerator.SetOptions(options);
        var apiClientGenerator = new TApiClientGenerator();
        apiClientGenerator.SetLogger(logger);
        apiClientGenerator.SetOptions(options);

        foreach (var endpointMethodDefinition in endpointMethodDefinitions)
        {
            var reqTypeName = dtoGenerator.AddDtoType(endpointMethodDefinition.RequestType);
            if (reqTypeName != endpointMethodDefinition.RequestType.Name)
            {
                endpointMethodDefinition.OverrideRequestTypeName(reqTypeName);
            }

            var resTypeName = dtoGenerator.AddDtoType(endpointMethodDefinition.ResponseType);
            if (resTypeName != endpointMethodDefinition.ResponseType.Name)
            {
                endpointMethodDefinition.OverrideResponseTypeName(resTypeName);
            }
        }
        if (additionalTypeGenAssemblies != null && additionalTypeGenAssemblies.Count > 0)
        {
            foreach (var asm in additionalTypeGenAssemblies)
            {
                dtoGenerator.AddAssembly(asm);
            }
        }

        var genStr = new StringBuilder();
        var dtoContent = await dtoGenerator.GenerateDtoContentAsync();
        var apiClient = await apiClientGenerator.GenerateApiClientAsync(endpoints);
        if (options.ApiClientOptions.FileHeader != null)
        {
            genStr.AppendLine(options.ApiClientOptions.FileHeader);
        }

        genStr.AppendLine(apiClient);
        genStr.AppendLine(dtoContent);
        return genStr.ToString();
    }
}