using System.Xml.Linq;
using OpenWorker.Batch.Entities.Basic;
using OpenWorker.Batch.Extensions;
using OpenWorker.Domain.Batch.Enums;

namespace OpenWorker.Batch.Entities;

public sealed record LuaFunctionBox : BasicEntity
{
    public LuaFunctionBox(XElement x) : base(x)
    {
        Type = x.GetEnum<LuaFunctionType>("m_eType");
        Function = x.GetString("m_szFunction");
        Check = x.GetInt32("m_iCheckID");
    }

    /// <summary>
    ///     Type of Target
    /// </summary>
    public LuaFunctionType Type { get; }

    /// <summary>
    ///     LuaFunction
    /// </summary>
    public string Function { get; }

    /// <summary>
    ///     ID of Monster or NPC for check.
    /// </summary>
    public int Check { get; }
}