using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenWorker.Wire.SourceGenerators;

/// <summary>
/// The fields of one struct and the code that reads and writes them.<br/>
/// A field is a public instance auto-property (one with a field the compiler made) without <c>[WireIgnore]</c>, in the
/// order of declaration; for a positional record, a parameter of its primary constructor, in their order. A computed
/// property is not a field. A request is read, a response is written, any other value both, unless the attribute says.
/// </summary>
internal sealed class WireModel
{
    private const string Wire = "global::OpenWorker.Wire";
    private const string Reader = "reader";
    private const string Writer = "writer";

    // The values of the enums of OpenWorker.Wire, as the attributes carry them.
    private const int MethodsRead = 1;
    private const int MethodsWrite = 2;

    private static readonly string[] LengthNames = ["Byte", "Signed16", "Unsigned16", "Signed32"];

    private static readonly string[] StringFormats =
    [
        "Ascii", "AsciiWithoutTerminator", "Utf8Ascii", "Utf16Ascii", "Utf8AsciiWithoutTerminator", "Utf8Unicode",
        "Utf16Unicode", "Utf8UnicodeWithoutTerminator", "Utf16UnicodeWithoutTerminator"
    ];

    private readonly INamedTypeSymbol _type;
    private readonly TypeDeclarationSyntax _syntax;
    private readonly AttributeData _attribute;
    private readonly CancellationToken _cancellationToken;
    private readonly List<DiagnosticInfo> _diagnostics = [];

    private int _locals;

    public WireModel(INamedTypeSymbol type, TypeDeclarationSyntax syntax, AttributeData attribute,
        CancellationToken cancellationToken)
    {
        _type = type;
        _syntax = syntax;
        _attribute = attribute;
        _cancellationToken = cancellationToken;
    }

    private string TypeName => _type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public GeneratedMessage Generate()
    {
        var hint = _type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty) + ".Wire.g.cs";

        var source = TryGenerate();

        return new GeneratedMessage(hint, _diagnostics.Count == 0 ? source : null, _diagnostics);
    }

    private string? TryGenerate()
    {
        if (!_syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            Report(WireDiagnostics.NotPartial, _syntax.Identifier.GetLocation(), _type.Name);

            return null;
        }

        if (_type.ContainingType is not null || _type.IsGenericType)
        {
            Report(WireDiagnostics.Shape, _syntax.Identifier.GetLocation(), _type.Name,
                "a nested or generic struct is not supported");

            return null;
        }

        var positional = _syntax is RecordDeclarationSyntax { ParameterList: not null } record ? record.ParameterList : null;

        if (positional is null && _syntax.ParameterList is not null)
        {
            Report(WireDiagnostics.Shape, _syntax.Identifier.GetLocation(), _type.Name,
                "a struct with a primary constructor; give its properties no initializers and drop the constructor");

            return null;
        }

        var methods = Methods(_type, _attribute);
        var read = (methods & MethodsRead) != 0;
        var write = (methods & MethodsWrite) != 0;

        CheckHandWritten(read, write, positional is not null);

        var fields = Fields(positional);

        if (_diagnostics.Count > 0)
        {
            return null;
        }

        var codecs = new List<(IPropertySymbol Property, Codec Codec)>();

        foreach (var field in fields)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var wireAs = Attribute(field, "WireAsAttribute");
            var wireString = Attribute(field, "WireStringAttribute");

            var codec = CodecOf(field, field.Type,
                wireAs is { ConstructorArguments.Length: 1 } ? new Options(wireAs.ConstructorArguments[0]) : null,
                wireString is { ConstructorArguments.Length: 1 }
                    ? new Options(wireString.ConstructorArguments[0], Named(wireString, "Length").Value as int? ?? 0)
                    : null,
                Attribute(field, "WireArrayAttribute"), read, write, element: false);

            if (codec is not null)
            {
                codecs.Add((field, codec));
            }
        }

        if (_diagnostics.Count > 0)
        {
            return null;
        }

        return Emit(codecs, read, write, positional is not null);
    }

