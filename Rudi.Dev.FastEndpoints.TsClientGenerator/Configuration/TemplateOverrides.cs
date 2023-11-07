namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Configuration;

public class TemplateOverrides
{
    internal readonly Dictionary<TemplateType, string> Overrides = new();

    public void AddOverride(TemplateType templateType, string content)
    {
        Overrides[templateType] = content;
    }
}