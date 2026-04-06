namespace OpenWorker.Commands.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class CommandTriggerAttribute(params string[] trigger) : Attribute
{
    internal string Trigger => string.Join('-', trigger);
}