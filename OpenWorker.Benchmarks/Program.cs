using BenchmarkDotNet.Running;
using OpenWorker.Benchmarks;

// The assembly of the benchmarks by one of its types: the servers it references have a Program of their own.
BenchmarkSwitcher.FromAssembly(typeof(BroadcastBenchmark).Assembly).Run(args);
