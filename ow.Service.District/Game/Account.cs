using SoulCore.Database.Accounts;

namespace ow.Service.District.Game
{
    public sealed record Account
    {
        public int Id { get; }

        public Account(AccountModel model) => Id = model.Id;
    }
}