#region The struct

    private static int Methods(INamedTypeSymbol type, AttributeData? attribute)
    {
        var declared = attribute is { ConstructorArguments.Length: 1 } ? (int)attribute.ConstructorArguments[0].Value! : 0;

        if (declared != 0)
        {
            return declared;
        }

        if (type.AllInterfaces.Any(i => i.Name == "IRequestHotspotMessage"))
        {
            return MethodsRead;
        }

        if (type.AllInterfaces.Any(i => i.Name is "IResponseHotspotMessage" or "IWritableData"))
        {
            return MethodsWrite;
        }

        return MethodsRead | MethodsWrite;
    }

    private void CheckHandWritten(bool read, bool write, bool positional)
    {
        foreach (var member in _type.GetMembers())
        {
            if (member is not IMethodSymbol { IsImplicitlyDeclared: false } method || method.Parameters.Length != 1)
            {
                continue;
            }

            var parameter = method.Parameters[0].Type.ToDisplayString();

            if (write && method.Name == "Write" && parameter is "System.IO.BinaryWriter" or "OpenWorker.Wire.PacketWriter")
            {
                Report(WireDiagnostics.HandWritten, method.Locations.FirstOrDefault(), _type.Name, $"Write({parameter})");
            }

            if (read && !positional && method.MethodKind == MethodKind.Constructor && parameter is "System.IO.BinaryReader")
            {
                Report(WireDiagnostics.HandWritten, method.Locations.FirstOrDefault(), _type.Name, "a constructor that takes a BinaryReader");
            }

            if (read && method.Name == "Read" && method.IsStatic && parameter is "OpenWorker.Wire.PacketReader")
            {
                Report(WireDiagnostics.HandWritten, method.Locations.FirstOrDefault(), _type.Name, "Read(ref PacketReader)");
            }
        }
    }

    private List<IPropertySymbol> Fields(ParameterListSyntax? positional)
    {
        var members = _type.GetMembers();
        var backed = new HashSet<ISymbol>(
            members.OfType<IFieldSymbol>().Where(f => f.AssociatedSymbol is not null).Select(f => f.AssociatedSymbol!),
            SymbolEqualityComparer.Default);

        var properties = members
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && !p.IsIndexer && p.DeclaredAccessibility == Accessibility.Public
                && p.ExplicitInterfaceImplementations.Length == 0 && backed.Contains(p) && Attribute(p, "WireIgnoreAttribute") is null)
            .ToList();

        if (positional is null)
        {
            return properties;
        }

        // A positional record: the parameters of its constructor, which are its properties, in their order.
        var ordered = new List<IPropertySymbol>();

        foreach (var parameter in positional.Parameters)
        {
            var property = properties.FirstOrDefault(p => p.Name == parameter.Identifier.ValueText);

            if (property is null)
            {
                Report(WireDiagnostics.Shape, parameter.GetLocation(), _type.Name,
                    $"the parameter '{parameter.Identifier.ValueText}' is not a public property");

                continue;
            }

            ordered.Add(property);
        }

        foreach (var extra in properties.Where(p => !ordered.Contains(p, SymbolEqualityComparer.Default)))
        {
            Report(WireDiagnostics.Shape, extra.Locations.FirstOrDefault(), _type.Name,
                $"the positional record has the property '{extra.Name}' besides its parameters");
        }

        return ordered;
    }

#endregion The struct

