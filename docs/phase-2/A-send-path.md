# Phase 2, workstream A: the send path and the dispatcher

Scope: steps 1, 3, 7 and 8 of phase 2 in `REFACTORING.md` (branch `refactor/phase-2-send`), and the benchmarks that
measure them. The receive path (steps 4 and 5) is workstream B, `B-receive-path.md`; the `ref struct` writer and reader
and the generator (steps 2, 5, 6) are later work.

## Done

### Step 3. Encryption

`MessageEncryption.Exchange(Span<byte>, ushort version)` reads the key table as a `static ReadOnlySpan<byte>` over constant
data, takes the three keys of the version once (`version * 4`, the fourth byte of a version is not used) and steps by three,
with no division and no enumerator. From 48 bytes on, three `Vector128<byte>` cover a block of 48 bytes, a whole number of
periods of the key, so every block is XORed with the same three vectors; the keys of every version laid over a block are
built from the table once, at type initialisation. The bytes after the last block continue with the scalar loop, from the
first key again.

Measured on its own (BenchmarkDotNet, short job, same machine as below):

| Length | Before (`Enumerable.Range`, `IReadOnlyList<byte>`) | Scalar loop | Scalar + `Vector128` (kept) |
|---:|---:|---:|---:|
| 16 B | 26.2 ns, 40 B | 4.9 ns | 6.9 ns (scalar path) |
| 55 B | 75.9 ns, 40 B | 15.5 ns | 3.2 ns |
| 200 B | 251.9 ns, 40 B | 58.8 ns | 6.3 ns |
| 1500 B | 1 763 ns, 40 B | 437.7 ns | 28.1 ns |

Test: `MessageEncryptionTest` keeps the formula of the first version and the table as its oracle and compares every
version of the table and every length from 0 to 70 (the scalar loop alone, and one block with its tail).

### Step 1. One frame per message

- **`Frame`** (`OpenWorker.Hotspot/Frame.cs`): the header and the encrypted body in one array rented from
  `ArrayPool<byte>.Shared`, its length, and a reference count. `Frame.Encode(message)` returns it with one reference, the
  caller's; `SessionWriter.TryEnqueue(frame)` takes a reference for the queue and lets it go after the socket write, when
  it drops the frame (the queue is drained when the connection ends), or at once when the frame cannot be queued. The
  caller disposes its reference after queueing the frame to everyone. The last release returns the array to the pool it
  came from, exactly once, and the `Frame` object itself to a pool of frames (a `ConcurrentQueue`, at most 4096 kept), so a
  broadcast allocates nothing. A release below zero throws: it is a bug of our code.
