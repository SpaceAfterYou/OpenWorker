# The post (group 0x20)

Letters between persons, by name, with text, gold and up to five items, and letters of the game for an account. The
handlers are `PostService` in `OpenWorker.DistrictServer`; the letters are kept by `PostStore` (`OpenWorker.Persistence`)
behind `IPostStore` (`OpenWorker.Domain.Stores`); the mapping to the messages and the moving of items is `PostLetters`
(`OpenWorker.Gameplay.Modules.Post`).

## Where it runs

District only. Every request but `PostInfo` carries the NPC of the post box the player stands at, so the post is a thing of
a town. A request of the post that reaches a maze falls into the empty handler, as before. Not checked against the
client: whether it sends `PostInfo` (0x10) inside a maze. If it does, the maze answers nothing, which is what it did before.

The NPC of a request is not checked: any request is served wherever the player stands in the district.

## The table

`Posts`, one row per letter (migration `Posts`):

| Column | What |
|---|---|
| `Id` | bigint identity, the serial the client sees |
| `Kind` | 0 a letter of a person, 1 a letter of an account |
| `SenderId`, `SenderName`, `SenderHero` | the sender; the id is null for a letter of the game, and set to null when the sender is deleted (the name stays) |
| `ReceiverId`, `ReceiverName`, `ReceiverHero` | the receiver of a letter of a person (cascade on delete) |
| `ReceiverAccountId` | the receiver of a letter of an account (cascade on delete) |
| `Title` (40), `Body` (400) | the text |
| `Money` | gold for the receiver |
| `Postage` | what the sender paid, for the record |
| `Items` | jsonb, the attached items with everything that was done to them (owned type `PostItemPersistent`) |
| `MainType`, `SubType` | the kind of a letter of the game, as the client table of system letters numbers them; 0 for persons |
| `State` | flags: read 1, taken 2, sent back 4, kept 8, deleted by the sender 16, deleted by the receiver 32 |
| `ExpiresAt` | until when the letter is kept (30 days from sending or from sending back) |
| `CreatedAt`, `UpdatedAt` | as on every table |

Indexes: `ReceiverId`, `SenderId`, `ReceiverAccountId`. Checks: `Money >= 0`, `Postage >= 0`, and a letter is for a person
or for an account as its `Kind` says.

## How a letter is written

A letter is written by its store when it changes, never by the flush: sending, reading, taking and deleting are rare next to
what the write-behind collects, the receiver may be on another server and must see the letter at once, and "taken once"
must hold across servers. What moves into or out of an inventory goes through `InventoryService`, which marks the inventory
and the wallet dirty; the handler then flushes the person (`IPersonFlush`) at once.

- **Every change is one statement** whose condition says what the letter must still be (in the box of the one who asks, not
  taken yet): two servers that take one letter at the same moment cannot both get it. A claim the game cannot honour (no
  room, too much gold, the player gone) is put back (`ReleaseAsync`).
- **The box limit belongs to the database**: sending and sending back hold the row of the person whose box fills
  (`SELECT ... FOR UPDATE`) while they count it and insert, so two servers cannot overfill it. The limit is 40 letters, kept
  ones included: a list of 40 letters at their longest (about 1.4 KB each) still fits one packet. The sent list shows the
  newest 40.
- **Sending** takes the gold (money plus postage) and the items from the sender in memory, writes the sender
  (`IPersonCommit`) and only then the letter; when that write is not taken or the store fails, they are given back and
  the sender is answered 55306; an inventory that filled meanwhile gets the items as a letter to the account instead. A
  crash between the write and the letter loses them, it never doubles them. A store that commits and then fails to say
  so still doubles them (the gold and items are given back while the letter exists).
- **Taking** claims first and gives after: a crash in between loses what the letter carried, it never doubles it.
- Items go to the receiver under new serials, so the flush of the sender (which deletes the old row) and the flush of the
  receiver never touch the same row. An item that stacks is given as the inventory gives stacks (its attributes are those
  of the table); an item that does not stack comes back with every attribute it was sent with.
- A blocked sender is answered as if the receiver did not exist.

## The requests

