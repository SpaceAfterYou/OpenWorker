# The work on items (group 0x18)

Repairs, upgrades, sockets and the tools that restore, unseal or lower a count of an item, at the NPCs of a city. The
handlers are `ItemSetupService` in `OpenWorker.DistrictServer`; the rules are in `OpenWorker.Gameplay.Modules.ItemSetup`:
`ItemRepairService`, `ItemRestoreService`, `ItemSocketService`, `ItemUpgradeService`, and `ItemSetupAnswers`, which sends the
answer or the error of a request. Every request a handler takes is answered, refused or not: the client keeps its dialog
waiting until it hears back. A handler changes items through `InventoryService` and `Inventory.Touch`, so the dirty mask of
the person carries the change to the flush; nothing in them waits or throws on the loop.

## Where it runs

District only. A request that names an NPC is served only by an NPC of the city near the player that offers the service
(done: `docs/notes-npc-services.md`, "The NPC behind a service").

## The schema

Migration `ItemSocketsAndBroaches` adds four columns to `Items`, all `NOT NULL DEFAULT '{}'`, so the items written before
read as items without gems or broaches:

| Column | Type | What |
|---|---|---|
| `Sockets` | `integer[]` | the gem of each socket, by the place of the socket; 0 is an empty socket |
| `SocketOptionType` | `smallint[]` | five option types per socket, in the order of `Sockets` |
| `SocketOptionValue` | `integer[]` | five option values per socket |
| `Broaches` | `integer[]` | the fifteen broaches set into the item (three groups of five), or empty |

The serial of the socket and of the broach list is the serial of the item itself. `ItemInstance` keeps them as `Sockets`
(`ItemSocket`: gem and options) and `Broaches`; the flush writes them with the rest of the item. A letter of the post keeps
them in its JSON items (`PostItemPersistent`, nullable, so the letters written before still read), and an item that changes
hands in a trade keeps them.

At entry the answer to the bags (`08/01` for the common bag) is followed by `08/55` (the sockets of every item) and `08/56`
(the broaches of every item) when the person has any. `08/61` and `08/62`, the updates of the same lists, are declared with
the same layout.

## The server tables folder

The chances of an upgrade are in a table only the server has, under the same name as a client table (`tb_Reinforce`), so it
cannot share the folder of the client tables. Its row is marked
`[BinaryResourceTable("tb_Reinforce", Server = true)]` and is read from `res-server/` only; the convention for all
server-only tables is in `ARCHITECTURE.md`, "Table folders". Without the table every upgrade is refused.
`OPENWORKER_TEST_SERVER_RES` points the tests at the same files.

## What each request does

| Sub | Request | What the handler does | Answer |
|---|---|---|---|
| 01, 03, 04, 13, 18 | Make, Exchange, Disassemble, Evolution, DisassembleEx | see "Round 3" below | |
| 02 | Upgrade | the cost of the level the item is at from the client's `tb_Reinforce` (gold, battle points, ether, two materials and their counts), all checked before anything is taken; then a roll from 1 to 10000 against the server's `tb_Reinforce` of the same id: up to the chance of going up the item gains a level, past it and up to the chance of breaking it breaks (flag 2), past that it stays. From level 4 an item that protects it (use type `UPGRADE_HELP`) may be named, and is used up; it then breaks instead of the item. An item with a limit of tries (`UpgradeLimit`) spends one try on every upgrade and is refused when none are left. A broken item, an item at the last level of its row and an item without a row are refused. | `18/02`: result (1 up, 2 stayed, 3 broken, 4 the protection broke) and the item |
| 05 | SocketEquip | one gem at a time: the gem lies in the common bag, is a socket gem (use type `ITEM_SOCKET_EQUIP`) of no higher level than the item; the socket is open and empty; the gold of `tb_Socket` is charged. The gem is used up, the socket takes the gem and the options of the gem, and an unbound item is bound. | `18/05`: the socket list of the gem; an update of the item when it was bound |
| 06 | SocketActive | only the next socket opens, up to the sockets of the row of `tb_Socket` (through the socket column of `tb_Item`); its gold and its item are charged. | `18/06`: the item and the socket |
| 08..11 | Repair, RepairNpc, RepairEquip, RepairAll | as in the first round (`ItemRepairTest`) | `18/08`..`18/11` |
| 22 | Restore | the tool is a restoring item (use type `WEAPON_RESTORE`); the item is broken (flag 2) and has been restored fewer times than the restore limit of its row of `tb_Item`. The flag clears, the count of restorations grows, the tool is used up. | `18/22`: serial and restorations |
| 24 | Unseal | the tool is an unsealing item (use type `UNSEAL`); the item is bound and has no gem in its sockets; its count of unsealing is under the seal limit of its row, or the limit is 255 (no end, the count does not grow). The item is unbound, the tool used up. The request is read tool first; a request with the two slots the other way round is understood as well. | `18/24`: serial and unsealings |
| 26 | UseEffect | a tool that lowers the restorations (`RESTORE_COUNT_DEC`) or the upgrade tries (`UPGRADE_COUNT_DEC`) of an item by the value of its row (never below 0); the tool is used up. | an update of the item, then `18/26` (empty) |

