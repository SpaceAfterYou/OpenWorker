namespace OpenWorker.Domain.Enums;

/// <summary>
/// The set of components the entity of a connection is made with (<c>[EntityComponent]</c>), chosen by the server. A maze
/// takes the set of a district: the player in it is the same person, with the same components.
/// </summary>
public enum EntityComponentService
{
    All,
    Auth,
    Gate,
    District
}