# Columns of the skill, buff and akashic tables (client 1.4.2.3), checked on 2026-11-12

The same check as `notes-item-table-columns.md`, for `tb_Skill`, `tb_Skill_Script`, `tb_Buff`, `tb_Buff_Script`,
`tb_Akashic_Records`, `tb_Akashic_Parts` and `tb_Akashic_Make`. Sources: the client's reader of each table (the widths and
the order of the columns), the client code that reads a row of each (found from the lookup of a row by its id and from
the places that keep a row of the current skill), the values of all rows of this client, and the rows of the original
server's tables, matched column by column over the ids both have (they give meanings, not the layout). Research files
with addresses: `docs/agents/docs-table-columns-skills/` (git-ignored).

## Layout

The client reads every column of the seven tables with the width and in the order `Structures.json` has: 82 columns of
`tb_Skill`, 8 of `tb_Skill_Script`, 35 of `tb_Buff`, 5 of `tb_Buff_Script`, 33 of `tb_Akashic_Records`, 3 of
`tb_Akashic_Parts`, 7 of `tb_Akashic_Make`. Nothing was shifted. The client keeps a row as the packed columns, so the place
of a column in the row is the sum of the widths before it (a string takes 28 bytes); that is how a read of the row in
the client was tied to a column.

## Signedness: columns read as signed that hold unsigned values

| Table, column | Was | Now | What showed it |
|---|---|---|---|
| `tb_Skill` `RangeMin`, `RangeMax` (44, 45) | Int16 | UInt16 | Every read of them in the client is unsigned (six functions, among them the targeting of a marking skill and a check that a distance lies between the two). 74 monster skills have 50000 in `RangeMax`. |
| `tb_Skill` `Duration` (66) | Int16 | UInt16 | Three skills last more than 32767 ms (61830, 63000, 64170); the original server's table has it unsigned. |
| `tb_Skill` `PassiveValue` (19) | Int16 | UInt16 | It is the id of a buff for the passive skills that give one, and the client reads it unsigned to look that buff up; 115 rows (the skills of the akashic cards) hold 50011 and above. |
| `tb_Buff` `Script` (31) | Int16 | UInt16 | The id of a row of `tb_Buff_Script`; one is 35000. |
| `tb_Buff_Script` `Id` (0) | Int16 | UInt16 | The same ids, from the other side. |

Left signed on purpose: `Name` and `FamilyName` of `tb_Akashic_Records` hold ids of `tb_UI_String` above 32767 (they read
negative), but `tb_UI_String`'s own id is still `Int16`, so the two read the same 16 bits and match. Making them unsigned
needs `tb_UI_String` changed at the same time (not one of the tables of this task).

### What changed in the server because of it

- **Monster AI** (`MonsterBehaviorTables`, `AiMachine`): the 74 skills with a reach of 50000 had `RangeMax` = -15536, so
  `distance <= RangeMax + radius` never held and the monster never used them; it also tried to stand at the nearest place
  instead. They belong to 97 monsters of `tb_Monster`. Now the monster may use them from any distance.
- **Monster animation times** (`MonsterBehaviorTables.AnimationTimes`): the three skills that last over 32 s had a
  negative time.
- **`SkillActionProvider.GetMaxRange`** (the direction of a shot in `LuaMaze`) gave a negative reach for the same 74
  skills.
- `PassiveBuffs` already cast `PassiveValue` to `ushort`, so the passive buffs of akashic skills were right before.

## `tb_Skill`

Named (they were `FieldN`):

