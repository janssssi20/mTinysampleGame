# mTiny: 15 Missions in 3 Stages — Design

Date: 2026-10-30
Project: `mTinySample` (Unity 2022.3.62f3, 2D, uGUI + TextMeshPro)

## Intent

mTiny is a block-coding game for kindergarten to Grade 1 children. Today it has three
missions behind a three-button panel. The owner wants a full journey: 15 missions split
into Easy, Medium and Hard stages, chosen from a map where each mission unlocks only
after the one before it is finished, ending in a celebration.

The children are 5–7 years old. Many can't read yet, a teacher is often nearby, and
fingers are imprecise. Every decision below leans toward fewer choices on screen,
bigger targets, short programs and one new idea at a time.

## Goals

- 15 missions total: the 3 that exist plus 12 new ones.
- Three stages of 5 missions: Easy, Medium, Hard. Each stage introduces one new idea.
- A stage-select screen and a per-stage trail map (layout "Option A", chosen by the owner).
- Sequential locking: a mission unlocks when the previous one is finished; a stage unlocks
  when the last mission of the previous stage is finished.
- Progress and best stars persist on the device between sessions.
- After the final Hard mission, a celebration panel with an mTiny animation and a Home button.

## Non-goals

- No accounts, cloud save, or multiple player profiles.
- No new block types, no loops beyond the existing multiplier, no conditionals.
- No changes to the Explore (World) mode.
- No new art pipeline. Reuse existing sprites, buttons, panels and animations.

## Anti-goals (would make it a failure even if it works)

