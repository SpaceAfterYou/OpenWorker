# Phase 2, workstream C: the wire format

Scope: steps 2, 5 and 6 of phase 2 in `REFACTORING.md` (branch `refactor/phase-2-wire`): a `ref struct` writer over
`Span<byte>`, a `ref struct` reader over `ReadOnlySpan<byte>`, and a source generator that writes both for a message from
its fields. The send path (`Frame`, `FrameEncoder`) and the receive path (`PipeReader`, `HandlerExecutor`) are the ones of
workstreams A and B; this branch adds the second path through each of them.

## The reader and the writer

`OpenWorker.Wire` is a project with no dependencies, under `OpenWorker.Domain`, so the values of the domain
(`ActorValue`, `ActorComponent`, `SessionValue`) carry their format too.

- **`PacketReader`** reads little endian from a `ReadOnlySpan<byte>`. Every read checks what is left: a value past the end
  is `EndOfStreamException`, as `BinaryReader` gives; a length or a count that the packet cannot hold is
  `InvalidDataException` (`ReadLength(prefix, size)` checks `count * size` against what is left), refused before anything
  is allocated for it. Both are counted as malformed by the error budget (`HandlerErrorBudget.Classify`, unchanged). The
  readers of `BinaryReaderExtension` have their counterparts with the same names and layouts: the strings
  (`ReadUtf8UnicodeString`, `ReadUtf16UnicodeString`, `ReadUtf8AsciiStringWithoutTerminator`, ...), the arrays
  (`ReadInt32Array`, `ReadInt16Array`, `ReadSingleArray`, `ReadByteArray`, and `ReadBytes`, a slice without a copy),
  `ReadTimeInSeconds32`, plus `ReadVector3`, `ReadDateTime`, `ReadDateTimeOffset`. One fix: `ReadUtf16UnicodeString`
  drops the whole two-byte terminator; the reader of `BinaryReader` dropped one byte and left the other in the string as a
  U+FFFD (`B-receive-path.md`, "Left"). The bytes it reads are the same.
- **`PacketWriter`** writes little endian into a `Span<byte>`, or into an array of an `ArrayPool<byte>` that it grows by
  renting a larger one, up to a limit. It never throws for a lack of room: the write that does not fit is dropped,
  `Overflowed` says so, and the writer keeps the message cut where it was cut (nothing after the first write that did not
  fit is written, even a smaller one), so a caller checks once at the end. A pool may hand out a larger array than asked;
  what lies past the limit is not room. Every method has the layout of the helper of `BinaryWriter` with the same name, quirks
  included: `WriteUtf16UnicodeString` writes the length `2 × characters + 1`, the UTF-16 bytes and one zero byte, as before.
  A missing string or list is written as an empty one. `WriteLength(prefix, count)` writes the count of a list and throws
  when the prefix cannot hold it (`checked`), as the `checked` casts of the hand-written writers did; the unchecked ones
  sent a wrong count with all the elements, which the client misreads.
- **`IWireReadable<T>`** (`static abstract T Read(ref PacketReader)`) and **`IWireWritable`** (`Write(ref PacketWriter)`)
  are what a value with a span format implements, generated or by hand.
- **`WireBridge`** carries such a value to code that still holds a `BinaryReader` (it reads from the bytes left in the
  stream and moves the stream past what it read) or a `BinaryWriter` (it writes through a pooled `PacketWriter` and hands
  the bytes over). The generator calls it from the constructor and the `Write(BinaryWriter)` it writes, so both paths go
  through the same generated code; the wire tests, which call `new XRequest(reader)` and `message.Write(writer)`, test the
  generated code unchanged.

## The generator

`OpenWorker.Wire.SourceGenerators` is an incremental generator (`ForAttributeWithMetadataName`), referenced by
`OpenWorker.Hotspot` as an analyzer, under the analyzers of `Directory.Build.props` and with warnings as errors. For a
`partial` struct marked **`[WireMessage]`** it writes:

- `private T(ref PacketReader reader)`, which reads the fields in order, and `public static T Read(ref PacketReader)`;
  for a positional record, `Read` reads the fields into locals and calls the primary constructor;
