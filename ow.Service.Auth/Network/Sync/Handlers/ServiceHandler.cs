﻿using Microsoft.EntityFrameworkCore;
using SoulCore.Database.Accounts;
using SoulCore.Game.Enums;
using SoulCore.IO.Network.Relay.Global;
using SoulCore.IO.Network.Sync.Attributes;
using SoulCore.IO.Network.Sync.Commands.Old;
using SoulCore.IO.Network.Sync.Permissions;
using SoulCore.IO.Network.Sync.Requests;
using SoulCore.IO.Network.Sync.Responses;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ow.Service.Auth.Network.Sync.Handlers
{
    public sealed class ServiceHandler
    {
        [Handler(ServerOpcode.AuthEnter, HandlerPermission.Anonymous)]
        public void Enter(SyncSession session, SAuthEnterRequest request)
        {
            if (GetAccount(request.Nickname, request.Password) is AccountModel model)
            {
                session.Account = new(model);
                session.Permission = HandlerPermission.Authorized;

                session.SendDeferred(new SAuthLoginResponse
                {
                    Mac = request.Mac,
                    AccountId = model.Id,
                    SessionKey = _relay.Session.Register(new() { Account = model.Id }).Key,
                });
            }
            else
            {
                session.SendDeferred(new SAuthLoginResponse
                {
                    ErrorCode = AuthLoginErrorMessageCode.WrongUsernameOrPassword
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

        public ServiceHandler(RGClient relayClient, IDbContextFactory<AccountContext> accountFactory) =>
            (_relay, _accountFactory) = (relayClient, accountFactory);

        private readonly IDbContextFactory<AccountContext> _accountFactory;
        private readonly RGClient _relay;
    }
}
