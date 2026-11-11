# What a player does at the NPCs of a city

Shops, the NPC behind every service, the credit with groups of NPCs, the bank. Checked against the client 1.4.2.3 (its
senders and receivers of the groups 3, 5, 8, 9 and 0x18, and the tables `tb_NPC`, `tb_Shop`, `tb_Item`,
`tb_Customer_Grade`, `tb_Customer_Benefit`, `tb_Credit_Raise`, `tb_Bank_Slot_Extend`, `tb_Inven_Slot_Extend`,
`tb_District_Transport`, `tb_Transport_Info`) and against the rules of the reference server. The code: `ServiceNpcs`,
`NpcCatalog`, `NpcCreditManager`, `NpcCreditTables`, `NpcCreditBook` (`OpenWorker.Gameplay/Modules/Npcs`), `NpcShopService`
(`OpenWorker.Gameplay/Modules/Shop`), the handlers `ShopService`, `ItemSetupService`, `ItemCraftService` and the places
of the NPCs `DistrictNpcPlaces` (`OpenWorker.DistrictServer`).

## The audit

What the client sends and reads, per message, before and after this branch.

| Message | Direction | Before | Now |
|---|---|---|---|
| `09/01` Buy: `int npc, int line, short count` | request | real but loose: any line at any distance, the price of the item only, the currency written as an int | real: the NPC, its shop group, the credit grade of the line, the currency of the line, the count, the room |
| `09/01` answer: `int n, n × (byte storage, short slot, item), long price, byte currency` | answer | sent with a 4-byte currency | fixed: one byte, as the client reads it |
| `09/02` Sell: `int npc, byte n, n × (byte storage, short slot)` | request | no handler | real |
| `09/02` answer: `int n, n × (storage, slot, item)` | answer | declared empty | real |
| `09/03` RepurchaserList: `int npc` / answer `byte n, n × item` | both | no handler, declared empty | real |
| `09/04` Repurchaser: `int npc, item` / answer `int n, n × (storage, slot, item)` | both | no handler | real |
| `09/05` RepurchaserAdd: `byte n, n × item` | answer | declared empty, never sent | sent after a sale |
| `09/06` RepurchaserDelete: `long serial` | answer | declared empty, never sent | sent after buying back |
| `09/10`, `09/11`, `09/12` shop limits (load, update, reset) | answers | declared, never sent | not sent: no line of the client's `tb_Shop` has a limit (see Shops) |
| `09/20` .. `09/28` cash shop, gacha, banners | both | invented answer to `09/20` (one made-up line), the rest empty | unchanged (see "The cash shop") |
| `03/65` NpcCreditLoad: `byte n, n × (int group, byte grade, int point, short day point, long time)` | answer | declared (19 bytes a group, checked), never sent | sent at the entry into a city |
| `03/66` NpcCreditUpdate: one group | answer | declared, never sent | sent when credit rises |
| `03/67` NpcCreditInit | answer | declared, never sent | not sent (the reference sends it from a game-master command) |
| `18/09`, `18/10`, `18/11` repairs at an NPC, `18/02` upgrade, `18/01` make, `18/03` exchange, `18/13` evolution, `18/17` akashic compose, `18/14`, `18/25`, `18/15` akashic make and take out, `18/18` disassembly at an NPC | requests | the NPC of the request read and not checked | checked (see "The NPC behind a service") |
| `08/10` BankInfo, `08/02` Move (bag ↔ bank), `08/0F` AddSlot | requests | real | real; a worn item and an item barred from storage stay out of the bank |
| `05/16` TransportTake: `int actor, short row, int npc` | request | handler that answers nothing | real, in the maze of District 6 (see "Transport"); `05/16` and `05/17` are sent to those around |
| `04/40` DistrictTransportReq | request | no handler | none needed: this client never sends it |

## The NPC behind a service

`ServiceNpcs.Find` is the one check. It takes the actor the request names, the function the service needs and, for some
services, the id of that function:

- the actor is an NPC the map of the city places (`DistrictNpcPlaces` over `NpcManager`; the NPCs stand in every channel of
  the city alike). The client writes the actor as the city gave it, kind bits and all; an actor without kind bits is taken as
  well;
- the player stands within 1000 units of it on each of the three axes (`ServiceNpcs.Reach`), the box the reference measures
  for its shops and its transport;
- its row of `tb_NPC` offers the function: one of its three function slots (`FunctionType1..3`) is of that kind, and when
  the service names an id (`Function1..3`) it is that one.

`TryServe` answers the request with the error of its service when the NPC does not serve, so the dialog stops waiting and
nothing changes. The services and what they ask:

