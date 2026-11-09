# The work on items (group 0x18)

Repairs, upgrades, sockets and the tools that restore, unseal or lower a count of an item, at the NPCs of a city. The
handlers are `ItemSetupService` in `OpenWorker.DistrictServer`; the rules are in `OpenWorker.Gameplay.Modules.ItemSetup`:
`ItemRepairService`, `ItemRestoreService`, `ItemSocketService`, `ItemUpgradeService`, and `ItemSetupAnswers`, which sends the
answer or the error of a request. Every request a handler takes is answered, refused or not: the client keeps its dialog
waiting until it hears back. A handler changes items through `InventoryService` and `Inventory.Touch`, so the dirty mask of
the person carries the change to the flush; nothing in them waits or throws on the loop.

## Where it runs

District only. The requests carry the NPC the player talks to where the client sends one; the NPC is not checked (see
"Not done").

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
| 01 | Make | nothing yet (empty handler, as before) | none |
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

Of the 38 requests of the group, 11 have a handler now (Make's is empty); 27 have none. 16 of those the client of this
version never sends. The 11 the client sends and the server does not handle, and why:

| Sub | Request | Missing |
|---|---|---|
| 03 | Exchange | the rules of `tb_Reinforce_Exchange` and the meaning of its fields |
| 04, 18 | Disassemble, DisassembleEx | `tb_Disassemble` (the client's has only an id column) and the fields of the answer |
| 13 | Evolution | the request carries an item that has no span reader yet, and the answer is not read |
| 14, 15, 17, 25 | AkashicMake, AkashicDisassemble, AkashicCompose, AkashicMakeEx | the akashic tables and cards; the answers of 15 and 17 are only partly read |
| 20, 21, 23 | BroachEquip, BroachActive, BroachCompose | the broach rules and `TB_BroachData`; the answer of 21 is only partly read. The schema already keeps the broaches. |

## Assumptions

- The columns of `tb_Item` read as the restore limit (the last byte of a row), the seal limit (the byte before it) and the
  value of a tool (the byte after the endurance) were named from their values over the table, not from the client's use of
  them.
- The socket id of a gem is the item of the gem, and its options are the options of the gem item as it lies in the bag. The
  server gives a new item no rolled options, so a gem of this server adds nothing to an item yet.
- Which sockets an item may have is only its row of `tb_Socket`: few items point at a row the client has, so most items have
  no sockets to open. Gems are not limited by their category or by the kind of item.
- The tool that lowers the count of unsealing may be used only once per item, which the item does not keep: it is refused.

## Not done

- The NPC of a request is not checked (that it stands near the player and offers the service), here as for the repairs.
- Nothing wears endurance down on attack, defence or death yet: the rules use a table of endurance the client does not have
  in a form that is known.
- The credit of an NPC and the boosters that lower prices or raise chances.
- The stats of an item do not change with its gems or its upgrade level on the server.

## Tests

`ItemSetupTest` (with `OPENWORKER_TEST_RES`, and `OPENWORKER_TEST_SERVER_RES` for the upgrade): an upgrade with a fixed roll
goes up at the highest roll that still succeeds and breaks at the next, charging the gold of the client's table; opening a
socket and setting a gem puts the gem in, uses it up and shows it in the entry load and in the saved row; a restoring item
mends a broken item and an unsealing item frees a bound one. `ItemRepairTest` shares the fixture. `ServerTablesTest` reads
the server `tb_Reinforce` to its end.
