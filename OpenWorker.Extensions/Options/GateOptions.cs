using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// The gate a server belongs to, in the <c>Gate</c> section. A gate is the group of servers a player
/// reaches after the server list: its district servers, its maze servers and the gate server itself.
/// </summary>
public sealed class GateOptions
{
    public const string SectionName = "Gate";

    [NumberRange(1, short.MaxValue, ErrorMessage = "Gate:Id must be a gate number from 1 to 32767.")]
    public short Id { get; set; }
}

[OptionsValidator]
internal sealed partial class GateOptionsValidator : IValidateOptions<GateOptions>;
