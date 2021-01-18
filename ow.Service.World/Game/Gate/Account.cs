using SoulCore.Database.Accounts;

namespace ow.Service.World.Game.Gate
{
    internal sealed class Account
    {
        internal int Id { get; init; }

        internal Account(AccountModel model) => Id = model.Id;
    }
}
