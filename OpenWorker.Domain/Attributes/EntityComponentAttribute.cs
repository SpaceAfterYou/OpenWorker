using OpenWorker.Domain.Enums;

namespace OpenWorker.Domain.Attributes;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class EntityComponentAttribute(EntityComponentService service) : Attribute
{
    public EntityComponentService Service { get; } = service;
}