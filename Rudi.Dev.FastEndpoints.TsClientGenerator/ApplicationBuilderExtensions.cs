using System.Reflection;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator;

public static class ApplicationBuilderExtensions
{
    public static TsClientGenerator UseTsClientGenerator(this IApplicationBuilder app, Action<TsClientGeneratorOptions>? config = null)
    {
        var options = new TsClientGeneratorOptions();
        config?.Invoke(options);
        
        var logger = app.ApplicationServices.GetRequiredService(typeof(ILogger<TsClientGenerator>)) as ILogger;
        
        // Don't look at this, naughty
        var feAssembly = typeof(EndpointWithoutRequest).Assembly;
        var epDataType = feAssembly.GetType("FastEndpoints.EndpointData")
            ?? throw new Exception("EndpointData type not found in FastEndpoints assembly - this may be due to a change in the FastEndpoints library.");
        var epData = app.ApplicationServices.GetService(epDataType)
                     ?? throw new Exception("EndpointData not found in DI container - this may be due to a change in the FastEndpoints library.");
        var fields = epDataType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).FirstOrDefault(m => m.Name.Contains("Found"))
                     ?? throw new Exception("EndpointData.Found field not found - this may be due to a change in the FastEndpoints library.");
        var epDefinitions = fields.GetValue(epData) as EndpointDefinition[]
            ?? throw new Exception("EndpointData.Found field is not an array of EndpointDefinition - this may be due to a change in the FastEndpoints library.");

        return new TsClientGenerator(epDefinitions, logger, options);
    }
}