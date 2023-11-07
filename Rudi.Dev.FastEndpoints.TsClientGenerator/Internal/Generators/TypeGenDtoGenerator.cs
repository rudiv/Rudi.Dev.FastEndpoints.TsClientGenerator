using System.Reflection;
using System.Text;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using Rudi.Dev.FastEndpoints.TsClientGenerator.Abstractions;
using Rudi.Dev.FastEndpoints.TsClientGenerator.Internal.TypeGen;
using TypeGen.Core;
using TypeGen.Core.Generator;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Internal;

public class TypeGenDtoGenerator : IDtoGenerator
{
    protected List<GenerationType> TypesToGenerate = new();
    protected List<Assembly> AssembliesToGenerate = new();

    protected ILogger logger;
    protected TsClientGeneratorOptions options;

    public void SetLogger(ILogger logger)
    {
        this.logger = logger;
    }

    public void SetOptions(TsClientGeneratorOptions options)
    {
        this.options = options;
    }

    public string AddDtoType(Type type)
    {
        if (type != typeof(object) && type != typeof(EmptyRequest) && type != typeof(EmptyResponse) && TypesToGenerate.All(m => m.Type != type))
        {
            GenerationType? existingType;
            if ((existingType = TypesToGenerate.FirstOrDefault(m => m.Type.Name == type.Name)) != null)
            {
                if (options.ThrowOnDuplicateTypeName)
                {
                    throw new InvalidOperationException("Duplicate Type Name found: " + existingType.Type.Name + " (between " + type.FullName + " and " + existingType.Type.FullName + ")" +
                                                        ". Set TsClientGeneratorOptions.ThrowOnDuplicateTypeName to false to ignore this error with varying results, or name your DTOs appropriately.");
                }
                
                logger.LogWarning("Duplicate Type Name found: " + existingType.Type.Name + " (between " + type.FullName + " and " + existingType.Type.FullName + "). Try not to do this.");
                
                // Convert the FullName to a usable type name
                var typeName = type.FullName!.Replace(".", "_");
                if ((existingType = TypesToGenerate.FirstOrDefault(m => m.Name == type.Name)) != null)
                {
                    throw new InvalidOperationException($"Seriously, what are you doing? There's 2 types at {type.FullName}. Please name your DTOs correctly, even in different projects.");
                }
                
                TypesToGenerate.Add(new GenerationType(type, typeName));

                return typeName;
            }
            else
            {
                TypesToGenerate.Add(new GenerationType(type));
            }
        }
        return type.Name;
    }

    public void AddAssembly(Assembly assembly)
    {
        AssembliesToGenerate.Add(assembly);
    }

    public async Task<string> GenerateDtoContentAsync()
    {
        Generator generator;

        if (TypesToGenerate.Any(m => m.Name != null))
        {
            var generatorOptions = new GeneratorOptions
            {
                FileNameConverters = options.TypeGenOptions.FileNameConverters,
                TypeNameConverters = options.TypeGenOptions.TypeNameConverters,
                PropertyNameConverters = options.TypeGenOptions.PropertyNameConverters,
                EnumValueNameConverters = options.TypeGenOptions.EnumValueNameConverters,
                EnumStringInitializersConverters = options.TypeGenOptions.EnumStringInitializersConverters,
                ExplicitPublicAccessor = options.TypeGenOptions.ExplicitPublicAccessor,
                SingleQuotes = options.TypeGenOptions.SingleQuotes,
                TypeScriptFileExtension = options.TypeGenOptions.TypeScriptFileExtension,
                TabLength = options.TypeGenOptions.TabLength,
                UseTabCharacter = options.TypeGenOptions.UseTabCharacter,
                BaseOutputDirectory = options.TypeGenOptions.BaseOutputDirectory,
                //CreateIndexFile = options.TypeGenOptions.CreateIndexFile, // Useless here
                CsNullableTranslation = options.TypeGenOptions.CsNullableTranslation,
                CsAllowNullsForAllTypes = options.TypeGenOptions.CsAllowNullsForAllTypes,
                CsDefaultValuesForConstantsOnly = options.TypeGenOptions.CsDefaultValuesForConstantsOnly,
                DefaultValuesForTypes = options.TypeGenOptions.DefaultValuesForTypes,
                TypeUnionsForTypes = options.TypeGenOptions.TypeUnionsForTypes,
                CustomTypeMappings = options.TypeGenOptions.CustomTypeMappings,
                EnumStringInitializers = options.TypeGenOptions.EnumStringInitializers,
                FileHeading = options.TypeGenOptions.FileHeading,
                //UseDefaultExport = false, // Useless here
                //IndexFileExtension = null, // Useless here
                ExportTypesAsInterfacesByDefault = options.TypeGenOptions.ExportTypesAsInterfacesByDefault,
                //UseImportType = false, // Useless here
                TypeBlacklist = options.TypeGenOptions.TypeBlacklist,
            };
            var dynamicTypeNameConverter = new DynamicTypeNameConverter();
            foreach(var type in TypesToGenerate.Where(m => m.Name != null))
            {
                dynamicTypeNameConverter.AddMapping(type);
            }
            generatorOptions.TypeNameConverters.Add(dynamicTypeNameConverter);
            generator = new Generator(generatorOptions);
        }
        else
        {
            generator = new Generator(options.TypeGenOptions);
        }
        
        var generationSpec = new DynamicGenerationSpec(TypesToGenerate);
        generator.UnsubscribeDefaultFileContentGeneratedHandler();

        var typesGenerated = new Dictionary<Type, string>();
        generator.FileContentGenerated += (_, args) =>
        {
            if (typesGenerated.ContainsKey(args.Type))
            {
                logger.LogWarning("Type {TypeName} was generated multiple times, and has been overridden by the latest generation", args.Type.Name);
            }
            
            // Remove any lines from args.FileContent that start with "import"
            // This is because we're generating the DTOs in a single file, so we don't need to import them
            var lines = string.Join(Environment.NewLine, args.FileContent.Split(Environment.NewLine).Where(l => !l.TrimStart().StartsWith("import")));
            typesGenerated[args.Type] = lines;
        };
        
        // Generate the DTOs we found first
        await generator.GenerateAsync(new [] { generationSpec });

        // Then optionally, if assemblies were passed, use TypeGen default generation (ie. attributes)
        // This call will override the Type if it had attributes
        if (AssembliesToGenerate.Count > 0)
        {
            await generator.GenerateAsync(AssembliesToGenerate);
        }
        
        return typesGenerated.Values.Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine(s)).ToString();
    }
}