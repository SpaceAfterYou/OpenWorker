using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using Microsoft.CSharp;

namespace OpenWorker.SourceGenerator.ResourceStructures;

internal static class TypeHelper
{
    internal static async ValueTask<string> GetTypeName(Type? type)
    {
        var builder = new StringBuilder();
        await using var writer = new StringWriter(builder);

        var expr = new CodeTypeReferenceExpression(type);

        using var provider = new CSharpCodeProvider();
        provider.GenerateCodeFromExpression(expr, writer, new CodeGeneratorOptions());

        return builder.ToString();
    }
}