| Sub | Request | What the server does | Answer |
|---|---|---|---|
| 01 | `SendList` | the letters the person sent and still lists, newest 40 | `SendList`: letters, then 1 |
| 02 | `RecvList` | the received letters that are not kept | `RecvList`: letters, then 1 |
| 03 | `Send` | checks, takes gold and items, writes the letter, tells the receiver (`Recv` 09) | `Send`: 0 and the letter as sent, or a code and an empty letter |
| 04 | `Read` | marks it read | `Read`: serial, flag, remaining seconds, unread count |
| 05 | `Receipt` | takes the gold and the items once | item and gold messages, then `Receipt`: serial, flag, remaining seconds |
| 06 | `SendDel` | the sender stops listing it; the receiver keeps it | `SendDel`: serial |
| 07 | `RecvDel` | refused while it carries something; else gone for the receiver | `RecvDel`: serial, unread count |
| 08 | `SendBack` | sides change places, state "sent back", the first sender is told (`Recv` 09) | `SendBack`: 0 or a code, serial |
| 10 | `Info` | unread letters of the person and of the account | `Info`: two counts |
| 12 | `Save` | moves it to the kept letters (and marks it read) | `Save`: serial |
| 13 | `SaveList` | the kept letters | `SaveList`: letters, then 1 |
| 14 | `AccountList` | the letters of the account | `AccountList`: letters, then 1 |
| 16 | `AccountRead` | marks it read | `AccountRead`: serial, flag, remaining seconds, unread account count |
| 17 | `AccountReceipt` | takes the gold and items once, into this character | item and gold messages, then `AccountReceipt` |
| 18 | `AccountDel` | refused while it carries something; else deleted | `AccountDel`: serial, unread account count |

A failure of `Send` and `SendBack` is their own result field; every other failure is the error answer (`sub | 0x80`, u16).
The codes are numbers of the UI strings of the client (`PostMessages`):

| Code | Text (translated) | Used for |
|---|---|---|
| 2262 | the mail has attachments not taken | deleting a letter that still carries something |
| 2266 | you cannot send mail to yourself | sending to oneself |
| 2276 | a returned mail cannot be returned again | sending back a returned letter |
| 2277 | a system message cannot be returned | sending back a letter of the game, or one whose sender is gone |
| 55301 | the receiver does not exist | no such name, blocked by the receiver, full box (no better text) |
| 55302 | not enough gold (attached gold and fee) | sending |
| 55303 | an item that cannot be traded | an item not in the slot, from a storage that may not send, twice, or more than lies there |
| 55304 | bound items cannot be attached | a bound item, or an item whose table binds it from the start |
| 55306 | the mail was deleted | a letter that is not there; also a letter the server does not take at all (too long text, negative gold) |
| 55307 | too much gold | taking gold that would take the purse past 99 999 999 999 999 |
| 55308 | not enough room in the inventory | taking items |
| 55619 | account mail could not be deleted | deleting an account letter that still carries something |

A letter of the game for an account is made by the store (`SendToAccountAsync`) and, for testing, by the chat command
`/mail item [count] [gold]`, which also sends `AccountRecv` (15).

## Decisions on what is not verified

- **The trailing byte of a list.** The client refreshes the window when it is not zero; the server sends 1 with one list
  that holds everything (at most 40 letters), never pages.
- **`PostCount`** in `Read`, `RecvDel`, `AccountRead`, `AccountDel`, `Recv` and `AccountRecv` is taken as the count of
  unread letters (the mark of new mail), of the person or of the account.
- **`Flag`/`PostFlag`** is the low nibble of `State`: read 1, taken 2, sent back 4, kept 8. A letter that carried nothing
  shows no "taken". The client's own meaning of the bits is not known.
- **Times.** `RegisteredTime` is Unix seconds of the sending; `RemainTime` is the seconds left until `ExpiresAt`.
- **Character of a letter.** In the received and kept lists it is the sender; in the sent list it is the receiver (the
  client has texts for "sent by {0}" and "sent to {0}").
- **Attachments after they are taken** are not shown: the money is 0 and the item places are empty.
- **`Type`/`SubType`** of a letter of a person are 0; a letter of the game would carry the main and sub type of the client
  table of system letters. `Event` is 0; the socket and brooch lists are empty.
- **Postage** is 100 gold per letter (`PostLetters.Postage`), whatever it carries. The client shows a fee of its own; if it
  computes another one, the shown fee and the charged one differ.
- **Cash on delivery** (the client has texts for it) is not supported: the request has one money field and no sign of it.
- **`State` of `RecvDel`** (the byte after the serial) is read and ignored.
- **Deleting a letter that still carries something** is refused (2262) instead of destroying what it carries, although the
  client warns that the attachments go with it; sending it back is the way to return them.
- **Items that may be sent**: those of the common and the costume bag, free of binding (`ItemBinding.HeldBack`, see
  `notes-item-rules.md`): an item that is bound is refused (55304), so is one whose table binds it from the start even if it is
  not marked yet, and a broken item (flag 2) is refused with 55303. The reference answers 55620 for the table rule and 55602 for
  a bound state and a broken item; the client has a text for 55304 and 55620 ("Bound items cannot be attached") and none for
  55602, so the texts decide here. An item worn once is bound.
- **Expiry is not enforced**: `ExpiresAt` only feeds `RemainTime`. There is no automatic return or deletion.

## What remains

- A sweep that sends back or deletes expired letters (the client has an "automatic return time").
- Letters of the game from the game itself: maze or daily mission rewards that do not fit the inventory (`IRewardGranter`),
  shop gifts, the exchange.
- Paging of lists if a box larger than 40 letters is wanted.
- A check of the NPC of the post box and of the distance to it.
- A stand run with the real client: the flags, the times, the trailing byte, the fee.
