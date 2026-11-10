# Trade and private shops (group 0x0A)

The eleven requests of the trade group have handlers in `OpenWorker.DistrictServer/Services/TradeService.cs`; the logic is in
`OpenWorker.DistrictServer/Trade/`. Formats of the messages: `docs/TRADE_POST_PROTOCOL.md`.

## Where it runs

The district only. A trade needs two players in one place, and in a maze every player is alone for now (`MazeAudience`),
so the maze has no handler and the requests fall into the empty handler there. When mazes are shared, the logic can move to
`OpenWorker.Server.Common` with a channel of the maze.

**Who can trade with whom:** two players of the same channel of the same district. Not "in sight": the reference behaviour
checks the area of the requester (the whole map instance, which is our channel), not the distance. Every step of a trade
checks it again, and a partner who changed channel or left cancels the trade with 52402. A private shop is opened by a
player of the same channel too; the client itself only lets a player click a shop within 300 units.

## Design

- `Trades` keeps one `TradeSession` per pair in a dictionary on the world loop, found from either player. No locks, no
  concurrent collections; everything is called from handlers, the close handler or the expiry service, all on the loop.
- `TradeSide` holds what one side offers (`TradeOffer`: storage, slot, serial, item, count, as they were when offered), its
  gold, and two flags: `Locked`, `Confirmed`.
- The items stay in the bags until the exchange. Nothing locks them in the inventory; the exchange checks that each offered
  item is still in its slot with the same serial, item and count (`TradeGoods.StillHolds`).
- `TradeGoods` is the movement shared by trades and shops: what may be offered (only the bags `Common`, `Costume`, `Cash`;
  not `OnPickUp`-bound items; not items with flag 2), whether the items fit (one free slot per item in the home bag of the
  item, counting the slots the receiver frees in the same exchange), whether the gold fits (no overflow of `long`), the take
  and the give.
- **The exchange** (`Trades.Exchange`) is one synchronous block after the second confirmation: check both sides again (items,
  gold, room), then take the items of both sides, give them, move the net gold, send the result. No `await` inside.
- **Items change serials.** A traded item is destroyed in the bags of the giver (its row is deleted by the next flush of the
  giver) and placed as a copy with a new serial in the bags of the receiver. The upsert of an item (`ItemStore`) does not
  change `PersonId`, so moving the row would leave it with the old owner.
- **Persistence:** the dirty masks of both persons (the inventories and `TryAddGold` mark them). After a trade or a sale both
  players are also flushed (`IPersonFlush`) in one loop callback, so the two halves are queued together and usually land in
  one batch of the writer, instead of up to one flush interval apart. The handler awaits that flush after everything moved.
- **Disconnect:** `TradeCloseHandler` is inserted as the first `ISessionCloseHandler`. It cancels the trade (cause 52410 to
  the other side) and closes the shop before `PersonFlush` takes the last snapshot of the leaving player; otherwise the
  partner could confirm during that flush and the leaving player's half would never be written (a duplicated item).
- **Timeout:** an invitation not answered in 99 seconds is cancelled with 52412 to both (`TradeExpiryService`, once a
  second on the loop). An open trade has no timeout, as in the reference; either side can cancel.

## Session states

| State | Meaning | Leaves by |
|---|---|---|
| invited (`IsOpen` false) | the invitee got `TradeResponse` | accept (open), decline (end), expiry 52412, disconnect 52410 |
| open | both change their offers | lock, cancel, disconnect, partner away 52402 |
| one or both locked | a locked side cannot change its offer; a change by the other side unlocks the locked side | unlock (both unlock), confirm when both are locked |
| one confirmed | `TradeConfirmResponse` sent to both | the second confirmation: the exchange |
| ended | result 0 / 1 / 2, or a cancel | |

## The requests

