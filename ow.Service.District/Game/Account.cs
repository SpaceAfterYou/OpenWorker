using ow.Framework.Database.Accounts;

namespace ow.Service.District.Game
{
    internal sealed record Account
    {
        internal int Id { get; }

        internal Account(AccountModel model) => Id = model.Id;
    }
}