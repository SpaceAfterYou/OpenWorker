# The room of a player (group 0x26)

Branch `feature/my-room`. A player makes a room once for its account, goes into it from a district, arranges the furniture
of its furniture bag in it, names it, opens or closes it to others, sends visitors out; others go into it from the friend
list or the menu of a character, and everybody in it sits or lies on the furniture. Client 1.4.2.3 decided every layout below
(its senders, its receivers and the callers of each sender were read); the original server gave the rules, the order of the
checks and the codes. The comparison is at the end.

Code: `OpenWorker.Protocol/Modules/MyRoom/` (the messages), `OpenWorker.Gameplay/Modules/MyRoom/` (`MyRoomBook`: the room of
the account a person carries, 26/12 and 26/10; `MyRoomState`: a room while somebody is in it; `MyRoomCatalog`: the tables),
`OpenWorker.Server.Common/Services/MyRoomEntryService.cs` (26/10 and 26/11 on the district and the maze server),
`OpenWorker.MazeServer/Services/MyRoomService.cs` (everything inside a room), `MyRooms.cs` (the rooms of a maze server),
`OpenWorker.Persistence/MyRoomStore.cs`, `OpenWorker.Infrastructure.Redis/Cache/Types/MyRoomCache.cs`.

## What a room is

- One room per account and gate (the key of `MyRooms`); every character of the account on the gate owns it. The original keeps
  the room per account too. A room is made from a row of `tb_Myroom_Info` (the client has one: the maze 20011, 19 visitors, level
  10, 5000 gold) and is played in that maze (`tb_Maze_Info` 20011, kind 6, map `R01_MYROOM`).
- The furniture is the items of the furniture bag (storage 0x0B). A piece placed in the room leaves the bag and is kept in
  `MyRoomFurniture` under the serial of its item (the key: a serial stands in one room once); taken back, it comes into the bag
  as a new item of the same kind. `tb_Item.Furniture` names the row of `tb_Myroom_Furniture`; of its columns the server reads
  `Seats` (how many may use a piece at once, at most 4); `Placement`, `Width` and `Depth` are named for the client's grid.
- A room runs as an instance of its maze on a maze server while somebody is in it. The record of the room in Redis
  (`MyRoomCache`, key `<gate>-<account>`) names the server and the instance, so the owner and every visitor, from any district or
  room, are sent into the same one; it also tells how many visitors are in and whether the owner arranges, for the server that
  sends a player in. The maze server decides at the door and removes the record when the last player leaves. The reservation of
  the maze carries the account of the room and the district to go back to (`MazeReserveCache.Room`, `Return`).

## The messages

Owner info: `u32` account, `i16` row of the room table, `i16` grid, `u32` maze, `u8` open level (0 open, 1 closed), name
(counted UTF-16, 20 characters at most). Furniture: `i64` serial, `u32` item, `u8` rotation, `u32` cell (17 bytes).

| Message | Request | Answer |
|---|---|---|
| 26/10 make | `u32` row (the client sends 1, after checking its gold) | owner info, or an error |
| 26/11 enter | `u32` character: one's own, a friend's, any character's | a move into the room (04/02), or an error |
| 26/12 | - | owner info, on every entry into a server, for an account with a room |
| 26/11, 26/01, 26/24 | - | on coming into a room: owner info, the furniture (`u8` count), who sits where (`u8` count of `u32` actor, `i64` serial, `u8` place, `u8` animation; only when somebody sits) |
| 26/05 arrange | empty | empty |
| 26/20 place | furniture | the same, to the owner, after 08/05 (the item leaves the bag) |
| 26/21 take back | `i64` serial | the serial, after 08/06 (the item comes into the bag); 58212 when the bag is full |
| 26/06 done | `u8` count of furniture (the pieces that moved or turned) | empty |
| 26/22 sit | `u32` actor, `i64` serial, player `f32` x y z yaw, piece `f32` x y z yaw, `u8` animation | to everybody in the room: `u32` actor, `i64` serial, `f32` x y z (where the player stood), `u8` place, `u8` animation; 29064 when the piece is full |
| 26/23 get up | `u32` actor, `i64` serial | to everybody: `u32` actor, `i64` serial, `f32` x y z yaw (where it stood before) |
| 26/14 name | `u8` open level, name | the same, to everybody in the room |
| 26/15 send out | `u32` actor | - |
| 26/13 leave | empty | a move back to the district (04/02) |
| 26/25 | - | `u32` reason, before a player is sent out; the client shows it where it lands |

