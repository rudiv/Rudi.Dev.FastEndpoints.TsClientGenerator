using System.Text;
using System.Text.Json;
using FastEndpoints;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Internal.Fluid;

internal class FluidEndpointMethodDefinition : EndpointMethodDefinition
{
    internal FluidEndpointMethodDefinition(EndpointMethodDefinition ep) : base(ep)
    {
    }

    public bool RouteIsInterpolated => RouteParts.Any(m => m.IsDtoProperty);
    public IReadOnlyCollection<string> InterpolatedParts => RouteIsInterpolated ? RouteParts.Where(m => m.IsDtoProperty).Select(m => m.Content).ToList() : Array.Empty<string>();
    public IReadOnlyCollection<string> InterpolatedPartsCamel => InterpolatedParts.Select(m => JsonNamingPolicy.CamelCase.ConvertName(m)).ToList();

    public IReadOnlyCollection<string> InterpolatedDtoProperties => RouteIsInterpolated ? InterpolatedParts.Where(m => RequestProperties.ContainsKey(m)).ToList() : Array.Empty<string>();
    public IReadOnlyCollection<string> InterpolatedNonDtoProperties => RouteIsInterpolated ? InterpolatedParts.Where(m => !RequestProperties.ContainsKey(m)).ToList() : Array.Empty<string>();

    public IReadOnlyCollection<string> InterpolatedUnboundDtoProperties =>
        Verb == Http.GET && RouteIsInterpolated && EndpointHasRequest ? RequestProperties.Where(m => !InterpolatedDtoProperties.Contains(m.Key)).Select(m => m.Key).ToList() : Array.Empty<string>(); 

    // TODO - Use this in the future to map? ResponseDates needs to pull out properties of children too, which it doesn't right now
    public bool RequiresJsonReviver => ResponseDates.Any();
    public IReadOnlyCollection<string> ResponseDates => ResponseProperties.Where(m => m.Value == typeof(DateTime) || m.Value == typeof(DateTimeOffset) || m.Value == typeof(DateOnly)).Select(m => m.Key).ToList();

    public string ParameterList
    {
        get
        {
            var sb = new StringBuilder();
            if (EndpointHasRequest)
            {
                sb.Append("request: ");
                sb.Append(RequestTypeName);
            }
            
            if (InterpolatedParts.Any() && InterpolatedNonDtoProperties.Count > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                if (EndpointHasRequest)
                {
                    sb.Append(string.Join(", ", InterpolatedNonDtoProperties.Select(m => $"{JsonNamingPolicy.CamelCase.ConvertName(m)}: any")));
                }
                else
                {
                    sb.Append(string.Join(", ", InterpolatedParts.Select(m => $"{JsonNamingPolicy.CamelCase.ConvertName(m)}: any")));
                }
            }

            return sb.ToString();
        }
    }
    
    public new string Name => Verb.ToString().ToLowerInvariant() + base.Name;
    
    public string Route {
        get
        {
            var sb = new StringBuilder();
            foreach (var routePart in RouteParts)
            {
                if (routePart.IsDtoProperty)
                {
                    sb.Append($"${{{JsonNamingPolicy.CamelCase.ConvertName(routePart.Content)}}}");
                }
                else
                {
                    sb.Append(routePart.Content);
                }
            }

            if (InterpolatedUnboundDtoProperties.Any())
            {
                sb.Append("?");
                foreach (var unboundDtoProperty in InterpolatedUnboundDtoProperties)
                {
                    sb.Append($"{unboundDtoProperty}=${{{JsonNamingPolicy.CamelCase.ConvertName(unboundDtoProperty)}}}&");
                }
                // Remove trailing &
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }
    }
}