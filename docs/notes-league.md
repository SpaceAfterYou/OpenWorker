# The league

The league (group 22) was already in the database with its twenty requests (see the league section of the social notes:
the 56 messages rebuilt from the client, `LeagueStore`, `LeagueRules`, `LeagueBook`, `LeagueNotifier`, `LeagueServiceBase`).
This file is the account of what was finished after that. The client 1.4.2.3 decided what it shows and what it checks (its
league window, the readers of the messages and the reader of `tb_League_Info` were read for this work); the reference
server gave the rules of the growth.

## Finish (2026-11-09)

### What grows a league

A member who runs a maze brings the league experience and gold:

| When | Experience of the league | Gold of the league |
|---|---|---|
| The member enters a maze whose fatigue cost is more than 1 | half the cost, rounded down | none |
| The member finishes the maze (the window of the results) | half the cost, rounded down | a hundredth of the gold the run gave, rounded down |

The fatigue cost is the column `FatiguePoint` of `tb_Maze_Info` (named for this work; 4 to 18 in the client table, 0 for
districts, tutorials and rooms). The gold of a run is what the window of the results shows as the gold of the run
(`MazeResults`), since this server gives the gold of a maze as drops picked up in it. The member's own experience in the
league (`LeagueMemberPersistent.Exp`, shown in the list of members) grows by the same share. Nothing else brings anything:
not quests, not levels, not gold picked up outside a maze.

A player enters the maze before the server learns their league (`LeagueComponent` is set when the community hears of the
player, after the maze is made, and every player starts with an empty one), so a share without a known league goes out with
league 0 and the treasury finds the league of the person in the database; a person in no league brings nothing.

Code: `OpenWorker.Gameplay/Modules/League/LeagueGrowth.cs` holds the rule and asks `ILeagueTreasury`;
`OpenWorker.Channel/Community/LeagueTreasury.cs` writes it (`LeagueStore.GrowAsync`) and tells the members. The game calls it
in two places, each with one optional constructor parameter: `MazeResults` (`Start` and `Complete`) and `ExperienceService`
(a level reached, see below).

### The level of a league

`tb_League_Info` is read the way the client reads it (columns named in `Structures.json`):

| Column | Name | Meaning |
|---|---|---|
| 0 | `Id` | the row |
| 1 | `Level` | the level of the row (1 to 10) |
| 2 | `Exp` | the experience a league needs to rise to this level from the one below (0, 1000, 2000, 4000, ... 240000) |
| 3 | `MemberLimit` | how many members a league of this level holds (20, 25, ... 60) |
| 4, 5 | unnamed | not read by the client's league window (30..126 and 1..2) |

The member limit was taken from column 4 before (30 at level 1); the client takes it from column 3 (20 at level 1) and
refuses to invite into a full league itself, so a league of 21 to 30 members was full for the client and not for the
server. The rows are looked up by `Level` now (`LeagueLevels`).

The experience of a league counts inside its level: the client shows it as `exp * 100 / Exp of the next level`, and 100 % at
level 10. So when the experience reaches what the next row asks, that much is spent and the level (`LeaguePersistent.Rating`)
rises, as many times as it reaches. At the last level the experience keeps growing (it orders the ranking). Every step is an
update the database checks (`Rating = level and Exp >= need`), so servers adding at once lose nothing and never raise a
level twice.

What the members see:

- every share: `22/43 LeagueInfoUpdate` (league, rank, gold, members) to every member online, wherever they are;
- a level reached: the whole league again (`22/07` with `Joined = 0`) to every member online. The client has no message of
  its own for the experience or the level of a league (its receive switch has no case for them), and it changes them only
  when it reads the whole league;
- the room for members follows the level at once: invitations and applications are measured against the new limit.

### The ranking of leagues

The league list has two pages (`22/03` with type 0 or 1); the client groups the second by level itself. That page is
ordered by level, then experience. The rank (`LeagueInfo.Rank`, also in `22/43`) is one more than the number of leagues
with a higher level, or the same level and more experience; it is computed when a league is read. The ranking module of
group 2C has no ranking of leagues in this client.

### The rights

The bits of `LeagueRight` are the ones the client tests before it offers a button of the league window, and the reference
names the same bits:

| Bit | Right | What the client offers with it | What the server checks |
|---|---|---|---|
| 1 | `Join` | invite, the list of applicants, accept or refuse one | invite, accept, refuse; who is told of a new applicant |
| 2 | `KickOut` | throw out a member (below oneself) | kick |
| 4 | `StoreIn` | (no storage of the league in this client) | nothing |
| 8 | `StoreOut` | (as above) | nothing |
| 16 | `Notice` | edit the notice | notice |
| 32 | `Shop` | (no shop of the league in this client) | nothing |
| 64 | `SetPosition` | give positions, name the positions, change what positions may do | member position, position names, the rights in `22/32` |
| 128 | `LimitOut` | change the gold each position may take out | the limits in `22/32` |
| 256 | `RecruitNotice` | edit the call for members | recruit notice |

The master may do everything whatever the bits say. Opening or closing the league is the master's alone (the client
checks the master, not a bit, and shows 57015 otherwise); so is disbanding. The board needs no right.

The rights of a position are kept in the slot of its number (0 to 7; the client reads `auth[position]`), the ninth slot is
the master's and unused by the client.

Positions, as the window of the client gives them:

- the client gives a position with `22/39 (member, position, 0)` and takes it away with `(member, 0, 1)`. The server read
  a state of 0 as "take away" before, so every position the real client gave was taken away instead;
- the window offers the sub master (7, one at a time), managers (5, three), elders (4, ten) and the named positions 3, 2, 1
  only when they have a name; chairman (6) is never offered. The server refuses the same (57030);
- a member with `SetPosition` works only on members below their own position and gives only positions below it; the master
  works on anybody.

A member with the rights may change `22/32` only for positions below their own.

### What the list of members shows

The level of every member is the level of their person: online from the player (`LevelComponent`, through the presence
record that every server renews every 30 seconds), offline from `Persons.Level`. The applicants show the level of their
person too. The hero and the last time a member left were already read from `Persons`. When a member reaches a level, the
other members get `22/22 LeagueMemberUpdate` with it at once. The presence record carried the constant 25 before, which the
friends list and the community saw as well; it now carries the real level.

### Accepted without checking

- **Who gets the experience of a share, and the daily cap.** The reference server sends the share of a member to a league
  server that is not among the reference material, so that side (whether the member's own experience grows by the same
  amount, whether there is a cap per day) is not known. The member gets the same share; there is no cap: the client's
  table has no column for it.
- **The rights of a new league.** Base 0, named positions `Join`, elder, manager and chairman `Join | Notice`, sub master and
  master everything. The client starts from what the server sends; the reference does not show defaults. Leagues made
  before this change keep the rights they were made with, in the old numbering of the bits: the master should set them
  again in the window (or a migration of data rewrites them).
- **Working only below oneself** for the rights and positions of a member who is not the master: the client's window shows
  the buttons only for members below oneself; the rule for `22/32` is ours.
- **The gold of a run** is the gold picked up in the maze, as the window of the results counts it.
- **The rank** of a league with the same level and experience as another: both get the same rank.

### What to check with the real client

1. Run a maze with fatigue while in a league: the gold and rank of the league window change at the entry and at the end;
   when the level rises, the window shows the new level, the percentage and the room for members.
2. Give and take away a position from the member popup; the named positions appear in the menu only once named.
3. A member with bit 64 only: the position menu and the permission page work; one without it: the client shows no button
   and the server refuses with 57006.
4. The list of members of a league whose members are offline shows their real levels.

### Tests

- `LeagueGrowthTest`: a run brings half the fatigue on the way in and on the way out and a hundredth of the gold; a maze
  without fatigue brings nothing; a player whose league the server has not learnt yet brings the share with league 0.
- `LeagueStoreTest` (Postgres): the experience of the next level raises the level, keeps the rest, raises the room for
  members, and the member keeps their whole share; the member shows the level of the person.
- `LeagueWireFormatTest`: the bits of the rights, who may change rights, which positions are given and by whom.
- `docs/tools/e2e_league.py` (local): positions given and taken away as the client does, the named position before it has a
  name, one sub master, rights and opening refused without the right, the growth from entering a maze, the levels and the
  rank in the whole league.
