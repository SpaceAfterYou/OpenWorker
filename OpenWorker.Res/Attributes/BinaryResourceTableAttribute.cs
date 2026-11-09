namespace OpenWorker.Res.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class BinaryResourceTableAttribute(string table) : Attribute
{
    public string Table { get; } = table;

    /// <summary>
    /// The table is not part of the client files and may be missing: the server then goes without what it gives.
    /// </summary>
    public bool Optional { get; init; }

    /// <summary>
    /// The table is the version only the server has of a table the client has under the same name: it is read from
    /// <see cref="ServerFolder"/>, never from the folder of the client tables, and it is optional.
    /// </summary>
    public bool Server { get; init; }

    /// <summary>
    /// The folder of the server-only tables, next to <c>res</c>, which the operator fills (<c>INSTALL.md</c>).
    /// </summary>
    public const string ServerFolder = "res-server";
}
