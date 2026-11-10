# Parties: the party, the recruitment board, the awaiters and the auto matching

Group 0x12. The client 1.4.2.3 sends 22 commands of it and reads 34; the layouts were checked against the client before
this work (all the messages of the group, two left open: 12/44 and 12/47). This file says what the server keeps, where,
for how long, and what each message does. It holds no research data.

## Where it lives

Everything is in Redis, so a party is the same on every district and maze server: a member may stand in a district, another
in a maze, a third on another district. Nothing is in Postgres (parties do not survive a restart of everything, as in the
game; the schema did not change).

| Key | What | Lifetime |
|---|---|---|
| `ow:party:{id}` | hash: `master`, `type` (0 normal, 1 auto matching), `m:{person}` the card of a member (name, hero, level), `off:{person}` since when a member is not online (Unix seconds) | 5 min, renewed every 30 s by the servers of its online members |
| `ow:party:of:{person}` | the party of a person | as the party |
| `ow:party:seq` | the numbers of parties (a recruitment of somebody alone takes one ahead) | none |
| `ow:party:invite:{person}` | the invitation that waits: inviter and name | 60 s |
| `ow:party:recruit:{id}` | hash of a recruitment: master, name, message, levels, purpose, place, `until`; `a:{person}` per applicant (card and end of the application) | its end plus 30 min |
| `ow:party:recruits` | sorted set of recruitments by their end | none (expired ones are taken out) |
| `ow:party:recruit-of:{master}` | the recruitment of a master | 30 min |
| `ow:party:applied:{person}` | what a person applied to | 30 min |
| `ow:party:recruit-penalty:{master}` | the wait after taking one's own recruitment down | 10 min |
| `ow:party:awaiters` | hash person → card and end of the entry | entries 30 min |
| `ow:party:match:{id}` | hash of an auto matching group: maze, state, deadline (ms), limit, leader, `p:`/`l:`/`c:` card, level, answer per person | 5 min |
| `ow:party:match-maze:{maze}`, `ow:party:match-of:{person}`, `ow:party:match-seq` | the groups of a maze, the group of a person, the numbers | 5 min / none |

Every change that has a limit or a race is one Lua script: joining (size 4, the master must still be master), leaving (a
master hands over, the last but one ends the party), handing over, applying (10 applicants, 5 applications, levels), taking
a recruitment down (only one server gets it), joining a matching group (same maze, collecting, level within 5 of the
average, at most 4) and moving a group on.

## The code

- `OpenWorker.Channel/Community/PartyRules.cs`: sizes, times, the string numbers of the client.
- `PartyBook`: the parties and invitations; it is the `IPartyRoster` of the server (party chat and the maze read it).
- `PartyNotifier`: changes a party and tells the members wherever they are (`CommunityNetwork.SendAsync`). For other parts
  of the game: `SendAsync(person, message)` (the whole party), `SendToOthersAsync`, `MemberHealthAsync` (12/12),
  `MazeClearAsync` (12/46), `CardOf`, `Info`, `ListAsync` (12/09). It is a community listener: at the entry of a member
  on any server it marks them online, sends them the party (12/09) and the others their info (12/06); at a disconnect it
  marks them offline and sends the others 12/06 with the member not logged in.
- `PartyRecruitBoard`, `PartyAwaiterBoard`: the board and the awaiters. `PartyRecruitNotifier`: who hears what of them; when
  somebody joins a party (`IPartyJoinListener`) their applications, their entry among the awaiters and a recruitment of
  their own for another party go, and a full party's recruitment is taken down; at the entry of an applicant the masters
  get 12/3B; expired recruitments are taken down every second by whichever server sees them first.
- `PartyMatching`: the auto matching groups. The server that started a group moves it on (every second); a group whose
  server stopped is gone after 5 min.
- `PartyUpkeepService`: every second the clocks (board, matching), every 30 s the upkeep of the parties of the people of
  this server: renews them, marks members who are online nowhere, and takes out a member offline for more than 90 s
  (the presence lifetime): a move between servers takes less, a crash or a logout more.
- Handlers: `PartyServiceBase`, `PartyRecruitServiceBase`, `PartyMatchingServiceBase` in `OpenWorker.Channel`, sealed in
  `OpenWorker.Server.Common/Services/PartyService.cs`, so the district and the maze both answer them.

## The messages