#region Codecs

    private Codec? CodecOf(IPropertySymbol field, ITypeSymbol type, Options? wireAs, Options? wireString,
        AttributeData? wireArray, bool read, bool write, bool element)
    {
        if (wireArray is not null && !element)
        {
            return ListOf(field, type, wireArray, read, write);
        }

        if (wireAs is not null)
        {
            var target = wireAs.Value.Value as ITypeSymbol;
            var primitive = target is null ? null : Primitive.Of(target.SpecialType);

            if (primitive is null)
            {
                return Unsupported(field, "[WireAs] takes a primitive type: byte, sbyte, short, ushort, int, uint, long, ulong, float, double or bool");
            }

            return new ConvertCodec(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), type.SpecialType == SpecialType.System_Boolean, primitive);
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            if (wireString is null)
            {
                return Unsupported(field, "a string needs [WireString] to name its format");
            }

            var format = wireString.Value.Value as int? ?? -1;
            var length = wireString.Length;

            if (format < 0 || format >= StringFormats.Length)
            {
                return Unsupported(field, "the format of the string is not one of WireStringFormat");
            }

            if (read && StringFormats[format] == "AsciiWithoutTerminator" && length <= 0)
            {
                return Unsupported(field, "a string without a length or a terminator is read only with [WireString(..., Length = n)]");
            }

            return new StringCodec(StringFormats[format], length);
        }

        if (Primitive.Of(type.SpecialType) is { } simple)
        {
            return new PrimitiveCodec(simple.Method, simple.Size);
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: { } underlying })
        {
            var primitive = Primitive.Of(underlying.SpecialType)!;

            return new ConvertCodec(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), false, primitive);
        }

        var display = type.ToDisplayString();

        if (display == "System.Numerics.Vector3")
        {
            return new PrimitiveCodec("Vector3", 12);
        }

        if (display == "System.DateTime")
        {
            return new PrimitiveCodec("DateTime", 8);
        }

        if (display == "System.DateTimeOffset")
        {
            return new PrimitiveCodec("DateTimeOffset", 8);
        }

        if (display == "System.TimeSpan")
        {
            return new PrimitiveCodec("TimeInSeconds32", 4);
        }

        if (type is INamedTypeSymbol { IsValueType: true } named)
        {
            var readable = IsReadable(named);
            var writable = IsWritable(named);

            if ((read && !readable) || (write && !writable))
            {
                return Unsupported(field, $"'{named.Name}' needs [WireMessage] or to implement "
                    + (read && !readable ? "IWireReadable<T>" : "IWireWritable") + " to be a field");
            }

            return new NestedCodec(named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        if (!element && type is IArrayTypeSymbol or INamedTypeSymbol { IsGenericType: true })
        {
            return Unsupported(field, "a list needs [WireArray] to name its prefix or its count");
        }

        return Unsupported(field, $"the type '{type.ToDisplayString()}' is not one the generator knows");
    }

    private Codec? ListOf(IPropertySymbol field, ITypeSymbol type, AttributeData wireArray, bool read, bool write)
    {
        ITypeSymbol? elementType = null;
        var kind = ListKind.Array;

        if (type is IArrayTypeSymbol { Rank: 1 } array)
        {
            elementType = array.ElementType;
        }

        else if (type is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } generic)
        {
            elementType = generic.TypeArguments[0];

            switch (generic.ConstructedFrom.ToDisplayString())
            {
                case "System.Collections.Generic.List<T>":
                    kind = ListKind.List;
                    break;

                case "System.Collections.Generic.IReadOnlyList<T>":
                case "System.Collections.Generic.IList<T>":
                    kind = ListKind.Indexed;
                    break;

                case "System.Collections.Generic.IReadOnlyCollection<T>":
                case "System.Collections.Generic.ICollection<T>":
                    kind = ListKind.Counted;
                    break;

                default:
                    elementType = null;
                    break;
            }
        }

        if (elementType is null)
        {
            return Unsupported(field, "[WireArray] takes an array, List<T>, IList<T>, IReadOnlyList<T>, ICollection<T> or IReadOnlyCollection<T>");
        }

        var hasPrefix = wireArray.ConstructorArguments.Length == 1;
        var prefix = hasPrefix ? (int)wireArray.ConstructorArguments[0].Value! : -1;
        var count = Named(wireArray, "Count").Value as int? ?? 0;

        if (hasPrefix == count > 0)
        {
            return Unsupported(field, "[WireArray] takes either a prefix or a Count");
        }

        if (hasPrefix && (prefix < 0 || prefix >= LengthNames.Length))
        {
            return Unsupported(field, "the prefix is not one of WireLength");
        }

        var elementAs = Named(wireArray, "ElementAs");
        var elementString = Named(wireArray, "ElementString");

        var element = CodecOf(field, elementType,
            elementAs.Value is ITypeSymbol ? new Options(elementAs) : null,
            elementString.Value is int ? new Options(elementString) : null,
            null, read, write, element: true);

        if (element is null)
        {
            return null;
        }

        return new ListCodec(element, elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), kind,
            hasPrefix ? LengthNames[prefix] : null, count, elementType.SpecialType == SpecialType.System_Byte && elementAs.IsNull);
    }

    private static bool IsReadable(INamedTypeSymbol type)
    {
        if (type.AllInterfaces.Any(i => i.Name == "IWireReadable" && i.ContainingNamespace.ToDisplayString() == "OpenWorker.Wire"))
        {
            return true;
        }

        var attribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == WireMessageGenerator.AttributeName);

        return attribute is not null && (Methods(type, attribute) & MethodsRead) != 0;
    }

    private static bool IsWritable(INamedTypeSymbol type)
    {
        if (type.AllInterfaces.Any(i => i.Name == "IWireWritable" && i.ContainingNamespace.ToDisplayString() == "OpenWorker.Wire"))
        {
            return true;
        }

        var attribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == WireMessageGenerator.AttributeName);

        return attribute is not null && (Methods(type, attribute) & MethodsWrite) != 0;
    }

    private Codec? Unsupported(IPropertySymbol field, string why)
    {
        Report(WireDiagnostics.Unsupported, field.Locations.FirstOrDefault(), field.Name, _type.Name, why);

        return null;
    }

    private static TypedConstant Named(AttributeData attribute, string name)
    {
        return attribute.NamedArguments.FirstOrDefault(a => a.Key == name).Value;
    }

    private static AttributeData? Attribute(ISymbol symbol, string name)
    {
        return symbol.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == name && a.AttributeClass.ContainingNamespace.ToDisplayString() == "OpenWorker.Wire");
    }

