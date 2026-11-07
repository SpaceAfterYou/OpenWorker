using System.Collections.Concurrent;
using Lua;
using Lua.Standard;
using Microsoft.Extensions.Logging;
using OpenWorker.Gameplay.Ai;

namespace OpenWorker.Lua;

/// <summary>
/// Reads the scripts that say how a monster thinks. A script tells a profile what to do through calls, nothing in it
/// has to be understood by the server: it is run once and what it says is kept.
/// </summary>
public sealed class AiScriptLoader
{
    private const string EntryPoint = "Initial_Normal";
    private const string Extension = ".lua";
    private static readonly string[] Folders = ["scripts/AI", "scripts/ai", "scripts"];

    private static readonly byte[] ByteOrderMark = [0xEF, 0xBB, 0xBF];

    private readonly ILogger<AiScriptLoader> _logger;
    private readonly ConcurrentDictionary<string, AiProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lazy<Dictionary<string, string>> _files;

    public AiScriptLoader(ILogger<AiScriptLoader> logger)
    {
        _logger = logger;
        _files = new Lazy<Dictionary<string, string>>(IndexFiles);
    }

    /// <summary>
    /// A loader for scripts of any folder.
    /// </summary>
    public AiScriptLoader(ILogger<AiScriptLoader> logger, string folder)
    {
        _logger = logger;
        _files = new Lazy<Dictionary<string, string>>(() => IndexFiles([folder]));
    }

    /// <summary>
    /// How many scripts the loader has read so far.
    /// </summary>
    public int Count => _profiles.Count;

    /// <summary>
    /// The profile a script makes. A monster whose script cannot be read does not think.
    /// </summary>
    public AiProfile Get(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "0")
        {
            return AiProfile.Empty;
        }