- `public T(BinaryReader reader)`, the bridge for code that still reads that way;
- `public void Write(ref PacketWriter writer)` and the bridge `public void Write(BinaryWriter writer)`;
- the interfaces `IWireReadable<T>` and `IWireWritable`.

A request (it implements `IRequestHotspotMessage`) is read, a response (`IResponseHotspotMessage` or `IWritableData`)
is written, any other value both; `[WireMessage(WireMethods.Write)]` and the other values of `WireMethods` say otherwise.

**The fields** are the public instance auto-properties (those with a field the compiler made), in the order they are
declared, which is the order on the wire; a computed property such as `Opcode => new(Group, Command)` is not a field, and
`[WireIgnore]` leaves one out. A positional record has its parameters as fields. The type of a field says how it goes:

| Type | On the wire |
|---|---|
| `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double` | itself, little endian |
| `bool` | one byte, 1 or 0; read as not zero |
| an enum | its underlying type |
| `Vector3` | three floats |
| `DateTime`, `DateTimeOffset` | 64-bit seconds since 1970 (a `DateTime` that is not UTC is local, as before) |
| `TimeSpan` | 32-bit whole seconds |
| a struct with `[WireMessage]` or `IWireReadable<T>`/`IWireWritable` | its own `Read`/`Write` |
| `string` | needs `[WireString(WireStringFormat.X)]`: the format of the helper of the same name |
| `T[]`, `List<T>`, `IList<T>`, `IReadOnlyList<T>`, `ICollection<T>`, `IReadOnlyCollection<T>` | needs `[WireArray(WireLength.X)]` (a count in `Byte`, `Signed16`, `Unsigned16` or `Signed32`, then the elements) or `[WireArray(Count = n)]` (no count; always `n` elements, those past it not sent, the missing ones sent as default values, as `MovementTraceBt` pads its fuzzy values); `ElementString` and `ElementAs` stand for the attributes an element cannot carry |

**`[WireAs(typeof(byte))]`** gives a field another width: an enum the client reads wider than it is declared
(`ChatMessageAppearance`, a byte enum, goes as an `int` in a response), a flag sent as an `int` (`bool Gazing` is
`Gazing ? 1 : 0`), a number kept wider than the client sends it (`int bPlayMotion` read from one byte), a value with
conversions to and from a number.

A field the generator cannot place, a struct that is not `partial`, a struct that writes its own bridge (`Write(BinaryWriter)`
or a constructor that takes a `BinaryReader`) or has a primary constructor, is a compile error: `OWW001` to `OWW004`, at
the field, with what is missing (`The field 'Warp' of 'WorldWarpMazeResponse' has no wire format: 'WarpValue' needs
[WireMessage] or to implement IWireWritable to be a field`).

Example, `MovementTraceBtResponse` (the hand-written `Write` walked its lists and padded two of them; now it is declared):

```csharp
[WireMessage(WireMethods.Write)]
public readonly partial record struct MovementTraceAggro(int Actor, float Value);

[WireMessage]
[HotspotMessage(Group, Command, HotspotMessageDirection.Response)]
public readonly partial struct MovementTraceBtResponse : IResponseHotspotMessage
{
    public const int FuzzyCount = 4;
    public const int ActionCount = 7;

    public MessageOpcode Opcode => new(Group, Command);

    public ActorValue Actor { get; init; }
    public Vector3 Position { get; init; }
    public float Yaw { get; init; }
    public float Pitch { get; init; }
    public int Animation { get; init; }
    public byte DefenseType { get; init; }
    [WireArray(WireLength.Byte)]
    public IReadOnlyCollection<Vector3> Projectiles { get; init; }
    public byte AiState { get; init; }
    [WireArray(Count = FuzzyCount)]
    public IReadOnlyList<float> Fuzzy { get; init; }
    [WireArray(Count = ActionCount)]
    public IReadOnlyList<int> Actions { get; init; }
    public float MoveX { get; init; }
    public float MoveY { get; init; }
    public ushort WayPoint { get; init; }
    [WireArray(WireLength.Byte)]
    public IReadOnlyCollection<MovementTraceAggro> Aggro { get; init; }
}
```

