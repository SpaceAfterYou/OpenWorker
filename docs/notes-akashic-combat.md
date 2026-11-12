# Akashic cards in a fight (06/20, 06/21, 06/22, 06/7B)

Round 12, `feature/akashic-combat`. The book of cards, making, composing and taking cards apart are in
`docs/notes-item-setup-handlers.md`; the passive cards (cards of the quick slots that give a buff on a condition) are in
`docs/notes-buffs.md`. This note is about calling a card into a fight and what the called card does.

## What a player does and what the client expects

The akashic record book has five quick slots. In a maze, a key of a quick slot calls the card in it (06/20: the slot and the
place and yaw of the player). The client checks the card's own cooldown first and does not wait for an answer.

- 06/21 to the caller: a `u16` code (0), the card, the player and the place. For a card that brings a creature in (types 1
  and 2 of `tb_Akashic_Records`), the client then starts the hero's akashic skill (`Use_Skill`, 30000: skill type 3) and the
  cooldowns of the card (its group and the wait of all cards). A refusal is the error answer (06/21 with the error flag and a
  code); the client then does nothing.
- 06/22 to the others in the maze: the card, the player and the place. Their client remembers the card on that player and
  plays the summon when the player's akashic skill reaches them (06/10).
- 06/7B to everybody in the maze, the caller too: the player, the creature (an actor of kind 3), how it appears, its offset
  from the player and whether it turns with the player. The client puts the creature where it stands itself.
- 06/13: the blows of the creature, shown as the creature's.

In a city nothing happens: the original returns before any check there, with no answer, and the client waits for none.

## What the server does

`AkashicCalls` (Gameplay/Combat) decides a call, `AkashicCallService` (maze) answers it:

1. A fallen player is not answered (as the original).
2. The card of the slot (0 to 4) must be in the book; the maze must not bar cards (the server's `tb_Maze_Info`, 22 mazes of
   the server's table bar them: `MazeRuleRow.CardsBarred`). Otherwise code 1.
3. Types 1 and 2: the card's skill (the column `Buff` of the row, which holds the buff of a passive card) must be in
   `tb_Skill` (else 56001), and neither the card's group nor the wait of all cards may be running (else code 2). The card is
   remembered with the player's yaw (`AkashicCallComponent`), its group cools down for `Cooldown` ms and all cards for
   `CooldownGlobal` ms (`AkashicCooldownComponent`, apart from the cooldowns of skills; no reductions apply).
4. Other types (passive cards, the three cards of type 6) are answered and do nothing more.

The akashic skill of the player (`LuaMaze.StartSkill` of skill type 3) with a card called brings the card in instead of
playing the skill's own attacks:

- When: the hero's `Akashic_Active_Time` (`tb_Character_Info`) after the skill starts. A new skill before that moment, a fall or
  leaving the maze calls it off.
- Where: the akashic file of the heroes (`PC_A_Akashic.adf`; the other heroes' files are empty and the first hero's is used for
  all, as the original does) lists, under `"<card's ActionGroup>:<animation of the skill>"`, the events of kind 18 with the offset
  of the card from the player, turned by the yaw of the call. A key with two events brings two creatures.
- Then the creature plays the card's skill from its own action file (`<CodeName>.adf`, e.g.
  `Akashic_MOB_9066_DignitySnacher`): every attack of the skill's animations lands at its moment from where the creature
  stands, facing the yaw of the call. The damage is the player's (the player's stats, the card's skill row), the message is the
  creature's. The blows of a card do not count toward the player's combo and do not wear the player's weapon. A creature
  whose player has fallen or left strikes no more. It lives as long as its skill: nothing is sent when it is over.
- The call is used up by the skill that brings the card in.

## Compared with the original

| What | Original | Here | |
|---|---|---|---|
| Checks of 06/20 and their codes | slot and book 1, barred maze 1, unknown card 1, card without a skill 1, unknown skill 56001, cooldown 2 | same | same |
| City | returns before any check, no answer | no answer | same |
| Answer and broadcast | 06/21 with the request's place and a tick block the client does not read; 06/22 to the others | 06/21 and 06/22 with the place the server keeps and the yaw of the request | kept different: the place is the server's, the tick block is not sent |
| Cooldowns of cards | the card's group and the wait of all cards, their own map | same | same; not kept across a change of server or a relog |
| Summon moment and place | the akashic events of the first hero's file, at the hero's time | same | same |
| 06/7B | to everybody, with the offset of the event and a yaw the client does not read | same without the yaw | same for the client |
| The called card after the summon | kept until the player is reset: the akashic skill used again brings it again | used up by the summon | kept different: a call brings one card |
| Blows of the creature | the player's damage with the card's skill, shown as the creature's; combo only for cards of a combo kind | same; no combo | approximately: the column of the combo kind is not in the client's table |
| Buffs and auras of the creature's attacks | on the targets hit, from the creature | through the player's own path: on the monsters hit; a buff for the side of the player reaches the allies around the player | approximately |
| Projectiles of the creature's skill | fired by the creature | not fired | not done |
| The creature's end | at the end of its animation, no message seen | same | same |
| Option effects of the condition "uses an akashic card" | fired for type 1 | not fired | not done (no effect of the client's tables names it as far as checked) |

## Tests

`AkashicCallTest`: a called card waits for its cooldowns; only a card of the book is called where cards are allowed; a passive
card is answered and calls nothing; with the client's tables and files, the akashic skill brings the called card in (06/7B with
the offset of the file) and the card strikes the monster it stands on with blows shown as its own (06/13). `SkillWireFormatTest`:
the layout of 06/21 and 06/7B.

## To check with the real client

- Call a card (quick slot of the book) in a maze: the akashic animation plays, the creature appears before the player, its
  attacks show damage on the monsters, the quick slot shows the cooldown.
- A second player in the party sees the creature and its damage.
- In a maze that bars cards (for example 21100) the call is refused: what the client shows for code 1.
- In a city the key does nothing (the client prints its own message).
