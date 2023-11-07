using TypeGen.Core.SpecGeneration;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Internal.TypeGen;

internal class DynamicGenerationSpec : GenerationSpec
{
    public DynamicGenerationSpec(IReadOnlyList<GenerationType> types)
    {
        foreach (var type in types)
        {
            var specBuilder = AddInterface(type.Type);
        }
    }
}

public record GenerationType(Type Type, string? Name = null);