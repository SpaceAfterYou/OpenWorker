using ow.Framework.Database.Accounts;

namespace ow.Service.Login.Game
{
    public sealed class Account
    {
        public uint Id { get; init; }

        public Account(AccountModel model) => Id = model.Id;
    }
}