Error codes are the client's, one per reason where one is known: upgrade 52201 (cannot be upgraded further),
52210 (gold), 52219 (wrong item or protection, no tries left, no row), 52220 (battle points), 52223 (materials), 52224
(ether); sockets 52280 (gem or item wrong), 52282 (gem level), 52283 (socket not open or full), 52284 (gold), 52286 (no
sockets), 52288 (gold), 52289 (not the next socket), 52290 (the opening item); unseal 52510; use effect 52521; a slot that
holds nothing, and every refusal of a restore, 52012.

## Requests without a handler

Of the 38 requests of the group, 22 have a handler now; 16 have none, and the client of this version never sends any of
them (see "What the client sends and reads" in the research notes of round 1). Every request the client sends is handled.

## Assumptions

- The columns of `tb_Item` read as the restore limit (the last byte of a row), the seal limit (the byte before it) and the
  value of a tool (the byte after the endurance) were named from their values over the table, not from the client's use of
  them.
- The socket id of a gem is the item of the gem, and its options are the options of the gem item as it lies in the bag.  A new
  item gets its rolled options when it is made (`docs/notes-stats.md`), so a gem carries them into the socket.
- Which sockets an item may have is only its row of `tb_Socket`: few items point at a row the client has, so most items have
  no sockets to open. Gems are not limited by their category or by the kind of item.
- The tool that lowers the count of unsealing may be used only once per item, which the item does not keep: it is refused.

## Not done

- ~~The NPC of a request is not checked~~: done, `docs/notes-npc-services.md`.
- ~~Nothing wears endurance down on attack, defence or death yet.~~ Done with the server's own `tb_Item_Endurance`:
  `docs/notes-stats.md`, "Endurance".
- ~~The credit of an NPC and the boosters that lower prices or raise chances~~: done for repairs, upgrades and production,
  `docs/notes-npc-services.md`.
- ~~The stats of an item do not change with its gems or its upgrade level on the server.~~ Done: `docs/notes-stats.md`. A new
  item now gets the options of its row of the server's `tb_Random_Option` and the sockets its row opens, so gems carry options.

## Tests

`ItemSetupTest` (with `OPENWORKER_TEST_RES`, and `OPENWORKER_TEST_SERVER_RES` for the upgrade): an upgrade with a fixed roll
goes up at the highest roll that still succeeds and breaks at the next, charging the gold of the client's table; opening a
socket and setting a gem puts the gem in, uses it up and shows it in the entry load and in the saved row; a restoring item
mends a broken item and an unsealing item frees a bound one. `ItemRepairTest` shares the fixture. `ServerTablesTest` reads
the server `tb_Reinforce` to its end.

## Round 3: disassembly, exchange, evolution and production

The handlers of these five requests are `ItemCraftService` in `OpenWorker.DistrictServer` (its own class, so that the
handlers of the other item work stay in `ItemSetupService`); the rules are `ItemMakeService`, `ItemExchangeService`,
`ItemDisassembleService` and `ItemEvolutionService` in `OpenWorker.Gameplay.Modules.ItemSetup`. Each checks everything
before it takes anything, so a refusal changes nothing, and each refusal answers with the client's code.

