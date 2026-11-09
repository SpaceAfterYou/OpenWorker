# Localization of the client

Can the text of client 1.4.2.3 (the HanPurple release, Japanese) be replaced or extended with another language, such as
Russian or English? Research of 2026-11-09, on the unpacked client data and the client in IDA. Nothing of the server
changed.

## The answer

**Yes, as a client-side package that players install; the server cannot deliver it.** Three findings make it work:

1. **Almost all text a player reads is in 23 tables of `data12.v`**, stored as UTF-16 with a length prefix, and the
   interface takes it from there at run time. The tables can be rewritten: each ends with a check sum the client verifies,
   and that sum is now understood and reproduced (all 167 checked tables round-trip byte for byte).
2. **The client accepts changed data in two ways**, both read from its code: a rebuilt `datas/data12.v` (nothing else
   checks the archives), or a loose `Localization/table/` folder that the client reads when `General.ini` says
   `File Archive=2` (a localization mode the client was built with).
3. **The interface fonts already contain the 66 Russian letters and Latin-1**, so a Russian or English translation shows
   without any font work. The Russian letters are drawn full-width, though (as wide as a kanji); a tool here puts
   proportional glyphs (and any missing letters, for example Ukrainian) into the same font file, under the same names.

Limits: one client carries one language (the table set is replaced; there are no per-language columns, and the
`Language` setting chooses fonts, voice and a code page, not text). About 1.27 million UTF-16 characters of Japanese in the
tables, plus help pages and screenshots with Japanese text that need an artist. Text fields have fixed widths in the
interface movies; translations longer than the field are cut or wrap. What could not be proven without the game window
is listed at the end; a demonstration package with install steps is ready for that.

How sure each claim is: **read** = read in the client code (IDA); **measured** = checked on the files with the tools
below; **inferred** = concluded, not seen run.

## 1. Where the text lives

### Tables (`bin/Table`, inside `datas/data12.v`)

168 tables; 167 are read with a check sum (the 168th, `Data_c.res`, is a 16-byte table version). 82 tables have string
columns, 303,229 non-empty strings, 2,675,782 UTF-16 units in all, but most of them are asset paths and keys. The cells a
player reads (strings with kana, kanji, hangul or Latin words, measured by `text_census.py`):

| Table | Columns with text | Cells | UTF-16 units | Longest / limit | What |
|---|---|---:|---:|---|---|
| `tb_Speech_String` | 1 | 11,431 | 399,573 | 112 / 511 | Lines characters and NPCs speak |
| `tb_item_script` | 12, 13 | 10,099 | 365,757 | 373 / 511 | Item names (5,124) and descriptions (4,975) |
| `tb_Skill_Script` | 1, 2, 3 | 2,295 | 109,395 | 351 / 511 | Skill names, descriptions, notes |
| `tb_Quest_Script` | 1–5 | 4,097 | 93,121 | 428 / 511 | Quest titles, texts, objectives |
| `tb_UI_String` | 2 | 5,115 | 84,450 | 430 / 511 | Every interface label and message; 626 carry markup |
| `tb_Cinema_String` | 1–5, 7, 8 | 3,311 | 80,261 | 121 / 511 | Cutscene and in-maze talk; one column per class |
| `tb_ChattingFilter` | 1 | 6,441 | 37,326 | 23 / 511 | Forbidden chat words (a word list, not a translation) |
| `tb_Buff_Script` | 1, 2, 4 | 2,176 | 24,393 | 60 / 511 | Buff names and descriptions |
| `tb_Daily_Mission` | 15, 16 | 806 | 15,314 | 19 / 511 | Daily missions (ASCII) |
| `tb_Title_String` | 1, 2, 3 | 981 | 14,574 | 67 / 511 | Titles |
| `tb_soul_metry_string` | 1 | 202 | 13,074 | 287 / 511 | Soul metry stories |
| `tb_Monster_script` | 1 | 1,465 | 10,896 | 16 / 511 | Monster names |
| `tb_Achievement_Script` | 1, 2 | 846 | 10,234 | 33 / 511 | Achievements |
| `tb_Booster_Script` | 1, 2 | 388 | 5,141 | 27 / 511 | Boosters |
| `tb_Tooltip_String` | 1, 2 | 182 | 3,183 | 70 / 511 | Tooltips |
| `tb_NPC_Script` | 1 | 350 | 2,011 | 16 / 511 | NPC names |
| `tb_NameFilter` | 1 | 526 | 1,931 | 13 / 511 | Forbidden names (a word list) |
| `tb_SystemMail` | 3, 4, 5 | 63 | 1,665 | 89 / 511 | System mail sender, title, body |
| `tb_Shop_String` | 1 | 221 | 753 | 13 / 511 | Shop labels |
| `tb_FX_BattleMaterial_Link`, `tb_ChattingCommand`, `Tb_Broach_Set`, `tb_Talk_String` | | 145 | 647 | | Korean leftovers (chat command words, test labels) |
| **All** | | **51,140** | **1,273,699** | | |

