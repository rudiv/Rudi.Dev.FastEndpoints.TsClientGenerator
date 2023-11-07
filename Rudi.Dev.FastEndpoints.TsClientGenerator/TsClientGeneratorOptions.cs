using System.Reflection;
using Rudi.Dev.FastEndpoints.TsClientGenerator.Configuration;
using Rudi.Dev.FastEndpoints.TsClientGenerator.Internal.TypeGen;
using TypeGen.Core.Converters;
using TypeGen.Core.Generator;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator;

public class TsClientGeneratorOptions
{
    public ApiClientOptions ApiClientOptions { get; set; } = new();
    
    public TemplateOverrides TemplateOverrides { get; set; } = new();

    /// <summary>
    /// Set this to "true" to throw if there are 2 types with the same name.
    ///
    /// Setting this to false will attempt to prefix a type name with the namespace. This may not work.
    /// </summary>
    public bool ThrowOnDuplicateTypeName { get; set; } = true;
    
    public GeneratorOptions TypeGenOptions { get; set; } = new()
    {
        CustomTypeMappings = new Dictionary<string, string>
        {
            { "System.DateOnly", "Date" },
            { "System.TimeOnly", "string" },
            { "System.Uri", "string" }
        },
        //TypeNameConverters = new TypeNameConverterCollection(new DateOnlyTimeOnlyConverter()),
        PropertyNameConverters = new MemberNameConverterCollection(new PascalCaseToCamelCaseConverter(), new JsonMemberNameConverter()),
        SingleQuotes = false,
        FileHeading = string.Empty,
        ExportTypesAsInterfacesByDefault = true,
        //UseImportType = true,
    };
}