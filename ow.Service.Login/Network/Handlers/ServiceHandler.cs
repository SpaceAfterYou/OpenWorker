using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.IO.Lan;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Auth;
using ow.Service.Login.Game;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ow.Service.Login.Network.Handlers
{
    internal static class ServiceHandler
    {
        [Handler(ServerOpcode.AuthEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(Session session, EnterRequest request, LanContext lan)
        {
            AccountModel model = GetAccount(request.Nickname, request.Password, request.Mac);
            if (model is null)
            {
                // TODO: Not Work
                session.SendLogin(TableMessageId.LoginFailed);
                return;
            }

            ulong sessionKey = lan.SetAccountIdBySessionKey(model.Id);

            session.Account = new(model);
            session.SendLogin(model.Id, request.Mac, sessionKey);
        }

        private static AccountModel GetAccount(string nickname, string password, string mac)
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