A message whose layout is not declarative keeps its hand-written implementation and the generator is not forced onto it:
a field present or not by a flag, a list whose count is sent somewhere else, a loop with state, constants sent where no
property stands (`WorldEnter` sends two `false`), a value of the clock (`WorldCurrentDateClientMessage`). A value whose
order on the wire is not the order of its properties, or that writes only a part of its elements, implements
`IWireWritable` by hand: `PersonValue` (its worn items go by their identifiers alone), `EquipItemValueEntry` (the upgrade
before the item), and the values packed into one number (`AppearanceValue`, `AbilityValueEntry`, `TitleValue`, ...).

## The two paths

They live side by side until every message has moved.

- **Receive.** `HandlerBuilder` compiles, for a request that implements `IWireReadable<T>`, a second shape next to
  `HandlerDelegate`: the call of the handler takes the parsed request, and `PacketHandler<T>.Create` wraps it into a
  `PacketHandlerDelegate(instance, player, ReadOnlySpan<byte> body, cancellationToken)` that parses the request from the
  span, whole, before the handler runs; a `ref struct` cannot cross an `await`, the parsed struct can. `HandlerExecutor`
  reads the opcode from the span of the packet and takes `Handler.Packet` when there is one; the other requests read from
  the reused `BinaryReader` as before (it is rented only for them now). `HandlerDelegate` is unchanged; a request on the new
  path keeps one too, over its bridge constructor.
- **Send.** `FrameEncoder.Encode` writes a response that implements `IWireWritable` straight into the array of the frame
  through a `PacketWriter` over the pool, from just after the header, with the limit of a frame; the writer is a local, so
  a message written while another one is needs no encoder of its own. An overflow drops the message as the other path does
  (warning `Dropped the message {Opcode}`, `frames.dropped`). The other responses go through the `BinaryWriter` over
  `FrameStream`. Both then encrypt in place and write the header (`Seal`).
- The writers of `BinaryWriterExtension` for the values that moved (`PersonValue`, `WorldValue`, `LeagueValue`, ...) go
  through `WireBridge`, so a message that still writes through `BinaryWriter` and holds such a value sends the bytes the
  generated code writes: one declaration of the layout.

## Verification

- **The wire tests and the dumps** pass unchanged: they read through the bridge constructors and write through the bridge
  `Write(BinaryWriter)`, which run the generated code.
- **A comparison against `dev`.** A scratch tool (not committed) loads `OpenWorker.Hotspot`, fills every response with
  deterministic random values (12 seeds; strings with and without non-ASCII characters, lists of 0 to 4 elements, arrays and
  lists for the interfaces) and writes it, and reads every request that has a `BinaryReader` constructor from 72 packets
  (zeros, repeated patterns, random bytes, small counts, every length from 0 to 47), recording the bytes, the values read,
  the position reached or the exception. The same run against `dev` at `2d42f46` and against this branch after each module
  compares 680 message types (the empty requests have a reader now, through the bridge) and finds no difference in any byte
  written or any value read. The only differences are the exception of two requests on a packet cut short:
  `GestureSlotUpdate` (24 packets, `EndOfStreamException` where it was `InvalidDataException`) and `SkillPreTargetList`
  (43 packets, `InvalidDataException` before the actors where it was `EndOfStreamException` after some); both are malformed
  for the error budget. 4 more types are left out because a read of the old code allocates for a hostile count until it
  times out (see "Left"). The tool lives with the notes of this branch, not in the repository.
