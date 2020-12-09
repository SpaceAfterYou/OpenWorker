using Core.Systems.DatabaseSystem.Accounts;
using Core.Systems.LanSystem;
using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests.Auth;
using LoginService.Systems.GameSystem;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LoginService.Systems.NetSystem.Handlers
{
    internal static class ServiceHandler
    {
        [Handler(HandlerOpcode.AuthEnter, HandlerPermission.UnAuthorized)]
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