| Sub | Request | What the handler does | Answer |
|---|---|---|---|
| 01 | Make | a recipe of the client's `tb_Make`, 1 to 100 items: the gold and the battle points of the recipe times the count, then each of its four materials times the count (at most 32 000 of one), then room for the product; the materials, the gold and the battle points are taken and the product is given, `count` of it | `18/01`, empty: the client closes the production dialog |
| 03 | Exchange | a row of `tb_Reinforce_Exchange`: the count the client sends is how many products it wants and must be a whole number of exchanges (a multiple of the product count of the row); each exchange takes the material count of the row | `18/03`, empty: the client refreshes the upgrade or the akashic dialog |
| 04 | Disassemble | the tool (first slot) lies in the common bag and is a disassembly tool (use type `DISASSEMBLE`); the item (second slot) lies in the common, costume or cash bag and its row of `tb_Item` names a disassembly row. The row of the server's `tb_Disassemble` gives ether between its two bounds and, by a roll from 1 to 10 000 over the chances of its seven items, one item in a count between the two bounds of that item (or none when the roll passes them all). The tool and the item are used up (one of a stack) | `18/04`: what came out |
| 18 | DisassembleEx | the same for up to 20 items at an NPC, without a tool; a stack may be named once for each of its items. Each item has its own roll | `18/18`: what came out of each |
| 13 | Evolution | the item lies in the common bag, is upgraded to 9 or more and its row of `tb_Item` names a row of `tb_Item_Evolution`; the gold, the battle points, the ether and the material of the row must be there. A roll from 1 to 10 000 up to the success rate of the row evolves the item: it gives way to one of the six items of the row, by a second roll over their chances, which keeps the upgrade level of the old one. A failed roll costs the same and leaves the item | `18/13`: 1 and the serial of the new item, or 0 and 0 |

The answer of 04 and 18 is `int n`, then for each item taken apart `int item, int result, short count, int ether` (the
client writes one line in the chat for each, message 881, or message 882 when the list is empty). It is a generated format.
The evolution request carries the whole item as the client sees it after the slot; `ItemValue` reads itself from a packet now
(`IWireReadable`), and the handler takes the item from its own bag, not from the request.

### Columns named

- `tb_Item`: `Evolution` (the `Int32` after `Reinforce`, the row of `tb_Item_Evolution`; only one item of the client has one)
  and `Disassemble` (the `Int16` after it, the row of `tb_Disassemble`; 179 of its 182 values are ids of the client's
  `tb_Disassemble`).
- `tb_Make`: `Product`, `Material1..4`, `MaterialCount1..4`, `BattlePoint`, `Gold`. The two columns after the group (values
  2100..4150 and 1406..4156) look like the tabs of the production dialog and stay unnamed; `Field5` and `Field6` (two more
  products) are 0 on every row.
- `tb_Reinforce_Exchange`: `Material`, `MaterialCount`, `Product`, `ProductCount`; `Field1` is the kind of NPC that offers it.
- `tb_Item_Evolution`: `SuccessRate`, `Material`, `MaterialCount`, `Ether`, `Gold`, `BattlePoint`, `Result1..6`,
  `ResultRate1..6`; `Field1` is not read.
- The server's `tb_Disassemble` (`DisassembleResultRow`, `Server = true`): the id, the bounds of the ether, seven items, their
  chances out of 10 000 and the bounds of their counts. The client's table has the id column only. Without the server's
  table nothing can be taken apart (every request is refused with 52272).

### Error codes

Make 52254 (materials), 52256 (no room), 52257 (no recipe, a count of 0 or over 100, too many of a material), 52310 (gold or
battle points). Exchange 52261 (no row), 52262 (the product does not exist), 52264 (materials or room), 52265 (not a whole
number of exchanges). Disassembly 52012 (a slot that holds nothing), 52058 (the ether would pass 99 999 999 999 999), 52270
(an empty list or more than 20 items), 52271 (the tool not in the bag, the item not in a bag of items), 52272 (not a tool,
an item that cannot be taken apart, no row), 52273 (no room). Evolution 52012 (the item is not in the common bag), 52471
(no item, the material short), 52472 (no row, upgrade below 9), 52473 (gold, battle points or ether), 52474 (the row gives no
item), 52475 (no room).

### Assumptions

- The room for what comes out is counted as if what is used up stayed in its slot (only the item taken apart by a tool, or
  the evolved item, frees its own slot): a full bag that the used-up items would have made room in is refused.
- An item evolves only from the common bag: the new item goes into the bag, and an equipped item would leave the equipment
  without the stats being worked out again.
- The second roll of an evolution picks the first item whose chance covers what is left of the roll (the same rule as
  disassembly), so a row whose chances make 10 000 always gives an item.
- The two bytes that follow the count of a make request are not read.
- Production has no limit of items per day or week: the client's `tb_Make` has no columns for it.

### Not done

- ~~The NPC of a request is not checked~~: done, `docs/notes-npc-services.md`.
- A product whose hero is not open to players is not refused. (The credit of an NPC now lowers the gold of a recipe and
  the materials past the first: `docs/notes-npc-services.md`.)
