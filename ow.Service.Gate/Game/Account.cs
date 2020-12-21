using ow.Framework.Database.Accounts;

namespace ow.Service.Gate.Game
{
    internal sealed class Account
    {
        internal int Id { get; init; }

        internal Account(AccountModel model) =>
            Id = model.Id;
    }
}