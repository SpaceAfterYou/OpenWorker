# The last requests outside ItemSetup: party, persons and the maze reward

Five requests had no handler outside the item work of `ItemSetup`: two of the party group (0x12), two of the person group
(0x03) and one of the item group (0x08). What the client 1.4.2.3 sends was taken from the client itself; the reference
server gave the meaning and the error codes. This file says what each request does now and why. The guesses that a run
with the real client should check are listed at the end.

## Summary

| Request | Opcode | Sent by the client | Now |
|---|---|---|---|
| `ItemMazeRewardItemRequest` | 08/47 | yes, from the window at the end of a maze | handled in the maze (`MazeRewardService`) |
| `PartyRecruitApplyDelRequest` | 12/3F | no, the client only reads 12/3F | the type is now `PartyRecruitApplyDelResponse` |
| `PartyUpdateMemberSgRequest` | 12/13 | no, and the client does not read 12/13 either | both types removed |
| `PersonAchieveRewardRequest` | 03/74 | from the window of the achievements | no handler: the server keeps no achievements |
| `PersonGetRewardSharePointRequest` | 03/63 | only for districts the server told it about | no handler: the server keeps no progress of districts |

The count of request types without a handler goes from 38 (of 276 request types) to 35 (of 274): the two party types are
gone, the maze reward has a handler. Of the 35, the 33 of ItemSetup belong to their own branch; the two of the persons
wait for state the server does not keep (see below).

## The key reward of a maze (08/47)

The window shown when a maze is finished (`MazeRewardInfoResponse`, 11/75) has, after the base reward, a second reward:
an item and a count (`CashItemCount`, `CashItem`). The client shows it when both are above zero. To open it, the player
needs a key: the only item of the client tables whose kind is `ItemUseType.REWARD_ADD`. When the player clicks, the client
looks for a key in its bags and sends the storage and the slot of that stack (`ItemMazeRewardItemRequest`), once per
window. It reads nothing of the answer 08/47; the items come and go by the messages of the inventory.

What the server does:

- **Where the reward comes from.** `tb_MazeReward_Item`, a table the server has and the client does not
  (`OpenWorker.Res/ServerRows/MazeRewardItemRow.cs`, optional like the drop tables). A row per maze names a random box of
  `tb_Item_RandomBox` and how many keys open the reward. The row also holds a reward by rank and a second key reward;
  those are read to reach the end of the row and not used.
- **When.** `MazeResults.Complete`, the end of the maze, asks `MazeKeyRewards.Roll` for the maze the player entered
  (`MazeResults.Start` now takes the maze). One item is rolled from the box by the rates of its items; the gold, battle
  points and ether a box may also hold are not part of this reward. The item must exist in the item tables. The offer is
  kept for the player and its item and count go into the window. A maze without a row, without a box, or whose box gives
  no item offers nothing: the window shows no second reward and the client does not send the request.
- **The request** (`MazeRewardService`, maze only): `MazeKeyRewards.Take` checks, in this order, that an offer waits
  (else 52011), that the named slot is in a bag (common, costume, cash, cube) and holds a key with at least the keys the
  reward asks for (else 52011), and that the reward fits the bags (else 52013). Then the keys are spent from that stack and
  the item given, both through `InventoryService`, so the dirty mask of the inventory marks them for the flush, and the
  offer is dropped. A refusal is the error of 08/47 and the offer stays until the player leaves the maze, though the
  client itself sends the request only once per window.
- **Once.** The offer exists from the end of the maze until it is taken, a new maze is started, or the session closes
  (`MazeResults.OnSessionClosedAsync` forgets it). A second request after the reward was taken gets 52011 and nothing.
- **The answer** on success is the empty 08/47.

Left: the reward by rank (the base item of the window) is not given; the window still shows none. The rank itself has no
scoring behind it (`MazeResults.BestRank`).

**Data.** The stand has the client tables only, so it offers nothing. With the server tables in the `res` folder, the
rows name boxes that the client copy of `tb_Item_RandomBox` lacks; the server copy of that table has them, in the same
layout, with items the client knows. Which copy the server should read is for whoever assembles the `res` folder.

## The party (12/3F, 12/13)

The client 1.4.2.3 has no sender for either opcode.

- **12/3F** is an answer the client reads: an actor and a number. It takes the actor off the list of applicants of the
  recruitment it shows, when the number is that recruitment. `PartyRecruitApplyDelResponse` (`Applicant`, `Recruit`)
  replaces the request type. Nothing sends it: the recruitment board of the district (`PartyService`) answers with fixed
  examples and keeps no applications, so no application is ever withdrawn. It can be sent once applications are kept.
- **12/13** is neither sent nor read by the client. Its request and its answer, which threw `NotImplementedException`,
  are removed. The opcode stays in `PartyOpcode`.

## The persons (03/74, 03/63)

Both are sent from windows of the client, and both depend on state the server does not keep. They have no handler, so
they fall into the empty handler as before; the reference server answers nothing either to an achievement that is not
finished.

- **03/74, the reward of an achievement.** The client sends the index of a row of `tb_Achievement`. Answering it needs
  the progress of every achievement of a person and the set of rewards taken (a bit field of 128 bytes in the reference),
  neither of which exists, so the server could not tell a finished achievement from one never started. The server does not
  send the achievements either (`PersonAchieveSelectResponse`). Implementing it needs a table or columns for the progress
  and the bits, and the counting of the conditions.
- **03/63, the reward of the progress of a district or a group of mazes.** Type 0 is a district, type 1 a group of
  mazes; the identifier is a row of `tb_DS_Point`. The client sends it only for a district it was told about by
  `PersonLoadDistrictStateResponse`, `PersonLoadMazeStateResponse` or `PersonUpdateSharePointResponse`, and the server
  sends none of them, so the client never sends it now. Implementing it needs the points of each district and the clear
  count and reward step of each maze group stored per person, their growth when a maze is cleared, and these three
  messages. The answer, `PersonGetRewardSharePointResponse`, already has its layout: the identifier, the step reached, a
  result (0 given, 1 not reached or unknown, 2 cannot be given, 3 no table row) and the items.

Neither fits without a schema change, which this branch does not make.

## To check with the real client

- That the window shows the key reward from `CashItemCount` and `CashItem` (the client checks the fourth and fifth
  numbers after the base reward; the names of the fields come from before).
- That 52011 and 52013 are shown sensibly for 08/47 (they are the reference codes for "the keys could not be taken" and
  "the item does not fit").
- That the client sends 08/47 once per window and never from a district.