- The new item of an evolution keeps the upgrade level only, not the options, gems or broaches of the old one.

### To check with the real client

- That the chat lines of 04 and 18 name the item taken apart, what came out, its count and the ether.
- That the upgrade dialog shows a successful and a failed evolution from `18/13` (the only evolution row the client can reach
  is item 113000030 at upgrade 9).
- That the count the exchange dialog sends is the count of products, not of exchanges.

### Tests

`ItemCraftTest` (with `OPENWORKER_TEST_RES`, and `OPENWORKER_TEST_SERVER_RES` for disassembly): a disassembly with a tool and
one of two items at an NPC take the items and give the first item and the least ether of their row; an exchange takes the
materials of two exchanges and gives two of the product; an evolution turns the item into the item of its row with the same
upgrade level and charges the row; a make takes the materials and the gold of two items and gives two; a make that lacks a
material changes nothing. `ItemWireFormatTest` reads an evolution request whose item was written by the writer of the server.
`ServerTablesTest` reads the server `tb_Disassemble` to its end.

## Round 3: akashic records

`AkashicService` (Gameplay, `Modules/ItemSetup`) handles AkashicMake (14), AkashicDisassemble (15), AkashicCompose (17) and
AkashicMakeEx (25), and the use of a card from the bag (`ItemUseService`, use type `AKASHIC_RECORD`).

### What an akashic record is on this client

- A **card** is an item of the bag (use type 5, `AKASHIC_RECORD`) whose id is also a row of `tb_Akashic_Records`. It carries
  experience in the `Exp` of the item, which the client shows as a gauge against what making the next card needs.
- The **akashic record book** is a per-person list of cards, one per place of the book (the `Position` column of the card's
  row). The skill window shows it. Using a card from the bag puts it into the book and removes it from the bag; taking it out
  (AkashicDisassemble) puts it back into the bag.

### The schema

There is no new table or column. The book is a document of the person (`PersonStates`, kind `AkashicRecords` = 8, jsonb:
`{"Records":[{"Card":…,"Position":…,"Exp":…}]}`). It is kept on `Inventory.AkashicRecords` and loaded with the items
(`InventoryPersistence`). A new part of the dirty mask, `PersonSections.AkashicRecords` (1 << 7), writes it at the next flush
whenever a card goes into the book or comes out. `dotnet ef migrations has-pending-model-changes` reports no change.

### Messages

| Message | Layout | Sent |
|---|---|---|
| `08/57` AkashicRecordLoad | `int count`, then `count × (int card, int position)` | at entry, with the quick slots, after the client asks for the common bag (always, even when empty: the client marks the book loaded) |
| `08/58` AkashicRecordAdd | `int card, int position` | when a used card goes into the book |
| `18/14` AkashicMake | empty | the answer to both AkashicMake and AkashicMakeEx; the client has no receiver for `18/25` |
| `18/15` AkashicDisassemble | `byte result (1), int card, int position` | the client drops the card at that position from the book whatever the byte, so a refusal is only ever an error answer |
| `18/17` AkashicCompose | `long serial, int exp` | the client sets the experience of the item with that serial |

Requests: AkashicMake is `int npc, int index, slot`. AkashicMakeEx is `int index, int npc, slot, byte n, n × slot`: the recipe
comes **before** the NPC, unlike AkashicMake. The round 1 declaration had the order wrong; it is fixed here. AkashicDisassemble
is `int npc, int card`. The card is the id of a card in the book, not a slot. AkashicCompose is `int npc, slot target, byte n,
n × slot`, and the client sends at most five materials.

### Columns named

- `tb_Akashic_Records`: `Position` (place in the book, unique per row), `RarePoint`, `Disassemble` (row of
  `tb_Akashic_Disassemble`), `ExpGiven` (experience the card gives when it is fed to another), `MakeExp` (experience a card
  needs to make the next one; 0 on the last card of a line).
- `tb_Akashic_Make` (id = the card made): `MaterialKind` (1 a card, 0 a plain item), `Material`, `Card` (a card item the recipe
  also takes, or 0), `ExtraCount` (extra cards, 0 to 2; a recipe with extra cards has no card item), `Gold`, `Result`.
