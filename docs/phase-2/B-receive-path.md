# Phase 2, workstream B: the receive path

Scope: steps 4 and 5 of phase 2 in `REFACTORING.md` (branch `refactor/phase-2-receive`). The send path, the encryption
and the dispatcher (`HandlerExecutor`) belong to another workstream and are not touched here.

## Done

### Step 4. Receive through `System.IO.Pipelines`

`ServerService.SessionLoopAsync` no longer rents `MessageHeader.MaxContentLength` (64 KB) from `MemoryPool` for the life
of a connection, and no longer reads a packet with two `ReadExactlyAsync` calls.

- **Pipe.** `PipeReader.Create(socket.GetStream(), ReaderOptions)` with `useZeroByteReads: true` and `leaveOpen: true`
  (the socket is closed by the session, as before). With zero-byte reads the pipe waits for data without a buffer, so a
  connection that sends nothing holds none; the pipe's segments (4 KB, from the shared pool) exist only while there are
  unread bytes. The pipe is completed when the loop ends, which returns its segments.
- **Framing** (`ReadFramesAsync`, `TryReadFrame`). The five bytes of the header are parsed from the
  `ReadOnlySequence<byte>` as soon as they are there; a frame whose body has not fully arrived waits for more data; every
  whole frame of one read is handled, in order. `AdvanceTo(consumed, examined)` takes the frames handled and marks the
  whole buffer examined, so the next read waits for new bytes. A frame cut anywhere (byte by byte, the header apart from
  the body, a frame that starts at the end of a read) is handled once.
- **Buffer per packet** (`HandleFrameAsync`). The body is copied into an array rented from `ArrayPool<byte>.Shared` for
  the size of the frame (the shared pool is bucketed, so the steady state allocates no array), decrypted in place with
  `MessageEncryption.Exchange`, counted (`metrics.Received(header + body)`), dumped (`DumpPacket`, Debug builds,
  `Hotspot:PacketDumpPath`), posted to the world loop exactly as before
  (`loop.InvokeAsync(() => executor.ExecuteAsync(buffer, entity, scope, cancellationToken))`) and awaited, so the next
  frame is handled only after the handler finished. Then the array goes back to the pool. A handler must not keep the
  buffer after it returns, which was true before too (the 64 KB buffer was reused for the next packet).
- **Cancellation.** `ReadAsync` takes `reading`, which the drain cancels; frames of one read are not handled once
  `reading` is cancelled, so the frame being handled is finished and the next one is not started, as before. The
  handler gets the host's `cancellationToken`, as before.
- **Close reasons.** End of the stream: `Client`, with the Debug line "Connection closed: the client closed the
  connection." (or "... inside a frame."); no exception is thrown for it any more. A socket error or a disposed socket:
  `Client`. Cancellation: `Shutdown` when `reading` is cancelled, `Server` otherwise. The drain between two frames:
  `Shutdown`. Any other failure: `Server` with the Critical line, as before.
- **Malformed header.** A header that fails `MessageHeader`'s own validation ends the connection at once with
  `SessionCloseReason.Malformed` and a Warning ("The client sent a header that cannot be right, and is cut off"), without
  reading the length it claims. After a bad header the next frame cannot be found, so there is nothing to count against
  the error budget: the session closes as one that ran out of it would. `MessageHeader` checks:
  - a length smaller than the header (as before; before this branch it failed the session loop as Critical, reason
    `Server`);
  - new: a version past the key table (`MessageHeader.MaxVersion`, 24: four bytes of the 100-byte table per version,
    three used). Before, such a version made `MessageEncryption.Exchange` throw `ArgumentOutOfRangeException` after the
    body was read, again as Critical. The client sends version 0 (every dump in `OpenWorker.Hotspot.Test/Datas`), the
    server sends 5.

  The length field is 16 bits and counts the header, so a header cannot claim more than `MaxContentLength`; the largest
  claim (`0xFFFF`) together with a bad version is what the test sends.

#### Memory per idle connection

1000 clients connected to a `ServerService` on a `ServerHarness` (the real host of `AddHotspot`, auth registrations,
database in memory), Release, `GC.GetTotalMemory(true)` after the 1000 sessions exist minus before they connected, divided
by 1000. The `TcpClient` objects of the clients are created before the first measurement. Three runs each:

| | Bytes per idle connection |
|---|---|
| Before (`MemoryPool` 64 KB per connection) | 69 716, 69 720, 69 729 |
| After (pipe with zero-byte reads) | 4 793, 4 795, 4 800 |

About 69.7 KB to about 4.8 KB: 10 000 idle connections hold about 48 MB instead of about 700 MB. What is left is the
session (entity, `ServerSessionComponent`, `SessionWriter` and its channel, the log scope), the socket and its
`NetworkStream`, the pipe object and the task of the read loop. The measurement was a temporary test, not committed.

#### Tests

`OpenWorker.Hotspot.Test/ServerServiceReceiveTest.cs`, against the real `ServerService` over TCP with `ProbeHandler`
(which now records what a request of the new mode `ProbeRequest.Records` carried after the mode):

- `AFrameCutAnywhereIsHandledOnceWhenItsLastByteComes`: one frame byte by byte, one frame header then body (3000 bytes);
  each is handled once and carries exactly the bytes sent.