- **New tests** (four): `PacketReaderTest.AHostileLengthIsRefusedBeforeAnythingIsAllocated` (a count of 2^31 for an array
  and for a list prefix: `InvalidDataException`, less than 64 KB allocated; a string longer than the packet; a value past
  the end); `PacketWriterTest.AWritePastTheLimitIsRecordedAndNothingAfterItIsWritten` (a fixed span and a pooled array that
  grows to its limit); `FrameTest.AWireMessageLongerThanAFrameIsDroppedAndTheOthersGoOut` (the new path drops a message one
  byte too long, counts and logs it, sends one that fills a frame, and gives the same frame as the old path for the same
  body); `HandlerExecutorTest.ARequestWithASpanFormatIsParsedFromThePacketAndAHostileCountIsMalformed`. The generated code of
  a representative message is covered by its existing wire test (`MovementWireFormatTest`) through the bridges.

## Migrated

405 of the 686 messages of `OpenWorker.Hotspot` (276 requests, 410 responses; `Messages` is the folder
`OpenWorker.Hotspot/Messages`) read or write through the new path: 209 requests and 196 responses. One commit per module.

| Module | Requests (new / all) | Responses (new / all) |
|---|---:|---:|
| Boosters | 0 / 0 | 3 / 4 |
| Channels | 2 / 2 | 0 / 2 |
| Chat | 5 / 5 | 7 / 7 |
| DailyMissions | 2 / 2 | 2 / 4 |
| Drops | 0 / 1 | 0 / 3 |
| Events | 2 / 2 | 1 / 6 |
| Friends | 14 / 14 | 5 / 17 |
| Gestures | 2 / 2 | 0 / 2 |
| Helpers | 10 / 10 | 4 / 14 |
| ItemSetup | 38 / 38 | 0 / 0 |
| Items | 7 / 13 | 25 / 38 |
| League | 16 / 20 | 14 / 36 |
| Login | 3 / 6 | 1 / 5 |
| Maze | 13 / 13 | 12 / 20 |
| Messages | 0 / 0 | 0 / 6 |
| Monster | 0 / 0 | 7 / 8 |
| Movement | 15 / 15 | 22 / 22 |
| Party | 23 / 35 | 7 / 34 |
| Persons | 16 / 18 | 20 / 38 |
| Post | 0 / 15 | 10 / 17 |
| Quests | 6 / 6 | 6 / 11 |
| Ranking | 3 / 3 | 0 / 3 |
| Shop | 4 / 10 | 14 / 17 |
| Skill | 16 / 21 | 0 / 45 |
| SoulMetry | 1 / 1 | 1 / 6 |
| System | 2 / 4 | 0 / 3 |
| Trade | 0 / 11 | 12 / 15 |
| Vaccum | 2 / 2 | 4 / 6 |
| World | 7 / 7 | 19 / 21 |
| **All** | 209 / 276 | 196 / 410 |

- **The hot path first.** Every request and response of `Modules/Movement` (the move broadcast `MovementMoveBt` among them);
  the entry into a map and the persons on it (`WorldInInfoPc`, `WorldOutInfoPc`, `WorldOtherPersonList`, with
  `PersonValue` and its values); the whole chat; the use of a skill (`SkillActiveSkill`), the ping and the keep-alive, the
  broadcasts of the monsters.
- **Then** every module whose messages are read or written field after field, migrated by a script that rewrites a request
  whose properties are initialized by reads, or a response whose `Write` writes every property once in declaration order
  with a write the generator does the same way; the generator then refuses what it cannot place (`OWW002`), and those files
  were put back.
- **Left, and why.** A response that holds a value without a format yet (`MoveItemValue`, `PostValue`, `ItemValue`,
  `LeagueMember`, `FriendValue`, `PrivateShopItemValue`, `EnterMapResultValue`, `ST_STAT`, ...; each is a small step: give it
  `[WireMessage]` or implement `IWireWritable`); a writer that walks a list with a count written by hand, a `Debug.Assert`, a
  constant sent where no property stands, or values computed on the way (`WorldEnter`, `WorldOtherInfosMonster`, the
  current date); the 45 responses of `Modules/Skill`, which also read themselves from a `BinaryReader` (a primary
  constructor), so both directions need checking against each other before they move; requests whose lists use a reader
  of a module (`ReadCashItemList`, `ReadStorageGroupList`, `ReadSkillDeckValueList`, which clips its count) or a fixed
  string (`ReadFixedString`).