#endregion Codecs

#region Emit

    private string Emit(List<(IPropertySymbol Property, Codec Codec)> fields, bool read, bool write, bool positional)
    {
        var source = new StringBuilder();
        var name = _type.Name;
        var modifiers = (_type.IsReadOnly ? "readonly " : string.Empty) + "partial " + (_type.IsRecord ? "record struct" : "struct");
        var interfaces = new List<string>();

        if (read)
        {
            interfaces.Add($"{Wire}.IWireReadable<{TypeName}>");
        }

        if (write)
        {
            interfaces.Add($"{Wire}.IWireWritable");
        }

        source.AppendLine("// <auto-generated/>");
        source.AppendLine("// Written by OpenWorker.Wire.SourceGenerators from the fields of the struct, in the order they are declared.");
        source.AppendLine("#nullable enable");
        source.AppendLine();

        if (!_type.ContainingNamespace.IsGlobalNamespace)
        {
            source.Append("namespace ").Append(_type.ContainingNamespace.ToDisplayString()).AppendLine(";");
            source.AppendLine();
        }

        source.Append(modifiers).Append(' ').Append(name).Append(" : ").AppendLine(string.Join(", ", interfaces));
        source.AppendLine("{");

        if (read)
        {
            EmitRead(source, name, fields, positional);
        }

        if (write)
        {
            EmitWrite(source, fields);
        }

        source.AppendLine("}");

        return source.ToString();
    }

    private void EmitRead(StringBuilder source, string name, List<(IPropertySymbol Property, Codec Codec)> fields, bool positional)
    {
        const string sets = "[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]";

        if (positional)
        {
            source.AppendLine($"    public static {TypeName} Read(ref {Wire}.PacketReader {Reader})");
            source.AppendLine("    {");

            var locals = new List<string>();

            foreach (var (property, codec) in fields)
            {
                var local = NextLocal();

                codec.Read(source, "        ", property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " " + local, this);
                locals.Add(local);
            }

            source.AppendLine($"        return new {TypeName}({string.Join(", ", locals)});");
            source.AppendLine("    }");
            source.AppendLine();

            return;
        }

        source.AppendLine($"    {sets}");
        source.AppendLine($"    private {name}(ref {Wire}.PacketReader {Reader})");
        source.AppendLine("    {");

        foreach (var (property, codec) in fields)
        {
            codec.Read(source, "        ", "this." + Identifier(property.Name), this);
        }

        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine($"    public static {TypeName} Read(ref {Wire}.PacketReader {Reader}) => new {TypeName}(ref {Reader});");
        source.AppendLine();
        source.AppendLine("    /// <summary>Reads through <see cref=\"global::OpenWorker.Wire.PacketReader\"/>, for code that still holds a <see cref=\"global::System.IO.BinaryReader\"/>.</summary>");
        source.AppendLine($"    {sets}");
        source.AppendLine($"    public {name}(global::System.IO.BinaryReader {Reader})");
        source.AppendLine("    {");
        source.AppendLine($"        this = {Wire}.WireBridge.Read<{TypeName}>({Reader});");
        source.AppendLine("    }");
        source.AppendLine();
    }

    private void EmitWrite(StringBuilder source, List<(IPropertySymbol Property, Codec Codec)> fields)
    {
        var member = _type.IsReadOnly ? string.Empty : "readonly ";

        source.AppendLine($"    public {member}void Write(ref {Wire}.PacketWriter {Writer})");
        source.AppendLine("    {");

        foreach (var (property, codec) in fields)
        {
            codec.Write(source, "        ", "this." + Identifier(property.Name), this);
        }

        source.AppendLine("    }");
        source.AppendLine();
        source.AppendLine("    /// <summary>Writes through <see cref=\"global::OpenWorker.Wire.PacketWriter\"/>, for code that still holds a <see cref=\"global::System.IO.BinaryWriter\"/>.</summary>");
        source.AppendLine($"    public {member}void Write(global::System.IO.BinaryWriter {Writer}) => {Wire}.WireBridge.Write({Writer}, this);");
    }

    internal string NextLocal()
    {
        return "__wire" + (_locals++).ToString(CultureInfo.InvariantCulture);
    }

    private static string Identifier(string name)
    {
        return SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;
    }

    private void Report(DiagnosticDescriptor descriptor, Location? location, params string[] arguments)
    {
        _diagnostics.Add(DiagnosticInfo.Create(descriptor, location, arguments));
    }