- `FramesThatArriveInOneReadAreAllHandledInOrder`: two frames and the start of a third in one write, then the rest of
  the third.
- `AHeaderThatCannotBeRightEndsTheConnectionAtOnceAsMalformed`: a bad version with the largest length, and a length
  shorter than the header; the client sees the end of the stream within the timeout and `sessions.closed` counts both
  under `malformed`.

The socket tests of the server share one host, `ServerServiceHost` (taken out of `ServerServiceMetricsTest`).

### Step 5. Lengths from the network are checked

In `OpenWorker.Extensions/BinaryReaderExtension.cs` every reader that takes a count or a length checks it before it
allocates or reads (`CheckLength`): a negative count, or one that needs more bytes than the stream still has
(`BaseStream.Length - BaseStream.Position`), is refused with `InvalidDataException` whose message names the reader and
the count, for example `ReadInt32AsArray: the length 2147483647 needs 8589934588 bytes and 4 are left.` The executor
classifies `InvalidDataException` as `Malformed` and counts it against the error budget (`HandlerErrorBudget.Classify`,
unchanged).

- Arrays: `ReadInt32AsArray`, `ReadUInt16AsArray`, `ReadByteAsArray`, `ReadSingleAsArray`. Plain loops over an array of
  the right size replace `Enumerable.Repeat(...).Select(...).ToArray()`; `ReadByteAsArray` reads the bytes at once.
- `ReadInt32AsEnumerable`, `ReadUInt16AsEnumerable`, `ReadByteAsEnumerable` return the checked arrays: they were lazy, so
  a count was never checked and the elements were read whenever someone enumerated them. Nothing calls them today.
- Strings without a terminator: `ReadAsciiStringWithoutTerminator(length)`, `ReadUtf8AsciiStringWithoutTerminator`,
  `ReadUtf8UnicodeStringWithoutTerminator`, `ReadUtf16UnicodeStringWithoutTerminator` (a count of characters, two bytes
  each). A zero length is an empty string, as before; a short packet used to give a shorter string silently.
- Strings with a terminator: `ReadUtf8UnicodeString`, `ReadUtf16UnicodeString` (commit 020471a refused a length that
  cannot hold the terminator). They now also refuse a length past the end with `InvalidDataException` instead of
  `EndOfStreamException`; both count as malformed.
- A stream that cannot seek is checked for a negative count only and ends early, as before, on a count it cannot fill.
  Every stream that reaches these readers today is seekable: the executor's `MemoryStream` over the packet, the tests,
  and the client tables (`File.ReadAllBytes` into a `MemoryStream`).

`OpenWorker.Havok/Extensions/BinaryReaderExtensions.cs` reads navigation meshes from files only, never network data, and
is not changed.

Test: `BinaryReaderExtensionTest.ALengthPastTheEndOfThePacketIsRefusedBeforeAnythingIsAllocated` reads a count of
`0x7FFFFFFF` and asks `ReadInt32AsArray` for it: `InvalidDataException` naming the reader and the count, less than
64 KB allocated on the thread (`GC.GetAllocatedBytesForCurrentThread`), and nothing read past the count.
`AStringThatTheStreamCannotFillIsAnEarlyEnd` became `AStringThatTheStreamCannotFillIsInvalidData`.

The wire tests, the dumps of `OpenWorker.Hotspot.Test/Datas` and the readers of the client tables pass unchanged with
every `OPENWORKER_TEST_*` variable set (1030 of 1030).

## Verification

- `dotnet build OpenWorker.slnx -c Release --no-incremental`: 0 errors, 196 warnings (as on `dev`).
- Tests with Redis only: 1030 total, 941 passed, 89 skipped for the data variables, 0 failed; `eng/check-trx.py` exit 0.
  With every data variable set: 1030 of 1030 passed.
- `bash eng/check-forbidden.sh`: no forbidden patterns.
- Smoke on the host: the Release auth server (`ConnectionStrings__Redis` on a private Redis, a throw-away Postgres
  database, `Health__Port=18092`) got the login dump `0x02,0x01.bin` re-encrypted with its own version, in three pieces
  (three bytes of the header, then up to the middle, then the rest, 300 ms apart): one `LoginResponse` came back. The same
  frame twice in one write: two responses. A header with version `0xFFFF`: the connection closed within a millisecond
  with the Warning of a malformed header.

## Left

- The `ref struct PacketReader` over `ReadOnlySpan<byte>` (step 5 proper) and the source generator of `Read`/`Write`
  (step 6) are the next branch. Until then the executor still wraps the rented buffer in a `MemoryStream` and a
  `BinaryReader` per packet, and the string readers allocate a `byte[]` before the string.
- The closure and the work item of `loop.InvokeAsync` per packet remain (the dispatcher's workstream).
- `ReadUtf16UnicodeString` drops one byte of its two-byte terminator, so the decoded string ends with U+FFFD; the wire
  format is kept as it was (the existing test only checks the start of the string). The fix belongs to the new reader.
- Readers of counts outside `OpenWorker.Extensions` (for example `ReadStorageGroupList` in
  `Hotspot/Modules/Items/Extensions`) take their count from the caller and still use LINQ; they move to the new reader.
