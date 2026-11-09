namespace OpenWorker.Res.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class BinaryResourceTableAttribute(string table) : Attribute
{
    /// <summary>The folder of the client tables.</summary>
    public const string ClientFolder = "res";

    /// <summary>
    /// The folder of the server-only tables, next to <see cref="ClientFolder"/>, which the operator fills (<c>INSTALL.md</c>).
    /// </summary>
    public const string ServerFolder = "res-server";

    public string Table { get; } = table;

    /// <summary>
    /// The table is one only the server has, and may be missing: the server then goes without what it gives. It is read from
    /// <see cref="ServerFolder"/>, and from <see cref="ClientFolder"/> when it is not there.
    /// </summary>
    public bool Optional { get; init; }

    /// <summary>
    /// A server-only table under the name of a client table (the server's own <c>tb_Reinforce</c>): optional as well, and
    /// read from <see cref="ServerFolder"/> only, never from the folder of the client tables.
    /// </summary>
    public bool Server { get; init; }

    /// <summary>
    /// Where the table is read from, or <c>null</c> for a server-only table that is in neither folder.
    /// </summary>
    public string? PathIn(string root = "")
    {
        var client = Path.Join(root, ClientFolder, Table + ".res");

        if (!Optional && !Server)
        {
            return client;
        }

        var server = Path.Join(root, ServerFolder, Table + ".res");

        if (File.Exists(server))
        {
            return server;
        }

        return !Server && File.Exists(client) ? client : null;
    }
}
