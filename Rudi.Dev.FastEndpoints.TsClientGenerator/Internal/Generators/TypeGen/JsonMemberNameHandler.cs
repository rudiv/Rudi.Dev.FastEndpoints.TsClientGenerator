using System.Reflection;
using System.Text.Json.Serialization;
using TypeGen.Core.Converters;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Internal.TypeGen;

public class JsonMemberNameConverter : IMemberNameConverter
{
    public string Convert(string name, MemberInfo memberInfo)
    {
        var attribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
        return attribute != null ? attribute.Name : name;
    }
}