#endregion Emit

#region Kinds of field

    /// <summary>
    /// The primitives the reader and the writer have a method for, by the suffix of the method.
    /// </summary>
    internal sealed class Primitive
    {
        private Primitive(string method, string type, int size)
        {
            Method = method;
            Type = type;
            Size = size;
        }

        public string Method { get; }

        public string Type { get; }

        public int Size { get; }

        public static Primitive? Of(SpecialType type)
        {
            return type switch
            {
                SpecialType.System_Byte => new Primitive("Byte", "byte", 1),
                SpecialType.System_SByte => new Primitive("SByte", "sbyte", 1),
                SpecialType.System_Boolean => new Primitive("Boolean", "bool", 1),
                SpecialType.System_Int16 => new Primitive("Int16", "short", 2),
                SpecialType.System_UInt16 => new Primitive("UInt16", "ushort", 2),
                SpecialType.System_Int32 => new Primitive("Int32", "int", 4),
                SpecialType.System_UInt32 => new Primitive("UInt32", "uint", 4),
                SpecialType.System_Int64 => new Primitive("Int64", "long", 8),
                SpecialType.System_UInt64 => new Primitive("UInt64", "ulong", 8),
                SpecialType.System_Single => new Primitive("Single", "float", 4),
                SpecialType.System_Double => new Primitive("Double", "double", 8),
                _ => null
            };
        }
    }

    internal enum ListKind
    {
        Array,
        List,
        Indexed,
        Counted
    }

    /// <summary>
    /// How one value is read into a target and written from an expression.
    /// </summary>
    internal abstract class Codec
    {
        /// <summary>
        /// The fewest bytes a value takes, to check the count of a list against what the packet holds.
        /// </summary>
        public abstract int Size { get; }

        public abstract string ReadExpression(WireModel model);

        public virtual void Read(StringBuilder source, string indent, string target, WireModel model)
        {
            source.Append(indent).Append(target).Append(" = ").Append(ReadExpression(model)).AppendLine(";");
        }

        public abstract void Write(StringBuilder source, string indent, string value, WireModel model);
    }

    private sealed class PrimitiveCodec(string method, int size) : Codec
    {
        public override int Size => size;

        public override string ReadExpression(WireModel model) => $"{Reader}.Read{method}()";

        public override void Write(StringBuilder source, string indent, string value, WireModel model)
        {
            source.Append(indent).AppendLine($"{Writer}.Write{method}({value});");
        }
    }

    /// <summary>
    /// A field that goes as another primitive: an enum by its underlying type, or anything with <c>[WireAs]</c>.
    /// </summary>
    private sealed class ConvertCodec(string type, bool isBoolean, Primitive wire) : Codec
    {
        public override int Size => wire.Size;

        public override string ReadExpression(WireModel model)
        {
            var raw = $"{Reader}.Read{wire.Method}()";

            if (isBoolean)
            {
                return wire.Method == "Boolean" ? raw : $"{raw} != 0";
            }

            return wire.Method == "Boolean" ? $"({type})({raw} ? 1 : 0)" : $"({type}){raw}";
        }

        public override void Write(StringBuilder source, string indent, string value, WireModel model)
        {
            string converted;

            if (isBoolean)
            {
                converted = wire.Method == "Boolean" ? value : $"({wire.Type})({value} ? 1 : 0)";
            }

            else
            {
                converted = wire.Method == "Boolean" ? $"({value} != 0)" : $"({wire.Type}){value}";
            }

            source.Append(indent).AppendLine($"{Writer}.Write{wire.Method}({converted});");
        }
    }

    private sealed class StringCodec(string format, int length) : Codec
    {
        public override int Size => format switch
        {
            "Ascii" => 1,
            "AsciiWithoutTerminator" => length,
            _ => 2
        };

        // The helpers are named Utf8AsciiString, Utf8AsciiStringWithoutTerminator, ...: the word String before the
        // suffix.
        private string Method => format.EndsWith(Suffix, System.StringComparison.Ordinal)
            ? format.Substring(0, format.Length - Suffix.Length) + "String" + Suffix
            : format + "String";

        private const string Suffix = "WithoutTerminator";

        public override string ReadExpression(WireModel model)
        {
            return format == "AsciiWithoutTerminator"
                ? $"{Reader}.ReadAsciiStringWithoutTerminator({length.ToString(CultureInfo.InvariantCulture)})"
                : $"{Reader}.Read{Method}()";
        }

        public override void Write(StringBuilder source, string indent, string value, WireModel model)
        {
            source.Append(indent).AppendLine($"{Writer}.Write{Method}({value});");
        }
    }

    private sealed class NestedCodec(string type) : Codec
    {
        public override int Size => 1;

        public override string ReadExpression(WireModel model) => $"{type}.Read(ref {Reader})";

        public override void Write(StringBuilder source, string indent, string value, WireModel model)
        {
            source.Append(indent).AppendLine($"{value}.Write(ref {Writer});");
        }
    }

    private sealed class ListCodec(Codec element, string elementType, ListKind kind, string? prefix, int count, bool bytes) : Codec
    {
        public override int Size => prefix is null ? count * element.Size : 1;

        public override string ReadExpression(WireModel model) => throw new System.NotSupportedException();

        public override void Read(StringBuilder source, string indent, string target, WireModel model)
        {
            var items = model.NextLocal();
            var index = model.NextLocal();
            var length = prefix is null
                ? count.ToString(CultureInfo.InvariantCulture)
                : $"{Reader}.ReadLength({Wire}.WireLength.{prefix}, {element.Size.ToString(CultureInfo.InvariantCulture)})";

            // A local of a positional record is declared first, then filled.
            var space = target.LastIndexOf(' ');

            if (space >= 0)
            {
                source.Append(indent).Append(target).AppendLine(";");
                target = target.Substring(space + 1);
            }

            source.Append(indent).AppendLine("{");

            if (bytes)
            {
                source.Append(indent).AppendLine($"    var {items} = {Reader}.ReadByteArray({length});");
            }

            else
            {
                source.Append(indent).AppendLine($"    var {items} = new {elementType}[{length}];");
                source.Append(indent).AppendLine($"    for (var {index} = 0; {index} < {items}.Length; {index}++)");
                source.Append(indent).AppendLine("    {");
                element.Read(source, indent + "        ", $"{items}[{index}]", model);
                source.Append(indent).AppendLine("    }");
            }

            source.Append(indent).AppendLine(kind == ListKind.List
                ? $"    {target} = new global::System.Collections.Generic.List<{elementType}>({items});"
                : $"    {target} = {items};");
            source.Append(indent).AppendLine("}");
        }

        public override void Write(StringBuilder source, string indent, string value, WireModel model)
        {
            var items = model.NextLocal();
            var item = model.NextLocal();
            var index = model.NextLocal();
            var counted = kind == ListKind.Array ? "Length" : "Count";

            source.Append(indent).AppendLine("{");
            source.Append(indent).AppendLine($"    var {items} = {value};");

            if (prefix is not null)
            {
                source.Append(indent).AppendLine(
                    $"    {Writer}.WriteLength({Wire}.WireLength.{prefix}, {items}?.{counted} ?? 0);");
                source.Append(indent).AppendLine($"    if ({items} is not null)");
                source.Append(indent).AppendLine("    {");

                if (bytes && kind == ListKind.Array)
                {
                    source.Append(indent).AppendLine($"        {Writer}.WriteBytes({items});");
                }

                else if (kind == ListKind.Counted)
                {
                    source.Append(indent).AppendLine($"        foreach (var {item} in {items})");
                    source.Append(indent).AppendLine("        {");
                    element.Write(source, indent + "            ", item, model);
                    source.Append(indent).AppendLine("        }");
                }

                else
                {
                    source.Append(indent).AppendLine($"        for (var {index} = 0; {index} < {items}.{counted}; {index}++)");
                    source.Append(indent).AppendLine("        {");
                    element.Write(source, indent + "            ", $"{items}[{index}]", model);
                    source.Append(indent).AppendLine("        }");
                }

                source.Append(indent).AppendLine("    }");
            }

            else
            {
                // Always the count of elements: those past it are not sent, the missing ones go as default values.
                var fixedCount = count.ToString(CultureInfo.InvariantCulture);
                var list = kind == ListKind.Counted ? $"global::System.Linq.Enumerable.ToArray({items})" : items;
                var indexed = model.NextLocal();

                source.Append(indent).AppendLine($"    var {indexed} = {items} is null ? null : {list};");
                source.Append(indent).AppendLine($"    for (var {index} = 0; {index} < {fixedCount}; {index}++)");
                source.Append(indent).AppendLine("    {");
                source.Append(indent).AppendLine(
                    $"        var {item} = {indexed} is not null && {index} < {indexed}.{(kind == ListKind.Counted ? "Length" : counted)} ? {indexed}[{index}] : default({elementType})!;");
                element.Write(source, indent + "        ", item, model);
                source.Append(indent).AppendLine("    }");
            }

            source.Append(indent).AppendLine("}");
        }
    }

    /// <summary>
    /// What an attribute says of a field, or the options of a list say of its elements.
    /// </summary>
    internal sealed class Options(TypedConstant value, int length = 0)
    {
        public TypedConstant Value { get; } = value;

        public int Length { get; } = length;
    }

#endregion Kinds of field
}