## Benchmarks

`OpenWorker.Benchmarks/WireBenchmark.cs` (BenchmarkDotNet 0.15.8, `MemoryDiagnoser`, default job). Machine: AMD Ryzen 9
5950X (16 cores, 32 threads), CachyOS Linux, .NET SDK 10.0.403, runtime 10.0.14, x86-64-v3 RyuJIT, workstation GC. Code
at `f3102e7` (movement, world and chat migrated), benchmarks of `bbd3797`. Run:
`dotnet run -c Release --project OpenWorker.Benchmarks -- --filter '*WireBenchmark*'`.

- *Parse a movement request*: the 46 bytes of a move after the opcode, read by a `BinaryReader` over a reused
  `MemoryStream` with the constructor the request had before (`LegacyMoveRequest`, kept in the benchmark), and by
  `PacketReader` with the generated `MovementMoveRequest.Read`.
- *Encode a movement broadcast*: `Frame.Encode` and `Dispose` of the move broadcast, through the `BinaryWriter` of the encoder
  (`LegacyMoveBroadcast`, the hand-written `Write` it had) and through `PacketWriter` (the generated one): opcode, body,
  encryption, header. The setup fails the run if the two frames or the two requests differ.

| Benchmark | Old path | New path | Allocated |
|---|---:|---:|---:|
| Parse a movement request | 30.47 ns (`BinaryReader`) | 2.25 ns (`PacketReader`) | 0 B, 0 B |
| Encode a movement broadcast | 94.31 ns (`BinaryWriter`) | 51.75 ns (`PacketWriter`) | 0 B, 0 B |

The two benchmarks of `A-send-path.md`, run in the same session with the movement on the new path:

| Benchmark | `A-send-path.md` (`557977f`) | This branch | Allocated |
|---|---:|---:|---:|
| `DispatchBenchmark.DispatchAMove` (opcode, table, parse, stub handler) | 108.8 ns | 41.9 ns | 0 B |
| `BroadcastBenchmark.EncodeAndQueueToFifty` | 21.2 us | 24.5 us | 0 B |

The dispatch of a move is 2.6 times faster: no reader is rented, no stream is set, the request is parsed from the span.
The broadcast is dominated by the queueing to 50 writers (about 420 ns each, `A-send-path.md`), which this branch does not
touch; the encode inside it is 40 ns shorter, within the noise of the queueing between two sessions.

## Left

- **The other 281 messages.** The list above; the generator takes most of them once the values they hold have a format.
  When every message has moved, `IResponseHotspotMessage` no longer needs `IWritableData`, the bridges, `HandlerDelegate`,
  `RequestReader`, `FrameStream`, `BinaryReaderExtension` and `BinaryWriterExtension` go, and `HandlerDelegate` becomes
  `PacketHandlerDelegate` alone (step 7 of the plan).
- **Boxing.** A handler still passes a struct response as `IResponseHotspotMessage`; a generic `Frame.Encode<T>(in T)` with
  `T : IWireWritable` would remove the box per message.
- **Behaviour that changed on the new path**, all for data that was already wrong: a list longer than its prefix throws
  (it was a wrong count with every element); a count past the end of the packet is `InvalidDataException` before anything
  is read (the old LINQ readers allocated the claimed count or read until `EndOfStreamException`); a fixed list read element
  by element ends as `EndOfStreamException` where `ReadInt32AsArray` said `InvalidDataException`; a missing string or list
  is written as empty instead of throwing `NullReferenceException`; `ReadUtf16UnicodeString` no longer leaves a U+FFFD.
- **Hostile counts in the old readers.** The comparison tool timed out on four messages because a reader of the old path
  allocates the count the packet claims (`Enumerable.Range(0, count).Select(...).ToArray()` preallocates): the requests
  `ShopCashBuy` (`ReadCashItemList`) and `ShopCashGift`, and the responses `PartyAwaiterList` and `PartyRecruitApplyInfo`
  (those are never read by the server). The two requests are an allocation the client can trigger; moving them to the
  generator closes it.