- `tb_Akashic_Disassemble`: `Item` (what comes back; the card itself in every row), `CostKind` (0 gold, 1 battle points,
  2 ether; only 0 in the client's table), `Cost`. The second column (10000 in every row) stays unnamed: nothing reads it.

### Rules

- **Make** (14, 25): the recipe of the index. The material lies in the common or cash bag and is the recipe's material; a card
  material must be full (its `Exp` at least the `MakeExp` of its row). The request must name exactly `ExtraCount` extra cards,
  and when the material is a card they must have its rarity. The card item of the recipe is needed when the recipe names one.
  Then the gold. Then room for the result. All of it is taken, and the result is given with experience 0. Refusals:
  52012 for a slot outside the bags, 52002 when the items do not fit the recipe, 52437 for gold, 52010 for no room. Each is
  sent with the sub of the request.
- **Compose** (17): the target is a single card that is not full and whose row has a `MakeExp`. Every material is a card in
  the bags other than the target. The target gains the sum of the materials' `ExpGiven`, capped at `MakeExp`, and the
  materials are used up. Feeding is free (the client's table has no price column). Refusals: 52012 and 52002.
- **Use a card**: the card goes into the book at its place with its experience, and one card leaves the bag. A place that
  is taken answers `08/11 | 0x80` with 6028 ("an A.R card already obtained"), and the card stays.
- **Disassemble** (15): the card must be in the book at its place. Its row of `tb_Akashic_Disassemble` gives the item and the
  price. The item comes into a free slot (never a stack) with the experience the card had, and the price is charged. 52002
  for a card not in the book or without a row, 52437 for the price, 52010 for no free slot.

### Assumptions

- The card item column of a make recipe is consumed. The client requires the person to own one, and the request does not
  name its slot, so the server takes one from any stack.
- A plain-item material (kind 0) has no experience to check. The rarity rule for extra cards applies only to a card material.
- The NPC of the requests is checked as for the other handlers of the group (`docs/notes-npc-services.md`); compose asks for the akashic
  function, make and taking out only for an NPC near the player.
- No experience bonus (events, a lucky compose) and no quest condition (`AkashicMake`) are applied yet.
- A card taken out of the book is created with the bind type of its item row, like any new item.

### To check with the real client

- The book loads at entry (the skill window shows the cards) and `08/58` adds a card after the confirmation dialog of a use.
- The make dialog closes after `18/14` both for a recipe with extra cards (sent as 25) and without (14), and does not stay
  waiting on 25.
- The compose result: the gauge shows the new experience.
- The disassemble result window shows on `result = 1`.

### Tests

`ItemSetupTest`, on the client tables:
- a recipe with extra cards (sent as AkashicMakeEx) takes the full card, the extra cards and the gold, and gives the card;
- feeding cards adds their experience and stops at `MakeExp`;
- a used card goes into the book and comes back out with its experience, for the price of the table;
- a refused make (a card that is not full) and taking out a card that is not in the book change nothing.

`PersistenceTest.ALogoutAndANewLoginKeepWhatThePlayerHad` also checks that a card of the book survives the logout and loads
at the next entry.

## Round 3: broaches

Branch `feature/item-setup-3-broach`. The three broach requests the client sends are handled: `ItemBroachService` in
`OpenWorker.Gameplay.Modules.ItemSetup`, registered in `ItemSetupService`. The schema did not change: the fifteen broaches
are `Items.Broaches`, the places are the fifteen characters of `Items.BroachState`.

### Places on clothing

Clothing (use type `SHAPE`) has fifteen places for broaches, five groups of three. The first place of a group takes an
attack broach, the second a defence one, the third a utility one (classify 160201 and 160204, 160202 and 160205, 160203 and
160206). A place is `'0'` (the clothing has none), `'1'` (open) or `'2'` (closed until an item opens it), as the client reads
the broach state of an item. The row of the clothing in `tb_Costume_Socket` (through the socket column of `tb_Item`) gives,
per group, the open places and then the closed ones; a group of more than three ends the groups there.

`CostumeBroachPlaces` (Items) builds that state. `InventoryService.Create` gives it to every new item, and
`InventoryFactory.Load` gives it to clothing whose saved state is still all `'0'` (clothing made before this round); for an
item that is not clothing with a row the state stays all `'0'`, so neither changes anything else. An empty place in the
broach list is `-1`, which is what the client fills its own list with.

### What each request does

| Sub | Request | What the handler does | Answer |
|---|---|---|---|
| 20 | BroachEquip | reads the clothing first, then the broach and the place (0 to 14); the round-1 declaration had the two slots the other way round. The place must be open and of the kind of the broach (place mod 3), the broach of use type `ITEM_BROACH`. The broach is used up; a broach that was in the place is lost. A bound broach binds the clothing: a free one becomes 2, a bound one (1) becomes 3. | `18/20`: the serial of the clothing and its fifteen broaches (an update of the clothing first when its binding changed) |
| 21 | BroachActive | the clothing has a row and the place is closed; the item and the count of the row (`OpenItem`, `OpenCount`, 834000006 × 1 on every row of this client) are taken from the bags; the place opens. | `18/21`: the serial of the clothing and its new state (`long`, then a `short` length and the characters, 1 to 15 of them) |
| 23 | BroachCompose | the first slot is the composing item (use type `BROACH_COMPOSE`), then exactly five broaches, in the common or the cash bag, unbound, a slot named twice needs two in its stack. Each broach counts 2, 4, 6, 8 or 10 for its rank (2 to 6), 1, 10 or 100 for its kind and 1, 10, 100 or 1000 for its theme (the effect value of `tb_Item`, 1 to 4). The sums pick the rows of `TB_BroachCompose_Rank`, `_Type` and `_Theme`; a roll from 1 to 10000 against each row gives the rank (1 to 5), the kind (1 to 3) and the theme (1 to 4) of the result, falling through the chances in order. The group `theme × 100000 + kind × 10000 + rank × 1000` of the server's `TB_BroachData` says on its first row how many broaches it has; a last roll picks one. The result must be an item of the client and fit in the bags. The five broaches and one composing item are used up, the broach is given. | `18/23`: the serial of the new broach, after the bags were told about it: the client looks the broach up by that serial to show it. A refusal answers the serial 0, which is how the client ends the wait of its compose window. |

Setting and opening are refused with `18/A0` and `18/A1`, code 52012 for every reason: the client has no more specific
code for them that is known.

### Tables

- `tb_Costume_Socket` (client): columns named `OpenPlaces1..5`, `ClosedPlaces1..5`, `OpenItem`, `OpenCount`. The byte after
  the id stays unnamed (a count of pieces, not used).
- Server-only, in `OpenWorker.Res/ServerRows`: `BroachComposeRankRow` (`TB_BroachCompose_Rank`, `Optional`),
  `BroachComposeTypeRow` (`TB_BroachCompose_Type`, `Optional`), `BroachComposeThemeRow` (`TB_BroachCompose_Theme`,
  `Optional`), and `BroachComposeItemRow`, the server's own `TB_BroachData` (`Server`, read from `res-server/` only: the client
  table of the same name has only the index and the item, and the first theme only). Without them every compose answers 0.
- `TB_BroachData` and `Tb_Broach_Set` of the client are not read: the first is the client's list of broaches, the second the
  set effects of broaches, which the server does not apply yet.

### Assumptions

- The bind values 2 and 3 for clothing a bound broach was set into are the server's own; nothing of this client was checked
  for them. No broach of this client's tables is bound when it is made, so the case does not come up yet. Clothing is not
  unbound again when a broach is replaced.
- The compose answer is read by the client as a serial; it is the serial of the new item, not its item number.
- The rank of a broach for the compose is its `tb_Item` rank (2 to 6); the theme its effect value. All broaches of this
  client are of the first theme, so a compose always gives a broach of the first theme.

### Not done

- ~~The stats of a broach and the set effects of `Tb_Broach_Set` are not applied to the person wearing the clothing.~~ Done:
  `docs/notes-stats.md`, "Clothing".
- A server option that raises the ranks of a compose during an event is not read.
- Removing broaches (`28 BroachRemove`, `39 BroachRemoveEx`): this client never sends them.

### What to check with the real client

- That setting a broach shows it in the broach window and the message of success, and that the inventory shows the new
  state after opening a place.
- That the compose window shows the result window for the serial it gets, and takes a new compose after a refusal (serial 0).
- That an error answer of 20 and 21 closes the wait of the broach window.

### Tests

`ItemSetupTest`: opening a closed place takes the item of the row and opens it, and a broach set into it is used up, is in
the place and shows in the entry load and the saved row; a compose with fixed rolls gives the broach the server table names
and uses up the five broaches and one composing item; a broach of the wrong kind and a closed place without its item change
nothing. `ItemWireFormatTest`: the answer of 20 is one broach list entry without a count. `ServerTablesTest` reads the four
server tables to their end.

Checked on stand 2 with a scripted client as well: new clothing carries its places, opening answers the new state, a broach
of the wrong kind is refused with 52012, setting answers the 68 bytes of the broaches, a compose of four answers 0, a compose
of five (with the server tables mounted) answers the serial the bags were just told about, and the saved row has the open
place and the broach.