- A Medium or Hard mission a K–G1 child can't finish with an adult's light help.
- Programs needing more than 8 blocks (Start flag and Run don't count).
- Screens crowded with small buttons, or text a child must read to make progress.
- A child losing progress, or a mission that unlocks without the previous one being won.

## Difficulty progression

The owner chose to keep the existing Missions 1–3 as Easy 1–3. Those three already teach
moving, multipliers and emotions one at a time, so the stages are shaped like this:

| Stage | What it's for | Required blocks | Max blocks | Turns |
|---|---|---|---|---|
| Easy | Meet each block once: moving, multipliers, emotions, then a first turn | Varies by mission | 5 | 0–1 |
| Medium | Turns combined with multipliers on longer paths | Movement + multiplier | 7 | 1–2 |
| Hard | Emotions and city tiles (hospital, crossing, police) on two-turn paths | Movement + emotion (+ multiplier where it fits) | 8 | 1–2 |

The robot always starts facing up, so children only have to reason about one starting
direction.

"Max blocks" is a rule for designing missions, not a limit the game enforces. A child who
takes a longer route can still finish (designer's call: a cap would need non-reading
feedback and could block a correct but long program).

## Where Missions 1–3 go (owner's decision)

| Existing mission | New slot | Teaches |
|---|---|---|
| Mission1 (movement) | Easy 1 | Moving and turning |
| Mission2 (movement + multiplier) | Easy 2 | Multipliers |
| Mission3 (movement + multiplier + emotion) | Easy 3 | Emotions |

Their layouts, requirements and instructions are kept as they are today.

## Mission list

Grid notation: columns 0–5 left to right, rows 0–3 bottom to top. Cells are 1.8 world units
apart, matching the robot's `moveDistance`. Every cell not listed is empty, so stepping off
the path plays the existing "Bumped" reaction. Solutions are the intended shortest programs
(F = Forward, L/R = turn, 2×–5× = multiplier on the previous block).

### Easy

| # | Name | Start | Path → Goal | Required | Visible blocks | Intended solution | Blocks |
|---|---|---|---|---|---|---|---|
| E1 | (existing Mission1) | as built | as built | movement | movement | as built | — |
| E2 | (existing Mission2) | as built | as built | movement + multiplier | movement, multipliers | as built | — |
| E3 | (existing Mission3) | as built | as built | movement + multiplier + emotion | all | as built | — |
| E4 | Turn right | (1,0) | (1,1) (1,2) (2,2) → (3,2) | movement | movement | F F R F F | 5 |
| E5 | Happy trip | (1,0) | (1,1) (1,2) → (1,3) | movement + multiplier + emotion | all | Happy F 3× | 3 |

E4 practises a single turn without anything else to think about. E5 is a short recap that
uses all three block kinds in one tiny program before Medium starts.

### Medium (multiplier required; movement and multiplier blocks visible)

| # | Name | Start | Path → Goal | Intended solution | Blocks |
|---|---|---|---|---|---|
| M1 | Long road | (0,1) | (1,1) (2,1) (3,1) → (4,1) | R F 4× | 3 |
| M2 | Turn left | (4,0) | (4,1) (3,1) (2,1) → (1,1) | F L F 3× | 4 |
| M3 | Up and over | (0,0) | (0,1) (0,2) (0,3) (1,3) → (2,3) | F 3× R F 2× | 5 |
| M4 | Corner | (5,0) | (5,1) (5,2) (4,2) (3,2) (2,2) → (1,2) | F 2× L F 4× | 5 |
| M5 | Climb | (0,0) | (0,1) (1,1) (2,1) (3,1) (3,2) → (3,3) | F R F 3× L F 2× | 7 |

### Hard (emotion required, all blocks shown)

The goal stops the program as soon as the robot arrives, so emotion blocks are placed
before the last move. City tiles react automatically when the robot steps on them; they
are scenery and story, not requirements.

| # | Name | Start | Path → Goal | City tiles | Intended solution | Blocks |
|---|---|---|---|---|---|---|
| H1 | Zig-zag | (1,0) | (1,1) (2,1) → (2,2) | none | Happy F R F L F | 6 |
| H2 | Police help | (2,0) | (2,1) (2,2) (1,2) → (0,2) | Police at (2,1) | Surprise F 2× L F 2× | 6 |
| H3 | Hospital visit | (0,0) | (0,1) (0,2) (1,2) (2,2) → (3,2) | Hospital at (0,1) | Happy F 2× R F 3× | 6 |
| H4 | School crossing | (5,0) | (5,1) (4,1) (3,1) (2,1) (2,2) → (2,3) | Children crossing at (5,1) | Surprise F L F 3× R F 2× | 8 |
| H5 | Big city | (0,0) | (0,1) (0,2) (0,3) (1,3) (2,3) (3,3) (4,3) → (5,3) | Police at (0,2), Hospital at (3,3) | Happy F 3× R Playful F 5× | 7 |

Each new mission has a one-line instruction a teacher can read aloud, for example
"Help mTiny go up to the flag!" The builder writes these in the same tone as the existing
instruction panel.

## Stage select and trail map (Option A)

**Entry.** The Main Menu's Mission button opens the new stage screen, which replaces the
current `missionPanel` with its three mission buttons. The Explore button is unchanged.

**Stage screen.** Three large cards (Easy, Medium, Hard), each showing its star total
(for example ★ 9/15) and a padlock when locked. Tapping a locked card wiggles the padlock
and does nothing else. There's a close (X) button like the current panel's.

**Trail screen.** Five mission nodes on a winding dotted path over the existing `bg1`
background. Node states:
- Finished: green, best stars shown above.
- Next to play: highlighted, with the mTiny sprite beside it.
- Locked: padlock, not tappable.

A back arrow returns to the stage cards. Tapping an open node loads that mission through
the existing `LoadingManager`, opening the single mission scene with that mission selected.

**Finish panel changes.** Add a Next button beside Replay and Home, which loads the
following mission in the 15-mission order, crossing stage boundaries (Easy 5 → Medium 1,
Medium 5 → Hard 1). It is hidden after Hard 5. After Hard 5 is won, the celebration
panel opens instead of the normal finish panel.

**Celebration panel.** Full-screen panel with the mTiny excited animation (the existing
`mtinyMainmenuAnimation` clip, or `HappyAnimation` if that reads better at full size),
a short "You did it!" banner, the total stars earned, and a Home button. It shows once,
the first time Hard 5 is won. Replaying Hard 5 later shows the normal finish panel.

## Progress rules

- A mission is finished when the robot reaches the goal and the mission's block
  requirements are met: the same moment stars are awarded today.
- Any star count (1–3) unlocks the next mission. Best stars are kept on replay.
- Medium 1 unlocks when Easy 5 is finished; Hard 1 unlocks when Medium 5 is finished.
- Progress is saved on the device (`PlayerPrefs`), keyed by mission id, and survives app restarts.
- Teacher reset: pressing and holding the mTiny title image (`mtinyTitle` sprite) on the Main Menu for 3 seconds
  asks "Reset all progress?" with Yes and No. Children are unlikely to find it.

## One mission scene (owner's decision)

All 15 missions run in **one scene**, not one scene per mission. The owner decided this.

- The mission scene starts from a copy of Mission1 (camera, Canvas, block buttons,
  instruction, emotion and finish panels, `GameManager`, `MissionManager`), with the
  hand-placed path tiles removed.
- When the scene loads, it reads which mission was chosen on the map and **builds that
  mission's board**: path tiles, start tile, goal, and city tiles, placed on the grid from the
  mission's layout data. It then places the robot on the start tile facing up, and sets the
  mission's requirement flags, instruction text and visible blocks.
- Missions 1–3 are rebuilt as layout data, copied from their current scenes and snapped
  to the 1.8-unit grid. They play the same, just generated.
- Replay and Next reload the same scene with the current or next mission selected, which
  resets everything cleanly.
- The old `Mission1`, `Mission2` and `Mission3` scene files are removed from Build Settings
  but left in the project for reference; the owner can delete them later. World (Explore)
  stays its own scene.

## Builder's calls (how it's made)

These are my calls as the designer of this document. The builder can change them if
there's a good reason:

- **Mission data in one place**: all 15 missions (stage, order, title, instruction text,
  required blocks, visible blocks, start cell, path cells, goal cell, city tiles) live in one
  editable asset in the Unity Inspector, so adding or reordering missions never needs code.
- **Tiles from prefabs**: path, start, goal and each city tile type become small prefabs made
  from the existing sprites, so the board builder only instantiates them.
- **Hiding blocks per mission** (the "Visible blocks" column; Medium shows movement and
  multipliers; Hard shows all) so young children don't see buttons they can't use yet.
- **Reused art**: path tile `Layer 3`, start `Layer 15` and goal `Layer 2` from Mission1;
  city tiles from the World scene (Hospital, Children crossing and Police sprites).
- Build Settings end up as: MainMenu, the mission scene, World.

## Left to the builder

- Exact screen positions, node art and animations for the stage and trail screens, using
  existing sprites and button styles.
- Instruction wording for the 12 new missions.
- Code structure, file names, and the name of the mission scene.
- Whether the robot sprite is swapped or reused per stage (default: reuse).

## Known issue spotted while designing

One city tile in the World scene has an invalid type value (`7`, which isn't in the
`TileType` list). It doesn't affect this work, but should be fixed separately.