- **`FrameEncoder`**: one `BinaryWriter` over a `FrameStream` that writes straight into the array of the frame, from just
  after the header. The stream starts with 256 bytes (a movement takes about 55) and grows by renting a larger array and
  copying what is written; then the body is encrypted in place and the header written in front. No second copy.
  `IResponseHotspotMessage.Write(BinaryWriter)` keeps its signature. The world loop owns one encoder
  (`WorldLoop.Encoder`, one thread, one encoder; created with the loop's logger and `SessionMetrics`); a thread other than
  the loop that sends (tests, off-loop code) gets a thread-static encoder that reports to nobody. A message that sends
  another one while it is written gets a temporary encoder.
- **Too long.** A body longer than `MessageHeader.MaxContentLength` makes the stream stop at the limit and drop the rest
  instead of throwing (`REFACTORING.md` 2.9.3, `Memory stream is not expandable` from `WorldOtherPersonListResponse`). The
  message is dropped with a Warning (`Dropped the message {Opcode}: ...`) and counted in `frames.dropped` (`SessionMetrics`,
  tag `opcode`, the name built only on a drop); `TrySend` returns `false` and the session stays open. The dashboard shows
  the drops on the panel of the frames sent.
- **Callers.** `ServerSessionComponent.Send/TrySend(IResponseHotspotMessage)` encode once and enqueue; `Send/TrySend(Frame)`
  enqueue a frame that is already encoded. `ServiceChannel.Broadcast` and `BroadcastToOthers`, `CommunityNetwork.Deliver`
  and `LuaCreatureManager.Broadcast` (the maze) encode once and queue the same frame to every recipient, and encode nothing
  when there is nobody to send to.
- **Writer.** `SessionWriter` queues `Frame`s instead of `(byte[], int)`, with the same bound, backpressure and close
  reasons. It reuses one `CancellationTokenSource` for the write timeout (`CancelAfter`, then `TryReset`; a source that the
  timer cancelled just as the write ended is replaced) instead of making a source and a timer per frame.
  `SessionMetrics` counts frames and bytes as before. A writer, and so a session, can stand on a `Stream` instead of a
  `TcpClient` (internal constructor, for the tests and the benchmarks).

Tests (`FrameTest`): a broadcast to five members of a channel writes the message once and every member gets the same bytes;
the array of a frame goes back once, after the last connection has written it or dropped it (a connection that writes, one
whose write is held back, one that is closed, with a pool that watches every array); a message one byte too long is dropped,
logged and counted while a message that fills a frame exactly and the next one are sent, and the session stays open.

### Step 7. The dispatcher

- The handler table gives every class of handler a place (`Handler.Instance`); the executor keeps the instance of a class
  there once the first packet of that class has resolved it, so later packets never ask the container. **Deviation from the
  plan:** the instances are not resolved when the table is built. The table holds the handlers of every loaded assembly, and
  some need services that the server at hand does not register: resolving them all at once made the container of
  `TheContainerBuildsTheExecutorWithTheBudgetOfTheConfiguration` (and, in a process that loads more assemblies than it
  registers services for, a server) fail to start. Resolving each class once, on its first packet, gives the same steady
  state and keeps the old behaviour of a handler that is not registered (the packet fails with `handler_fault`).
- A packet is read by a `RequestReader`: a `BinaryReader` over a read-only, seekable `PacketStream` whose bytes are set per
  packet and cleared when the handler returns (the read loop gives the buffer back to the pool then). A handler holds its
  reader until it returns, awaits included, and other packets run on the loop meanwhile, so the executor keeps one spare
  reader in a field (taken and given back with one atomic exchange) and a stack under a lock for packets that overlap.
- `HandlerDelegate` keeps `BinaryReader`. The error budget, the metrics and the log scope are as they were.

Test (`HandlerExecutorTest.APacketFindsItsHandlerOnceAndAllocatesNothingAfterwards`): 10 000 packets to a handler that
completes at once ask the container once and allocate less than one byte per packet on the thread
(`GC.GetAllocatedBytesForCurrentThread`); in practice zero, the margin is for what the runtime may allocate once.

### Step 8. Channel counters

`ServiceChannel` is a sealed class (a struct could not share a field between its copies; nothing compared channels by
value). Its members are a `Dictionary` touched only on the world loop, and `Online` is a field written with
`Volatile.Write` at every join and leave and read with `Volatile.Read`. The readers off the loop are
`ChannelLifecycleService` (the count it puts in Redis every second) and `ChannelBootstrapService` (the first record);
`PersonService` and `Workload` read on the loop. Test (`ServiceChannelTest`): two joins and a leave, read from the thread
pool as 2, then 1.

## Benchmarks

`OpenWorker.Benchmarks`, BenchmarkDotNet 0.15.8 with `MemoryDiagnoser` (it counts the allocations of every thread, so the
writer tasks are in the numbers). Run: `dotnet run -c Release --project OpenWorker.Benchmarks -- --filter '*'`. Neither
benchmark needs Redis, a database or the client tables.

- `BroadcastBenchmark.EncodeAndQueueToFifty`: one `MovementMoveBtResponse` (boxed once in the setup) encoded and queued
  to 50 sessions whose connection is `Stream.Null`; their writers send on their own tasks. The run fails if a session closed.
- `DispatchBenchmark.DispatchAMove`: one move request (the decrypted body, 48 bytes with the opcode) through the
  `HandlerExecutor` of a container (`AddHotspotHandlers`) to a stub handler that reads the request. The run fails if the
  move did not reach the handler.

Machine: AMD Ryzen 9 5950X (16 cores, 32 threads), CachyOS Linux, .NET SDK 10.0.403, runtime 10.0.14, x86-64-v3 RyuJIT,
BenchmarkDotNet default job, workstation GC (the default of a benchmark process).

- **Before:** `dev` at `1bdc141` (the receive path merged, nothing of this branch), in a scratch copy with one seam added
  so that a session can stand on a stream (the same seam this branch adds); the broadcast there is
  `foreach (session) session.Send(message)`, which is what `ServiceChannel.Broadcast` did.
- **After:** this branch at `557977f` (the code) with the benchmarks of `60df34b`.

| Benchmark | | Mean | Allocated | Gen0 / 1000 ops |
|---|---|---:|---:|---:|
| Broadcast to 50 | before | 40.20 us | 14.06 KB | 0.8545 |
| | after | 21.20 us | 0 B | 0 |
| Dispatch of a move | before | 112.2 ns | 104 B | 0.0062 |
| | after | 108.8 ns | 0 B | 0 |

Before, the broadcast made 50 encodes (a `MemoryStream`, a `BinaryWriter`, a 64 KB rent, a copy into a second rent) and
50 `CancellationTokenSource`s with timers in the writers; the 104 B of the dispatch were the stream and the `BinaryReader`
of the packet. The plan's target, 0 B per operation in steady state apart from the pooled frame, is met by both.

Encoding alone (`Frame.Encode` and `Dispose`, measured once with a third method that is not kept) takes 65 ns and 0 B, so
almost all of the 21 us of the broadcast is the queueing: about 420 ns per connection, most of it waking the write task of
each connection through its channel and the thread pool. See below.

## Not done, left open

- **Steps 2, 5, 6** (`ref struct` writer and reader over `Span<byte>`, the generator of `Read`/`Write`): the 499 `Write`
  methods still go through `BinaryWriter` and its virtual calls, and `HandlerDelegate` takes a `BinaryReader`.
- **Waking the writers.** One write task per connection and one wake-up per frame make the queueing of a broadcast cost
  far more than the encode. Coalescing (a writer that drains everything queued into one `WriteAsync`, or a send loop shared
  by many connections) is the next measurable win on the send path; with interest management (phase 3) fewer recipients
  per frame.
- **Handlers that box their response.** `MovementServiceBase` passes a struct response as `IResponseHotspotMessage`, one box
  per move; the benchmark boxes once in its setup. A generic `Broadcast<T>(in T) where T : IResponseHotspotMessage` or the
  writer of step 2 removes it.
- **The pool of frames** keeps at most 4096 `Frame` objects; the arrays follow the limits of `ArrayPool<byte>.Shared`, so
  a process with more frames in flight than that rents and collects some. Not measured under the load test yet.
- **Load test.** The criterion "p99 of the answer to a move and CPU per bot below the baseline" needs a run of
  `eng/loadtest.sh`, which this branch has not done.
