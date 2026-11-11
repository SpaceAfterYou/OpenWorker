# Item rules: bound items, the gold limit, collecting (round 10)

Items, money and the trade password, compared with the original server. The research (addresses, names) is in the git-ignored
`docs/agents/feature-item-rules/research.md`; this file has the rules only.

## Bound items

Two things were one: the **rule of the table** (`ItemRow.BindType`: 0 none, 1 on equip, 2 on pick-up, `ItemBindType`) and the
**state of an item** (`ItemInstance.BindState`, `ItemBindState`, the byte of the item the client reads). The server used to copy the
rule onto every new item. The original keeps them apart, and so does the client: for an item it shows the rule ("Bound on Equip",
"Bound on Acquire") and, once the item is bound, "Bound Item, untradable".

| State | Meaning |
|---|---|
| 0 `Free` | may be traded, mailed and put into a shop of a player |
| 1 `Bound` | bound for good: made with the rule 2, worn with the rule 1, or a gem set in; only an unsealing item frees it |
| 2 `BoundByBroach` | free in itself, bound for as long as a bound broach is set into the clothing |
| 3 `BoundAndBroach` | bound for good and a bound broach set in |

Everything that is not state 0 counts as bound. `ItemBinding` (Gameplay) holds the rules; every place asks it.

| Event | Original | Here | |
|---|---|---|---|
| An item is made (drop, shop, reward, letter, craft) | state 0, or 1 when the rule is 2 | the same (`InventoryService.Create`) | fixed (was a copy of the rule) |
| A character is made | its items are bound (1) | the same | fixed |
| An item is worn (weapon, gear, clothing) | rule 1: 0 becomes 1; clothing 2 becomes 3 | the same, when the item goes from a bag into an equipment storage (`InventoryService.Move`, both sides of a swap) | fixed |
| A gem is set in | a free item becomes 1 | the same | same |
| Unsealing | state 1 becomes 0 | the same | same |
| A bound broach is set in | 0 becomes 2, 1 becomes 3 | the same | same |
| A broach is taken out | 2 becomes 0, 3 becomes 1 (unless a removed broach is bound from the start) | not built (the request has no handler) | open |
| New items join a stack | only stacks bound alike, unless the rule is 2 | the same (`ItemBinding.CanStack`) | fixed |
| Two stacks are combined | no comparison | refused when the states differ | kept different: a bound stack never becomes free (audit finding 6) |
| A stack is divided | both pieces "bound" or not | the piece keeps the state | same for the states stacks can have |

### Which way out a bound item is refused

| Way out | Original | Here |
|---|---|---|
| Trade, put on the table | broken item (flag 2): 52407, then bound: 55602 | the same (`ItemBinding.HeldBack`) |
| Shop of a player | not asked by the server | broken or bound refused: kept different, stricter on purpose |
| Letter | table rule 2: 55620; flag 2 and bound: 55602 | table rule 2 or bound: 55304; broken: 55303 (the client has texts for 55304, 55620 and 55303, none for 55602) |
| Sale to an NPC | allowed, a broken item sells for one gold | the same |
| Bank and account bank | refused by the sell limit of the item (4 to 7), never by the state | the same |
| League storage, auction | refused when bound | not built |

### Items stored with the old values

The database held the rule in the state (0, 1 or 2), changed by gems, unsealing and broaches. The conversion, per stored item and per
item in a letter (the item table gives the rule `t`):

| Stored | Becomes |
|---|---|
| `t` = 2, any value | 1 |
| `t` = 1, value 1, in the bags or a bank, without a gem | 0 (not worn yet) |
| `t` = 1, value 1, worn (equipment, look equipment, a helper) or with a gem | 1 |
| any other value (a free item, a gem on another rule, a broach state 2 or 3) | unchanged |

The SQL is generated from the client table (`bind_conversion.py` in the research folder of the round) and runs once in the migration
of the round. Checked on ten sample rows and on letters.

## The wallet limit

One limit in `InventoryService`: `WalletLimit` = 99 999 999 999 999 for gold, battle points, ether and recycle points, `CashLimit` =
999 999 999 for cash and the cash points (`LimitOf`, `CanReceive`). `TryAddCurrency` refuses a gift past it. The original asks before
every gift and refuses the whole action; only what is picked up from the ground fills the purse and leaves the rest
(`AddUpToLimit`).

| Where | Original | Here |
|---|---|---|
| Quest reward, reward granter | refused: 52056 gold, 52057 battle points, 52058 ether | refused whole (the granter says it does not fit) |
| Box opened | refused with the same codes | the same (`ItemUseService`) |
| Achievement | refused (silently in the original, the codes before here) | the same codes as before, one limit |
| NPC sale | refused 52056 | the same |
| Trade money | refused 52417, rechecked at the exchange | the same, rechecked with the net gold |
| Letter taken | refused 55613 | refused 55307 (the text the client has) |
| Private shop purchase | not asked by the server | refused 52417 for the owner's purse (as before, with the new limit) |
| Pick-up of money | clamped to the limit | the same |
| Maze reward | gold that does not fit is sent by a letter of the game | the same |
| Ether of a disassembly | refused 52058 | the same |

## Collecting

See `notes-quests.md` (quests) and the daily missions: items count **as they arrive** from the inventory (`ItemArrivals`) and are
never taken off when they leave. Trades, shops of players and the bank place items without an arrival. Letters give their stackable
items through the inventory, so they count, as in the original. A collecting condition is set to the items held when an episode is
taken and whenever another condition is done (`QuestManager.RecountHeldAsync`). Daily missions listen to the same arrivals (the pick-up
special case that counted a quest item twice in the original is gone); the client table has no mission that collects an item.

## The trade password

The count of wrong passwords belongs to the account, not the login: see "Trade password" in `notes-district-gaps.md`.

## To check in the play session

- What the shop window and the tooltip show for an on-equip item before and after it is worn ("Bound on Equip" then "Bound Item").
- A bound item dragged into the trade window, the shop window of a player and the letter: the messages (55602 has no text).
- The lock message at the next login and in the trade window; whether the client sends the check at all at the state 3.
- A collecting quest: items sold and bought again, items mailed between two characters of the account.
