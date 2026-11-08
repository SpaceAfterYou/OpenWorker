using System.Numerics;
using System.Reflection;
using Arch.Core;
using Lua;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWorker.Domain.Components;
using OpenWorker.Domain.Enums;
using OpenWorker.Gameplay.Messages.Response.Person;
using OpenWorker.Gameplay.Messages.Response.Person.Components;
using OpenWorker.Lua;

namespace OpenWorker.Hotspot.Test;

/// <summary>
/// A script may call anything the server lets it see. Every member a script can reach does something or does nothing,
/// but none of them is left unwritten: called with the default of each argument, none throws
/// <see cref="NotImplementedException"/>. The members are found by reflection, so a new one is covered without a line
/// here, and no script of the client is needed.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class LuaMemberTest
{
    private static object? Argument(Type type, LuaMaze maze)
    {
        if (type == typeof(LuaMaze))
        {
            return maze;
        }

        if (type == typeof(VVector3))
        {
            return new VVector3();
        }

        if (type == typeof(string))
        {
            return string.Empty;
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static object Instance(Type type, LuaMaze maze)
    {
        if (type == typeof(LuaMaze))
        {
            return maze;
        }

        if (type == typeof(LuaGameHelper))
        {
            return new LuaGameHelper(NullLogger<LuaGameHelper>.Instance);
        }

        return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"{type.Name} cannot be made for the test.");
    }

    [TestMethod]
    public void NoMemberAScriptCanCallIsLeftUnwritten()
    {
        var world = World.Create();

        try
        {
            var player = world.Create();

            world.Add(player, new ActorComponent { Identifier = 1, Type = ActorType.User });
            world.Add(player, new WorldComponent { Location = 0, Position = Vector3.Zero, Rotation = 0f, Jump = 0 });
            world.Add(player, new PersonInfoComponent { Name = "Tester", Hero = Hero.Haru });
            world.Add(player, new ServerSessionComponent(Stream.Null));

            var maze = TestMazes.Create(world, player);

            world.Add(player, maze);
            maze.TryStart();

            var types = typeof(LuaMaze).Assembly.GetTypes().Where(e => e.GetCustomAttribute<LuaObjectAttribute>() is not null).ToArray();
            var called = 0;
            var unwritten = new List<string>();
            var throwing = new List<string>();

            foreach (var type in types)
            {
                var instance = Instance(type, maze);
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                foreach (var member in type.GetMembers(flags).Where(e => e.GetCustomAttribute<LuaMemberAttribute>() is not null))
                {
                    void Call(Action call)
                    {
                        called++;

                        try
                        {
                            call();
                        }

                        catch (TargetInvocationException e) when (e.InnerException is NotImplementedException)
                        {
                            unwritten.Add($"{type.Name}.{member.Name}");
                        }

                        catch (TargetInvocationException e)
                        {
                            throwing.Add($"{type.Name}.{member.Name}: {e.InnerException?.GetType().Name} {e.InnerException?.Message}");
                        }
                    }

                    switch (member)
                    {
                        case MethodInfo method:
                            Call(() => method.Invoke(method.IsStatic ? null : instance, method.GetParameters().Select(e => Argument(e.ParameterType, maze)).ToArray()));
                            break;

                        case PropertyInfo property:
                            Call(() => property.GetValue(instance));

                            if (property.CanWrite)
                            {
                                Call(() => property.SetValue(instance, Argument(property.PropertyType, maze)));
                            }

                            break;
                    }
                }
            }

            Assert.IsGreaterThan(40, called, "The members of the scripts were not found.");
            Assert.IsEmpty(unwritten, string.Join(", ", unwritten));
            Assert.IsEmpty(throwing, string.Join(" | ", throwing));

            maze.Close();
        }

        finally
        {
            World.Destroy(world);
        }
    }
}
