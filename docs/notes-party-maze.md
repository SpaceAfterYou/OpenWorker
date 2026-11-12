# A party in one maze

Branch `feature/party-maze`. A party plays a maze together: one instance of the maze for all its members, entered from
a district after a question to every member, with the experience of a kill shared by the party and the end-of-maze
window for each member. The layouts come from the client 1.4.2.3; the rules (who may start, what a refusal does, how the
experience of a party is counted) from the reference server. This file says what the server does now, what each message
carries, what was assumed and what to check with the real client. The party itself (invitations, the list, the board)
is the work of `feature/party` (`docs/notes-party.md`); this branch reads it through `IPartyRoster` (`PartyBook`) and tells
the party through `PartyNotifier`.

## The instance

Before, the maze was a component of one player: `LuaMaze` was built for the player and everything it did was sent to
that player. Now a `LuaMaze` is an instance that several players share, and the `LuaMaze` component of each member points
at the same object.

- **What is shared**: the script and its state, the monsters and their minds, sectors, gates, portals, interactions,
  timers, the progress (`MazeProgress`), the clock of the maze. The players of the instance are the players of its
  creature manager (`LuaCreatureManager.Members`): the monsters already chose among several players, so they now fight
  every member.
- **What is per member** (`LuaMaze.Fighter`): the skill being played, its key presses and its generation, the trap that
  follows the cursor, the projectiles and chains in flight, the projectile session counter, whether the client has
  loaded the maze. The maze steps the flights and chains of every member with its own step.
- **Who hears what**: everything the maze says about itself (monsters shown and dead, hits, sector clear, gate and portal
  changes, interactions enabled, timers, client sync of the script, switch NPCs, warp in sector, notices to everybody)
  goes to every member, written once (`LuaCreatureManager.Broadcast`). The clear info (11/66 group state) is sent to each
  member with its own actor. What answers one request stays with the one who asked (11/78 of an interaction click, the
  cutscene state).
- **Scripts that ask about the player** (`IsHaveQuest`, `IsHaveCondition`) are true when any member is; those that change
  the player (`AcceptQuest`, `CompleteQuest`, `CompleteCondition`, `RemoveQuestAll`) do it for every member; `ApplyBuff`
  is shown to every member; `ChatMessage(0, ...)` goes to all, a message for an actor to that member; "the current
  sector" is the sector of the first member. A script that listens to `OnUpdateQuest` hears the quests of every member,
  each call naming the member (a `QuestWatch` per member); the tutorial mazes of the prologue always have one player.
- **Reports per member**: a kill counts for the missions, quests and achievements of the killer; a cleared sector and the
  cleared maze for every member in the maze then; soul metry objects shown are those of the member who starts the maze,
  a clicked object counts for the one who clicked it.
- **Instances** (`MazeInstances`, maze server): by the identifier the reservation carries. A player alone gets a new
  identifier. The first member to arrive opens the instance (script, navigation mesh, creatures); a member who arrives
  while it is being opened waits for the same one, a member who comes later joins it where it stands. An instance of
  another maze, or one that ended meanwhile, is not joined: a new one is opened.
- **Leaving**: the exit of the maze (11/23, after the answer is sent), the end of the connection and leaving the game all
  take the player out of the instance (`MazeInstances.LeaveAsync`). The others get 04/12 (out of sight) for that actor,
  the monsters forget the player, what the player set going stops. The maze goes on for the rest. The last one out ends it
  (`LuaMaze.Close`) and takes the record of the party maze out of Redis, if it still names this instance.
