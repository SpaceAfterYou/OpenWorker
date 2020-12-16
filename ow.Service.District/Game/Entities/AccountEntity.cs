using ow.Framework.Database.Accounts;

namespace ow.Service.District.Game
{
    internal sealed class AccountEntity
    {
        internal int Id { get; }

        internal AccountEntity(AccountModel model) => Id = model.Id;
    }
}