using OpenWorker.Hotspot.Messages.Response.Person;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Wire;

namespace OpenWorker.Hotspot.Dtos;

[WireMessage(WireMethods.Write)]
public readonly partial record struct PersonWorldPair(PersonValue Person, WorldValue World);
