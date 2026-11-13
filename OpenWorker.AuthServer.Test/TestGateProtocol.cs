using System.Net;
using System.Net.Sockets;
using System.Text;
using Arch.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes.Enums.Opcodes;
using OpenWorker.Gameplay.Modules.Login;
using OpenWorker.Gameplay.Modules.Login.Components;
using OpenWorker.GateServer.Gameplay;
using OpenWorker.Hotspot;
using OpenWorker.Hotspot.Handler.DataTypes;
using OpenWorker.Hotspot.Messages.Response.Person.Enums;
using OpenWorker.Hotspot.Messages.Response.Person.Values;
using OpenWorker.Hotspot.Modules.Persons.Enums;
using OpenWorker.Hotspot.Modules.Persons.Requests;
using OpenWorker.Hotspot.Modules.System.Responses;
using OpenWorker.Hotspot.SoulWorker.Network;
using OpenWorker.Persistence;
using OpenWorker.Persistence.Utils;
using OpenWorker.Wire;

namespace OpenWorker.AuthServer.Test;

[TestClass]
public sealed class TestGateProtocol
{
    private const int AccountId = 7;

    private sealed class Harness : IDisposable
    {
        private readonly TcpClient _client = new();
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly BinaryReader _reader;

        public World World { get; } = World.Create();
        public Entity Player { get; }
        public IDbContextFactory<PersistenceContext> Factory { get; }
        public SecondPasswordGameplay Gameplay { get; }

        public Harness()
        {
            _listener.Start();
            _client.Connect(IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port);

            Player = World.Create(
                new ServerSessionComponent(_listener.AcceptTcpClient()),
                new ClaimsComponent(AccountId, 1),
                new SecondPasswordComponent { Password = string.Empty });

            _reader = new BinaryReader(_client.GetStream());

            var name = Guid.NewGuid().ToString();

            Factory = new ServiceCollection()
                .AddDbContextFactory<PersistenceContext>(o => o.UseInMemoryDatabase(name))
                .BuildServiceProvider()
                .GetRequiredService<IDbContextFactory<PersistenceContext>>();

            using var database = Factory.CreateDbContext();

            database.Accounts.Add(new AccountPersistent
            {
                Id = AccountId,
                Username = "user",
                PasswordHash = [],
                SaltHash = []
            });
            database.SaveChanges();

            Gameplay = new SecondPasswordGameplay(
                World,
                Factory,
                new ServerContent([new ServerContentPersistent { Index = ServerContentIndex.SecondPassword, State = true }]));
        }

        public async Task<(int Error, PasswordProtectionState State)> SendAsync(PasswordCheckType type, string password)
        {
            using var stream = new MemoryStream();
            await using var writer = new BinaryWriter(stream, Encoding.ASCII, true);

            writer.Write((byte)type);
            writer.Write((short)password.Length);
            writer.Write(Encoding.ASCII.GetBytes(password));
            writer.Flush();
            stream.Position = 0;

            using var requestReader = new BinaryReader(stream);

            await Gameplay.HandleAsync(
                new ServiceHandleContext(Player, CancellationToken.None),
                new PersonSecondPasswordRequest(requestReader));

            var header = new MessageHeader(_reader);
            var payload = _reader.ReadBytes(header.ContentLength);
            MessageEncryption.Exchange(payload.AsSpan(), header.Version);

            return (BitConverter.ToInt32(payload, 2), (PasswordProtectionState)payload[6]);
        }

        public void Dispose()
        {
            _reader.Dispose();
            _client.Dispose();
            _listener.Stop();
            _listener.Dispose();
            World.Dispose();
        }
    }

    /// <summary>
    /// A second password is six digits without three repeated or consecutive ones in a row: one case for each way a
    /// password breaks the rule, and one that keeps it.
    /// </summary>
    [TestMethod]
    public void SecondPasswordRules()
    {
        (string Password, PersonErrorCode? Error)[] cases =
        [
            ("135790", null),
            ("12345", PersonErrorCode.SecondPasswordInvalid),
            ("12a456", PersonErrorCode.SecondPasswordInvalid),
            ("111357", PersonErrorCode.SecondPasswordRepeatedDigits),
            ("912345", PersonErrorCode.SecondPasswordSequentialDigits),
            ("975431", PersonErrorCode.SecondPasswordSequentialDigits)
        ];

        foreach (var (password, error) in cases)
        {
            Assert.AreEqual(error, LoginRules.ValidateSecondPassword(password), password);
        }
    }

    /// <summary>
    /// A name of a character is two to twenty letters or digits of any script: the bounds of the length, a name in
    /// another script and a name with a space.
    /// </summary>
    [TestMethod]
    public void PersonNameRules()
    {
        (string Name, bool Valid)[] cases =
        [
            ("Ab", true),
            ("名前", true),
            ("A", false),
            ("With Space", false),
            ("123456789012345678901", false)
        ];

        foreach (var (name, valid) in cases)
        {
            Assert.AreEqual(valid, LoginRules.IsValidPersonName(name), name);
        }
    }