| Request | Function | Error |
|---|---|---|
| Buy, buy back, the list of what a shop sells back | shop (1) | 52305 buy, 52308 buy back and the list |
| Sell | any NPC | 52312 |
| Repair at an NPC, of the equipment, of everything | shop (1) | 52454 |
| Upgrade, evolution | upgrade (3) | 52217 |
| Make | production (6), the group of the recipe (`tb_Make.Group`) | 52308 |
| Exchange | exchange (14) and the kind of NPC the row names (`tb_Reinforce_Exchange.NpcFunction`) | 52260 |
| Akashic compose | akashic mix (11) | 52296 |
| Akashic make, take a card out | any NPC | 52002 |
| Disassembly of a list at an NPC | any NPC | 52270 |

Functions of `tb_NPC` (the enum `NpcFunction`): 1 shop, 2 bank, 3 upgrade, 4 post, 5 auction, 6 production, 7 league,
8 transport, 9 sockets, 10 warlord, 11 akashic mix, 12 repair, 13 helpers, 14 exchange, 15 gacha, 16 recycle, 17 my room,
18 renovate, 19 dye, 20 cash mileage. The client marks the merchants that repair with 12 next to their shop; every one of
them keeps a shop.

The quests keep their own distance (`DistrictQuestNpcs`, 1500 units on the ground): a talk is not a service.

## Shops

The columns of `tb_Shop` named: `Group` (the shop group, which the shop function of an NPC names), `CreditGrade` (the
grade of credit with the group of the NPC the line asks for), `PriceType` (1 gold, 2 battle points, 4 tokens), `Price` (the
price of one; 0 takes the price of the item). Of `tb_Item`: `SellPrice` and `SellLimit` (the kinds of items a shop does not
take: 1, 3, 6, 7; a bank does not keep 4 to 7). The client's table has 485 lines in ten groups: 336 for gold, 128 for
tokens, 21 for battle points; 16 lines ask for a grade (1 to 3); no line has a limit of days or a time of sale.

- **Buying** (`NpcShopService.Buy`): the line belongs to the shop group of the NPC (52309), the grade of credit with its group
  reaches the line's (52309), the item exists (52305), the count is 1 to a stack (52306); the price is the count times the
  line's price or the item's; gold may not be below what the item sells for (52302); the currency must be there (52301 gold,
  battle points, ether; 52316 tokens, the item 800000999 of the bags; any other currency 52302); the bags must take it all
  (52311). Then the price is taken, the items go into the bags (`InventoryService`, told with `08/15` and `08/06`), `09/01`
  answers, the purchase counts for the quests that ask to buy the item and raises the credit with the group of the NPC.
- **Selling** (`Sell`): 1 to 10 slots of the common, costume or cash bag, each holding something a shop takes, none twice
  (52312 otherwise, nothing sold). A whole stack is sold, for its count times the sell price; a broken item for one gold.
  A sale that would take the purse past 99 999 999 999 999 is refused whole with 52056. The items leave the bags, the gold comes (`08/20`), `09/02` answers with the slots, `09/05` adds them to what the shop
  would sell back, and each counts for the quests that ask to sell it. Any NPC buys.
- **Buying back** (`RepurchaseList`, `Repurchase`): a shop sells back the last 12 items sold in this city, oldest first, for
  what they sold for (one gold at the least). The item comes back as it was (bound, upgraded, its gems and broaches) under a
  new serial into a free slot of its bag (52311 without one; 52307 for an item not on the list; 52301 for the gold); `09/04`
  answers and `09/06` drops the old serial from the window.
- **The kinds of shop**: the client sends no kind of shop with a purchase, so every shop is the shop function of its NPC.
  The groups of the client: 1001..1004 (weapons and gear of the four cities), 2001..2004 (helper items and potions, at the
  NPC of the helpers), 7001 (battle points), 9001 (one NPC with production as well). The recycle shop, the cash mileage shop
  and the gacha are functions no NPC of this client offers.

### The cash shop

The client asks for the cash shop (`09/20`) and can buy, set, gift and draw. The server answers the load with one made-up
line and nothing else; buying, gifting and drawing answer nothing. This branch leaves it as it was: the client's
`tb_CashShop` and billing tables would be the source, and the reference runs the cash shop through a billing service this
server does not have.

## Credit with groups of NPCs

Credit is kept per **group** of NPCs (`tb_NPC.Group`: 1001..1005 in the client; 0 keeps none), not per NPC. Each group has
five grades in `tb_Customer_Grade`, found by ten times the group plus the grade: `NeedCredit` (what the grade takes from
the one before), `DayLimit` (what a day may give towards it), `RewardItem` and `RewardCount` (given on reaching it) and six
benefits. `tb_Customer_Benefit`: `Type`, `Rate` (out of ten thousand), `Item1..3`.