- **A member who comes in** (11/22, the client has loaded): the first one starts the maze as before. A later one gets the
  progress (11/53..11/56), the monsters and townsfolk that are in sight already (04/23 and 04/22; those that wait for
  their delay come with everybody's), every other member (04/11), and the others
  get 04/11 of the newcomer. Then the script hears `OnEnterPlayer` for the newcomer.
- **Seeing each other** (`MazeAudience`): in a maze everybody sees everybody, there is no grid: a party is four at most.
  A move of a member goes to every member at the end of the pass of the world loop, the last move of the pass only, and
  anything else the member does for the others sends the waiting move first, as in a district (`docs/phase-3`). Chat and
  what the shared handlers send "nearby" go to the members. A player alone still gets everything at once and only for
  themselves: the paths of a maze of one are those of before.
- **Life of a member** (12/12 `PartyUpdateMemberHpResponse`: actor, life, maximum life): when the life of a member
  changes (`PlayerVitals.HealthChanged`), the other members in the maze get it at the end of the pass, once however often
  it changed. The order of the two numbers is the client's: it keeps the second as the life and the third as the
  maximum, and shows the member dead when the life is zero.
- **Script failure**: the maze ends for every member, as it did for the one player.

## Entering as a party

The question is asked in the district (`PartyMazeEntry`, district server), when a member of a party of two or more takes
a way into a maze location (04/01, and 04/41, the request to create a maze, which goes the same way):

1. **The party plays this maze already** (a record `PartyMazeCache` in Redis under the party, its maze server still
   registered): the member is sent into that instance at once, from any district and channel. No question.
2. **Only the master opens a maze for the party**: anyone else gets 55004 on the request.
3. **Every member must be there**: in this district (on this server) and on the channel of the master. A member who is
   not in the game is 55040, in the game but elsewhere 55041, on another channel of this district 55047; the master gets
   the error and nobody is asked.
4. **The question** (12/47, `MazeEnterPartyAskResponse`: `int` maze, `byte` 0) goes to every member, the master too. The
   client of the master shows a window to wait in with a button to call it off; a member gets the question with the name
   of the maze and yes/no. The client answers with 11/48 (`MazeEnterPartyAnswerRequest`: `int` maze, `byte` agree); it
   sends no to a yes when the player has no weapon equipped.
5. **No, or called off** (the master answering at all, a member answering no, a member of the question leaving the
   district): 11/49 (`MazeCancelEnterPartyResponse`: `int` actor who ended it) to every member of the question, and the
   question is over. The client closes the question and says who it was.
6. **The last yes**: one maze server is picked (the least busy of the gate, as for a player alone) and a new instance
   identifier; the record `PartyMazeCache` (party, location, instance, server identifier, host, port; six hours at most)
   is written; then every member of the question who is still connected is flushed and gets a maze reservation that names
   the instance and the party, and its 04/02 with the address. They all land in the one instance.
7. **A new question of the master** replaces an open one. **A question nobody answers ends after 45 seconds**: the members
   are told by 11/49 naming a member who did not answer, as if that member had said no. The original server holds a
   question until someone ends it; the client counts a time down in the question of a member and answers when it is over,
   so the 45 seconds only catch a client that never answers (`PartyMazeEntry.ExpireQuestions`, run every second by
   `PartyMazeQuestionService`).
8. **A party of the auto matching** (`PartyMatching`) is sent in without a question: its members said they go when the
   group asked them. When the group becomes a party, the district that moves the group on opens one instance for it
   (`IPartyMatchListener`, `PartyMazeEntry.OnMatchedAsync`) and publishes an event to the members; the server each member
   is on writes the reservation of that member and sends the 04/02, unasked by the client.

`MazeReserveCache` has two more fields: `Instance` (empty for a player alone) and `Party` (zero). `MazeDirectory` gives the
identifier of the server it picks too (`PickServerAsync`), so that the record of a party maze can say which server it is
on and a later member can tell whether that server still runs.

The 12/47 type is the client's reader of the party group (the opcode is `PartyOpcode.Unknown5`); it lives with the maze
messages because it is about the maze, and nothing of the party protocol changed.

## Experience and rewards

- **A kill in a maze of a party** gives the experience to the members in the maze who are on their feet
  (`ExperienceService.GiveForParty`), each the whole of it, not a share:
  experience of the monster (as for a player alone) × the rate of `tb_PartyExp_Level` for the level gap of the party
  (the highest level of the members less the lowest) × the rate of `tb_PartyExp_Mob` for the highest level less the level
  of the monster, each ×1 when its table or row is missing. Each member's gain goes through `ExperienceService.Add`, so
  the level reached still reaches the achievements and the league of that member. A player alone is counted as before (own level less the level
  of the monster against `tb_PartyExp_Mob`). The killer's tally counts the kill; the others' tallies count the
  experience. What drops is the killer's.
- **The member count** does not enter the experience in the reference: it chooses the strength of the monsters (below)
  and a party booster (`tb_PartyExp_Member`, not built). `tb_MazeReward_PartyValue` of the client keeps only its key.
- **`tb_PartyExp_Level`** is a new server table (`OpenWorker.Res/ServerRows/PartyExpLevelRow.cs`, optional): a byte of
  level gap and a float rate.
- **The end of the maze** (`PartyMazeEvents.Completed`): every member in the maze gets its own window (11/75 with its own
  experience, gold and the key reward offer of `MazeKeyRewards`, which is per player) and 11/76
  (`MazeRewardPartyInfoResponse`) with every member: order (1..n in the order they came), rank, actor, name, play point.
  Each member has its own score and rank, the members are put in the order of their scores for the shares of a party, and
  the list carries every rank and score (`docs/notes-maze-results.md`); a player alone gets no list. Each member leaves by the
  exit of the maze to a district of their own choosing, as before.
- **The party** gets 12/46 (`PartyMazeClearResponse`, empty) through `PartyNotifier.MazeClearAsync`, as the reference
  sends it to the whole party; the client shows the clear when it waits for it.
- **Where a member is** (12/06 `PartyUpdateMemberInfoResponse`): every change of map is a change of server here, and the
  party already tells the others when a member comes into a server (`PartyNotifier.OnEnteredAsync`), so the maze sends
  nothing of its own.

## Monsters made stronger by the party

`PartyRevision` (`OpenWorker.Gameplay/Modules/Maze`) reads two tables of the server: `tb_PartyRevise` (a row per group of
monsters and per number of members, one to four: the rates of life, attack, defence, attack speed and speed of movement, the
gauge of the super armor as a percent of the life, and the milliseconds it takes to come back) and the server's own
`tb_Monster`, which names the group of each monster. The client's copies have neither (its monster table has another column
where the group is, and its `tb_PartyRevise` has only the group, the number and one time).

