using FastEndpoints;
using Fluid;
using Microsoft.Extensions.Logging;
using Rudi.Dev.FastEndpoints.TsClientGenerator.Abstractions;
using Rudi.Dev.FastEndpoints.TsClientGenerator.Internal.Fluid;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Internal;

public class FluidApiClientGenerator : IApiClientGenerator
{
    protected ILogger logger = default!;
    protected TsClientGeneratorOptions options = default!;
    protected readonly FluidParser parser = new();
    protected List<EndpointMethodDefinition> endpoints = new();
    protected Dictionary<TemplateType, IFluidTemplate> templates = new();
    protected readonly TemplateOptions defaultOptions = new TemplateOptions();

    public FluidApiClientGenerator()
    {
        defaultOptions.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
        defaultOptions.ValueConverters.Add((val) => (val as Enum)?.ToString() ?? val);
        defaultOptions.Filters.AddFilter("camel", Filters.Camel);
        parser.RegisterExpressionBlock("trim", Blocks.TrimBlock);
    }
    
    public void AddEndpoint(EndpointMethodDefinition endpointDefinition)
    {
        endpoints.Add(endpointDefinition);
    }

    public void AddEndpoints(IReadOnlyCollection<EndpointMethodDefinition> endpointMethodDefinitions)
    {
        endpoints.AddRange(endpointMethodDefinitions);
    }

    protected IFluidTemplate GetTemplateFile(TemplateType templateType)
    {
        if (templates.TryGetValue(templateType, out var templateFile))
        {
            return templateFile;
        }

        if (options.TemplateOverrides.Overrides.TryGetValue(templateType, out var file))
        {
            templates.Add(templateType, parser.Parse(file));
            return templates[templateType];
        }

        var templateName = templateType + "Template.liquid";
        var assembly = typeof(FluidApiClientGenerator).Assembly;
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(m => m.EndsWith(templateName));
        if (resourceName == null)
            throw new Exception($"Template file {templateName} not found in assembly {assembly.FullName}.");
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new Exception($"Template file {templateName} not found in assembly {assembly.FullName}.");
        
        using var reader = new StreamReader(stream);
        var template = reader.ReadToEnd();
        templates.Add(templateType, parser.Parse(template));
        return templates[templateType];
    }

    public async Task<string> GenerateApiClientAsync(IReadOnlyCollection<EndpointMethodDefinition> endpoints)
    {
        List<string> endpointMethods = new();
        foreach (var endpoint in endpoints)
        {
            endpointMethods.Add(await RenderEndpointMethodAsync(endpoint));
        }

        var model = new
        {
            ApiClient = options.ApiClientOptions,
            Methods = endpointMethods
        };
        var classBaseTemplate = GetTemplateFile(TemplateType.ClassBase);
        var output = await classBaseTemplate.RenderAsync(new TemplateContext(model, defaultOptions));
        return output;
    }

    protected async Task<string> RenderEndpointMethodAsync(EndpointMethodDefinition endpointMethod)
    {
        var tmpl = endpointMethod.Verb switch
        {
            Http.GET => TemplateType.GetMethod,
            Http.POST => TemplateType.PostMethod,
            _ => TemplateType.GetMethod
        };
        var fluidEndpointMethodDefinition = new FluidEndpointMethodDefinition(endpointMethod);
        var template = GetTemplateFile(tmpl);
        var context = new TemplateContext(new
        {
            Options = options,
            Method = fluidEndpointMethodDefinition
        }, defaultOptions);
        return await template.RenderAsync(context);
    }

    public void SetLogger(ILogger logger)
    {
        this.logger = logger;
    }

    public void SetOptions(TsClientGeneratorOptions options)
    {
        this.options = options;
    }
}