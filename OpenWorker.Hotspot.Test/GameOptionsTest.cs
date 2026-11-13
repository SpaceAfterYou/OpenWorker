using Microsoft.Extensions.DependencyInjection;
using OpenWorker.Commands;
using OpenWorker.Commands.Abstractions;
using OpenWorker.Domain.Enums;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// The options of the game: the contents a server offers and what the client is told of them.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class GameOptionsTest
{
    /// <summary>
    /// A content switched by the command is switched for the whole server, and everybody on it is told with 01/07: the client in
    /// the game reads the contents only from that message.
    /// </summary>
    [TestMethod]
    public void TheOptionCommandTellsEverybodyOnTheServer()
    {
        var community = new MemoryCommunity();
        var contents = new ServerContent();

        using var server = new TestServer(community);
        using var alice = server.Join(1, "Alice");
        using var bob = server.Join(2, "Bob");
        using var services = new ServiceCollection()
            .AddSingleton(server.World)
            .AddSingleton(contents)
            .AddSingleton(server.Network)
            .AddSingleton(CashShopTest.EmptyWindow(server.World, contents))
            .BuildServiceProvider();

        ICommand command = new OptionCommand(services);
        var done = false;

        server.Run(async () => done = await command.TryExecute(alice.Entity, ["3", "true"]));

        Assert.IsTrue(done);
        Assert.IsTrue(contents[ServerContentIndex.Ranking]);

        foreach (var player in new[] { alice, bob })
        {
            var frame = player.Read().Single();

            Assert.AreEqual((1, 7), (frame.Group, frame.Command));
            CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 1, 0 }, frame.Content);
        }
    }
}