        return _profiles.GetOrAdd(name, Read);
    }

    /// <summary>
    /// Makes a profile out of the text of a script.
    /// </summary>
    public AiProfile Parse(string name, ReadOnlySpan<byte> script)
    {
        var builder = new AiProfileBuilder().Named(name);
        var state = LuaState.Create();

        state.OpenStandardLibraries();
        state.Environment["Soulworker"] = new LuaValue(CreateConstants());

        if (script.StartsWith(ByteOrderMark))
        {
            script = script[ByteOrderMark.Length..];
        }

        var chunk = state.Load(script, name, "t", null);
        var run = state.RunAsync(chunk);

        Complete(run);

        if (!state.Environment[EntryPoint].TryRead<LuaFunction>(out var entry))
        {
            return builder.Build();
        }

        var monster = new LuaTable
        {
            ["GetAi"] = new LuaFunction("GetAi", (context, _) =>
            {
                context.Return(new LuaValue(CreateAi(builder, name)));

                return new ValueTask<int>(1);
            })
        };

        Complete(state.CallAsync(entry, [new LuaValue(new LuaTable()), new LuaValue(monster)]));

        return builder.Build();
    }

    /// <summary>
    /// A script only ever calls the functions of the table and never waits, so a call is over when it returns.
    /// </summary>
    private static void Complete<T>(ValueTask<T> task)
    {
        if (!task.IsCompleted)
        {
            throw new InvalidOperationException("A script of the AI waited for something.");
        }

        task.GetAwaiter().GetResult();
    }

    private AiProfile Read(string name)
    {
        try
        {
            if (!_files.Value.TryGetValue(name, out var path))
            {
                _logger.LogWarning("No script of the AI named {Name}, the monster will not think", name);

                return AiProfile.Empty;
            }

            return Parse(name, File.ReadAllBytes(path));
        }

        catch (Exception e) when (e is IOException or UnauthorizedAccessException or LuaParseException or LuaCompileException or LuaRuntimeException or InvalidOperationException)
        {
            _logger.LogWarning(e, "The script of the AI named {Name} cannot be read, the monster will not think", name);

            return AiProfile.Empty;
        }
    }

    private static Dictionary<string, string> IndexFiles() => IndexFiles(Folders);

    private static Dictionary<string, string> IndexFiles(IEnumerable<string> folders)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in folders.Where(Directory.Exists))
        {
            foreach (var path in Directory.EnumerateFiles(folder, "*" + Extension, SearchOption.TopDirectoryOnly))
            {
                files.TryAdd(Path.GetFileNameWithoutExtension(path), path);
            }
        }

        return files;
    }

    private static LuaTable CreateConstants()
    {
        var table = new LuaTable();

        foreach (var (name, value) in AiScriptNames.States)
        {
            table[name] = value;
        }

        foreach (var (name, value) in AiScriptNames.Variables)
        {
            table[name] = value;
        }

        foreach (var (name, value) in AiScriptNames.StateVariables)
        {
            table[name] = value;
        }

        foreach (var (name, value) in new[] { ("eFUZZY_ATTACK", 0), ("eFUZZY_MOVE", 1), ("eFUZZY_RUNAWAY", 2), ("eFUZZY_PROTECTION", 3) })
        {
            table[name] = value;
        }

        return table;
    }

    private static bool TryNumber(LuaFunctionExecutionContext context, int index, double fallback, out double value)
    {
        value = fallback;

        if (index >= context.ArgumentCount)
        {
            return true;
        }

        return context.GetArgument(index).TryRead(out value);
    }

    private static string Text(LuaFunctionExecutionContext context, int index)
    {
        return index < context.ArgumentCount && context.GetArgument(index).TryRead<string>(out var text) ? text : string.Empty;
    }

    /// <summary>
    /// The object a script gets as the AI of its monster. What it does not know is accepted and left alone.
    /// </summary>
    private LuaTable CreateAi(AiProfileBuilder builder, string name)
    {
        var ai = new LuaTable();

        // The first argument of every call is the object itself.
        void Add(string method, Action<LuaFunctionExecutionContext> call)
        {
            ai[method] = new LuaFunction(method, (context, _) =>
            {
                try
                {
                    call(context);
                }

                catch (Exception e) when (e is InvalidOperationException or LuaRuntimeException or InvalidCastException)
                {
                    _logger.LogDebug("{Script}: {Method} was called with something it cannot take: {Message}", name, method, e.Message);
                }

                return new ValueTask<int>(context.Return());
            });
        }

        bool N(LuaFunctionExecutionContext c, int i, out double v, double fallback = 0) => TryNumber(c, i, fallback, out v);

        Add("SetAiCheckTime", c => { if (N(c, 1, out var a)) { builder.SetAiCheckTime((float)a); } });

        Add("SetFindEnemyInfo", c =>
        {
            if (N(c, 1, out var a) && N(c, 2, out var b) && N(c, 3, out var d))
            {
                builder.SetFindEnemyInfo(a != 0, (float)b, (float)d);
            }
        });

        Add("SetFirstAttacker", c => { if (N(c, 1, out var a) && N(c, 2, out var b)) { builder.SetFirstAttacker(a != 0, (float)b); } });
        Add("SetTakeTargetInfo", c => { if (N(c, 1, out var a)) { builder.SetTakeTargetInfo((float)a); } });
        Add("SetTargetLostInfo", c => { if (N(c, 1, out var a)) { builder.SetTargetLostInfo((float)a); } });
        Add("SetReturnDistance", c => { if (N(c, 1, out var a) && N(c, 2, out var b)) { builder.SetReturnDistance((float)a, (int)b); } });
        Add("SetRunDistance", c => { if (N(c, 1, out var a)) { builder.SetRunDistance((float)a); } });
        Add("SetDmgAggressive", c => { if (N(c, 1, out var a) && N(c, 2, out var b)) { builder.SetDmgAggressive((int)a, (float)b); } });
        Add("SetDmgAggroReset", c => { if (N(c, 1, out var a) && N(c, 2, out var b)) { builder.SetDmgAggroReset((float)a, (float)b); } });
        Add("SetSpawnAggro", c => { if (N(c, 1, out var a) && N(c, 2, out var b)) { builder.SetSpawnAggro((float)a, (float)b); } });
        Add("SetEnableClearTarget", c => { if (N(c, 1, out var a)) { builder.SetEnableClearTarget(a != 0); } });
        Add("SetSuicideTime", c => { if (N(c, 1, out var a)) { builder.SetSuicideTime((float)a); } });
        Add("SetDeathAction", c => builder.SetDeathAction(Text(c, 1)));

        Add("SetRunawayInfo", c =>
        {
            if (N(c, 1, out var a) && N(c, 2, out var b) && N(c, 3, out var d) && N(c, 4, out var e))
            {
                builder.SetRunawayInfo((int)a, (int)b, (float)d, (float)e);
            }
        });

        Add("SetCommonAction", c => { if (N(c, 1, out var a)) { builder.SetCommonAction((int)a, Text(c, 2)); } });

        Add("SetSkillGroupInfo", c =>
        {
            if (N(c, 1, out var a) && N(c, 2, out var b) && N(c, 3, out var d) && N(c, 4, out var e) && N(c, 5, out var f))
            {
                builder.SetSkillGroupInfo((int)a, (int)b, (int)d, (int)e, (int)f);
            }
        });

        Add("SetSkillGroupRate", c =>
        {
            var rates = new int[AiProfile.SkillSlots];

            for (var i = 0; i < rates.Length; i++)
            {
                if (N(c, i + 1, out var a))
                {
                    rates[i] = (int)a;
                }
            }

            builder.SetSkillGroupRate(rates);
        });

        Add("RegisterStateFunctions", c => { if (N(c, 1, out var a)) { builder.RegisterStateFunctions((AiState)(int)a); } });

        Add("RegisterStateVars", c =>
        {
            if (N(c, 1, out var a) && N(c, 2, out var b) && N(c, 3, out var d) && N(c, 4, out var e))
            {
                builder.RegisterStateVars((AiState)(int)a, (AiStateVariable)(int)b, (int)d, (int)e);
            }
        });

        Add("RegisterConditions", c =>
        {
            if (N(c, 1, out var a) && N(c, 2, out var b) && N(c, 3, out var d) && N(c, 5, out var e) && N(c, 6, out var f) && N(c, 7, out var g, 1))
            {
                builder.RegisterConditions((AiState)(int)a, (AiState)(int)b, (AiVariable)(int)d, Text(c, 4), e, f, (int)g);
            }
        });

        Add("SetReservedCondition", c =>
        {
            if (N(c, 1, out var a) && N(c, 2, out var b) && N(c, 4, out var d) && N(c, 5, out var e))
            {
                builder.SetReservedCondition((int)a, (AiVariable)(int)b, Text(c, 3), d, e);
            }
        });

        Add("CombineReservedConditions", c =>
        {
            if (N(c, 1, out var a) && N(c, 2, out var b))
            {
                var indexes = new int[5];

                for (var i = 0; i < indexes.Length; i++)
                {
                    indexes[i] = N(c, i + 3, out var x, -1) ? (int)x : -1;
                }

                builder.CombineReservedConditions((AiState)(int)a, (AiState)(int)b, indexes);
            }
        });

        Add("RegisterSkillConditions", c =>
        {
            if (N(c, 1, out var a) && N(c, 2, out var b) && N(c, 4, out var d) && N(c, 5, out var e) && N(c, 6, out var f, -1))
            {
                builder.RegisterSkillConditions((int)a, (AiVariable)(int)b, Text(c, 3), d, e, (int)f);
            }
        });

        Add("RegisterActionAfterSkill", c =>
        {
            if (N(c, 1, out var a) && N(c, 2, out var b) && N(c, 3, out var d) && N(c, 5, out var e) && N(c, 6, out var f) && N(c, 7, out var g))
            {
                builder.RegisterActionAfterSkill((int)a, (AiState)(int)b, (AiVariable)(int)d, Text(c, 4), e, f, (int)g);
            }
        });

        Add("RegisterCancelSkillConditions", c =>
        {
            if (N(c, 1, out var a) && N(c, 3, out var b) && N(c, 4, out var d))
            {
                builder.RegisterCancelSkillConditions((AiVariable)(int)a, Text(c, 2), b, d);
            }
        });

        // A call to a name the table does not have must not stop the script: it does nothing.
        ai.Metatable = new LuaTable
        {
            ["__index"] = new LuaFunction("__index", (context, _) =>
            {
                context.Return(new LuaValue(new LuaFunction("ignored", (inner, _) => new ValueTask<int>(inner.Return()))));

                return new ValueTask<int>(1);
            })
        };

        return ai;
    }
}
