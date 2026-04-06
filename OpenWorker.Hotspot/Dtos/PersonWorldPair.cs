using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Values;

namespace OpenWorker.Hotspot.Dtos;

public readonly record struct PersonWorldPair(PersonValue Person, WorldValue World);
