using System.Globalization;
using System.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenWorker.DependencyInjection;

/// <summary>
/// Logs, once when the server starts, which garbage collector the process runs with. The settings come from
/// <c>runtimeconfig.json</c> (<c>Directory.Build.props</c> writes them) unless an environment variable of the runtime
/// (<c>DOTNET_gcServer</c>, <c>DOTNET_GCDynamicAdaptationMode</c>, ...) replaces one, which it does: this line is how a run
/// of the load test proves the mode it was measured with. The values are the ones the collector itself read
/// (<see cref="GC.GetConfigurationVariables"/>), not the ones that were asked for.
/// </summary>
internal sealed partial class RuntimeSettingsLogService(ILogger<RuntimeSettingsLogService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var variables = GC.GetConfigurationVariables();

        var concurrent = variables.TryGetValue("ConcurrentGC", out var value) && value is true;
        var heaps = variables.TryGetValue("HeapCount", out value) ? Convert.ToInt64(value, CultureInfo.InvariantCulture) : 0;
        var adaptation = variables.TryGetValue("GCDynamicAdaptationMode", out value) ? Convert.ToInt64(value, CultureInfo.InvariantCulture) : 0;

        // Dynamic adaptation (DATAS) is a mode of Server GC; the workstation collector reads the setting and ignores it.
        var datas = GCSettings.IsServerGC && adaptation != 0;

        LogGarbageCollector(GCSettings.IsServerGC, concurrent, datas, adaptation, GCSettings.LatencyMode, heaps, Environment.ProcessorCount);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "GC: server {ServerGc}, concurrent {Concurrent}, DATAS {Datas} (adaptation mode {AdaptationMode}), latency mode {LatencyMode}, " +
            "heaps {HeapCount}, processors {ProcessorCount}.")]
    private partial void LogGarbageCollector(bool serverGc, bool concurrent, bool datas, long adaptationMode, GCLatencyMode latencyMode, long heapCount, int processorCount);
}
