using ow.Framework.Database.Accounts;

namespace ow.Service.Login.Game.Gate
{
    internal sealed class Account
    {
        internal static Account Empty { get; } = new();

        internal int Id { get; }

        internal Account(AccountModel model) => Id = model.Id;

        private Account()
        { }
    }
}