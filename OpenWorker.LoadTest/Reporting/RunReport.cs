using OpenWorker.LoadTest.Monitoring;
using OpenWorker.LoadTest.Stats;

namespace OpenWorker.LoadTest.Reporting;

internal sealed record ReportMachine(string Os, string Framework, int Cores);

internal sealed record ReportBots(
    int Requested,
    long Started,
    long ReachedTheWorld,
    long ActiveAtEnd,
    long PeakActive,
    long FailedToEnter,
    long Lost,
    int MazeBots,
    long InMazeAtEnd);

internal sealed record ReportStage(string Name, long Succeeded, long Failed, LatencySummary Duration);

internal sealed record ReportError(string Kind, long Count, string Example);

internal sealed record ReportTraffic(
    long MovesSent,
    long PingsSent,
    long ChatsSent,
    long SkillsSent,
    long SkillsAnswered,
    long FramesReceived,
    long MovesSentWhileMeasured,
    double MovesPerSecond,
    double DeliveriesPerSecond,
    double AverageListeners,
    long Unmatched);

internal sealed record ReportSelf(
    double CpuAverageCores,
    double CpuPeakCores,
    double WorkingSetMaxMegabytes,
    long GcGen0,
    long GcGen1,
    long GcGen2,
    double GcPauseMilliseconds,
    int ThreadsMax,
    long PendingWorkMax);

internal sealed record ReportServer(
    string Name,
    string Source,
    int Samples,
    double CpuAveragePercent,
    double CpuMaxPercent,
    double MemoryAverageMegabytes,
    double MemoryMaxMegabytes);

internal sealed record TimelinePoint(double Seconds, string Phase, long Active, long Entering, long Failed, long MovesSent, double BotCpuCores);

/// <summary>
/// What a run found, ready to be written as Markdown or JSON. Latencies are in milliseconds.
/// </summary>
internal sealed record RunReport(
    string Tool,
    DateTimeOffset StartedAt,
    double TotalSeconds,
    double MeasuredSeconds,
    bool Interrupted,
    ReportMachine Machine,
    IReadOnlyDictionary<string, string> Options,
    ReportBots Bots,
    IReadOnlyList<ReportStage> Stages,
    IReadOnlyDictionary<string, LatencySummary> Latency,
    ReportTraffic Traffic,
    IReadOnlyList<ReportError> Errors,
    ReportSelf Self,
    IReadOnlyList<ReportServer> Servers,
    PrometheusReport? Prometheus,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Notes,
    IReadOnlyList<TimelinePoint> Timeline);