- **What it scales.** The life, the physical and magical attack (the minimum and the maximum) and the defence of the monster
  are multiplied by the rates, the whole number of the stat times the rate. A monster with a super armor gets a gauge of
  the row's percent of its (scaled) life and the row's time to recover; before, the gauge was a guess of a fifth of the
  life and three seconds. The rates of attack speed and speed of movement multiply the speed stats of the monster (shown to
  the client in its stat list); the monsters' own movement and attacks are set by their scripts and do not read them.
- **The count.** The maze counts the members it was opened for: the members of the question (or of the matched group),
  carried by the reservation (`MazeReserveCache.Members`, `PartyMazeCache.Members`). A player alone counts one. More than four
  count four. A monster that is not of the plain kind (bosses and the like, `Type` other than 0) always counts one.
  The rates apply to a player alone as well: the rows of one member are not all 1, and for most of the early monsters they
  are below 1.
- **When it is fixed.** A monster is made as it appears, with the count of the maze at that moment. The count changes only
  when a member leaves the party (leaving, a kick, a member offline too long) or the party ends: the community event
  `party.resized` carries the party and the members left to the servers of those who remain, and a maze that has one of
  them in it makes its living plain monsters as strong as the new count makes them, each keeping the share of its life it
  had (a broken super armor stays broken until its time and then comes back whole at the new gauge; a defense the mind of the monster forced on it stays), and tells its members: the changed stats of each monster (17/12) and the
  notice 80 (11/51, `MazeNoticeResponse`), which the client has a text for. A member who joins the party or the instance
  later changes nothing, as in the original.

## Portals that wait for the party

A portal that leads to another place on the map (04/07) takes everyone in the maze through, so it waits (`PortalWait`,
`LuaMaze.EnterPortal`):

