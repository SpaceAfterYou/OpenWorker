using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenWorker.Wire.SourceGenerators;

/// <summary>
/// For every <see langword="partial"/> struct marked <c>[WireMessage]</c>: <c>Read(ref PacketReader)</c>,
/// <c>Write(ref PacketWriter)</c> and their bridges to <c>BinaryReader</c> and <c>BinaryWriter</c>, from the fields of the
/// struct in the order they are declared. The rules are in <see cref="WireModel"/> and on the attributes of
/// <c>OpenWorker.Wire</c>.
/// </summary>
[Generator]
public sealed class WireMessageGenerator : IIncrementalGenerator
{
    internal const string AttributeName = "OpenWorker.Wire.WireMessageAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var messages = context.SyntaxProvider.ForAttributeWithMetadataName(
            AttributeName,
            static (node, _) => node is StructDeclarationSyntax or RecordDeclarationSyntax,
            static (attributeContext, cancellationToken) => Build(attributeContext, cancellationToken));

        context.RegisterSourceOutput(messages, static (production, message) => message.Emit(production));
    }

    private static GeneratedMessage Build(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntax = (TypeDeclarationSyntax)context.TargetNode;
        var type = (INamedTypeSymbol)context.TargetSymbol;

        return new WireModel(type, syntax, context.Attributes[0], cancellationToken).Generate();
    }
}