- **What raises it** (`tb_Credit_Raise`, ten times the group plus the kind): 1 buying, 2 repairing, 3 making, 4 upgrading,
  5 exchanging, 6 evolving, 9 a transport; each gives a roll between `Minimum` and `Maximum`. Of the client's rows: group
  1001 buying 1 and repairing 1..2, 1002 buying 1, 1003 making 1..2, 1004 upgrading 1..2, exchanging 1, evolving 1..3, 1005
  transport 2..3. Quests give no credit.
- **How** (`NpcCreditManager.Raise`): what a day gives is capped by the `DayLimit` of the next grade; reaching
  `NeedCredit` passes to the grade and keeps the rest, and may pass several; the fifth grade is the last. Each grade
  reached gives its item: into the bags, or kept in the book until there is room and given at the next raise or entry.
  The day starts at 09:00, as the other daily things of the game. The client is told with `03/66` and writes the gain, a
  new grade and the day's limit in the chat.
- **What the grades give**: 1 a part off the price of a repair, 2 off the gold of production, 3 off the materials of
  production past the first (one left at the least), 4 off the gold of an upgrade, 5 added to the chance of an upgrade, 6
  off a transport, 7 the lines of a shop that ask for that grade, 8 more of those lines a day. The client's grades: group
  1001 repairs 25 % (grades 1 to 4) and 35 % (5) and opens item 800004001 from grade 2; 1002 opens the helper items; 1003
  makes for 10 % and 20 % less gold and materials; 1004 upgrades for 10 % to 20 % less gold and 2 % to 3 % more chance;
  1005 transports for 5 % to 30 % less. The boosters that lower prices (`ReduceDec`) add their part to a discount, the
  boosters of upgrades (`IncUpgradeItem`) to the chance (`NpcCreditManager.Discount`, `UpgradeBonus`).
- **Where it is kept**: a document of the person in `PersonStates` (kind `NpcCredit` = 11, jsonb:
  `{"Groups":[{"Group","Grade","Point","DayPoint","Day","UpdatedAt"}],"Rewards":[{"Item","Count"}]}`), marked with
  `PersonSections.MiscState` and written by the flush. A document because the credit is a handful of groups read and written
  as a whole with the person, like the other books of the person; a column would need a migration and a table would need
  rows the flush does not write. **No change of the EF model.** It is loaded and told at the entry into a city.
- **The services that read it**: repairs at an NPC (`ItemRepairService`), upgrades (`ItemUpgradeService`), production
  (`ItemMakeService`); exchanges and evolutions only raise it. Each takes the NPC of its request as one optional parameter.

## Storage

The bank of the person is the storages 5 (common) and 6 (costume). The client asks for them with `08/10`, moves items in
and out with `08/02` and extends them with `08/0F`; all three were handled before. The reference adds two rules to a move,
now kept: an item goes from the equipment to the bank only through the bags, and items whose row bars them from storage
(`SellLimit` 4 to 7) stay out of it (`52044`). There is no fee and no NPC: the reference asks for none. Every row of the
client's extension tables pays with an item (`837000001` for the bags, `837000002` for the bank), which `GradeUp` takes. The
account storages (16..18) and money in the bank are not reached by this client.

## Transport

Built on `feature/district-gaps` (round 7): `docs/notes-district-gaps.md`. The client asks for a ride at the NPC of transports
(`05/16`: the actor, the row of `tb_Transport_Info`, the NPC) and waits for `05/17` to give the player back; it has no request to end
a ride, so the server ends it. The NPCs with the transport function stand only in District 6, the map `D01_DISTRICT6`, which is a maze
(30021), so the ride is the maze server's (`TransportService`, the townsfolk of a maze by `MazeNpcPlaces`): the NPC near the player
(58000), the row among the five of `tb_District_Transport` its function names (58002), the gold of the row less the credit and the
boosters (58001), `05/16` to those around, the end after one second and the seconds of the row on the simulation clock, the player put
in the middle of the common position box of the row, `05/17` to those around; credit 9.

## Compared with the reference

