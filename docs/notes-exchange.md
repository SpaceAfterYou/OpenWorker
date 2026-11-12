# The exchange (group 0x2B)

Players give items to the exchange, put them on sale at a price, search the listings of everybody and buy. The handlers
are `ExchangeService` in `OpenWorker.Server.Common` (a district and a maze both answer: the client asks for the lists at
every entry); the listings are kept by `ExchangeStore` (`OpenWorker.Persistence`) behind `IExchangeStore`
(`OpenWorker.Domain.Stores`); the rules are `ExchangeRules` and the mapping to the messages and to letters is
`ExchangeListings` (`OpenWorker.Gameplay.Modules.Exchange`).

## The requests of client 1.4.2.3

| Sub | Request | What the server does | Answer |
|---|---|---|---|
| 01 | `Search` (page, item, kind, sub kind, character, level, grade, price, upgrade) | the listings on sale that match, ten a page, the cheapest first | `Search`: page, pages, listings |
| 02 | `PriceHistory` (person, item) | the last ten sales of the item | `PriceHistory`: person, item, sales |
| 03 | `InterestList` (person) | the items the player keeps an eye on (asked at every entry) | `InterestList`: person, items |
| 04 | `InterestItem` (person, item, add) | puts the item on the list (at most ten) or takes it off | `InterestItem`: person, item, result, add |
| 05 | `WaitRegister` (item, count, storage, slot) | some of the item leaves the bags and waits at the exchange | `WaitRegister`: result; then `MyList` with the listing |
| 06 | `SellRegister` (serial, price of one, three tickets) | a waiting listing goes on sale; the fee of its kind is paid | `SellRegister`: result; then `MyList` with the listing |
| 07 | `ItemBuy` (listing, item, count, commission mark) | the purchase (see below) | `ItemBuy`: result, listing, count |
| 08 | `ItemRecall` (serial) | the listing is taken back, whatever its state; what is left comes back by letter | `ItemRecall`: result, serial; `Post` 09 for the letter |
| 09 | `MyList` (person) | the listings of the player (asked at every entry) | `MyList`: listings |

The client keeps the list of the player by the serial of the item: an entry with a count of 0 takes the listing off, any
other replaces or adds it. The states are 1 waiting and 2 on sale; a listing on sale whose end is past is shown as ended.
Times are Unix seconds.

A result other than 0 is the number of a UI string the client shows; the search answers an unknown item with the error
answer (`sub | 0x80`, 52004).

## The rules

