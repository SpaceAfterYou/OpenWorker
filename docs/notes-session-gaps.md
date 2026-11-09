# Small gaps of a session: channel change, options, gear, the maze's system group, commands, megaphone, trade password, boxes, levels

Branch `fix/session-gaps` (2026-11-09). It closes the small gaps of `docs/notes-coverage.md` that break or cheapen an ordinary
session: backlog A1, A7, A9, A11, A12, B2, the windows of the group "District here, maze there" and the boxes of "Hostile input
not checked". Research data (the functions of the client and of the original, addresses) is in the git-ignored
`docs/notes-coverage-research.md`, section "fix/session-gaps"; scripts and outputs in `docs/agents/fix-session-gaps/`.

## What was built

| Gap | Where | What the server does now |
|---|---|---|
| Change of channel (F1/02) | `ServerChannels.Switch`, `ChannelService` | moves the player and answers with the enter-map record the client reads (the same as 04/02 without the two flags): the map carries the new channel, the location and the gate, the server flag is 0. The client takes the map as its own and asks what is around it (04/06). The player is told that the people of the old channel are gone (04/12), the old channel that it left, the new one that it came. Refusals are `F1/82` + code: 51008 for the same channel and a full one, 50004 for one the district does not run, 55003 during a trade. |
| Options (01/02) | `SystemService` of `Server.Common` | the district and the maze save the 64 option bytes to the account, as the gate and auth do, update the person's copy and renew its presence at once (who may whisper, who may look). |
| Change of gear (08/0D) | `ItemServiceBase` move, `ItemService` of `Server.Common` | after a move that took place, the worn slots it touched (look, gear, costume look: storages 0, 1, 3) go to those who see the player (`IPlayerAudience.NearbyToOthers`): the actor, a count, then storage, slot and the item now there (`-1` for an empty slot). A player who comes into sight later reads the gear from the person record (04/11), built from the bag at that moment. |
| System group in the maze (01/05, 01/06) | `KeepAliveService`, `KeepAliveComponent`, `SystemService` moved to `Server.Common` | the keep-alive now walks an Arch query of the players who entered (not the channels), so it runs in the maze too; entering starts it (`PersonRegistry`). The ping is answered by both servers. |
| Skill window, gestures, daily missions in the maze | `SkillWindowService`, `GestureService`, `DailyMissionService` in `Server.Common` | learn, reset, deck, deck slot, divergence and bonus (06/71-79), gestures (23/01, 23/03) and daily missions (24/02, 24/04) are answered by both servers. The gesture slots are loaded at entry by both (`PersonRegistry`); a gesture goes to the player and to those who see it. |
| Test commands (07/0A) | `AccessComponent`, `CommandAccess`, `CommandManager`, `CommandOptions` | an account has an access level (0 player, 1 game master, 2 management), read at entry. A command names the level it needs (`ICommand.Required`, a game master's by default); a command above the level is not run and not answered, as the original does. `Commands:OpenToEveryone` (false unless set) makes every account a game master, for a test stand; the stand sets it. The person record shows the level (the GM byte the original sends), instead of "game master" for everybody. |
| Megaphone (07/06) | `InventoryMegaphone` (`IMegaphoneSource`) | the slot the client names must hold an item of the use kind 71; one is spent (`InventoryService.Spend`). 52011 without an item, 52002 for another kind. |
| Trade password (03/18) | `TradePasswordService` of `Server.Common` | create (1): only without a password; four of the digits 0 to 7, not one digit four times (59504, 59505); kept hashed on the account (`PasswordHash`, as the account's password), written only where the account has none. Check (2): a wrong one answers 59504, five lock it (state 3) until the next entry. 3 and 4 answer the state. The answer is `int code, byte state`; the state starts as "has one, not given" or "none" at every entry. |
| Boxes a maze client names (11/31, 11/35, 11/7E, 11/77) | `MazeService.StandsAt` | the player must stand at the box (the same test as the quest move check: within half the diagonal of the box plus 1000 units). A claim from afar does nothing and counts against the error budget (`violation`); an interaction from afar is refused with 54007. The quest move check (11/65) counts its false claims the same way now. |
| Level of a person away | `FriendStore`, `RankingStore`, `FriendBook`, `FriendServiceBase`, `FriendNotifier`, `RankingService` | the friends list, the block list, a search, the recommendations, the friend notices and both rankings show the level of the person's row when it is not online, and the online level otherwise. |

## Compared with the original

| Rule | Original | Here | |
|---|---|---|---|
| Answer of a channel change | the control server picks the channel and the game server answers with the enter-map record | the district answers with the same record itself (one process holds every channel) | same layout |
| Channel refusals | not ready, in a maze, outside a safe zone of District 6: 55010; matching: 53150; in a trade: 55003; social item: 55092, 55093; control server: cannot change world 50023, no such channel 50004, full or server not ready 51008 | missing 50004, full 51008, in a trade 55003 | kept: the maze has no channel handler (a maze player never sends it), District 6, matching and social items do not exist yet. The same channel is refused with 51008; the original would move the player to it again |
| A pending trade request on a channel change | cancelled (52411) | only a trade in progress refuses | kept: a pending request expires by itself |
| Options in the game | saved to the account | the same | same |
| 08/0D | after the move answer, to everybody around but the player; the slots of storages 0, 1 and 3 that changed, with the item now there | the same | same |
| Keep-alive and ping in a maze | one process for district and maze | both servers | same |
| Test commands | allowed when the server is a test server or the character has a game master level, otherwise ignored without an answer | level of the account, `Commands:OpenToEveryone` for a test stand | same rule; the level is the account's, the original's is in the character info from its database |
| Megaphone | blocked chat 50111; item missing or locked 52011; not use kind 71 52002; break failed 52013; one item taken | missing 52011, not a megaphone 52002, one taken | kept: chat blocks and item locks do not exist yet |
| Trade password format | four of '0'..'7', four equal 59505, else 59504 | the same | same |
| Trade password state | kept by the control server across servers; the database decides a wrong one and the lock | given again on every server, locked after five wrong ones for the entry | kept different: there is no control server; a lock lasts until the next entry |
| Boxes of a maze | the event spawn box needs the player's sector, the others no place at all | the player must stand at the box | stricter than the original, on purpose: these boxes set off spawns and scripts |
| Level of a person away | from the database | from the row | same |

## Not done, and what it needs

- **Trade and private shop do not ask for the trade password.** The client checks the state itself before a trade; the original
  also checks it on the server (state 2 needed, 1234 for a locked one). Needs one call in `Trades` and `PrivateShops`.
- **Attendance in a maze.** The maze does not load attendance, so 2A/02 and 2A/05 stay district-only; whether the client sends
  them in a maze is on the play-session list.
- **Item locks, chat blocks** (the megaphone's 50111 and the lock of 52011) do not exist.
- **Who sets the access level.** Only the database (`Accounts.AccessLevel`); no tool or command sets it.

## Model change (for the migration of the round)

`AccountPersistent.AccessLevel` (`byte`, column `AccessLevel smallint not null default 0` of `Accounts`). Nothing else: the trade
password uses the existing `Accounts.TradePassword`.

## Play session

The items are in `docs/notes-coverage.md`, "One play session": the channel change, the options in town and in a maze, gear seen
by a second player, the windows of a maze, idle in a maze, the boxes, the megaphone, a command of a player, the GM byte of the
person record, the trade password across servers.

## Verification

Tests (one per gap): `ChannelChangeTest`, `AccountStateTest` (options, trade password, levels; Postgres), `WornGearTest`,
`CommandAccessTest`, `MegaphoneTest`, `MazeRequestGuardTest.ABoxNamedFromAfarDoesNothing`; `KeepAliveTest` follows the move.
Stand (`OW_STAND=6`): see the report of the branch for the counts of `e2e_move`, `e2e_chat`, `e2e_friends` and `e2e_maze`
before and after; the extended scripts are in `docs/agents/fix-session-gaps/e2e/` until the branch is merged.