| Request | What happens | Answer |
|---|---|---|
| 12/01 invite (by name) | target online, not self, neither in a maze, the inviter alone or master of a party that is not full, target in no party, no block either way, no invitation waiting | 12/01 to the target; errors 53011, 53003, 53017, 53001, 53002, 53010, 53004, 53013, 53014, 53015 (you invited already), 53018 (another invitation waits) |
| 12/02 accept | the invitation kept by the server (not the one the client names); master online, neither in a maze; makes a party of the two or joins the master's | new party: 12/09 (update 1) to both; join: 12/10 to the others, 12/09 (update 2) to the new one; failure: 12/02 with the code (the client shows 53016) |
| 12/08 cancel | the invitation goes | 12/08 to the inviter (0 or 53028 busy) |
| 12/03 change master | master only, a member, not self | 12/03 to all; 53006, 53002, 53003, 53011, 53007 |
| 12/04 kick | master only, a member, not self | 12/05 with the kick flag to all, the kicked too; a party left with one ends (12/07); 53006, 53003, 53002, 53011 |
| 12/05 leave | a master hands over to the member with the lowest number (12/03 to the rest) | 12/05 to all; with one left, 12/07 to both; 53006 |
| 12/09 list | asked by the client at every entry | 12/09 (update 0); nothing for somebody alone (the reference only marks the person for its next sync; fixed 2026-11-10, an error popped up a window at each entry) |
| 12/36 list | recruitments that last | 12/36 |
| 12/37 add | the master of a party that is not full, or somebody alone; message up to 20, levels 1..100; renews one's own | 12/37 success with 1800 s; the penalty: 12/37 with success 0 and the seconds left; 12/3E to the other members; 53002, 53010, 53012 |
| 12/38 apply | in no party; outcome as `PartyApplyError`: 1 no recruitment, 2 applied already, 5 five applications, 6 ten applicants, 7 level | 12/38 (outcome, applications now); 12/45 to the master; 53004 in a party |
| 12/39 accept applicant | the master's recruitment, a listed applicant, online and alone; the party keeps the number of the recruitment | 12/39 to the master and the party messages as for an invitation; 53027, 53021, 53011, 53004, 53010 |
| 12/3A reject | | 12/3A to the master, 12/3F to the applicant; 53021 |
| 12/3C delete | the penalty of 10 min starts | 12/3C to the master and the party, 12/3F to every applicant; 53027 |
| 12/3D applicants | | 12/3D, ten places |
| 12/43 recruitment's party | | 12/43: party number (0 when gone, the client reads no more), its members, the master |
| 12/40 awaiter add | in no party | 12/40 the entry (30 min); 53004 |
| 12/41 awaiter delete | | 12/41 |
| 12/42 awaiters | | 12/42 |
| 12/44 awaiter info | the window of another player, from wherever they are | 12/44 |
| 12/30 matching enter | a maze of the table, in no party and not in a maze | 12/30 to the group (id, four places, seconds left); 53012, 53004, 53017 |
| 12/31 matching exit | a collecting group goes on without them, an asking or waiting one falls apart | 12/31 (person, reason) to the group |
| 12/32 matching check | the answer is kept for the server that moves the group | none |

Pushes: 12/06 member info (entry, disconnect), 12/07 the party ended, 12/10 a member came, 12/12 health (by the maze),
12/32 the question, 12/34 the leader (wait), 12/31 with person 0 when a group falls apart (reason 1 a no or no answer, 3
too few at the end of collecting), 12/3B an applicant moved, 12/3E the recruitment of one's party, 12/3F an application
went, 12/45 a new applicant, 12/46 a cleared maze (by the maze). 12/33 (reset), 12/35 (auto penalty) and 12/47 are not sent:
the reference sends no penalty for leaving a matched party, and nothing that needs the reset.

The auto matching: a group collects for 30 s; a full group (4) names its leader at once; at the end of collecting a group of
at least two (and at least the maze's limit: `tb_Maze_Info.Admission` 2 → 2, 3 → 4) is asked (12/32, 10 s to answer);
when all say yes the leader (the highest level, then the lowest number) is named (12/34) and after 5 s the group becomes a
party of type 1 with the leader as master (12/09 with update 3). A no, a missing answer or too few dissolve it.

## Assumptions and what to check with the real client

- A disconnect does not leave the party at once (the reference never removes a member who logs out); the member is shown
  offline at once and taken out after 90 s offline. A member who comes back later than that finds no party.
- Errors go as `12/(sub | 0x80)` with the string number; the reference sends some of the invite errors inside 12/01. Check
  that the client shows both the same way.
- The invitation is not echoed to the inviter (the reference does not); check that the inviter's client waits for nothing.
- 12/02 is sent only on failure; 12/39 carries the applicant (the client only logs it).
- Health in the party list is the entity's for a member on the same server and 1/1 for the others; the maze sends the
  real value with 12/12 through `MemberHealthAsync`.
- The level of a member is the one their card had at their last entry; location and channel come from the presence.
- The recruitment number is the party's; a master alone gets a number ahead, which the party made on the first accepted
  application keeps. Check that the client shows a recruitment of somebody alone and accepts the number in 12/38 and 12/43.
- A recruitment whose master goes offline stays until it expires; the applicants of an offline master wait.
- Matching does not look at blocks between people (the reference does) and does not check what the maze needs to enter
  (level, items): the entry of the maze does that. The party it makes is not sent into the maze: that is the maze's work.
- 12/44 sends the window of the person with the titles they show; 12/47 is still unknown.