## The rules, in the order they are checked

- **Make** (26/10): the account has a room (58202); the row of the table (58203); the level (58200); the gold (58201). The gold
  is taken, the room is written, named after the character, open. A room made a moment before on another server answers 58202
  and gives the gold back.
- **Enter** (26/11): a character number of 0 (58204); from a maze that is no room (58249); the character asked for is unknown
  (58204); the player stands in that room already (58247); its own room is not made (58204), the other has none (58208); the room
  is closed to others (58207); the row of the table (58204); what the record tells: arranged (58205), full (58206). At the door of
  the maze server: the room is read from the database for the first player; arranged (58205), full (58206), closed (58207) send
  the player back with 26/25. The owner always comes in.
- **Arrange** (26/05): only the owner, in its own room; every visitor is sent out with 58205. **Place** (26/20) and **take back**
  (26/21) only while arranging, a serial once, an item of the furniture bag with that serial, that kind, a count of one, that
  is furniture. **Done** (26/06): the pieces named are moved and turned, a serial the room does not have is passed over, the
  room opens again.
- **Sit** (26/22): not while arranging; a place of the piece that is free, at random among the free ones, as many as its row
  gives (at most 4); the player who asks is the one who sits, whatever actor the request names; a position or a yaw that is not
  a number is the player's own. **Get up** (26/23): the player stands where it stood before.
- **Name** (26/14): the owner, in its own room; open level 0 or 1, a name of 1 to 20 characters; closing sends the visitors out
  with 58207. **Send out** (26/15): the owner, a visitor of its room; the visitor gets 58211.
- A player who leaves the room (26/13, sent out, or its connection lost) frees the place it sat on.
- **Achievement 54**: going into the room of a character who is in one's friend list counts one, when the player is sent.

## Compared with the original

| What | Here | Why |
|---|---|---|
| Checks of 26/10, their order and codes | same | |
| The check of 26/10 and 26/11 for a player waiting to be matched into a party (53150) | not made | the server has no matching queue |
| Checks of 26/11 and at the door, codes 58204 to 58249 | same | |
| The door: arranged, full, closed | checked at the door of the maze server, and before the move from what the record of the room says | the original asks the server that runs the room before it moves the player; here the move is from another process |
| 26/01 count | `u8` | the original writes a 16-bit count; client 1.4.2.3 reads one byte |
| 26/20, 26/21 outside the arrangement, for an unknown serial | nothing answered | same |
| A piece taken back | a new item of the same kind | the original puts the item back with its serial; the bag here gives every new item a new serial |
| 26/06 with a serial the room lacks | the others are moved, the arrangement ends and is answered | the original stops at that serial, writes nothing and does not answer, which leaves the client in the arrangement |
| 26/22 actor | the player who asks | the original trusts the actor of the request |
| 26/22 animation above 3 | refused with 29064 | the client picks one of four; the original takes any |
| A visitor in a closed room, a room being arranged | sent out with 58207, 58205 | same |
| Doors (26/26, 26/27), pollen, recommendations, favourites, the guest board, ranks (26/31 and up) | not made | client 1.4.2.3 has none of these messages |
| Daily missions of the room (finish kind 6) | not counted | the original counts recommendations and pollen help only, which client 1.4.2.3 cannot send, and the client's table has no mission of that kind (0 of 403) |

## Not verified with the real client

- That the client builds the furniture of 26/01 sent right after the entry answer, before it has loaded the map (the original
  sends it as the player enters the room).
- That the furniture bag shows a placed piece gone and a piece taken back (08/05 and 08/06 come before 26/20 and 26/21).
- That a player sent out of the room shows the reason of 26/25 in the district.
- That the grid cells and rotations the client sends land where it showed them (the server keeps them as they come).
