# Using items (08/11), their cooldowns and the limits of a day

How an item is used, as the code does it now. The client 1.4.2.3 decides every layout; the original game server decides the
rules, the order of the checks and the codes. Earlier notes: `docs/notes-item-setup-handlers.md` (the work on items at NPCs),
`docs/notes-buffs.md` (the buffs a potion gives), `docs/notes-maze-results.md` (the fatigue service).

## The request and what answers it

The client sends 08/11 with a bag (`StorageGroup`), a slot and the serial of the item it believes is there. It sends nothing
else: no count. The server uses one item. `ItemUseService.UseAsync` answers with an `ItemUseResult`: used, or the code of an
error, which `ItemServiceBase` sends as the error answer of 08/11 (`sub | 0x80` and a `u16`).

| Message | Layout (the client's reader) | When |
|---|---|---|
| 08/11 `ItemUseResponse` | `i32 item`, then seven `i32` (experience, gold, battle points, ether, two titles, helper), then `u8 count` and each item as `i32 id` and `u16 count` | a box or a package was opened (the window of the reward), a title was given (nothing in the gain) |
| 08/13 `ItemUseBtResponse` | `i32 actor, i32 item, i16 slot` | a potion was used; to everyone who sees the player and to the player, whose client starts the cooldown of the slot from it |
| 08/14 `ItemEquipSlotOpenResponse` | `i32 mask`, one bit for each opened place of the equipment | a place was opened, and at entry when the person opened any |
| 08/48 `ItemUseInfoListResponse` | `i64 updated at`, `u8 count`, each `i32 type, u8 count, i64 used at` | entry into a district and into a maze |
| 08/49 `ItemUseInfoUpdateResponse` | the list of 08/48 without the time | the game day turned and the counts went back to nothing |

08/48 is **not a list of cooldowns**: it counts how often the person used each kind of item today. The client reads the count of
the fatigue items from it to tell the person that the day's limit is reached. The cooldown of a slot in the bag is the client's
own, started by 08/13 from the item's cooldown columns.

## The checks, in the order of the original

Every item goes through these; the first that fails answers, and nothing is changed by a refusal.

| Order | Check | Code |
|---|---|---|
| 1 | an item of the serial is in the slot of a bag (common, costume, cash, cube) and its tables are there | 52011 |
| 2 | the person has the level the item asks for | 52601 |
| 3 | the person is of the class the item asks for | 52009 |
| 4 | a person who is down uses nothing, unless the kind says it can (use state bit 8) | 52026 |
| 5 | the cooldown group of the item is not running | 52602 |
| 6 | the item has at least one in the stack, and the way the maze is played takes items | 52011 |
| 7 | what the kind asks (below) | the kind's |

The cooldown of a group is the item's time in milliseconds, cut to whole seconds (a time under a second is no cooldown); every
item of the group is held by it. A used kind starts it. An item marked as saved keeps its cooldown in the document of the person;
no item of the client's tables is marked, and a cooldown that is not saved ends when the person changes server, as it does in
the original (it clears the cooldowns at every area it loads).

## The kinds

| Kind | What it does here |
|---|---|
| Potions and the like (`EFFECT`, 91 items) | A kind of item has a use state: bits for a district (1), a maze (2), standing (4), fallen (8). A potion is for mazes: the use state names the place, a maze that forbids potions (its row of the server table) or the infinite tower (52612) refuses. In a wrong place 52616. The item gives its buff (the buff engine of the maze, `notes-buffs.md`) for sure, is used up by one, starts its cooldown and plays 08/13. Items that bring a person back are refused (52607): a revive item is used from the window of the fallen, which sends the same request; `feature/maze-entry` owns that. An aura item is refused. A district has no buffs |
| Fatigue items (`FP`, `FP_FREE`) | Three of the first kind may be used a game day (52610, counted by `ItemUseLimits`, kept in the document, started again at the game day of the daily missions), none of the free kind is limited. Saved fatigue or a full day refuse (52611, `FatigueService.Restore`). Both count their use for 08/48 |
| Boxes and packages (`PACKAGE`, `RANDOM_BOX`, `BOX_CLASS`) | Only from the common and the cash bag. What comes out must fit (52010), the box is used up, the items and the money arrive, the client gets 08/11. A class box opens the box of its class (`tb_Item_Classreward_Link`) |
| Titles | The title is given unless the person has it; used up, empty 08/11 |
| Boosters, akashic record cards | As before; a booster has its cooldown, a card counts for 08/48 |
| Places of the equipment (`EQUIP_SLOT_OPEN`) | From the common bag, the slot type names the place (101 to 110) and must not be open yet. The mask is in the document of the person; the places are not locked by it |
| The infinite tower's ticket | 52613 |
| Everything else | 52011: experience items (no item of the tables has the effect they need), gacha, casual mazes, social objects, warps, appearance, count boxes, photos, quests, reseal and renovation |

Used by their own requests, and so by items the server takes there:

- A skill reset (06/72), the reset of one step of one skill (06/7A) and a new place in the deck (06/76) each take an item **of
  their kind** from the slot the request names (the cash bag for the deck). Without one the answer is the error 52011 (the original
  says nothing). The reset of one step takes a divergence back to the one before it, or a level back to the level before it,
  gives the points of the step back and puts the earlier level in the decks; a skill on its first level cannot go back, and the
  item stays. Learning, the deck and the divergence stay free of items.
- A step of the bag or the bank (08/0F) takes the item the table names, or the gold it names. A storage with no next step, a level
  too low and a payment the game does not take are 52023, too little gold 52301.
- No request of the client 1.4.2.3 carries a name change, a league name change or a character slot item.
- Fever items are used when they are picked up in a maze (their use state says so), not by 08/11: not built.

## Where it is kept

- `PersonStates` kind `ItemUse` (12): the cooldowns the items save, the counts of the game day (with the time of each) and the
  mask of the opened places. Marked as `MiscState`, loaded at entry by `ItemUseLimits.EnterAsync` in a district and in a maze.
- Columns named in `Structures.json`: `tb_Item` `SubType`, `CooldownGroup`, `CooldownMilliseconds`, `CooldownSaved`;
  `tb_Item_Classify` `Group`, `UseState`. `MazeRuleRow.PotionLimit` of the server row of a maze.
- `IItemField` (the maze registers `MazeItemField`; a server without one answers as a district) and `IItemTitles`.

## Tests

`ItemUseTest`: the places of a potion, the order of the checks, the cooldown of a group in whole seconds, the limit of a day and
the item a service takes. `ItemWireFormatTest`: the bytes of 08/11, 08/13 and 08/48. The scripted client `e2e_item_use.py` ran
on the stand: a potion refused in a district; a package with its 08/11; the fatigue limit and 08/48 after a relog; the skill reset,
the reset of one step and the deck place with and without their items; the place of the equipment and its mask after a relog; a bag
step kept after a relog; a potion in a maze with 08/13, the buff and the refusal inside the cooldown.

## What a play session must settle

- The window of the reward after a box is opened (08/11), and what it shows for a title (the gain is empty).
- The cooldown shown in the bag after a potion (08/13), and that the slot cannot be used inside it (the client refuses first).
- The motion of the use seen by a second player.
- Whether the client shows the memory places of the equipment locked before any 08/14 (the server sends the mask only when the
  person opened a place), and what the name-change dialog of a name-change item does.
- The line the client shows for the count of the fatigue items (08/48).

## Open

- A revive item (sub type 2 or 1 of an effect item) is refused; the rules of the revive window are `feature/maze-entry`'s.
- Buffs of potions in a district; the aura items.
- The blocked state of a person (a trade, a shop) that the original checks before it uses an item; the hidden-event condition and
  the titles opened by the use of an item.
- The money overflow check and the unpack types of a package row (an item given already upgraded).
- The places of the equipment are not locked until opened.
- The achievement the original counts for each potion or fatigue item.
