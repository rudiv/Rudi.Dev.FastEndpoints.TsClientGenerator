using TypeGen.Core.Converters;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Internal.TypeGen;

public class DynamicTypeNameConverter : ITypeNameConverter
{
    private readonly Dictionary<Type, string> mappings = new();
    
    public string Convert(string name, Type type)
    {
        if (mappings.TryGetValue(type, out var convert))
        {
            return convert;
        }
        return name;
    }
    
    public void AddMapping(GenerationType type)
    {
        mappings.Add(type.Type, type.Name!);
    }
}