using ow.Framework.Database.Accounts;

namespace ow.Service.Login.Game
{
    public sealed class Account
    {
        public int Id { get; }

        public Account(AccountModel model) => Id = model.Id;
    }
}