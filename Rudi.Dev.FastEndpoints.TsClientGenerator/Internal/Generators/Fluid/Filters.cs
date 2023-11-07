using System.Text.Json;
using Fluid;
using Fluid.Values;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Internal.Fluid;

public static class Filters
{
    public static ValueTask<FluidValue> Camel(FluidValue input, FilterArguments arguments, TemplateContext context) =>
        new StringValue(JsonNamingPolicy.CamelCase.ConvertName(input.ToStringValue()));
}