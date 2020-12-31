using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Network.CS;
using ow.Framework.IO.Network.Sync.Attributes;
using ow.Framework.IO.Network.Sync.Opcodes;
using ow.Framework.IO.Network.Sync.Permissions;
using ow.Framework.IO.Network.Sync.Requests;
using ow.Framework.IO.Network.Sync.Responses;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ow.Service.Auth.Network.Sync.Handlers
{
    public sealed class ServiceHandler
    {
        [Handler(ServerOpcode.AuthEnter, HandlerPermission.UnAuthorized)]
        public void Enter(Session session, AuthEnterRequest request)
        {
            if (GetAccount(request.Nickname, request.Password) is AccountModel model)
            {
                session.Account = new(model);

                session.SendAsync(new AuthLoginResponse
                {
                    Mac = request.Mac,
                    AccountId = model.Id,
                    Response = AuthLoginStatus.Success,
                    SessionKey = _lan.RegisterSessionKey(model.Id),
                });
            }
            else
            {
                session.SendAsync(new AuthLoginResponse
                {
                    Response = AuthLoginStatus.Failure,
                    ErrorMessageCode = AuthLoginErrorMessageCode.WrongUsernameOrPassword
                });
            }
        }

        private AccountModel? GetAccount(string nickname, string password)
        {
            byte[] hash = GetPasswordHash(password);
            using AccountContext context = _accountFactory.CreateDbContext();

            return context.Accounts.AsNoTracking().FirstOrDefault(a => a.Nickname == nickname && a.Password == hash);
        }

        private static byte[] GetPasswordHash(string password)
        {
            using SHA512Managed sham = new();
            return sham.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        public ServiceHandler(CSClient lan, IDbContextFactory<AccountContext> accountFactory) =>
            (_lan, _accountFactory) = (lan, accountFactory);

        private readonly IDbContextFactory<AccountContext> _accountFactory;
        private readonly CSClient _lan;
    }
}