`tb_Loading_String` and `tb_Option_String` hold only numbers (ids into `tb_UI_String`), so they need no translation.
Some cells are developer leftovers in Korean or test text (for example column 4 of `tb_Cinema_String`, a class the
release never had). Scripts by count of characters in the text cells: kana 690k, kanji 276k, ASCII 137k, full-width
41k, hangul 1.4k (measured).

### Interface movies (`GUI/*.gfx`, Scaleform)

273 movies, 3,101 text fields, no static text (every text is a field). 2,961 fields have an initial text, 1,320 of them
with CJK characters, nearly all **Korean placeholder text** ("일이삼사오육칠팔구십", "퀘스트이름퀘스트이름") that the client
replaces at run time with table strings (measured with `gfx_survey.py`). Since the Japanese release shows no Korean,
these placeholders are overwritten (inferred); which, if any, stay visible only a run shows. 106 movies import their
fonts from `gfxfontlib.swf` by the names `$normal` (319 fields), `$bold` (2,674) and `$header` (27); 33 small fonts are
embedded (Latin digits and a few letters in sample widgets).

### Text in pictures

- `GUI/LocalizeImage`: 97 pictures with Japanese text: 83 help screenshots (`HelpBook`), 11 chat channel tags
  (`Chat`, "一般" and so on), 3 tutorial pages. These need an artist or new screenshots taken in the translated client.
- `GUI/HelpPopUp/Image` (38) and `GUI/Description_Image` (43): gameplay screenshots, some with Korean interface text.
- `GUI/Html`: 68 help pages, UTF-8 HTML, 21,643 visible characters of Japanese: plain text to translate.
- Interface atlases (`GUI/*.dds`, 230, plus 141 in `GUI/data`, 163 in `GUI/Talk_Img`): the ones sampled (login,
  inventory, quick menu, character and server selection) carry only English labels ("LOGIN", "SHOP", "NEW") and icons;
  not every atlas was looked at.
- Loading pictures (`GUI/IllustImage/LoadingBG`, 42): place names in English ("ACHERON"). `Info_Image` (7) uses an
  invented script. `Damage_Font` is a digit font.

### Movies, cutscenes, voice

- `Movie/`: 36 videos (MPEG-1 1080p logo, login background and four prologue films; VP8 class and tutorial clips). The
  frames sampled show no subtitles; the tutorial clips show the Korean interface of the time (measured on frames).
- Cutscenes (`CutScene/`) are scenes of the engine; their talk comes from `tb_Cinema_String` and the prologue texts from
  tables through `prologuestring.gfx` and `cinamatalk.gfx` (inferred from the movie and table names).
- Voice (`SoundSystem*.vArc`, FMOD) is out of scope. The `Language` setting selects a voice parameter (see 5).

### The executable and the scripts

- The executable has no Japanese text: its few message boxes are English (a failed table, a failed engine start, the
  authentication module) (read).
