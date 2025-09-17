namespace OpenWorker.Hotspot.Commands.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class StuffCommandAttribute(params string[] trigger) : Attribute
{
    public string Trigger => string.Join('-', trigger);
}