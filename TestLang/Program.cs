// https://www.youtube.com/watch?v=7vWzIPUX5CQ&list=RD7vWzIPUX5CQ&start_radio=1
//

using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using Arch.Core;
using Lua;
using Lua.Standard;
using OpenWorker.Batch;
using OpenWorker.Domain.Components;
using OpenWorker.Hotspot;
using OpenWorker.Lua;
using OpenWorker.Lua.Managers;
using OpenWorker.UpdateContent.Res.Rows;
using TestLang;

await ReaderHavokHkt.Read().ConfigureAwait(false);

// var test = new TestComponent();
// test.Test();
//
// var world = World.Create();
// var player = world.Create([
//     ComponentRegistry.Add<ServerSessionComponent>(),
//     ComponentRegistry.Add<ActorComponent>()
// ]);
//
// var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
// socket.Bind(new IPEndPoint(IPAddress.Any, 5123));
// socket.Listen();
//
// var session = new ServerSessionComponent(new TcpClient("127.0.0.1", 5123));
// world.Add(player, session);
//
// var state = LuaState.Create();
// state.OpenStandardLibraries();
//
// var table = new LuaTable
// {
//     ["GetHelper"] = new LuaFunction((context, buffer, cancellationToken) =>
//     {
//         var span = buffer.Span;
//     
//         span[0] = new LuaValue(new LuaGameHelper());
//     
//         return ValueTask.FromResult(1);
//     })
// };
//
// state.Environment["Soulworker"] = new LuaValue(table);
//
// var batch = VBatchFile.CreateFromPath(@"T:\Games\HanPurple\soulworker\datas\World\Table\T02_TUTORIAL.vbatch");
//
// var creatureManager = new LuaCreatureManager(world, batch);
//
// creatureManager.Emplace(player);
//
// var buffManager = new BuffManager(world, new ReadOnlyCollection<BuffRow>([]));
//
// var maze = new LuaMaze(state: state, world, player, creatureManager, buffManager, batch);
//
// await state
//     .DoFileAsync(@"T:\Games\HanPurple\soulworker\datas\Scripts\Server\T02_TUTORIAL.lua")
//     .ConfigureAwait(false);
//
// await state
//     .OnEnterPlayerAsync(world.Get<ActorComponent>(player), maze)
//     .ConfigureAwait(false);
//
// await state
//     .CheckConditionAsync(0, 910503, maze)
//     .ConfigureAwait(false);
//
// await state
//     .CheckConditionAsync(0, 910601, maze)
//     .ConfigureAwait(false);
//
// await state
//     .OnNpcWayPoint(101001, 4101, maze)
//     .ConfigureAwait(false);
//
// Console.WriteLine("Done.");
//
// await WaitForKeyPressAsync().ConfigureAwait(false);
//
// return;
//
// static Task WaitForKeyPressAsync()
// {
//     return Task.Run(() => Console.ReadKey(true));
// }
// T:\Games\HanPurple\soulworker\datas\Scripts\Server\S02_STEELGRAVE_Stage_15.lua


// using Arch.Core;
// using TestLang;
//
// var world = World.Create();
//
// var entity = world.Create();
// world.Add(entity, new TestComponent());
//
// var component = world.Get<TestComponent>(entity);
// component.A = 11;
//
// var component2 = world.Get<TestComponent>(entity);
// Console.WriteLine(component);