- when all the players of the maze stand at it, it opens at once (a player alone is all);
- when half of them or more do, a countdown starts and is told to everybody by 04/0F (the seconds, 5 down to 1, then 0): after
  five seconds the portal opens for all, whoever is not there;
- a player who steps out of the portal (04/0D) or leaves the maze is not waited for; if too few stand there then, the
  countdown stops (04/0F with 0);
- opening is 04/0E for everybody, which puts them all at the same place, closes the portal and wakes the sector beyond.

Kept different from the original: a player stands at one portal (the original lists a player in each portal they stepped into
and removes them only from the portal that counts down), the check runs when a player steps in or out and not at the next
step of the maze, and a refusal (a portal that is shut, on another map or leading nowhere) is still answered with an error.

## Drops, interactions and what a kill counts for

- **Drops.** A kill drops for every player in the maze (on their feet or not), not for the killer alone: each rolls the drop
  of the monster by themself, sees it alone and is the only one who can take it. For a party the chance of every roll is
  multiplied by the rate of how far apart the levels of its members are (`tb_DropRate_Level`) and by the rate of how many
  there are (`tb_DropRate_Member`), and, unless the drop is exempt, by the rate of how far the highest level is above the
  monster (`tb_DropRate_Mob`); a player alone is not multiplied by the first two. (The original multiplies the last rate
  again for every member after the first; that looks like a mistake and is not copied.) The earlier note that a party's
  drops are the killer's was wrong.