- **Giving an item** (`WaitRegister`): the trade password must be given on this login (52002), the item must exist (52004),
  the count fit its stack (52013), the storage be the common or costume bag (52001), the slot hold that item (52011), the
  item be free and not bound from the start nor broken (52405), gear of slot types 1 and 2 be at full endurance (52011).
  At most ten waiting listings (58303). The item is taken from the bag and written before the listing is (see "What a
  crash leaves"), and given back if that write or the store refuses or fails.
- **Putting on sale** (`SellRegister`): a price (58301), a total of at most 999,999,999 (16857), the fee of the kind of the
  item in gold (52288); at most ten listings on sale, more with the booster, a hundred with the ticket (58302). The sale
  runs a day, a day more for each step of the booster and two with the ticket; the exchange keeps 9 % of a sale, nothing
  with the ticket or the booster that takes it away, less with the booster that lowers it. A ticket is spent only when it
  changes something and only once the sale is written; one asked for and not held refuses the sale (52011).
- **Buying** (`ItemBuy`): the trade password (52002); a count between one and the stack (52014); the listing must exist
  (58305), not be the buyer's (16860), be on sale with that item (52002), not have ended (58304) and hold the count
  (52014); the gold (52288). The gold is taken and written first; one statement then takes the count off the listing only
  while all of that still holds, so two buyers on two servers never both get the last item; the sale, the letter with the
  item to the buyer, the letter with the gold less the commission to the seller and the end of a listing sold out go in the
  same transaction. A refusal gives the gold back. The buyer is told of its letter; the seller is told wherever it plays of the
  listing as it is now and of its letter. Achievement 55 counts the purchase.
- **Taking back** (`ItemRecall`): the listing is held for its transaction, so a purchase that comes meanwhile waits and then
  finds it gone; the letter says "taken back", or "ended" for a sale whose end is past.
- **The end of a sale**: an ended listing is not found by a search and not bought; it stays in the list of the seller as
  ended until the seller takes it back (the client has a message for taking back an ended listing).
- The letters of the exchange are letters of the game to the person (sender "Exchange", main kind 2, sub kinds 1 bought,
  2 sold, 3 taken back, 4 ended, 5 sold without commission, as the client's table of system letters numbers them). They
  are written with the change of the listing and do not count against the box limit of 40.

## What a crash leaves

What a player pays (the item given to wait, the fee of a sale, the price of a purchase) is taken in memory and written to
the database (`IPersonCommit`, an awaited write of the player through the write-behind) before the store writes what it pays
for. A crash after that write and before the store loses what was paid; it never leaves an item both in the bags and at the
exchange, nor a purchase whose price is still in the purse. When the write is not taken (the writer gives it up after its
retries) the store is not asked, what was taken goes back, and the request is refused. When the store throws, what was taken
goes back and the request is answered with its code: 52011 for giving an item and for a sale, 52002 for a purchase (the
original answers a database result it does not name with 52011). A failure of the database is not counted against the
session.

Kept as they are:

- The tickets of a sale are spent after the sale is written: they are spent only when the sale is, and a stack given back
  would come with the attributes of the table, not its own. A crash in the few milliseconds between the sale and the next
  write leaves a ticket unspent.
- A store that commits and then fails to say so (the answer of the database lost) gets what was taken back to the player
  as well: the outcome is not known, and it would take a read of the listing or the letter to know it.

## The tables

`ExchangeListings` (one row per listing; the item as it was given in `Goods`, jsonb), `ExchangeSales` (one row per sale, for
the prices), `ExchangeInterests` (key person and item). The fee is the column of the server's own `tb_Item_Classify`
(`ItemClassifyFeeRow`, read from `res-server` only; without it there is no fee). The kinds the search filters by are those
of the client's `tb_Item_Classify` (group and sub group), the level, grade and character those of `tb_Item`.

## Compared with the original

| What | Here |
|---|---|
| Sub numbers | the client's: it has a request to give an item to wait (05) that the original lacks; the others move up by one |
| Layouts | the client's: no package list and no seller name in a search entry, no trailing byte after the list of the player, no high/low/average after the sales |
| Checks and their order | same, without the ones of states the server does not keep (matching, game master, blocked accounts, closed characters, sealed packages, net cafe, league skill) |
| Codes | the original's where the client has a text; where it has none, the client's own text: 52288 for gold (58307, 52251), 16857 for the price (58308), 52405 for a bound or broken item (52011), 58301 for no price (52011), 58305 for a purchase of a listing gone (58310) |
| Limits, rates, durations | same: 10 watched, 10 waiting, 10 (100) on sale, 9 %, 24 h, +24 h a booster step, +48 h the ticket |
| Delivery | same: letters for the item and the gold, the seller told on its server |
| The purchase | one statement in the database, as the original's one stored procedure |
| Search | ten a page; a search for nothing in particular drops every filter; the order is the cheapest first (the original's order is in its database) |
| Rate of searches | not limited here beyond the rate limit of the group (the original drops a second search within a second without an answer) |
| The waiting time of a new sale | none here: the client gives an item to wait by its own request (the original opened a sale half an hour after it was made) |
| Logs and statistics | a line in the log per sale |

## What is not verified with the real client

- That the system letters show their own texts by kind (the title and body are also written in plain words).
- The meaning of the price and upgrade filters of the search (taken as "price of one at most" and "upgrade at least").
- How the client shows codes 58306 (watched list full), for which it has no text.
- The fee the client shows before a sale, if any.