| Part | Reference | Ours | Verdict |
|---|---|---|---|
| NPC of an item service | in the area of the player, of the function; no distance | in the city, of the function, within 1000 units on each axis | kept different: the distance the reference keeps for its shops, so a request from across the city is refused |
| NPC not in the area | logged, no answer | answered with the error of the service | kept different: every request is answered |
| Repair function | the shop function | the shop function | same |
| Make, exchange, upgrade, evolution, akashic compose | 6 + recipe group, 14 + row kind, 3, 3, 11 | the same | same |
| Disassembly at an NPC, akashic make and take out | no check | the NPC near the player, any function | kept different: the request names an NPC, so it is held to one |
| Evolution refused for its NPC | 52217 with the sub of an upgrade | 52217 with the sub of an evolution | fixed: the client waits on the evolution |
| Buy: NPC, distance, row of its group | 52305, 52305, 52309 | the same | same |
| Buy: a grade too low | no answer | 52309 | kept different: answered |
| Buy: count, price of the line or of the item, gold not below the sell price | 52306, 52302 | the same | same |
| Buy: currencies | gold, battle points, ether, recycle, two tokens, cash mileage | gold, battle points, ether, the token of the bags | kept different: the client's lines use gold, tokens and battle points only |
| Buy: limits by day, week, month | per character and account | none | kept different: no line of the client has a limit |
| Buy: a sale period | dates of the line | none | kept different: the client's table has no dates |
| Buy: costume sets | one line gives the set | the item of the line | kept different: no line of the client sells a set |
| Buy: the answer | update and create of the items, then `09/01` with the price and the type | the same | fixed (the type was four bytes) |
| Buy: quests, credit | counted, raised | the same | same |
| Sell: NPC, distance, count | logged only | answered with 52312 | kept different: answered |
| Sell: what may be sold, the price, broken items | sell types 1, 3, 6, 7 refused; count × sell price; 1 for broken | the same | same |
| Sell: recycle shop | recycle prices | none | kept different: no NPC of the client recycles |
| Sell: quests | counted | the same | fixed (no handler before) |
| Buy back: list of 12, price, missing item | kept in the database | kept while the person stays in the city | kept different: the list is lost on leaving the city |
| Buy back: the item | the same serial | a new serial, `09/06` drops the old one | kept different: a serial removed and written again in one flush could be lost |
| Buy back: refusal of the NPC | 52308 with the sub of the list | 52308 with the sub of buying back | fixed: the client waits on buying back |
| Credit: per group, grades, day limit, rewards | as described | the same | same |
| Credit: grade rewards | by system post | into the bags, kept until there is room | kept different: the post of this server has no letters of the game to a person |
| Credit: league skill 5 (day limit) and 6 (repair) | read | not read | kept different: leagues have no skills here |
| Credit: benefits read | repair, make gold, make materials, upgrade gold, upgrade chance, transport | all | same (transport since round 7) |
| Credit: raises | buy, repair, make, upgrade, exchange, evolution, transport, renovate, refine | all but renovate, refine | kept different: renovate and refine are not sent by this client |
| Bank: worn items, items barred from storage | 52044 | the same | fixed |
| Bank: fee, NPC | none | none | same |
| Transport | charged, ridden, ended by the server | the same, in the maze of District 6 | same (the differences are in `notes-district-gaps.md`) |

## Assumptions

- The rate of a booster (`tb_Booster` values, 2000 for a fifth more of experience) is read out of ten thousand when it lowers
  a price, as the benefits of credit are, and added as it is to the chance of an upgrade.
- The repurchase list holds the item as it was sold; an item bought back keeps its gems and broaches but the client is not
  told them again (`08/61`, `08/62`), as the reference does after buying back.
- The time a credit entry carries is Unix seconds of its last change; the client does not read it.

## To check with the real client

- That the shop window shows the purchase from `09/01` with the one-byte currency, and the gold from `08/20`.
- That the sale clears the slots and writes its lines in the chat, and the window of what the shop sells back fills from
  `09/05` and `09/03` and empties from `09/04` and `09/06`.
- That the chat shows the credit gained, a new grade and the day's limit from `03/66`, and the NPC dialog shows the grade.
- That a merchant two or three steps away still serves (1000 units on each axis).

## Tests

`NpcServicesTest` (with `OPENWORKER_TEST_RES`): a purchase takes the price of the line in gold and gives the item, and is
refused without the gold and without room; a sale gives the sell price of a stack, refuses a list with an item a shop does not
take, and the stack is bought back for what it sold for; a repair far from its NPC and at an NPC without a shop is refused and
changes nothing; a purchase raises the credit with the group of the NPC and its first grade takes a quarter off a repair.
`ShopWireFormatTest`: the layouts of `09/01` to `09/05`. `PersistenceTest.AnItemMovedToTheBankAndBackAndTheCreditWithNpcsSurviveAFlush`:
an item moved into the bank and out again, a worn item kept out of it, and the credit with a group, through the flush and new
entries. The item tests name the NPC of each request by the function it needs (`ItemSetupFixture.NpcOffering`).

On the stand (`docs/tools/e2e_shop.py`, a local tool): a new character is refused from afar, walks to the merchant, buys,
sells, buys back, repairs, is refused at the bank NPC, sees its credit rise, and finds it again after signing in.

Stand 5, 2026-11-09, the same scripts before (on `dev`) and after this branch:

| Script | Before | After |
|---|---|---|
| `e2e_shop.py` | 4 pass, 17 fail (nothing of the above existed; the purchase answered a four-byte currency) | 21 pass |
| `e2e_helper.py` | 88 pass | 88 pass |
| `e2e_quests.py` | 28 pass, 7 fail | 28 pass, the same 7 fail (the boss of the last sector of the maze: not this branch) |
