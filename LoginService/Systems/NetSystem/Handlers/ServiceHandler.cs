using Core.DatabaseSystem.Accounts;
using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests.Auth;
using LoginService.Systems.GameSystem;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LoginService.Systems.NetSystem.Handlers
{
    internal static class ServiceHandler
    {
        [Handler(HandlerOpcode.AuthEnter, HandlerPermission.UnAuthorized)]
        public static void Enter(Session session, EnterRequest request)
        {
            AccountModel model = GetAccount(request.Nickname, request.Password, request.Mac);
            if (model is null)
            {
                // TODO: Not Work
                session.SendLogin(TableMessageId.LoginFailed);
                return;
            }

            session.Account = new(model);
            session.SendLogin(model);
        }

        private static AccountModel GetAccount(string nickname, string password, string mac)
        {
            byte[] hash = GetPasswordHash(password);
            using AccountContext context = new();

            AccountModel model = context.Accounts.FirstOrDefault(a => a.Nickname == nickname && a.Password == hash);
            if (model is null) { return null; }

            model.Mac = mac;
            model.SessionKey = GetSessionKey();

            context.SaveChanges();
            return model;
        }

        private static byte[] GetPasswordHash(string password)
        {
            using SHA512Managed sham = new();
            return sham.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private static ulong GetSessionKey()
        {
            byte[] buffer = new byte[8];

            Random random = new();
            random.NextBytes(buffer);

            return BitConverter.ToUInt64(buffer);
        }
    }
}