- `Scripts/` (1,540 Lua, 157 XML): Korean comments only; one debug literal; no text a player reads (measured).
- `data00.v` is not a launcher movie: it is the Japanese **font library** (a compressed SWF with two fonts, see 3).

### Text the server sends as text

The wire carries text as UTF-16 (`Encoding.Unicode`) with a length prefix, so any language passes. Messages with strings
a player reads (`OpenWorker.Protocol`):

- written by players and passed on: chat (`ChatNormal`, `ChatWhisper`, `ChatTrade`, `ChatMegaphone`), mail
  (`PostValue`, `PostAccountValue`), league notices and names (`LeagueInfo`, `LeagueBoardEntry`, position names), party
  recruit messages, profiles (`PersonCommunity`: comment, memo), private shop titles, person and league names;
- written by the server: **`ChatNoticeResponse`** (the `/notice` command; the maze script failure notice, a fixed English
  sentence in `OpenWorker.Lua/LuaMaze.cs`; `ChatMessage` of maze scripts), **gate names** (`GateInfo.Name`, from the
  `Gates` table of the database), and test stubs (fake party recruits in `PartyService`).

Kick-outs and errors are codes; the client shows its own table text for them. Default league position names are empty.
So a server operator translates only the gate names, the notices they send, and the one fixed English notice.

## 2. Encoding and limits

- **Table strings** (read): `u16 length` in UTF-16 units, then `2 × length` bytes, no terminator. The client copies
  them into a 512-unit buffer: a column accepts 1 to 511 units (268 columns) or 255 (57 columns); a longer string makes
  the client skip reading it and the rest of the table goes wrong, so the packer refuses it. A length of 0 is an empty
  string. 8-bit strings exist only in the trailer.
- **The text path is UTF-16**: tables are UTF-16, strings go to the interface as UTF-16, the wire is UTF-16. A code page
  appears in three places (read): file names, key names for key bindings, and a code page the `Language` setting chooses
  for the engine's own conversions (Japanese 932, Chinese 936, otherwise 949). None of them touches table text.
- **Line breaks** are a real U+000A inside the string. **Markup**: 626 `tb_UI_String` strings and some quest texts carry
  `{0}`-style placeholders, `<font>` tags or similar; a translation must keep them.
- **Field widths**: every interface text field has a fixed rectangle in its movie; some auto-size. A longer translation
  wraps or is cut (inferred; only a run shows each field). Japanese packs about two to three Latin or Cyrillic letters
  per character; at ×2.5, 156 table cells would exceed their 511 limit (74 in skills, 39 quests, 26 items, 9 soul metry,
  8 interface) and need a shorter wording (measured).
- **Player input** (chat, names): the font text input is Unicode (keyboard layouts are read as Unicode), but the game
  window is an ANSI window, so ordinary typed characters may arrive in the system code page (read; whether Cyrillic typed
  on a Russian layout arrives right is an open question). The server accepts any Unicode letter or digit in a name
  (`LoginRules.IsValidPersonName`).
- **Chat and name filters** (`tb_ChattingFilter`, 6,441 words; `tb_NameFilter`, 526) are Japanese word lists; a Russian
  server wants its own lists in the same tables.

## 3. Fonts

- The interface draws with Scaleform fonts. `GameEtc/fc.txt` (a loose UTF-8 file, read at start) has one section per
  `Language` value; each names a font library and maps `$normal`, `$bold`, `$header` to font names. For **Japanese** the
  library is `../datas/data00.v` (relative to `GameEtc`), a plain zlib-compressed SWF (not an archive) with two fonts:
  `NHN_SW_FTTP-RodinCattleya B` (for `$normal` and `$bold`) and `NHN_SW_FTTP-KafuTechno H` (for `$header`), 7,812
  glyphs each (read and measured).