- **Interactions.** A click on an object answers 11/78 to everybody in the maze, unless the table says the object is private
  to whoever uses it (`PrivateActivate`, a column of the client's `tb_Interaction_Object` now named), when it answers the one
  who clicked.
- **Kills.** A kill counts for every player in the maze, not only for the killer: the quests that hunt the monster or its
  group, the daily missions and the achievements of each (`LuaMaze.ReportKillAsync`). A cleared sector counted for every
  member before.
- **Objects.** The use of an object counts for the quests of everyone in the maze when the object says so
  (`PublicQuestCheck`, also a named column), for the one who used it otherwise.
- **Items.** Items that come into the bags of a player in a maze with others count for the others too, on the conditions
  that count for the group (the server's `tb_Quest_Condition`; nearly every condition that collects an item does). The others
  need not hold the items. See `docs/notes-quests.md`.
- **The window at the end** carries the play points of each member, the score the rank was worked out from; that was
  already so.

## Assumptions

- Every member must be on the master's district server and channel to be asked, as the reference demands; members on
  other districts join the running instance later through the record in Redis.
- The party of an instance is read from the reservation; a member who leaves the party while in the maze stays in the
  instance until leaving it. The level gap is counted over the members in the maze, not over the whole party.
- A member who is down when a monster dies gets nothing of it; when nobody is up (the killer's projectile landed after it
  fell) the killer gets it.
- Only the master opens a maze for a party: a member who takes a portal alone while the party plays no maze gets 55004, as
  in the reference. (`docs/tools/e2e_party.py` sends a member of a party into a maze alone and has to let the master do it.)
- A matched party is sent in by an unasked 04/02; the reference sends its own entry, the client is expected to follow any
  04/02.

## To check with the real client

- Round 8: the countdown of a waiting portal (04/0F shows string 661 with the seconds) and that the client sends 04/0D when
  a member steps out of the portal box of a portal that waits; that a member standing at a portal sees nothing happen while
  the others are away; the notice 80 text when a member leaves the party in a maze; the life of the monsters in the window
  of the target (the client takes the maximum from the monster list and the stat updates); that a drop is seen by its owner
  alone and a click on a public object changes it for all; the 12/06 of a party member after a change of channel and level.
- The question box of a member counts down by itself in the client (the popup table says 30 seconds for the question and 15
  for the wait of the master); what the client sends when the count ends.

- The question window of 12/47 for master and members, the answer 11/48 and the close on 11/49 with the right name.
- That 04/11 of a member arriving while the other is in the maze shows the character, and that 04/12 removes it.
- That the monsters shown to a member who comes later stand where they stand and fight the newcomer.
- That 12/12 updates the party window in the maze, and 12/46 in the district.
- That 11/76 lists the members in the window of each.
- The error codes 55004, 55040, 55041, 55047 on the 04/01 of the master.

## The original and this server (round 8)

| What | Original | Here | |
|---|---|---|---|
| Strength of monsters | rates of the group of the monster by the members of the maze, one to four, one for a monster of another kind | the same | same |
| Rates applied | life, attack, defence, attack speed, speed of movement, super armor gauge and times | the same; the speeds only change the stats shown, the scripts of the monsters set their pace | same (the speeds do not drive the monsters) |
| Members counted | the members the maze was made for; the party's members when one leaves; one when the party ends | the same | same |
| A monster already alive | recalculated with the share of life kept on a leave | the same; the break of an armor and a forced defense are untouched | same |
| Notice of the change | the notice 80 to the maze | the same | same |
| Group not in the table | nothing applied | nothing applied | same |
| Portal that waits | all stand: at once; half or more: 6 seconds with the seconds told; checked when somebody steps in or out | the same; a player stands at one portal | kept different (fixes a stale list) |
| Question to enter | no time on the server | 45 seconds | kept different (a client that never answers) |
| Drop in a party | each member, in the instance, rolls their own with the rates of level gap and size (and the mob rate, repeated per member) | the same without the repeat | same, one mistake not copied |
| 11/78 | to everybody unless the object is private | the same | fixed (it went to the one who clicked) |
| Kill in a maze | counted for every player in the maze | the same | fixed (killer only) |
| Use of an object | for everyone when the object says so | the same | fixed (clicker only) |
| Items in a maze | the others count them on the conditions that count for the group | the same | fixed (none) |
| Play points in 11/76 | the score the rank came from, sorted for the order | the same | same |
| 12/06 | on entering a server, a new level, channel, class, photo or name | entering, level, channel | kept different (class, photo and name are not sent) |
| Matching: blocks | a person does not join a group of someone who blocked them or whom they blocked | the same, checked before the join | same |
| Matching: the maze | the checks of the portal (weapon, hours, level, maze before, quest, daily limit, item, fatigue) | the same, wherever the player stands | same |

## Tests and scripts

`OpenWorker.Hotspot.Test/PartyMazeTest.cs` and `PartyMazeEntryTest.cs`, with doubles of `IPartyRoster` and of the routes
of a party maze: a monster
dies once for both members and both get the party experience of the tables; a member leaving keeps the maze for the other
and the last one out ends it; a later member joins the instance where it stands (opened once); a party reservation sends
every agreeing member to one instance, and a later member into it; a no ends the question for everybody; a player alone
has a maze of their own and a player without a party enters, both as before.

Round 8 added `PartyRevisionTest` (the rates by members, the count of a monster of another kind), `PortalWaitTest` (at once,
countdown, stepping out, leaving the maze), `PartyMazeTest` (a maze that weakens its monsters when a member leaves the party,
needs the client tables), `RewardsTest` (a drop of each member's own at the rates of the party), `PartyMazeEntryTest` (the
question ends after its time), `PartyTest` (a changed member is shown to the others; the matching keeps apart people who
blocked each other), `MazeAdmissionTest` (the rules asked of a player who waits), `QuestManagerTest` (items for the group)
and the new server tables in `ServerTablesTest`.

The end-to-end script `docs/tools/e2e_party_maze.py` (accounts tester4, tester5, tester8) forms a party of three in the
district, has a member try to open the maze, asks, says no, asks again, sends everybody in, checks that the two in the
maze see each other, the monsters and each other's moves, that a kill is seen once by both with the same experience, that
both get the window with both and the party the clear, that the maze goes on when one leaves, and that the third joins
it later where it stands: 24 checks pass on a stand. On the stand the monsters of 21311 are worth no experience (the
server tables of the stand), so the amounts of the party experience are covered by the unit test only.
