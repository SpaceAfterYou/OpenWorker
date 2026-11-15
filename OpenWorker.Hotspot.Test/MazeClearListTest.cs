using System.Buffers.Binary;
using Arch.Core;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Domain.Persistent;
using OpenWorker.Domain.Stores;
using OpenWorker.GameServer.SoulWorker.Network.DataTypes;
using OpenWorker.Gameplay.Modules.Persistence;
using OpenWorker.Gameplay.Modules.Progress;
using OpenWorker.Gameplay.Modules.Progress.Components;
using OpenWorker.Gameplay.Modules.State;
using OpenWorker.Hotspot.Modules.Maze.Responses;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The mazes a person has cleared open the board of the mazes: the client is told all of them as the person enters a
/// district, and the whole group of a maze when it is cleared.
/// </summary>
[TestClass]
public sealed class MazeClearListTest
{
    private const int Person = 7;
    private const int District = 10003;
    private const int Group = 33;

    private static readonly MazeEpisodes Episodes = MazeEpisodes.Of(
    [
        new MazeEpisode(21311, Group, 1, false),
        new MazeEpisode(21321, Group, 2, false),
        new MazeEpisode(21342, Group, 4, true)
    ]);

    /// <summary>
    /// Keeps the documents of the person as the database would, from one login to the next.
    /// </summary>
    private sealed class KeptStore : IPersonStore
    {
        private readonly Dictionary<PersonStateKind, string> _documents = [];

        public Task<List<ItemPersistent>> LoadItemsAsync(int person, CancellationToken cancellationToken = default) => Task.FromResult(new List<ItemPersistent>());

        public Task<long[]> ReserveSerialsAsync(int count, CancellationToken cancellationToken = default) => Task.FromResult(new long[count]);

        public Task<string?> LoadDocumentAsync(int person, PersonStateKind kind, CancellationToken cancellationToken = default) =>
            Task.FromResult(_documents.GetValueOrDefault(kind));

        public Task<Dictionary<PersonStateKind, string>> LoadDocumentsAsync(int person, CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<PersonStateKind, string>(_documents));

        public Task SaveChangesAsync(IReadOnlyList<PersonChanges> batch, CancellationToken cancellationToken = default)
        {
            foreach (var document in batch.SelectMany(e => e.Documents))
            {
                _documents[document.Kind] = document.Payload;
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// One login of the person: a world of its own, as a new process of the server would have.
    /// </summary>
    private sealed class Login : IDisposable
    {
        private readonly RecordingConnection _connection = new();

        public World World { get; } = World.Create();
        public SharePointManager Progress { get; }
        public Entity Player { get; }

        public Login(PersonStateStore store)
        {
            Progress = new SharePointManager(World, store, SharePointCatalog.Of([]), Episodes, null!, NullLogger<SharePointManager>.Instance);
            Player = World.Create(new ActorComponent(Person, ActorType.User), new ServerSessionComponent(_connection), new DirtyComponent(new PersonDirty(Person)), new SharePointComponent());
        }

        /// <summary>
        /// The lists of 11/64 so far: whether each was all the person has, and its groups with the normal and hard mazes.
        /// </summary>
        public async Task<(bool All, (int Group, byte Open, int[] Normal, int[] Hard)[] Groups)[]> ClearListsAsync(int count)
        {
            var opcode = new MazeClearInfoResponse().Opcode;

            await MetricAssertions.UntilAsync(() => Frames(opcode).Length >= count, "The client is told the mazes cleared.");

            return [.. Frames(opcode).Select(Read)];
        }

        private PlayerFrame[] Frames(MessageOpcode opcode) =>
            _connection.Frames.Select(PlayerFrame.Open).Where(e => e.Opcode.Value == opcode.Value).ToArray();

        private static (bool, (int, byte, int[], int[])[]) Read(PlayerFrame frame)
        {
            var body = frame.Body;
            var groups = new (int, byte, int[], int[])[body[0]];

            for (var i = 0; i < groups.Length; i++)
            {
                var at = 1 + i * 85;
                var slots = Enumerable.Range(0, 10).Select(e => (Normal: Int(body, at + 5 + e * 8), Hard: Int(body, at + 9 + e * 8))).ToArray();

                groups[i] = (Int(body, at), body[at + 4], [.. slots.Select(e => e.Normal).Where(e => e != 0)], [.. slots.Select(e => e.Hard).Where(e => e != 0)]);
            }

            return (body[1 + groups.Length * 85 + 4] != 0, groups);
        }

        private static int Int(byte[] body, int at) => BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(at));

        public void Dispose()
        {
            World.Dispose();
        }
    }

    [TestMethod]
    public async Task TheMazesClearedAreToldAgainAtTheNextLogin()
    {
        var store = new PersonStateStore(new KeptStore());

        using (var first = new Login(store))
        {
            await first.Progress.EnterDistrictAsync(first.Player, Person, District, CancellationToken.None);

            first.Progress.RecordClear(first.Player, 21311, 3, 100);
            first.Progress.RecordClear(first.Player, 21342, 3, 100);

            // What the flush writes as the person leaves.
            await store.SaveAsync(Person, PersonStateKind.SharePoints, first.World.Get<SharePointComponent>(first.Player).Book!, CancellationToken.None);
        }

        using var second = new Login(store);

        await second.Progress.EnterDistrictAsync(second.Player, Person, District, CancellationToken.None);

        var (all, groups) = (await second.ClearListsAsync(1)).Single();

        Assert.IsTrue(all, "the list at the entry is all the person has");
        Assert.HasCount(1, groups);
        Assert.AreEqual(Group, groups[0].Group);
        Assert.AreEqual((byte)1, groups[0].Open, "the board opens the episode after the last normal one cleared");
        CollectionAssert.AreEqual(new[] { 21311 }, groups[0].Normal);
        CollectionAssert.AreEqual(new[] { 21342 }, groups[0].Hard);
    }

    [TestMethod]
    public async Task AClearTellsTheWholeGroupOfTheMaze()
    {
        using var login = new Login(new PersonStateStore(new KeptStore()));

        await login.Progress.EnterDistrictAsync(login.Player, Person, District, CancellationToken.None);

        login.Progress.RecordClear(login.Player, 21311, 3, 100);
        login.Progress.RecordClear(login.Player, 21321, 3, 100);

        var lists = await login.ClearListsAsync(3);
        var (all, groups) = lists[2];

        Assert.IsFalse(all, "a clear tells its group alone and the client keeps the others");
        Assert.AreEqual((byte)2, groups.Single().Open);
        CollectionAssert.AreEqual(new[] { 21311, 21321 }, groups.Single().Normal, "the client replaces the group with what it is told");
    }
}