- Coverage of those fonts (measured with `swf_scan.py`): ASCII, Latin-1 (96), 10 letters of Latin Extended
  (`ıŁłŒœŠšŸŽž`), Greek (48), **Cyrillic 66 = the full Russian alphabet with Ё/ё**, kana, 6,720 kanji. Missing: the
  other Cyrillic letters (Ukrainian `ҐЄІЇ`, Belarusian `Ў`, Serbian), most of Latin Extended-A (Polish, Czech, Turkish).
  **The Cyrillic glyphs are full-width** (advance of 1 em, like kanji; Latin letters are proportional): Russian set in
  them is about twice as wide as normal and looks spaced out.
- Other libraries in the data: `GUI/Font/fonts_en.swf` and `fonts_kr.swf` (Malgun Gothic and its bold, 12,611 glyphs:
  proportional Cyrillic, Latin, hangul, kana, no kanji), `fonts_ch.swf` (one Chinese font, 50,550 glyphs), `fonts_jp.swf`
  (the same two fonts as `data00.v`), `gfxfontlib.gfx`/`.swf` (Malgun Gothic, NanumGothic, KafuTechno). The `English`,
  `Korean` and `Chinese` sections of `fc.txt` name `fonts_en.swf` and so on with no folder, which resolves to `GameEtc/`
  where they are not; the client also installs the system font provider, so those sections fall back to Windows fonts
  of the mapped names (inferred).
- **How to change the font** (three ways, best first):
  1. Put proportional glyphs into the fonts of `data00.v` under the same font names (`font_merge.py`): no other file
     changes. Done for the demonstration with Noto Sans Bold and Black (SIL Open Font License): 77 glyphs replaced
     (Cyrillic, the 10 Latin Extended letters, the space), 150 added (the rest of `U+0400–045F`, `Ґґ`, Latin
     Extended-A). The glyph codec of the tool re-encodes the font's own glyphs bit for bit (3,000 of 3,000 checked per
     font) and the library round-trips byte for byte when nothing is changed; previews drawn from the merged glyph data
     are in the package.
  2. Write a new font library and point `fc.txt` at it (`fontlib "../datas/<file>"`, `map "$normal" = "<name>"`).
  3. Map the names in `fc.txt` to a system font; the provider draws it if the player has it installed. Glyphs the font
     lacks (kanji of untranslated text) would be missing.

## 4. Whether changed files are accepted

Read in the client, its engine and its archive reader:

