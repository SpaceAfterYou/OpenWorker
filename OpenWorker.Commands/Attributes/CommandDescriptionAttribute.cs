namespace OpenWorker.Commands.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class CommandDescriptionAttribute(string description) : Attribute
{
    internal string Description => description;
}