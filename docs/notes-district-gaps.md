# Transport between the stations of District 6, and the trade password in trades and private shops

Branch `feature/district-gaps` (round 7, 2026-11-10). Closes the two gaps `notes-npc-services.md` ("Transport") and
`notes-session-gaps.md` ("Not done") left open. Research data (the functions of the original, addresses, the stored procedures) is in
the git-ignored `docs/notes-district-gaps-research.md`; scripts and outputs in `docs/agents/feature-district-gaps/`.

## What the audit found first

**The rides are not in the cities.** The five NPC rows of the client with the transport function (`tb_NPC`: 101205, 101214, 101215,
101216, 101217; their function id is the row of `tb_District_Transport`, 1 to 4) are placed only by the map `D01_DISTRICT6`, which is the
row 30021 of `tb_Maze_Info` (type 5, "District 6 as a maze") and is entered by the portal of the Rocco map. The four stations of the towns
are its common position boxes 5901 to 5904, and the paths the client plays (`Scripts/Transport`) run across that one map. So the ride is a
matter of the **maze server**, with the townsfolk of a maze, and the district server has nothing to carry. The map has 25 boxes of townsfolk and
103 of monsters; entering it works today (the first sector spawns all of them), and the missing script of the map is no obstacle (the maze
logs that it runs without one). The `tb_District_Transport`
column `District` (10003, 10021, 10031, 10041) names the four town maps, not District 6.

## Transport (05/16, 05/17)

A player asks the NPC of transports for a ride (`05/16`: `int actor, short row, int npc`; the client writes the actor it knows the NPC by,
the kind of actor in its three high bits). The client has no request to end a ride (there is a `05/17` request in the opcode table, but
the client has no sender), so the server ends it. `TransportService` (`OpenWorker.MazeServer`) does, in this order:

| Step | Rule | Refusal (`05/96`, a `u16` code) |
|---|---|---|
| Who asks | the actor is the player's own, entered through this connection | counts against the error budget, no answer |
| On a ride already | a rider takes no second ride | 58000 |
| The NPC | `ServiceNpcs.Find` with the function 8: an NPC of this maze, 1000 units at most from the player on each axis | 58000 |
| The ride | the transport function id names a row of `tb_District_Transport` that lists the row asked for among its five (`TransportTables`); the row exists in `tb_Transport_Info`; the map has the common position box the row ends at | 58002 |
| The fare | `NeedGold` less the credit benefit 6 of the NPC's group and the boosters that lower prices (`NpcCreditManager.Discount`, never below nothing); the player must have it | 58001 |
| Paid | the fare is taken (`08/20`); the rider gets the component `TransportRideComponent`; `05/16` (actor, row, NPC as asked, 10 bytes) goes to everybody who sees the player, the player too; nothing can hurt the rider for the whole ride (`PlayerVitals.Protect`); the credit with the group of the NPC rises (kind 9, `03/66`) | |
| The end | after `1 + RideSeconds` seconds on the simulation clock (one second at the start before the path plays, then the seconds of the ride; 61 s for the client's six rides) the rider is put in the middle of the box the row ends at, facing as the box does, and `05/17` (actor) goes to everybody who sees it | |

- The columns named in `Structures.json`: `tb_Transport_Info` `NeedGold` (gold), `PathFile` (the path the client plays; "0" means no path and an
  instant ride), `PositionBox` (the common position box of the map the ride ends at), `RideSeconds` (how long the path plays);
  `tb_District_Transport` `District` and `Transport1..5`. The maze loads the credit of the person at the first ride (it does not at entry),
  tells the client (`03/65`) and the flush writes the book like the district's.
- While it rides the player moves by no request of its own: the server ignores the places its client reports (no warp back, no answer, no
  charge against the error budget, nothing shown). The server does not follow the path; the place of the player changes once, at the end. The
  next move of the client then starts from the box, where the server keeps the player.
- A player who leaves meanwhile (disconnect, exit of the maze) takes the ride with it; the end finds the entity gone and does nothing, and a
  ride changes no saved place. A maze that closes does the same. A second ride, a move and an exit are the only things a rider can ask
  that this server answers differently from any other time; the client locks the rest.
- Wire: `MovementTransportTakeRequest` (`ActorValue` actor, `short` row, `ActorValue` NPC), `MovementTransportTakeResponse` (the same three),
  `MovementTransportOffResponse` (`ActorValue` actor); the layouts are in `MovementWireFormatTest`.

### Compared with the original

| Rule | Original | Here | |
|---|---|---|---|
| Place of the rides | the maps with transport NPCs (District 6) | the same, as a maze | same |
| NPC checks, distance, order of the three refusals | NPC in the area of the right kind with the function 8 (58000), within 1000 on each axis (58000), the row of offers (58002), the ride exists (58002), the gold (58001) | the same order and codes | same |
| A ride the NPC does not list | no error: the ride goes on and nothing is charged | 58002 | fixed (it looks like a hole of the original, a free ride for any row) |
| Fare | the price less the boosters, then less the credit benefit, one after the other | one discount of both parts added (`NpcCreditManager.Discount`, as the repairs and upgrades take it) | kept different: the same rule as the other services of this server; the two differ by the product of the two parts (10 gold on a fare of 1000 with 20 % and 5 %) |
| Charge, then 05/16 to the area, then the ride | yes | yes | same |
| Credit kind 9 | rolled between the minimum and maximum of the row | the same | same |
| Achievement count of the ride (kind 56) | counted | not counted | kept different: the achievement is not one of those the server counts yet |
| Ride time | a one second delay, then the path plays for `RideSeconds`; the end is seen at the first tick after | the same, on the simulation clock (`SimulationClock.Schedule`) | same |
| The place during the ride | the actor is moved along the path every tick | the player stays where it paid and changes place once, at the end | kept different: the client plays the path itself, and following it would need the path files and the interpolation of the engine for nothing the server reads |
| The place at the end | the middle of the box, the rotation of the box | the same | same |
| 05/17 and the move | the actor is moved to the end, then the area is told | the area is told, then the player moves (the district's grid then takes the player across) | kept different: so everybody who watched the ride is told; the maze tells everybody at once |
| During the ride | collision off, invincible, flying; a second request restarts the ride and charges again; moves are taken | invincible; a second ride refused (58000) without a charge; the client's places are ignored | kept different: the second charge is a bug of the original, and the moves a client reports along its own path would otherwise be refused by the speed check |
| A request to end the ride | ends it at once (a log line when early) | none: the client has no sender | kept different |
| Someone who comes into sight of a rider | is sent `05/16` with the elapsed time as a fourth field | not sent; the client reads three fields | kept different: this client does not read the time |
| Late viewer in a district | | | n/a: there are no rides in a district |

### Not done

- Rides between towns (`04/40`, "district transport"): this client has no sender.
- The achievement count; the elapsed time for a late viewer; the checks of level, quest and achievement the table has columns for (the
  original does not read them either).
- `D01_DISTRICT6` as a place to play: its 103 monsters and the rest of the kinds of maze (`notes-coverage.md` C3) are not looked at.

## Trade password in trades and private shops

The client asks for the password before it trades or opens a shop; the original asks the server too and the server does not take the
client's word for it. `TradePasswordComponent.Allows` (state 2, "given on this login") is the one condition, checked at the three places
the original checks it:

| Request | Original | Here | |
|---|---|---|---|
| Ask to trade (`0A/01`) | state 2 or `0A/81` 52403, after the target and busy checks and before the matching check | the same, after the two busy checks | same |
| Take an invitation (`0A/02`) | the state of the one who answers is not 2: the answer goes out as a refusal, both sides' trades are reset | the same: the answer to both is `IsAccept 0` and the trade is closed | same |
| Open a private shop (`0A/09`) | state 2 or `0A/89` 52431, after "already in a shop" and before the item checks | the same, after the busy check and before the title and item checks | same |
| Select and buy at a private shop | not asked | not asked | same |
| A letter with items or money | state 2 or 55602 | not asked | not done (the post service is another area; one test and one line) |
| Exchange (auction) | asked | does not exist | n/a |
| The server option of the second password | the checks run only when the option is on | always on | kept different: no such option here; the client asks anyway |

### Where it lives

The original keeps the wrong tries in the **account database** (a count and the time of the last wrong one, on the account row), and
"given" per online user on the servers. Here (round 10):

- The count and the time are columns of the account (`Accounts.TradePasswordFailed`, `TradePasswordFailedAt`), changed by one statement
  (`ITradePasswordTries`, `TradePasswordTriesStore`), so every login of the account on every server sees the same count and a lock
  outlasts the login. The rules are `TradePasswordRules`: every **third** wrong password stops the tries for **three minutes** (the
  tries made then count for nothing, the right password too, and are answered like a wrong one), the **tenth** wrong password locks
  the password (and every tenth after it), a right password sets the count to zero, making the password sets it to zero.
- "Given on this login" is a key in Redis next to the lease of the session (`ITradePasswordStates`, `RedisTradePasswordStates`,
  key `ow:trade-password-given:<session>`). It lives as long as the lease: every write gives it the length of a connected lease, the
  renewal of the lease extends it, and the close of the connection shortens it to the length of a disconnected lease. A wrong
  password takes it back. The lock is not kept there any more.
- Every entry reads both: the state at the entry comes from the account row (no password, locked, or one to give), and the entry of
  a server (`TradePasswordService.EnterAsync`, one line in the district and the maze) makes it "given" when the login gave it on
  another server. The gate shows the same state in the character list.
- A locked password is lifted by a game master with `/tradepassword reset` (own account): the account has no password and no wrong
  tries, as after the page of the account in the original.
- Codes of the answers (`03/18`, `int code, byte state`): a wrong password is **50105** with the state 1 (not given; it also takes back
  a password that was given), the tenth wrong one **52003** with the state 3, and every answer after that 52003 and 3 whatever the
  player sends, the right password included. A password that is not four of the digits 0 to 7, or one digit four times, is refused with
  59504 or 59505 and the state it had, and counts as no attempt.

### Compared with the original

| Rule | Original | Here | |
|---|---|---|---|
| State 2 needed, state 3 locked | yes | yes | same |
| Wrong password | 50105, state 1 | the same | same |
| Where the count lives | the account row | the account row (round 10; was Redis, per login) | fixed |
| The lock | the tenth wrong password (and every tenth), 52003, state 3, shown at every login until reset | the same | fixed (was five wrong ones, a lock that ended with the login) |
| The pause | every third wrong password, three minutes, tries counted for nothing | the same, answered 50105 with the state it had | same, but the original build answers such a try as a success (a hole of that build); kept different on purpose |
| Right password at the lock | the stored procedure checks it (the client does not offer the check at state 3) | refused, 52003 | kept different: the lock stays a lock for a client that sends it |
| What lifts the lock | the page of the account (no password), the admin tool, a right password | `/tradepassword reset` of a game master | kept different: no page |
| Format check before the attempt | invalid: 59504 or 59505 with the state, no attempt | the same | same |
| Make a password (type 1) | only at state 0; ends at state 2; the count is zero | the same | same |
| Types 3 and 4 | 3: the state from the database; 4: an account with no password goes on to state 2, one with a password is refused (59504) | the current state is answered | kept different: this client sends only 1 and 2 |
| Between servers | the control server tells the next game server | the key of the session | same effect |

## Model change, edits outside the area

Round 10 changed the model: two columns of `Accounts` (`TradePasswordFailed` smallint, `TradePasswordFailedAt` timestamp), see
"Where it lives". Redis: one key per login, `ow:trade-password-given:<session>` (the key `ow:trade-password:<session>` of the first
version is no longer written). Edits of the first version in files of other areas, all small:
`SessionLeaseManager` (the renewal script takes the key of the record beside each lease, and a write of the lease gives it the same length),
`PersonService` of the district and of the maze (one call, `TradePasswordService.EnterAsync`), `MovementServiceBase` (the 05/16 handler left
it for `TransportService`; a rider's places are not moves), `PlayerVitals` (`Protect` with a length), `LuaCreatureManager` (`TryFindNpc`), the
maze `Program` (`AddNpcServices`, the maze's `INpcPlaces`, `ITransportMap`).

## Tests

`TradeTest.ATradeAndAShopNeedThePasswordGivenOnThisLogin` (the three checks, the codes, nothing left open), `AccountStateTest` (the
make, the wrong and the right password, with the codes above; the count on the account: the pause, the lock, the new login, the reset),
`TradePasswordStatesTest` (Redis: a given password holds from the district to the maze and back, a wrong one takes it back, another
login is not touched; the record lives as long as the lease, through the entry, the renewal and the close), `TransportTest` (a ride is paid, told to the rider and to another player, carried by the clock for exactly its seconds, ended at the
box; a rider's moves are not moves; each refusal has its code and costs nothing), `MovementWireFormatTest.TheMessagesOfARideHaveTheLayoutTheClientReads`.

On the stand (`OW_STAND=9`, the scripts in `docs/agents/feature-district-gaps/`, output next to them):

| Script | What it shows | Result |
|---|---|---|
| `e2e_transport.py` | two players of a party in District 6; refusals 58000 (from afar, no such NPC), 58002 (not offered, row 0), 58001 (no gold); the ride of row 101 for 1000 gold (50000 to 49000, `08/20`), `05/16` seen by both (10 bytes, the actor, 101, the NPC), the credit of group 1005 up by 2 or 3, a second ride refused without a charge, a rider's move not shown; `05/17` 61.5 s after the request to both; a move from the box taken | all pass |
| `e2e_trade_password.py` | a trade and a shop refused without the password (52403, 52431), the one with none refused as invitee, both with a password trade, the same after a round through the maze, four wrong ones 50105 state 1, an invalid one no attempt, the fifth 52003 state 3, the right password at the lock 52003, the lock in the maze and back in the district, trade and shop still refused | all pass |

## To check in the play session (for the list in `notes-coverage.md`)

- The ride seen by the rider: the client fades and locks its input at `05/16`, plays the path of the row for 60 seconds, and gives the input
  back at `05/17`; whether it ends on the box where the server puts the player, with no jump; whether the one second of the start matches the
  client's own start.
- The ride seen by another player in District 6 (a party): the rider flies along the path on their screen and is gone from the station.
  A player who comes into sight after the start sees the rider stand at the station until the end (the time is not sent).
- The regional movement dialog at the NPCs of transports: it lists the rides of the NPC's row and shows the fare, and the refusals 58000
  to 58002 show their messages.
- District 6 itself: the portal in Rocco, the townsfolk and the monsters near the stations, the rider unhurt by them.
- The trade password window: a wrong password shows the message of the state 1, the tenth the lock; after a round through a maze the
  client does not ask again; a locked player gets the lock message in the trade window, and at the next login too (the gate shows the
  state 3 in the character list; what the client does with it there is not known). What the client shows after the third wrong
  password, when the tries are paused for three minutes (the answer is the same as for a wrong one). Whether the client sends the check
  at all at the state 3 (the original server would accept the right password then).
