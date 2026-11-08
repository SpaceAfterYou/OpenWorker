using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace OpenWorker.Extensions;

/// <summary>
/// Who this server process is and where clients reach it, in the <c>Instance</c> section.
/// </summary>
public sealed class InstanceOptions
{
    public const string SectionName = "Instance";

    /// <summary>
    /// Assigned by the server when it starts; the settings files do not carry it.
    /// </summary>
    [NotEmptyGuid(ErrorMessage = "Instance:Identifier must be set: the server assigns it before the options are read.")]
    public Guid Identifier { get; set; }

    [NumberRange(0, short.MaxValue, ErrorMessage = "Instance:Group must be a number from 0 to 32767.")]
    public short Group { get; set; }

    /// <summary>
    /// The location a district serves. A maze server has none, which is zero.
    /// </summary>
    [NumberRange(0, short.MaxValue, ErrorMessage = "Instance:Location must be a number from 0 to 32767.")]
    public short Location { get; set; }

    /// <summary>
    /// The address clients are sent to.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Instance:PublicHost must be set to the address clients reach this server at.")]
    public string PublicHost { get; set; } = string.Empty;

    [NumberRange(1, PortRange.AddressMax, ErrorMessage = "Instance:PublicPort must be a TCP port from 1 to 32767.")]
    public int? PublicPort { get; set; }
}

[OptionsValidator]
internal sealed partial class InstanceOptionsValidator : IValidateOptions<InstanceOptions>;

/// <summary>
/// What a district needs on top of <see cref="InstanceOptions"/>: the location it serves and the port clients reach it at.
/// A port cannot have a default, because every district listens behind its own.
/// </summary>
internal sealed class DistrictInstanceValidator : IValidateOptions<InstanceOptions>
{
    public ValidateOptionsResult Validate(string? name, InstanceOptions options)
    {
        List<string>? failures = null;

        if (options.Location <= 0)
        {
            (failures ??= []).Add("Instance:Location must be set to the location the district serves, for example 10003.");
        }

        if (options.PublicPort is null)
        {
            (failures ??= []).Add("Instance:PublicPort must be set to the port clients reach this district at.");
        }

        return failures is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