| Column | Name | What the client does with it |
|---|---|---|
| 1, Int16 | `Script` | Looks up the row of `tb_Skill_Script` (name, texts, icon) by it; every value is a row of that table. |
| 26, UInt16 | `MoveSpeed` | The speed of the user while the skill plays and the player moves: the client returns this value as the speed of the mover then. 22 rows. Unsigned read. |
| 41, 42, Int32 | `ChargeDistanceMin`, `ChargeDistanceMax` | For a charged skill: the distance ahead where the client puts the target marker, from the first to the second over the charge time. |
| 43, Byte | `MaxMarkedTargets` | For a skill of control type 6 (marking targets): the skill goes off when this many targets are marked. One row. |
| 50, Byte | `NoCollision` | 1: the client turns the user's collision off at the start of the skill and on at its end (534 rows). |
| 51, Int32 | `ChainSkill` | When the skill ends, the client asks to use this skill next (101 rows). |
| 64, Byte | `KeyPressFollowUp` | 1: pressing the skill's key again while it plays sends the key press to the server (39 rows; one row has 2, not known). |
| 71, String | `CameraFile` | A camera file the client plays (40 rows). |
| 72, String | `ChargeMarkerEffect` | The effect file of the marker of a charged skill (under FXs). |
| 74, String | `Movie` | A movie the client plays during the skill (one row). |
| 75, String | `Prefab` | A prefab under World the client places when column 73 is 2 (`"0"` in every row). |

Left unnamed, with what was found (the comment of each column in `Structures.json` says the same):

| Column | Values | Found |
|---|---|---|
| 27, Byte | 1 on 50 monster skills | No client read. The original's row has a flag about the state of a monster's target here (all common rows agree). |
| 34, Byte | 11 on most class skills, 10, 17..23 | No client read; the original's row has nothing here. |
| 40, Int32 | 0 in every row | Added, divided by the charge time, to a running amount while charging. |
| 56, String; 57, Int16; 58, Int32; 69, Int16; 70, String | constant | No client read. |
| 67, Byte | 1 on 9 | Tested against the user's state (1 using a skill, 2 some motions, 3 always) in a check whose caller was not found. |
| 68, Byte | 1 on 18, 2 on 2 | Kept while the skill plays; 2 stops the animation of a skill used in the air. |
| 73, Byte | 1 on the movie row | 2 makes the client place `Prefab`; 1 not known. |
| 76, Byte | 1, 0, 2, 16 | No client read. |
| 77, Byte | 1 on 161 | Turns the user to its facing as one of the client's own settings does. |
| 79, Byte | 1 on 58 | Passed along with `CameraFile`, but no row with it has a camera file. |
| 80, Byte | 1 on 3 | Another path when the skill is used; not followed. |
| 81, Byte | 1 and 2 on 38 each | Sets a state on each of six skill slots that hold such a skill. |

