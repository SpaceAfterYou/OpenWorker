using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.Game.Enums;
using ow.Framework.IO.Lan;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests;
using ow.Framework.IO.Network.Responses;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ow.Service.Login.Network.Handlers
{
    internal static class ServiceHandler
    {
        [Handler(ServerOpcode.AuthEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(Session session, AuthEnterRequest request, LanContext lan)
        {
            if (GetAccount(request.Nickname, request.Password) is AccountModel model)
            {
                session.Account = new(model);

                session.SendAsync(new AuthLoginResponse
                {
                    Mac = request.Mac,
                    AccountId = model.Id,
                    Response = AuthLoginStatus.Success,
                    SessionKey = lan.SetAccountIdBySessionKey(model.Id),
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

        private static AccountModel? GetAccount(string nickname, string password)
        {
            byte[] hash = GetPasswordHash(password);
            using AccountContext context = new();

            return context.Accounts.AsNoTracking().FirstOrDefault(a => a.Nickname == nickname && a.Password == hash);
        }

        private static byte[] GetPasswordHash(string password)
        {
            using SHA512Managed sham = new();
            return sham.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }
}
