using System.Diagnostics;
using System.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OpenWorker.Extensions;

public class PerformanceMonitoringService : BackgroundService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private readonly Process _currentProcess;

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger)
    {
        _logger = logger;
        _currentProcess = Process.GetCurrentProcess();
        
        // Initialize CPU counter if available
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize CPU performance counter");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                LogPerformanceMetrics();
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in performance monitoring");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private void LogPerformanceMetrics()
    {
        try
        {
            // Memory metrics
            var gcMemory = GC.GetTotalMemory(false);
            var workingSet = _currentProcess.WorkingSet64;
            var privateMemory = _currentProcess.PrivateMemorySize64;

            // GC metrics
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);

            // CPU metrics
            var cpuTime = _currentProcess.TotalProcessorTime;
            var cpuUsage = _cpuCounter?.NextValue() ?? 0;

            // Thread metrics
            var threadCount = _currentProcess.Threads.Count;

            _logger.LogInformation(
                "Performance Metrics - " +
                "GC Memory: {GCMemory:N0} bytes, " +
                "Working Set: {WorkingSet:N0} bytes, " +
                "Private Memory: {PrivateMemory:N0} bytes, " +
                "GC Collections (Gen0/Gen1/Gen2): {Gen0}/{Gen1}/{Gen2}, " +
                "CPU Usage: {CpuUsage:F1}%, " +
                "CPU Time: {CpuTime}, " +
                "Thread Count: {ThreadCount}",
                gcMemory,
                workingSet,
                privateMemory,
                gen0Collections,
                gen1Collections,
                gen2Collections,
                cpuUsage,
                cpuTime,
                threadCount);

            // Log warnings for concerning metrics
            if (gcMemory > 500_000_000) // 500MB
            {
                _logger.LogWarning("High GC memory usage detected: {GCMemory:N0} bytes", gcMemory);
            }

            if (gen2Collections > 10)
            {
                _logger.LogWarning("High Gen2 GC collection count: {Gen2Collections}", gen2Collections);
            }

            if (threadCount > 100)
            {
                _logger.LogWarning("High thread count detected: {ThreadCount}", threadCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
        }
    }

    public override void Dispose()
    {
        _cpuCounter?.Dispose();
        _currentProcess?.Dispose();
        base.Dispose();
    }
}