Named columns checked and kept: every one agrees with the original's row on (nearly) all 5794 common skills, and the
client reads most of them the way the name says: `RequiredPoints`, `MaxLevel`, `RequiredSpentPoints` (compared with the
points the player has, the learned level and the spent points), `RequiredSkill` (must be learned), `NextLevel` (the client
walks the levels by it), `DivergenceGroup1/2`, `SkipPriorityGroup` (compared between two skills), `PassiveType` (13 gives
the buff of `PassiveValue`), `BasicDamage` + `AddDamage` and `DamageRate` (the tooltip), `TargetCount` (a count of
targets), `Cost`, `ChargeCount`, `ChargeTime1..4` (an array of four), `UseState`, `NormalState`, `Cooldown`,
`CooldownGroup`, `LoopTime` (milliseconds; 0 lets the loop animation's own length count), the four animations.
`BehaviorCondition`, `CooldownSave`, `Direction`, `Attribute`: no client read found; the comment says so.

37 skill ids appear twice in this client's table (the monster tables keep the first).

## `tb_Skill_Script`

All columns were unnamed. Named from the client's tooltip and skill window: `Name`, `Description`, `LevelText` (what the
level gives, under its own heading), `Icon` (the client adds `.png`), `ShownSkill1`, `ShownSkill2` (skills whose icons the
tooltip shows in a section of their own; 0 in every row, so the section's purpose is not settled), `DescriptionImage`
(a picture of the skill window, `.png` added).

## `tb_Buff`

Named (they were `FieldN`):

| Column | Name | Found |
|---|---|---|
| 24, 26, 28, Int16 | `StartEffect1..3` | Effects started on the carrier when the buff lands: rows of `tb_ResVFX` (692 of 708 values of 24, all of 26; 28 is 0). |
| 25, 27, 29, Int16 | `EndEffect1..3` | The same when the buff is taken away; 0 in every row. |
| 30, String | `Sound` | A sound the client plays at the carrier when the buff lands (`Common/Item_Use/HP` and the like). |
| 31, UInt16 | `Script` | The row of `tb_Buff_Script`; the client looks it up by this column (1029 of 1044 values are rows of it). |
| 33, Byte | `ShowsLabel` | 1: the client shows the label of the buff's script over the carrier when the buff lands. |

Unnamed: 32 (Int32, 0 everywhere, no read) and 34 (Byte, 0 on three buffs only: the client lists a changed buff in a
frame of an actor when it is 1 or the actor passes a check of its own; not settled).

Named columns checked and kept: every one agrees with the original's row on most of the 1166 common buffs; the client
reads `Kind` (counts buffs and debuffs), `Effect`, `Group` (the slot of the buff), `SuccessRate` (rolled out of 10000),
`Grade`, `DurationMilliseconds`, `StandByMilliseconds`, `OverlapType`, `OverlapCount`, `ActionName`, `CallOutSkill` (looked
up as a skill when the buff is taken away), `Stat1..3`, `Value1..3`. `DeleteType` is a set of bits, and the client clears
the buff on an event whose bit is **not** set: it names what does not take the buff away (1 death, 2 leaving the game), as
the server already treats it. The name is kept; the comment says it.

## `tb_Buff_Script`

`Id` (now unsigned), `Name`, `Description`, `Icon` (the list of buffs of an actor), `Label` (shown over the carrier when
`ShowsLabel` is 1).

## `tb_Akashic_Records`

| Column | Was | Now | Found |
|---|---|---|---|
| 1, Int16 | `Field1` | `Name` | The client looks the card's name up in `tb_UI_String` by it; every row is a row of that table. |
| 2, Int32 | `Field2` | `Family` | The levels of one card share it; on 252 rows it is the id without its last two digits, 0 on 46. No client read. |
| 3, Int16 | `Field3` | `FamilyName` | A row of `tb_UI_String` shared by the levels: the name of the creature. |
| 7, Byte | `Field7` | `Rank` | 1 to 3, passed to the view of a card; agrees with the original's row on every common card. |
| 10, String | `Field10` | `Illustration` | The picture of the card. |
| 12, 13, 14, Int16 | `Field12..14` | `Part`, `Part2`, `Part3` | Rows of `tb_Akashic_Parts` (every value). The client builds the card's creature from `Part`; 13 and 14 have no client read. |

Unnamed: 6 and 9 (flags passed to the view of a card; the original's row has a flag of cash cards and of hidden cards
there), 11 (the same path as `Illustration` in every row), 23 (2 on all but three rows; the original's row has a flag of
keeping the cooldown there), 28 (1 everywhere), 29 to 32 (0 everywhere).

**`Buff` is half right.** It is the buff of a passive card (type 4) and the skill of the creature on a summon card
(type 1); the two id spaces overlap. The server code already reads it both ways. A better name (`Effect`) would touch
the combat and buff code that round 12 owns, so the name is kept for now and the comment says what it holds.

`Position` (one per card, 0 to 298) has no client read; the server keeps a recorded card at it. `ActionGroup`,
`InvokeCondition`, `InvokeRate`: no client read, the server uses them. `UseSkill` (30000 on every summon card) is started
by the client when a card is called; `Cooldown`, `CooldownGroup` are read the way the skill's are; `ExpGiven` is summed
over the cards fed to another; `MakeExp` is shown as a share; `Disassemble` is the card's own id in every row.

## `tb_Akashic_Parts`

`Model` (the client sets it as the model path of the item it builds for the creature) and `Animation` (loaded with
`.anim` and `.vTransition`).

## `tb_Akashic_Make`

No unnamed column; the names stand: the client reads `Material` and `Card` for the window of the recipe, `ExtraCount` for
the places of the extra cards and `Gold` against the money. `Result` and the id are the same in every row. The original's
table is different (it has 716 rows and more columns), so it gives nothing here.

## Open

- `tb_UI_String`'s id should be unsigned, and with it `Name` and `FamilyName` of `tb_Akashic_Records`.
- `UseEffect` of `tb_Item` (column 52) goes through the same client call as the buff effects: it is very likely a row of
  `tb_ResVFX` too (all 0 in this client).
- Rename `Buff` of `tb_Akashic_Records` once round 12 has merged.
- The unnamed columns above.
