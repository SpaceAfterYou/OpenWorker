using Core.DatabaseSystem.Accounts;

namespace LoginService.Systems.GameSystem
{
    public sealed class Account
    {
        public uint Id { get; init; }

        public Account(AccountModel model) => Id = model.Id;
    }
}