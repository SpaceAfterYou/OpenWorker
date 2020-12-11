using ow.Framework.Database.Accounts;

namespace ow.Service.Gate.Game
{
    public sealed class Account
    {
        public int Id { get; init; }

        public Account(AccountModel model) =>
            Id = model.Id;
    }
}