- **Archives.** At start the client lists its own folder recursively and mounts **every `*.v` and `*.vArc` file** it
  finds as a data directory, in the order the folder listing returns them; then `Shared.zip`. A `.v` is a zip whose
  bytes are xored with 0x55 on read. Inside, a path is looked up lowercased with `/` turned into `\`, so case and slash
  direction do not matter; directory entries are skipped; deflated and stored entries are read. There is no list of
  files, sizes or hashes, and no check of an archive as a whole. When two archives hold the same path, **the one mounted
  first wins**. There is no fixed limit on the number of archives.
- **Tables** are opened as `../bin/table/<name>.res` through those archives (they live in `data12.v`). There is **no
  fallback to loose files**: with archives mounted, a file on disk is never read for an engine path.
- **The table check sum.** While a table is read, every value is added into a 64-bit signed sum: bytes and unsigned
  16-bit values as they are, signed 8/16/32-bit values sign-extended, floats truncated to a 32-bit integer, and for each
  UTF-16 string its length plus every byte (unsigned). The row count is not part of it. After the rows comes the
  **34-byte trailer**: `u16 32` and 32 hex digits = **md5 of the sum written in decimal**. On a mismatch the client shows
  "Failed loading table file.(<table>)" and stops starting. This is exactly the 34-byte trailer the server notes mention
  on the reference tables. Verified on every table: `res_pack.py roundtrip` re-computes all 167 trailers and rebuilds all
  167 tables byte for byte (measured).
- **Version checks.** `Ver.ini` (`ver=1.4.2.3`) and `revision.ini` are read into a version string and shown in a debug
  overlay; `Data_c.res` (the table version) likewise; the login sends neither (the login request carries name, password
  and MAC address). Nothing compares the data with the server.
- **Anti-cheat.** The client starts the XIGNCODE module and fails without it; it guards the process, and its file checks
  would need the server to ask for them (this server answers the module's packets but asks nothing). Not studied further.

So, for the three ways in the question:

| Way | What the client does | Verdict |
|---|---|---|
| Loose file next to the archives (`datas/bin/Table/tb_UI_String.res`, or the path the entry names) | Never opened: archives are the only data directories, and tables are read through them | **Does not work** |
| A new archive (`data21.v`) holding the changed tables | Mounted automatically, but mounted after `data12.v` on Windows (alphabetical listing), so the old tables win; a name that sorts before `data12.v` (for example `data11_loc.v`) wins on Windows; under Wine the listing order is not alphabetical for sure | **Works only with a well chosen name, and not reliably under Wine** |
| `data12.v` rebuilt in place | The rebuilt archive is read like the original; only the per-table check sums are checked, and they are recomputed | **Works** (the recommended route) |
| `File Archive=2` in `General.ini` with `Localization/table/*.res` | The client then reads tables as `table/<name>.res`, which no archive holds, from the folder `Localization` it mounts in that mode; help pictures come from `Localization/help/` | **Works for tables** (all 168 must be there); it cannot override other files, because the folder is mounted after the archives |

`File Archive=0` is a development mode (tables read with plain file access from `<game>/Table/`, all other data from
`../Assets`); not useful for players.

## 5. Language selection

- `General.ini`, `[Locale Info] Language=` (default `Korean`; here `Japanese`) chooses: the `fc.txt` font section, a
  voice parameter of the sound engine (Korean 1, Japanese 2, Chinese 3, any other value 4), and the engine code page
  (read). It does not choose text: there is one set of tables, with one text column per field and no per-language columns.
- No command line argument chooses a language (the client takes `IP:`, `PORT:`, `SkipPurpleModule:`,
  `MultiExecuteClient:`, `GfxFlashLoad:`, `InGameUILoad:`, `SkipSegaAuthen:`).
- So one installed client carries one language. Switching languages means switching table sets: with route B a player
  can keep the Japanese tables in `data12.v` and a translation in `Localization/`, and switch with `File Archive=1`/`2`
  (inferred from 4). Keep `Language=Japanese`: another value changes the voice parameter and the font section.

## 6. What the server can do

- Nothing delivers files to the client: there is no patch or update step in the login flow (`patchw32.dll` belongs to
  the original launcher, which this setup skips). `OpenWorker.UpdateContent` is the database migration runner (it
  applies the EF migrations), not a content updater. A localization is a package players install.
- The server reads the client tables only for ids and numbers, so it needs no translated tables.
- Server-side text to translate: gate names (database), notices an operator sends, the maze script failure notice in
  `LuaMaze.cs` (English), any `ChatMessage` text in maze scripts, and mails sent by commands.

## Tools

In `docs/tools/` (git-ignored, main checkout):

- `res_pack.py`: reads and writes any client table with the layout of its loader, recomputes the trailer.
  `roundtrip <Table dir> <layouts.json>`, `strings <Table dir> <layouts.json> <out.jsonl>` (every string cell, for
  translators), `apply <Table dir> <layouts.json> <edits.json> <out dir>` (`{"tb_UI_String": {"5200": {"2": "text"}}}`,
  key = first column or `#row`). Refuses strings over a column's limit. The layouts are in
  `docs/agents/research-localization/table_layouts.json` (167 tables, from the client's loaders).
- `pack_datas.py`: `pack <folder> <out.v> --prefix ../bin/Table/ --like <original.v>` builds an archive with the shape of
  the shipped ones (deflate, stored when that is not smaller, NTFS extra field, attributes, entry order and times copied
  from the original); `verify a.v b.v` compares two archives file by file as the client looks them up. `data12.v`
  rebuilt from the unpacked tables: 168 of 168 files identical, all zip metadata identical except the compressed bytes;
  `unpack_datas.py` unpacks it to the same 168 files.
- `font_merge.py`: puts glyphs of a TrueType font into the DefineFont3 fonts of a font library, by code point ranges,
  scaled to the library's letter height, oriented like its glyphs, with the code table kept sorted; `--preview` draws a
  text with the result.
- In `docs/agents/research-localization/tools/`: `swf_scan.py` (fonts, coverage, text fields of a movie),
  `gfx_survey.py`, `v_list.py` (archive entries and zip parameters), `text_census.py`, `movie_frames.py`.

## The demonstration package

`docs/agents/research-localization/demo/` (`build_demo.py`, `edits_ru.json`; output in `out/`, steps in
`out/INSTALL.txt`): Russian with a `[RU]` mark on 33 interface strings (login window, server and character selection,
main menu), the NPC name Miriam, every speech line (prefix `[RU] `) plus one translated line, the money name and one
potion (`/item 610901000`), and the font. Install, first route A:

1. Close the game. Back up `datas/data12.v` **outside the game folder** (any `*.v` under the game folder is mounted).
2. Copy `out/route-A/datas/data12.v` over `datas/data12.v`.
3. Optionally back up `datas/data00.v` the same way and copy `out/font/data00.v` over it.
4. Start the game and look at the login window, the server and character lists, the menu, Miriam, NPC lines, the money.
5. Uninstall: copy the backups back.

Route B instead: copy `out/route-B/Localization` into the game folder and set `File Archive=2` in `General.ini`;
uninstall with `File Archive=1`.

## Work breakdown for a full translation

| Part | Size | Who |
|---|---|---|
| Interface (`tb_UI_String`, `tb_Tooltip_String`, `tb_Shop_String`, `tb_Title_String`, `tb_Option_String` ids) | 6,500 cells, 103k chars | Translator; check every screen for width; keep markup |
| Items (`tb_item_script`) | 10,099 cells, 366k chars | Translator; names short (inventory), descriptions within 511 |
| Skills and buffs (`tb_Skill_Script`, `tb_Buff_Script`, `tb_Booster_Script`) | 4,859 cells, 139k chars | Translator with game knowledge; 74 skill texts too long at ×2.5 |
| Story (`tb_Speech_String`, `tb_Cinema_String`, `tb_Quest_Script`, `tb_soul_metry_string`, `tb_SystemMail`) | 19,104 cells, 588k chars | Literary translator; per-class variants in `tb_Cinema_String` |
| Names (`tb_NPC_Script`, `tb_Monster_script`, `tb_Achievement_Script`, `tb_Daily_Mission`) | 3,467 cells, 38k chars | Translator, with a glossary |
| Filters (`tb_ChattingFilter`, `tb_NameFilter`) | 6,967 words | Replace with lists for the target language |
| Help pages (`GUI/Html`) | 68 files, 21.6k chars | Translator (UTF-8 HTML) |
| Pictures with text (`LocalizeImage`: 83 help screenshots, 11 chat tags, 3 tutorial pages; `HelpPopUp`, `Description_Image` screenshots) | 97 + 81 | Artist, or new screenshots from the translated client |
| Fonts | `data00.v` | Done by tool for Cyrillic and Latin Extended-A |
| Field widths, cut text | unknown | Play-testing; editing `.gfx` field rectangles needs a Scaleform-aware SWF editor |
| Server | gate names, notices | Operator |

## Open questions only a run of the real client answers

- Does the client start with the rebuilt `data12.v` and show the demonstration strings (route A), and with
  `File Archive=2` (route B)? The check sum and the archive format are proven on the files; the run is the last step.
- Does Scaleform draw the merged glyphs of `data00.v` correctly (orientation, size, align zones copied from `H`), and do
  Japanese texts still look the same?
- Which Korean placeholder texts of the movies stay visible, and which fields cut or wrap a longer translation?
- Does Cyrillic typed on a Russian keyboard layout reach the chat and the name fields intact, under Windows and under
  Wine?
- In which order does Wine list the game folder (it decides whether an extra archive can override `data12.v`)?
- Does an `fc.txt` font library path other than `../datas/data00.v` resolve as expected?