| Request | Does |
|---|---|
| 01 `TradeRequest` (name) | finds the player by name in the district (52409 if none; 52402 if in another channel); checks the block lists both ways (`IPersonRelations`, two awaits, then everything is checked again); 52416 if the requester blocked the target, a silent cancel (cause 0) if the target blocked the requester; 52408 if the requester trades or keeps a shop, 52401 if the target does. Sends `TradeResponse` (requester actor, name) to the target only. Errors are error answers of 01. |
| 02 `TradeAcceptRequest` (inviter, yes/no) | only the invitee, naming the inviter (else error 52403). Yes: opens the trade; no: ends it. `TradeAcceptResponse` (answer, invitee actor) to both. |
| 03 `TradeUpdateItemRequest` (1 add / 2 remove, slot) | add: 52407 for a slot of another storage or an empty one, 55602 for a bound item, 52405 if offered already, 52406 past five items. Remove: the offer of that slot, even if the item has gone since. `TradeUpdateItemResponse` (actor, type, item, no sockets, an empty brooch) to both. A change unlocks the other side if it was locked. A request while the requester is locked or a side has confirmed cancels the trade with 52403 (the client never sends one then). Another type is charged against the error budget. |
| 04 `TradeUpdateMoneyRequest` (gold) | 0 to the requester's gold (52404), not overflowing the other purse (52417). `TradeUpdateMoneyResponse` to both. Negative is charged. |
| 05 `TradeCheckRequest` (1 lock / 0 unlock) | lock the own offer, or unlock both sides; refused (52403) once someone confirmed. `TradeCheckResponse` (actor, flag) to both. |
| 06 `TradeConfirmRequest` | only when both are locked (52403). First: `TradeConfirmResponse` (empty, the client reads nothing) to both. Second: the exchange. |
| 08 `TradeCancelRequest` (cause) | ends the trade; `TradeCancelResponse` (requester actor, the cause the client gave) to both. |
| 09 `TradePrivateShopStartRequest` (slot, title, lines) | 52430 if the player keeps a shop, 52408 if it trades or is in no channel, 52431 for an empty or too long title (more than 20), no lines or more than five, a price of 0 or less, an item not in the bags (by serial), not tradable, or twice. Opens the shop selling, sets `ShopPrivateComponent` (type 2, title), and sends `TradePrivateShopStartResponse` to those who see the owner (the owner too). |
| 0A `TradePrivateShopItemRequest` (slot, gold) | from an owner: changes a line of a stopped shop (52430 while it sells); gold 0 removes the line of the slot (52432 if none), more sets or adds it (52431 if the item cannot be sold, 52432 past five lines). `TradePrivateShopItemResponse` (slot, gold) to the owner. From anybody else: **a purchase** (see below). |
| 0B `TradePrivateShopStateRequest` (0 close, 1 stop, 2 sell) | 52434 without a shop; stop needs selling (52435), sell needs stopped (52430) and a line (52434). Closing clears the component. `TradePrivateShopStateResponse` (owner actor, state) to those around. A state above 2 is charged. |
| 0E `TradePrivateShopSelectRequest` (owner) | 52436 if the owner keeps no shop, is the player, or is in another channel; 52435 if stopped; 52434 if no line still has its item. `TradePrivateShopSelectResponse` (owner, lines) to the player; the server remembers the serials of the lines it showed, in order. |

A purchase: the player names the line by its place in the list it was shown (`Slot.Slot`) and the price it was shown
(`Money`). Checked: the shop still sells (52435), the owner is in the channel (52436), the line is still there with that
price and its item (52431), the buyer's gold (52404), the owner's purse (52417), the buyer's room (52406). Then the item and
the gold move in one block; `TradePrivateShopSellResponse` (owner, serial) goes to those around the owner, the buyer gets
`TradePrivateShopBuyResponse` (serial, owner name, item), and a shop with no line left closes (state 0 to those around).

## Decisions on unverified points

- **The purchase request is a guess.** The client 1.4.2.3 has no request for buying: it sends only 09, 0A, 0B and 0E of the
  shop requests, 0A only from the owner's price dialog. The reference server reads 0C but does nothing with it. The shop UI of
  the buyer exists (it shows the list of 0E and handles 0C and 0D), so a buy must have had a path; 0A from a non-owner is
  the only request whose fields (a slot and a 64-bit number) can carry "which line, at what price", so it is used that way.
  If a real client never sends it, buying simply does not happen; nothing else breaks.
- **The slot of 09 (start) is ignored.** The reference uses up one of the item in that slot when the shop opens (a shop
  permit) and also checks that the player stands in a shop zone of the map. The permit item and the zones are not known
  here, so the server consumes nothing and checks no zone.
- **Bound items:** refused when `BindType` is `OnPickUp` and when the item flag is 2, as the reference refuses "bound" and
  flag 2. Whether an `OnEquip` item that was worn counts as bound is not tracked.
- **The brooch of an offered item** is sent empty (serial 0, fifteen zeros) and no sockets: the server keeps neither.
- **Result codes:** 0 done, 1 no room, 2 not enough gold (the client shows a message for 0 and 1 and closes the window for
  any). An item gone at the exchange cancels with 52406, as the reference cancels when it cannot gather the items.
- **Gold limit:** only an overflow of the 64-bit purse is refused; the game's real limit is not known.
- **Message codes** (524xx, 55602) are the codes the reference server answers with; their texts in the client were not
  checked one by one.
- **Unlock on change:** the reference lets a side change its offer while the other is locked without unlocking it; here
  the change unlocks the other side, so nobody confirms what it did not see.
- **The cancel cause of 08** is passed through from the client, as the reference does.

## What remains

- A player who is being moved to another server (portal, maze, back to the gate) can still trade while the flush before its
  reservation waits for the database; an exchange in that moment may not reach the next server. A "leaving" mark set by
  `WorldManager` and `LeaveManager` before that flush, which `Trades` would check, closes it.
- The two flushes after an exchange are queued together but are not one transaction when the writer splits a failed batch.
- The block type of an account and the matching state are not checked. (The trade password is: `notes-district-gaps.md`.)
- Shops do not show their sign to players who arrive after the start beyond `ShopPrivateComponent` in `WorldInInfoPc`; the
  title change request (0F) does not exist in this client.
- No e2e run on the stand: the behaviour was checked by `TradeTest` (handlers, fake district) only.
