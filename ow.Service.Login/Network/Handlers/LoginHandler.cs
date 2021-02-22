using ow.Framework.Database.Accounts;
using ow.Framework.IO.Network.Relay.Global.Protos.Requests;
using ow.Framework.Utils;
using ow.Service.Login.Game.Gate;
using SoulCore.IO.Network.Attributes;
using SoulCore.IO.Network.Commands;
using SoulCore.IO.Network.Permissions;
using SoulCore.IO.Network.Requests;
using SoulCore.IO.Network.Responses;
using SoulCore.IO.Network.Types;
using SoulCore.Types;
using System.Threading.Tasks;

namespace ow.Service.Login.Network.Handlers
{
    public static class LoginHandler
    {
        [Handler(CategoryCommand.Login, LoginCommand.EnterServerReq, SyncServerType.Login, HandlerPermission.Anonymous)]
        public static async ValueTask OnEnterAsync(SyncSession session, LoginEnterServerRequest request)
        {
            if (session.Server.Instance.Id != request.GateId)
            {
                NetworkUtils.Drop(session);
            }

            if (!(await session.Server.Relay.Session.ContainsAsync(new RGSSessionContainsRequest { Account = request.AccountId, Key = request.SessionKey })).Result)
            {
                NetworkUtils.Drop(session);
            }

            AccountModel model = await session.Server.GetAccountModelAsync(request.AccountId).ConfigureAwait(false);

            CharacterList? characters = await session.Server.CreateCharacterListAsync(model, request.GateId).ConfigureAwait(false);
            if (characters is null)
            {
                NetworkUtils.Drop(session);
            }

            session.Account = new(model);
            session.Characters = characters;
            session.Background = model.CharacterBackground;
            session.Permission = HandlerPermission.Authorized;

            await session.SendAsync(new GateEnterResponse { AccountId = request.AccountId, Result = GateEnterResult.Success }).ConfigureAwait(false);
            await session.SendAsync(new SWorldCurrentDataResponse()).ConfigureAwait(false);
        }
    }
}