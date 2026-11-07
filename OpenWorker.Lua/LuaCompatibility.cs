using Lua;

namespace OpenWorker.Lua;

/// <summary>
/// The scripts of the maps were written for Lua 5.1 and for the engine of the game, and the library of the server
/// follows a later Lua and has no engine.
/// </summary>
public static class LuaCompatibility
{
    /// <summary>
    /// Gives back what scripts still call: the length of a table as <c>table.getn</c>, and the vectors of the
    /// engine as <c>Vision.hkvVec3</c>.
    /// </summary>
    public static void Apply(LuaState state)
    {
        var table = state.Environment["table"].Read<LuaTable>();

        table["getn"] = new LuaFunction((context, _) =>
        {
            context.Return(context.GetArgument<LuaTable>(0).ArrayLength);

            return ValueTask.FromResult(1);
        });

        var vision = new LuaTable
        {
            ["hkvVec3"] = new LuaFunction((context, _) =>
            {
                context.Return(new VVector3
                {
                    X = context.GetArgument<float>(0),
                    Y = context.GetArgument<float>(1),
                    Z = context.GetArgument<float>(2)
                });

                return ValueTask.FromResult(1);
            })
        };

        state.Environment["Vision"] = new LuaValue(vision);
    }
}
