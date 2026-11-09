# Quests: what is real, how it works, what is left

The quests of a person (`OpenWorker.Gameplay/Modules/Quests`, the handlers in `OpenWorker.Server.Common/Services/QuestService.cs`)
checked against the client 1.4.2.3 (its receivers and senders of group `0x15`, the NPC dialog that sends them, and the
tables `tb_Quest_Episode`, `tb_Quest_Condition`, `tb_Quest_Reward`, `tb_Title_Reward`) and against the rules of the reference
server. The layouts of the 11 answers and 6 requests were already checked (`docs/notes-formats.md`, a local note); this note
is about what the server does with them.

## The audit

| Part | Before | Now |
|---|---|---|
| Entry: `15/02` list, `15/01` finished list | both sent, finished list first | **the list first, then the finished list**, as the client reads them: its handler of `15/01` works out which episodes it may offer (and takes some of them itself) against the episodes it already has in progress. Both are sent with the character info, while the client still loads; the client keeps them for its play scene. A book is mended at entry: episodes no longer in the tables, episodes that are also finished, conditions counted to their end but not marked |
| Taking (`15/03`) | limit of 30, taken, finished, level, hero, episodes before, items given | the same in the order of the reference (limit 54024, taken 54001, finished 54002, level 54018, hero 54019, a repeated episode done for the day 54019, before 54020, items 54021), **plus the character**: an episode taken from a character (`StartObject`) is taken only near it (refused with 54020) where the server knows where it stands |
| Episodes that start by themselves | missing | **real**: when an episode is finished, every episode it links to (`LinkEpisode1..5`) that starts by itself (`AutoStart`) is taken at once and told with `15/03`; at entry, such an episode whose first episode before it is finished and that the person lacks is taken (a main one only while no other main one is in progress), carried by the list |
| Conditions the server counts | kills, maze and sector clears, objects used, items picked up | kills (monster and monster group), sector clears (with their maze), objects of a maze used, **every item that comes into the bags** (`ItemArrivals`: pickups, purchases, rewards, letters, production), **items made, upgraded (every try), taken apart (one and many), akashic cards made, items bought at a shop**; collecting is also set from the bags when the episode is taken |
| Conditions the client tells (`15/08`) | talk, move, trigger, deliver, protect, survival, trace, guard, by parameter | **talk only** (and deliver, which the tables write as a talk that takes items): the client sends `15/08` from its NPC dialog only, for a talk condition, with the character it talks to. It counts when the condition is a talk, names that character, **the person stands at the character** (district) and **has what the condition takes** |
| Move in a maze (`11/65`) | counted once per box | counted once per box **where the person stands**: a box far from the place of the person is ignored |
| Items of an episode (`IQuestItems`) | the inventory stood behind it already (`InventoryQuestItems`) | the same; given on taking all or none, taken on finishing, taken back on giving up |
| Rewards (`15/05`) | gold, ether, items (by class too) through the granter; experience and battle points only told; titles only told | **experience** raises the level (`ExperienceService`, with the booster of quests told as bonus), **battle points** into the wallet, **titles** kept (`TitleBook`, a title group of `tb_Title_Reward` gives the title of the class), gold, ether, items, the helper of a helper episode; given once (the episode leaves the book before anything is waited for). An item list of type 1 is items, of type 2 rows of `tb_Quest_Reward` by class, any other type gives nothing |
| Handing in | conditions done | conditions done and, for an episode handed in to a character (`FinishObject`), **near the character** (54003 otherwise) |
| Giving up (`15/06`) | refused by `CannotGiveUp`, items of the episode taken back | the same, **plus** what done conditions took is given back (it has to fit, 54005) and what they gave is taken |
| Failing (`15/11`) | the client removes a failed episode | unchanged: the server marks it failed (`FailAsync`, for the maze to call), the client sees the flag in the list and asks to remove it |
| Helper (`15/09`) | 7 places | unchanged |
| Event update answer (`15/08` from the server) | none | none: the client reads nothing from it |
| Repeated episodes | never finished for good, taken again at once | **a repeated episode may be finished as often as its day allows** (once, the client's table has no count), the day starting at 09:00 local time; the count is kept in the book (`Repeats`). The client's tables have **no repeated episode** (contents: 140 main, 276 side, 8 helper), so this never shows with them |
| Resets `15/10`, `15/12` | sent by game-master commands | unchanged (they forget everything, or one episode) |
| Scripts | take, finish, finish a condition by one step | a script that finishes a condition now finishes it whatever its count. The end of the prologue (`PrologueService`) resets the quests, takes and finishes the skip episode through the same calls; the episodes that follow it and start by themselves are taken with it |
| Maze sectors behind a portal | a portal that names no next sector woke nothing: the second sector of Control Zone 43 never had monsters, so its quest could not be done | the sector a portal warps the player into wakes on arrival (`WorldWarpRequest` of the maze), as the first sector does when the maze has loaded |
| Persistence | the book in `PersonStates` (kind 6), written by the flush, loaded on both servers | the same; the book has one more list (`Repeats`), JSON, **no change of the EF model** |

## Compared with the reference server

Every part above compared with the handler of the reference server that does it: the checks and their order, the error codes,
the messages and their order, the tables read. "Fixed" names the commit of this branch.

51 rows compared: 17 the same, 17 fixed, 17 kept different.

| Part | Reference | Ours | Verdict |
|---|---|---|---|
| Entry | character info, district and maze states, then the list (15/02), then the finished list (15/01), in one step after the load | the same order | fixed (bc1173a) |
| Entry | an episode in progress that is also finished is deleted | the same (`Normalize`) | fixed (bc1173a) |
| Entry | a condition counted to its end but not marked is finished by force (its items given) | it is marked done; its items are not given again | kept different: a book of ours cannot hold such a condition except from old data |
| Entry | episodes with `AutoStart` whose first episode before is finished, neither finished nor in progress, a main one only while no main one is in progress, are taken (no limit) | the same, carried by the list instead of a 15/03 each | fixed (bc1173a) |
| Taking | a blocked person: 50111 | no blocking of persons | kept different: no such state |
| Taking | 30 episodes: 54024 (not for an episode taken by itself or by a script) | the same | same |
| Taking | in progress 54001, finished 54002, level 54018, class 54019, in this order | the same order and codes | same |
| Taking | a class above 99 is a group of classes | taken as any class | kept different: the client's table has classes 1 to 5 only |
| Taking | a repeated episode done as often as its daily count allows: 54019 | the same, once a day (the client's table has no count) | fixed (bc1173a) |
| Taking | episodes before not finished: 54020 | the same | same |
| Taking | items of the episode, room counted, all or none: 54021 | the same | same |
| Taking | no condition: 54004 | the same | same |
| Taking | no check of the character that gives the episode | refused (54020) far from it in a district | kept different: the server knows where the characters of a city stand; an honest client asks at the character |
| Taking | 15/03: episode, helper, repeat info | episode, helper | same for the client (it reads two fields) |
| Taking | collecting conditions set to the items held (at most the count) | the same | fixed (bc1173a) |
| Talk (15/08) | any condition whose target is the parameter, +1; nothing answered | a talk (or a hand-in) whose character is the parameter, only near it in a district, only with the items it takes; nothing answered | kept different: the client sends 15/08 for talks only; the place and the items are checked because the server can |
| Kill | the monster, each of its quest groups | the same | same |
| Kill | a kill in a maze also counts for a hunt that names the maze | not counted | kept different: no condition of the client's tables hunts by maze |
| Collect | every item added to the bags counts its number | the same, through `ItemArrivals` (before: pickups only) | fixed (bc1173a) |
| Sector clear | counts for the sector and the maze of the condition | the same | same |
| Maze clear | counts for a clear that names the maze | the same | same |
| Object used (11/77) | counts the box, for the party when the object says so | counts the box for the person | kept different: party counting is the maze's (other branch) |
| Move (11/65) | once per box, +1, for the party when the condition counts by group, 11/66 to all | once per box, +1, 11/66 to the person, only where the person stands | kept different: the place is checked because the server can; party counting is the maze's |
| Make | once per request, for each product of the recipe | once per item made | kept different: a request makes several items and a condition counts items |
| Upgrade | every try, whatever came of it | the same | fixed (bc1173a) |
| Disassemble | every item taken apart | the same | fixed (bc1173a) |
| Akashic make | counted | counted | fixed (bc1173a) |
| Buy, sell | a purchase and a sale count once | a purchase counts once; selling has no handler | fixed (bc1173a) (buy) |
| Skill deck, mass hunt, events, defence, survival | counted by their features | not counted | kept different: the features are not built; `OnEvent` takes them |
| Condition done | gives its items (room, else not done: 52010), takes its items if held, ends the episode that ends by itself | the same | same |
| Handing in | all conditions done, no check of the character | the same, and near the character it is handed in to (54003) | kept different: the place is checked because the server can |
| Handing in | the items the episode takes are taken before the reward | taken after the reward is given | kept different: a reward that does not fit leaves the items where they were |
| Reward items | list type 1 items, 2 rows by class, other none; 52010 when they do not fit | the same | fixed (bc1173a) |
| Reward choice | type 2 looks the row up by the reward list (not the choice) | by the choice | kept different: no episode of the client offers a choice |
| Titles | the title group gives the title of the class, kept | the same | fixed (bc1173a) |
| Experience | the reward times the booster of quests, the bonus told | the same, raising the level (before: only told) | fixed (bc1173a) |
| Gold, battle points, ether | added unless over the cap (52056/52057), the episode is finished anyway | added (battle points before: only told); no cap | fixed (bc1173a) |
| Helper | only for an episode of helpers | given when the row names one (only helper episodes do) | same |
| Awakening, class skills | given | not given | kept different: the client's table has no awakening and no class skill |
| Finish | the bit unless repeated, 15/05, the linked episodes that start by themselves, achievements | the same order | fixed (bc1173a) |
| Two requests at once | a flag refuses the second | the episode leaves the book before anything is waited for | same |
| Giving up | not there, or the episode forbids it: 54005 | the same | same |
| Giving up | the items given on taking (quest items only) go back | all items given on taking go back | kept different: they are quest items |
| Giving up | done conditions: what they gave goes back, what they took is returned (room, else 54005) | the same | fixed (bc1173a) |
| Giving up | an item named by the episode is sent by post | none | kept different: the client's table has no such column |
| Giving up | 15/06 once the database has it | 15/06 at once | same for the client |
| Failing | the flag and 15/11; the client asks to remove; removed without an answer | the same | same |
| Helper (15/09) | not there 54004, full 54006, the same flag accepted, 15/09 | the same | same |
| Repeated episodes | the day starts at 09:00, the count of a day resets, a message tells the client | the same day and count; nothing tells | kept different: the client 1.4.2.3 has no such message |
| Resets (game master) | 15/12 for one, 15/10 for all | the same | same |
| Maze sector behind a portal | the sector comes to life when the player enters it | a portal naming no sector woke nothing; now the arrival wakes it | fixed (54d4448) |


## What each message carries

Requests: `15/03 int episode`; `15/05 int episode, int chosen` (the item chosen, or the row of `tb_Quest_Reward` for a list by
class); `15/06 int episode`; `15/08 int episode, int condition, long character`; `15/09 byte add, int episode`;
`15/11 int episode`. Answers: `15/01 128 bytes` (bit = `CompleteBit`); `15/02 short count, (int episode, byte helper, short
done bits, byte failed, 10 × (int condition, byte step))`; `15/03 int episode, byte helper`; `15/05 int episode, int exp, int
gold, int bp, int ether, int title before, int title after, uint helper, byte count, (int item, short count)`; `15/06 int
episode`; `15/07 short count, (int condition, byte step)`; `15/09 byte, int`; `15/11 int`; `15/12 int`; `15/10` empty.

## The conditions of the client's tables

667 conditions, by type and target (in a maze when the condition names one):

| Type | Target | Count | Counted by |
|---|---|---|---|
| Talk 0 | NPC | 175 district, 28 maze | the client, `15/08`, checked (character, place in a district, items) |
| Move 1 | collision box 23, sector 1 | maze | the maze, `11/65`, where the person stands |
| Trigger 2 | world object | 80 maze | the maze, `11/77` |
| Hunt 3 | monster 102, monster group 52 | | the maze, kills |
| Collect 4 | item | 147 | the bags |
| Survival 7 | NPC | 1 maze | **not counted** (survival mode of a maze, not built) |
| Guard 9 | monster | 4 maze | **not counted** (escort of a maze, not built) |
| Clear 10 | sector 38, event 2 | maze | the maze; the events (hidden event, daily mission) are not reported |
| Make 13 | item | 10 | production |
| Upgrade 14 | item | 1 | upgrade, every try |
| Skill 15 | skill deck | 1 (tutorial maze) | **not counted**: the server does not know the bonuses of the skill deck; `QuestManager.OnEvent` takes it once it does |
| Disassemble 16 | item | 1 | taking apart |
| Akashic make 17 | item | 1 | akashic production |

No condition buys or sells, delivers, protects or traces; buying is counted anyway, selling has no handler yet.

## Columns read

`tb_Quest_Episode` (named in `Structures.json`): `Chapter`, `LinkEpisode1..5`, `AutoStart`, `StartObjectType`, `StartObject`,
`FinishObjectType`, `FinishObject`, besides the ones named before. The class skills a reward could teach (four columns after
the choices) are zero in every row and are not read. `tb_Title_Reward`: a group and the title of each of the six classes.

## Assumptions and what to check with the real client

- The client keeps `15/02` and `15/01` sent with the character info until its play scene reads them (the reference sends them
  at the same moment). If the quest window is empty after entering, this is where to look.
- The distance at which a character can be talked to is ours: 1500 units on the ground from where the server last saw the
  person. A talk the client allows farther away (lag) is ignored without an answer, as the reference ignores a refused talk.
- A talk in a maze is trusted: the server does not place the characters of a maze itself.
- Every item made counts for production (the reference counts a request once); every upgrade try counts.
- A repeated episode is allowed once a day; the client's table has no such episode and no count.
- The error for taking an episode far from its character (54020) and for handing it in far from it (54003) are ours.
- The title of a reward is told with the title of the class, the client is told the new title by `TitleBook` as well.

## Verified

- Unit tests (`QuestManagerTest`, `QuestInventoryTest`, `QuestRulesTest`): the order of the lists at entry, the items of an
  episode given when taken and taken when finished with the real inventory, the reward given once with its title, a repeated
  episode back after its day, a talk refused far from its character and a hand-in refused without its item, an episode that
  starts by itself after the one before it and at entry, production and the items it makes counted.
- Stand (`docs/tools/e2e_quests.py`, a local tool, `OW_STAND=7`, 35 checks, about twelve minutes): a new character with the
  tutorial written finished takes 100020 only at its character (refused far from it), talks to 101026 only there and only
  naming it, hands it in for 194 experience and 11 gold once (03/37, 08/20), takes and hands in 100025 (209, 12), takes 100035
  at 101022; in Control Zone 43 it clears the first sector, walks through the portal into the second and clears it, after
  which 100035 ends by itself and 100040 starts by itself; through the next portal the three monsters of the last sector
  bring the boss, whose death finishes 100040 and starts 100045; the talk in the maze counts; signed out and in, 100045 is in
  progress with its talk done and the finished episodes are kept; at 101021 it is handed in for 221 experience, 54 gold and
  the titles 100100 and 200100. The tutorial itself is the prologue's (`OW_PROLOGUE=true` plays the story from it).
