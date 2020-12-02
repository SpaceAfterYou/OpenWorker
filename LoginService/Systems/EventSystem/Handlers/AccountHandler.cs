using Core.DatabaseSystem.Accounts;
using Core.Systems.NetSystem.Attributes;
using Core.Systems.NetSystem.Opcodes;
using Core.Systems.NetSystem.Permissions;
using Core.Systems.NetSystem.Requests;
using LoginService.Systems.NetSystem;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LoginService.Systems.EventSystem.Handlers
{
    internal static class AccountHandler
    {
        [Handler(HandlerOpcode.Login, HandlerPermission.UnAuthorized)]
        public static void Login(Session session, LoginRequest request)
        {
            AccountModel model = GetAccount(request.Nickname, request.Password, request.Mac);
            if (model is null)
            {
            }
            else
            {
            }
        }

        private static AccountModel GetAccount(string nickname, string password, string mac)
        {
            byte[] hash = GetPasswordHash(password);
            using AccountContext context = new();

            AccountModel model = context.Account.FirstOrDefault(a => a.Nickname == nickname && a.Password == hash);
            if (model is null) { return null; }

            model.Mac = mac;
            model.SessionKey = GetSessionKey();

            context.SaveChanges();
            return model;
        }

        private static byte[] GetPasswordHash(string password)
        {
            using var sham = new SHA512Managed();
            return sham.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private static ulong GetSessionKey()
        {
            Random random = new();

            byte[] buffer = new byte[8];
            random.NextBytes(buffer);

            return BitConverter.ToUInt64(buffer);
        }
    }
}