    [TestMethod]
    public void ErrorResponseHasFlagAndCode()
    {
        var message = new ErrorMessageResponse(
            MessageOpcode.From(GroupOpcode.Character, CharacterOpcode.ListRes),
            PersonErrorCode.NameInUse);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        WireBridge.Write(writer, message);

        Assert.AreEqual((byte)GroupOpcode.Character, message.Opcode.Group);
        Assert.AreEqual(0x12 | 0x80, message.Opcode.Command);
        CollectionAssert.AreEqual(BitConverter.GetBytes((ushort)51005), stream.ToArray());
    }

    [TestMethod]
    public void ProtectionStateWritesTwoStateBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        WireBridge.Write(writer, new ProtectionStateValue
        {
            SecondPassword = PasswordProtectionState.Lock,
            TradePassword = PasswordProtectionState.UnAuthenticated
        });

        CollectionAssert.AreEqual(new byte[] { 3, 1 }, stream.ToArray());
    }

    [TestMethod]
    public void EncodedPasswordVerifies()
    {
        var encoded = PasswordHash.CreateEncoded("135790");

        Assert.IsFalse(encoded.Contains("135790"));
        Assert.IsTrue(PasswordHash.VerifyEncoded("135790", encoded));
        Assert.IsFalse(PasswordHash.VerifyEncoded("135791", encoded));
        Assert.IsFalse(PasswordHash.VerifyEncoded("135790", "plain"));
    }

    [TestMethod]
    public async Task SecondPasswordFlow()
    {
        using var harness = new Harness();

        var account = new AccountPersistent { Username = "user", PasswordHash = [], SaltHash = [] };
        harness.Gameplay.Load(harness.Player, account);

        Assert.IsFalse(harness.Gameplay.IsSatisfied(harness.Player));
        Assert.AreEqual(PasswordProtectionState.None, harness.World.Get<SecondPasswordComponent>(harness.Player).State);

        var invalid = await harness.SendAsync(PasswordCheckType.Create, "111111");
        Assert.AreEqual((int)PersonErrorCode.SecondPasswordRepeatedDigits, invalid.Error);
        Assert.AreEqual(PasswordProtectionState.None, invalid.State);

        var created = await harness.SendAsync(PasswordCheckType.Create, "135790");
        Assert.AreEqual((0, PasswordProtectionState.Authenticated), created);
        Assert.IsTrue(harness.Gameplay.IsSatisfied(harness.Player));

        await using (var database = await harness.Factory.CreateDbContextAsync())
        {
            account = await database.Accounts.FirstAsync(e => e.Id == AccountId);
        }

        Assert.AreNotEqual("135790", account.SecondPassword);

        // A new session starts locked behind the stored password.
        harness.Gameplay.Load(harness.Player, account);
        Assert.AreEqual(PasswordProtectionState.UnAuthenticated, harness.World.Get<SecondPasswordComponent>(harness.Player).State);

        var wrong = await harness.SendAsync(PasswordCheckType.Check, "135791");
        Assert.AreNotEqual(0, wrong.Error);
        Assert.AreEqual(PasswordProtectionState.UnAuthenticated, wrong.State);

        var right = await harness.SendAsync(PasswordCheckType.Check, "135790");
        Assert.AreEqual((0, PasswordProtectionState.Authenticated), right);

        harness.Gameplay.Load(harness.Player, account);

        for (var i = 0; i < 4; i++)
        {
            await harness.SendAsync(PasswordCheckType.Check, "135791");
        }

        var locked = await harness.SendAsync(PasswordCheckType.Check, "135791");
        Assert.AreEqual(PasswordProtectionState.Lock, locked.State);

        var afterLock = await harness.SendAsync(PasswordCheckType.Check, "135790");
        Assert.AreEqual(PasswordProtectionState.Lock, afterLock.State);
        Assert.AreNotEqual(0, afterLock.Error);
    }

    [TestMethod]
    public void DisabledSecondPasswordIsAlwaysSatisfied()
    {
        using var harness = new Harness();

        var gameplay = new SecondPasswordGameplay(harness.World, harness.Factory, new ServerContent([]));

        gameplay.Load(harness.Player, new AccountPersistent
        {
            Username = "user",
            PasswordHash = [],
            SaltHash = [],
            SecondPassword = "stored"
        });

        Assert.IsTrue(gameplay.IsSatisfied(harness.Player));
        Assert.AreEqual(PasswordProtectionState.Authenticated, harness.World.Get<SecondPasswordComponent>(harness.Player).State);
    }
}
