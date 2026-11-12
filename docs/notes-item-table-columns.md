# Columns of `tb_Item` and `tb_Make` (client 1.4.2.3), checked on 2026-11-12

Asked for by the user for the Items and Craftables tabs of `OpenWorker.Content.Tool`. Sources: the client's readers of
the two tables (the column widths and order), the client code that reads a row, the values of all 5566 item rows and
827 recipe rows, and the row structures of the original server (a different, server-side table: it gives meanings, not
the layout). Research files (git-ignored, with addresses): `docs/agents/coordinator/items/`.

## Layout

The widths and the order of all 60 columns of `tb_Item`, 18 of `tb_Make` and 14 of `tb_item_script` in
`Structures.json` are the ones the client reads. Nothing was shifted.

## The three unnamed columns of `tb_Item`

| Column | Was | Now | What it is | How sure |
|---|---|---|---|---|
| 52, Int16 | `Field52` | `UseEffect` | When the server tells that somebody used the item (08/13), the client reads this column and, if it is not 0, starts something with that number on the user (one call on the actor, with the number and 0). 0 in every row of this client, so nothing is ever shown. | The read is certain; what the number selects (an animation or an effect) is not known. |
| 56, Byte | `Field56` | `TuckedLook` | 1 on tall boots and on the variant of trousers made to be worn inside them (23 rows). When a character is dressed, the client compares this flag of the shoes with the flag of the trousers; if they differ, it draws the trousers' other variant (column 57) instead. | Certain: code and data agree (every flagged row is a pair of boots or the second variant of a lower piece). |
| 2, Byte | `Field2` | still `Field2` | Not found. No code that reads it from an item row turned up in the 956 client functions that touch the item table. Values: 3 on every weapon and piece of gear (classes 12, 14, 15), 6 on every akashic card (class 16), 2 on the blueprints of weapons and gear and on 8 odd costume pieces, 1 on 368 blueprints of one family of costumes, 0 on everything else (other costume blueprints too). The original server's row has no such column. | Open. |

## A named column that was wrong

| Column | Was | Now | Why |
|---|---|---|---|
| 57, Int32 | `SimilarGroup` | `OtherLookItem` | It is not a group: it is the id of an item, the same lower piece in its other look (inside boots / over shoes). 28 distinct values, every one an id of `tb_Item`, and the rows point at each other in pairs. Read only together with `TuckedLook`. |

## Named columns checked and kept

| Column | Check |
|---|---|
| `Classify` | Every value is a row of `tb_Item_Classify`. |
| `ScriptItem` | Every value is a row of `tb_item_script`. |
| `Model`, `SubModel` | 93 % and 100 % are rows of `tb_item_model`. |
| `Option` | 99 % are rows of `tb_Random_Option`. |
| `Socket` | Two kinds of id in one column: a 4 or 5 digit id on weapons (191 rows), an item-like id on costumes (643 rows; 93 % are rows of `tb_Costume_Socket`). |
| `SetItem` | 90 % are rows of `tb_Item_SetItem`. |
| `Evolution` | One row has a value, and it is a row of `tb_Item_Evolution`. |
| `Disassemble` | 98 % are rows of `tb_Disassemble` (settled earlier, `notes-itemsetup-disassemble.md`). |
| `Reinforce` | Same key space as `Disassemble`; kept on the earlier research. |
| `Furniture` | 547 rows, all of class 21, numbered 1 to 547 without a gap; `tb_Myroom_Furniture` has 523 rows. Round 12 (`feature/my-room`) reads that table: its notes decide the link. |
| `Level`, `LevelLimit` | Two different numbers (940 rows differ): the level of the item and the level a player needs. |
| `PhysicalAttack`, `MagicalAttack`, `PhysicalDefense`, `MagicalDefense` | Weapons fill the first, armour the third; the two magical ones are set only on 8 test rows. |
| `SealLimit`, `RestoreLimit` | Set only on gear and costumes, values 0 to 3 and 0, 3, 4, 5. |
| `CooldownSaved`, `CostumeSet`, `CostumeSetId` | 0 in every row: the names come from the order of the original's row and cannot be checked on this data. |

## `tb_Make`

No unnamed column. Checked:
- `Id` is the id of the blueprint item: all 827 are rows of `tb_Item`.
- `Product` and `Material1..4` are item ids (100 %); `Product2` and `Product3` are 0 in every row.
- `Group` (1 to 5), `Category` (8 values) and `SubCategory` (32 values) are the three levels of the menu.
- `BattlePoint` is 0 and `Priority` is 9999 in every row: the names cannot be checked on this data. The original's row
  has a cost in another currency at that place, so `BattlePoint` may be the wrong currency; nothing in this client's
  data would show it.

## Open

- `Field2` of `tb_Item`.
- What `UseEffect` selects.
- `BattlePoint` of `tb_Make`.
