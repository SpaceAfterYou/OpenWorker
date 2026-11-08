using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace OpenWorker.Wire.SourceGenerators;

/// <summary>
/// What one struct gives: its source, or the reasons it cannot have one. Kept as text so that the incremental pipeline
/// can compare two runs and skip the output when nothing changed.
/// </summary>
internal sealed class GeneratedMessage : IEquatable<GeneratedMessage>
{
    public GeneratedMessage(string hintName, string? source, IReadOnlyList<DiagnosticInfo> diagnostics)
    {
        HintName = hintName;
        Source = source;
        Diagnostics = diagnostics;
    }

    public string HintName { get; }

    public string? Source { get; }

    public IReadOnlyList<DiagnosticInfo> Diagnostics { get; }

    public void Emit(SourceProductionContext context)
    {
        foreach (var diagnostic in Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }

        if (Source is not null)
        {
            context.AddSource(HintName, SourceText.From(Source, System.Text.Encoding.UTF8));
        }
    }

    public bool Equals(GeneratedMessage? other)
    {
        return other is not null
            && HintName == other.HintName
            && Source == other.Source
            && Diagnostics.SequenceEqual(other.Diagnostics);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as GeneratedMessage);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(HintName) ^ (Source is null ? 0 : StringComparer.Ordinal.GetHashCode(Source));
    }
}

/// <summary>
/// A diagnostic without the syntax tree it points into, so that it can be compared.
/// </summary>
internal sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    private DiagnosticInfo(DiagnosticDescriptor descriptor, string path, TextSpan span, LinePositionSpan lines, string[] arguments)
    {
        Descriptor = descriptor;
        Path = path;
        Span = span;
        Lines = lines;
        Arguments = arguments;
    }

    public DiagnosticDescriptor Descriptor { get; }

    public string Path { get; }

    public TextSpan Span { get; }

    public LinePositionSpan Lines { get; }

    public string[] Arguments { get; }

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] arguments)
    {
        var lines = location?.GetLineSpan();

        return new DiagnosticInfo(descriptor, lines?.Path ?? string.Empty, location?.SourceSpan ?? default,
            lines?.Span ?? default, arguments);
    }

    public Diagnostic ToDiagnostic()
    {
        var location = Path.Length == 0 ? Location.None : Location.Create(Path, Span, Lines);

        return Diagnostic.Create(Descriptor, location, Arguments.Cast<object>().ToArray());
    }

    public bool Equals(DiagnosticInfo? other)
    {
        return other is not null
            && Descriptor.Id == other.Descriptor.Id
            && Path == other.Path
            && Span == other.Span
            && Arguments.SequenceEqual(other.Arguments);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DiagnosticInfo);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Descriptor.Id) ^ Span.GetHashCode();
    }
}

internal static class WireDiagnostics
{
    private const string Category = "OpenWorker.Wire";

    public static readonly DiagnosticDescriptor NotPartial = new(
        "OWW001", "A wire message must be partial",
        "'{0}' is marked [WireMessage] and must be declared partial: the generator adds its Read and Write",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Unsupported = new(
        "OWW002", "A field has no wire format",
        "The field '{0}' of '{1}' has no wire format: {2}",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor HandWritten = new(
        "OWW003", "A wire message writes its own bridge",
        "'{0}' is marked [WireMessage] and declares {1} itself; the generator writes it from the fields, so remove the hand-written one or the attribute",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Shape = new(
        "OWW004", "A wire message has a shape the generator does not take",
        "'{0}' cannot be generated: {1}",
        Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
}
