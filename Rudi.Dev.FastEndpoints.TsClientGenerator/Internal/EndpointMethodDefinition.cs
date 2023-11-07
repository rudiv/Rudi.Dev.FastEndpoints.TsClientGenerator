using System.Reflection;
using FastEndpoints;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Internal;

public class EndpointMethodDefinition
{
    public string Name { get; init; }
    public Type RequestType { get; init; }
    public string RequestTypeName { get; private set; }
    public IDictionary<string, Type> RequestProperties { get; init; }
    public Type ResponseType { get; init; }
    public string ResponseTypeName { get; private set; }
    public IDictionary<string, Type> ResponseProperties { get; init; }
    public IReadOnlyCollection<EndpointRoutePart> RouteParts { get; init; }
    public IReadOnlyCollection<string> Tags { get; set; }
    public Http Verb { get; init; }
    
    public bool EndpointHasRequest => RequestType != typeof(EmptyRequest);
    public bool EndpointHasResponse => ResponseType != typeof(EmptyResponse);
    public bool EndpointHasStronglyTypedResponse => EndpointHasResponse && ResponseType != typeof(object);

    /// <summary>
    /// Set if the route has no fixed request type, but accepts parameters in the route
    /// </summary>
    public bool MethodShouldHaveIndividualParameters => !EndpointHasRequest && RouteParts.Any(m => m.IsDtoProperty);

    internal EndpointMethodDefinition(EndpointMethodDefinition ep)
    {
        Name = ep.Name;
        RequestProperties = ep.RequestProperties;
        RequestType = ep.RequestType;
        RequestTypeName = ep.RequestTypeName;
        ResponseType = ep.ResponseType;
        ResponseTypeName = ep.ResponseTypeName;
        RouteParts = ep.RouteParts;
        Tags = ep.Tags;
        Verb = ep.Verb;
    }
    
    internal EndpointMethodDefinition(EndpointDefinition ep, int verbIdx, int routeIdx, int methodIdx = 0)
    {
        Name = ep.EndpointType.Name;
        if (methodIdx > 0)
        {
            Name += methodIdx;
        }

        RequestProperties = ep.ReqDtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(m => m.Name, m => m.PropertyType);
        RequestType = ep.ReqDtoType;
        RequestTypeName = RequestType == typeof(object) ? "any" : RequestType.Name;
        ResponseProperties = ep.ResDtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary(m => m.Name, m => m.PropertyType);
        ResponseType = ep.ResDtoType;
        ResponseTypeName = ResponseType == typeof(object) ? "any" : ResponseType.Name;
        RouteParts = GetRouteParts(ep.Routes![routeIdx]);
        Tags = ep.EndpointTags ?? emptyTags;
        Verb = Enum.Parse<Http>(ep.Verbs![verbIdx]);
    }

    internal void OverrideRequestTypeName(string name)
    {
        RequestTypeName = name;
    }

    internal void OverrideResponseTypeName(string name)
    {
        ResponseTypeName = name;
    }

    internal IReadOnlyCollection<EndpointRoutePart> GetRouteParts(string route)
    {
        // FastEndpoints "fixes" the route to match the properties of an inbound DTO, so we can build an appropriate route
        var routePattern = RoutePatternFactory.Parse(route);
        var endpointRouteParts = new List<EndpointRoutePart> { slashPart };
        foreach(var pathSegment in routePattern.PathSegments)
        {
            foreach (var part in pathSegment.Parts)
            {
                switch (part.PartKind)
                {
                    case RoutePatternPartKind.Literal:
                        var literalPart = (RoutePatternLiteralPart)part;
                        endpointRouteParts.Add(new EndpointRoutePart(literalPart.Content));
                        break;
                    case RoutePatternPartKind.Parameter:
                        var parameterPart = (RoutePatternParameterPart)part;
                        endpointRouteParts.Add(new EndpointRoutePart(parameterPart.Name, true));
                        break;
                    case RoutePatternPartKind.Separator:
                        var separatorPart = (RoutePatternSeparatorPart)part;
                        endpointRouteParts.Add(new EndpointRoutePart(separatorPart.Content));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(part.PartKind), part.PartKind, "Unknown RoutePatternPartKind");
                }
            }
            endpointRouteParts.Add(slashPart);
        }

        if (endpointRouteParts.Count > 1)
        {
            endpointRouteParts.RemoveAt(endpointRouteParts.Count - 1);
        }
        
        return endpointRouteParts;
    }

    public static IReadOnlyCollection<EndpointMethodDefinition> GenerateMethodDefinitions(EndpointDefinition ep)
    {
        // Note that Verbs & Routes are guaranteed by FastEndpoints
        var methodDefinitions = new EndpointMethodDefinition[ep.Verbs!.Length * ep.Routes!.Length];
        var idx = 0;
        for (var vIdx = 0; vIdx < ep.Verbs.Length; vIdx++)
        {
            for (var rIdx = 0; rIdx < ep.Routes.Length; rIdx++)
            {
                methodDefinitions[idx] = new EndpointMethodDefinition(ep, vIdx, rIdx, idx++);
            }
        }
        return methodDefinitions;
    }

    public record EndpointRoutePart(string Content, bool IsDtoProperty = false);
    private readonly EndpointRoutePart slashPart = new("/");
    private readonly IReadOnlyCollection<string> emptyTags = Array.Empty<string>();
}