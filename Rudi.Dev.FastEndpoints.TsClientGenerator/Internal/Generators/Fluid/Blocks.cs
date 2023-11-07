using System.Text;
using System.Text.Encodings.Web;
using Fluid;
using Fluid.Ast;

namespace Rudi.Dev.FastEndpoints.TsClientGenerator.Internal.Fluid;

public static class Blocks
{
    public static async ValueTask<Completion> TrimBlock(Expression value, IReadOnlyList<Statement> statements, TextWriter writer, TextEncoder encoder, TemplateContext context)
    {
        using var str = new MemoryStream();
        await using var memWriter = new StreamWriter(str);
        await statements.RenderStatementsAsync(memWriter, encoder, context);
        await memWriter.FlushAsync();
        var content = Encoding.UTF8.GetString(str.ToArray());
        await writer.WriteLineAsync(content.TrimEnd(' ', '\r', '\n', ','));

        return Completion.Normal;
    }
}