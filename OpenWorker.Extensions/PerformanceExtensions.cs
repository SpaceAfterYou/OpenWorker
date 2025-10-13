using System.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OpenWorker.Extensions;

public static class PerformanceExtensions
{
    /// <summary>
    /// Adds performance optimizations to the service collection
    /// </summary>
    public static IServiceCollection AddPerformanceOptimizations(this IServiceCollection services)
    {
        // Configure GC settings for server workloads
        GCSettings.LatencyMode = GCLatencyMode.Batch;
        
        // Add performance monitoring
        services.AddHostedService<PerformanceMonitoringService>();
        
        return services;
    }

    /// <summary>
    /// Configures runtime optimizations
    /// </summary>
    public static void ConfigureRuntimeOptimizations()
    {
        // Enable server GC if not already configured
        if (!GCSettings.IsServerGC)
        {
            // This can only be set via configuration or environment variables
            Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
        }

        // Configure thread pool for high-throughput scenarios
        ThreadPool.SetMinThreads(
            workerThreads: Environment.ProcessorCount * 4,
            completionPortThreads: Environment.ProcessorCount * 2
        );

        // Set process priority for better responsiveness
        try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        }
        catch
        {
            // Ignore if we don't have permissions
        }
    }

    /// <summary>
    /// Optimizes memory allocation patterns
    /// </summary>
    public static void OptimizeMemoryAllocation()
    {
        // Pre-allocate some memory to reduce initial allocation overhead
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Configure large object heap compaction
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
    }
}