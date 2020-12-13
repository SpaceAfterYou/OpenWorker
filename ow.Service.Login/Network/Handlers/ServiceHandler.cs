using Microsoft.EntityFrameworkCore;
using ow.Framework.Database.Accounts;
using ow.Framework.IO.Lan;
using ow.Framework.IO.Network;
using ow.Framework.IO.Network.Attributes;
using ow.Framework.IO.Network.Opcodes;
using ow.Framework.IO.Network.Permissions;
using ow.Framework.IO.Network.Requests.Auth;
using ow.Service.Login.Game;
using ow.Service.Login.Network.Extensions;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ow.Service.Login.Network.Handlers
{
    internal static class ServiceHandler
    {
        [Handler(ServerOpcode.AuthEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(GameSession session, EnterRequest request, LanContext lan)
        {
            AccountModel model = GetAccount(request.Nickname, request.Password);
            if (model is null)
            {
                // TODO: Not work. Message not showing.
                session.SendLogin(TableMessage.LoginFailed);
                return;
            }

            session.Entity.Set(new Account(model));
            session.SendLogin(model.Id, request.Mac, lan.SetAccountIdBySessionKey(model.Id));
        }

        private static AccountModel GetAccount(string nickname, string password)
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