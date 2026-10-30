# mTiny 15 Missions and Stage Map Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the three hand-built mission scenes with one generated mission scene that plays 15 missions in Easy, Medium and Hard stages, chosen from a locked, star-tracking stage map, ending in a one-time celebration.

**Architecture:** Mission data, progress rules and a grid simulator live in a small pure assembly (`mTiny.Missions`) that EditMode tests cover. A `MissionCatalog` asset holds all 15 missions. One `Mission` scene (copied from Mission1, tiles removed) builds the chosen mission's board at load time. The Main Menu's mission panel becomes a stage-card screen plus a 5-node trail screen.

**Tech Stack:** Unity 2022.3.62f3, C#, uGUI, TextMeshPro 3.0.7, Unity Test Framework 1.1.33 (EditMode), PlayerPrefs.

**Spec:** `docs/superpowers/specs/2026-10-30-mission-stages-design.md`

## Global Constraints

- Work directly in the main checkout `C:\Users\menes\source\repos\mTinysampleGame` on `main`. The owner asked for no extra branches. Don't touch the owner's uncommitted `Packages/` and `ProjectSettings/` changes. Commit only after the owner OKs committing.
- Unity project root: `mTinySample/`. All paths below are relative to it.
- Grid cell size is exactly `1.8` world units (the robot's `moveDistance`). Board tiles sit at `z = 90`, like the existing `platforms` objects.
- The robot always starts facing up. A left turn is counter-clockwise: up → left → down → right → up.
- Mission ids are `E1`–`E5`, `M1`–`M5`, `H1`–`H5`, in that play order.
- Any star count 1–3 finishes a mission. Best stars are kept. Medium 1 unlocks after Easy 5; Hard 1 unlocks after Medium 5.
- Progress is stored in PlayerPrefs under `mtiny.stars.<id>` and `mtiny.celebrated`.
- Teacher reset: hold the `mtinyTitle` image on the Main Menu for 3 seconds, then confirm "Reset all progress?" with Yes or No.
- Celebration panel: mTiny animation (`mtinyMainmenuAnimation`), "You did it!", total stars, Home button. Shown once, the first time H5 is won.
- Build Settings at the end: `MainMenu`, `Mission`, `World`. The old `Mission1`–`Mission3` scene files stay in the project, out of the build.
- Reuse existing art only. Path `Layer 3`, start `Layer 15`, goal `Layer 2` (from Mission1); Hospital `Layer 8`, Children crossing `Layer 13`, Police `Layer 4` (from World).
- Copy is short and readable aloud by a teacher. Use sentence case, and no "please".

## Review Focus

1. **Mission scene opened directly** (editor play, or a stale selection). Expected: it builds E1 instead of crashing. Test: Task 4, step 6.
2. **Replaying a finished mission for fewer stars.** Expected: the best score stays. Test: Task 2, `RecordWin_LowerScore_KeepsBest`.
3. **Winning H5 a second time.** Expected: normal finish panel, no celebration. Test: Task 2, `RecordWin_FinalMissionTwice_CelebratesOnce`.
4. **Tapping a locked card or node, or tapping an open node twice fast.** Expected: a locked one never loads a mission, and a double tap loads once. Test: Task 6, step 5 (manual check). `LoadingManager` already guards double loads.
5. **Releasing the title before 3 seconds, or dragging off it.** Expected: no reset prompt. Test: Task 7, step 3.

---

### Task 1: Mission model and grid simulator

**Files:**
- Create: `Assets/Script/Missions/mTiny.Missions.asmdef` (name `mTiny.Missions`, no references, auto-referenced)
- Create: `Assets/Script/Missions/MissionDefinition.cs`
- Create: `Assets/Script/Missions/MissionCatalog.cs`
- Create: `Assets/Script/Missions/MissionSimulator.cs`
- Create: `Assets/Tests/EditMode/mTiny.Missions.Tests.asmdef`. It references `UnityEngine.TestRunner`, `UnityEditor.TestRunner` and `mTiny.Missions`, with `includePlatforms: ["Editor"]`, `overrideReferences: true`, `precompiledReferences: ["nunit.framework.dll"]` and `defineConstraints: ["UNITY_INCLUDE_TESTS"]`.
- Test: `Assets/Tests/EditMode/MissionSimulatorTests.cs`

**Interfaces:**
- Produces:
  - `enum MissionStage { Easy, Medium, Hard }`
  - `[Flags] enum BlockGroups { None = 0, Movement = 1, Multiplier = 2, Emotion = 4, All = 7 }`
  - `enum CityTileKind { Hospital, ChildrenCrossing, PoliceStation }`
  - `[Serializable] class CityTilePlacement { Vector2Int cell; CityTileKind kind; }`
  - `[Serializable] class MissionDefinition`, with fields `string id`, `MissionStage stage`, `string title`, `[TextArea] string instruction`, `bool requireMovement`, `bool requireMultiplier`, `bool requireEmotion`, `BlockGroups visibleBlocks`, `Vector2Int start`, `List<Vector2Int> path`, `Vector2Int goal`, `List<CityTilePlacement> cityTiles` and `string intendedSolution`. It also has the method `bool IsWalkable(Vector2Int cell)`, which is true for start, goal, path and city-tile cells.
  - `class MissionCatalog : ScriptableObject` (menu `mTiny/Mission Catalog`) with `List<MissionDefinition> missions`, `int Count`, `MissionDefinition Get(int index)`, `int IndexOf(string id)` (-1 if absent), `List<int> IndicesInStage(MissionStage stage)` (in play order) and `int NextIndex(int index)` (-1 after the last).
  - `enum SimResult { ReachedGoal, LeftPath, EndedShort, MissingMovement, MissingMultiplier, MissingEmotion, BadToken }`
  - `static class MissionSimulator` with `SimResult Run(MissionDefinition m, string program)` and `int CountBlocks(string program)`.

Program syntax: space-separated tokens. `F`, `L` and `R` are moves. `2x`–`5x` sets the repeat count of the previous token. These emotion names are accepted: `Happy`, `Sad`, `Angry`, `Surprise`, `Dizzy`, `Sleepy` and `Playful`. `Run` stops at the first goal arrival, like `GoalZone`. A step onto a non-walkable cell returns `LeftPath`. The requirement checks run only when the goal is reached.

- [ ] **Step 1: Write the failing tests in `MissionSimulatorTests.cs`.** Build the definitions in code.
  - `Run_StraightUp_ReachesGoal`: start (2,0), path (2,1),(2,2), goal (2,3), program `F F F` → `ReachedGoal`.
  - `Run_LeftTurnIsCounterClockwise`: start (4,0), path (4,1),(3,1),(2,1), goal (1,1), program `F L F F F` → `ReachedGoal`.
  - `Run_MultiplierRepeatsPreviousBlock`: same grid as straight-up, program `F 3x` → `ReachedGoal`.
  - `Run_StepOffPath_LeftPath`: straight-up grid, `R F` → `LeftPath`.
  - `Run_ProgramTooShort_EndedShort`: straight-up grid, `F` → `EndedShort`.
  - `Run_RequiresMultiplierButNoneUsed_MissingMultiplier`: straight-up grid with `requireMultiplier = true`, `F F F` → `MissingMultiplier`.
  - `Run_RequiresEmotion_HappyCounts`: straight-up grid with `requireEmotion = true`, `Happy F 3x` → `ReachedGoal`.
  - `Run_UnknownToken_BadToken`: `F Jump` → `BadToken`.
  - `CountBlocks_CountsEveryToken`: `CountBlocks("Happy F 3x R")` returns 4.

- [ ] **Step 2: Run the EditMode tests.** Use the Test Runner window, or Unity MCP `run_tests` with mode `EditMode`. Expected: compile errors, because the types aren't defined yet.
- [ ] **Step 3: Implement the four runtime files** with the signatures above. `MissionCatalog.Get` clamps nothing; callers validate the index.
- [ ] **Step 4: Run the EditMode tests.** Expected: all `MissionSimulatorTests` pass.
- [ ] **Step 5: Commit** with message `feat(missions): add mission model, catalog type and grid simulator`.

---

### Task 2: Mission progress rules

**Files:**
- Create: `Assets/Script/Missions/MissionProgress.cs`
- Test: `Assets/Tests/EditMode/MissionProgressTests.cs`

**Interfaces:**
- Consumes: `MissionCatalog`, `MissionStage` (Task 1).
- Produces:
  - `interface IProgressStore` with `int GetInt(string key, int fallback)`, `void SetInt(string key, int value)`, `void DeleteKey(string key)` and `void Save()`.
  - `sealed class PlayerPrefsProgressStore : IProgressStore`
  - `sealed class MissionProgress`, constructed with `MissionProgress(MissionCatalog catalog, IProgressStore store)` (an ordinary constructor; Unity 2022.3 doesn't support primary constructors), with:
    - `int GetBestStars(int index)`
    - `bool IsMissionUnlocked(int index)`
    - `bool IsStageUnlocked(MissionStage stage)`
    - `int GetStageStars(MissionStage stage)`
    - `int GetTotalStars()`
    - `bool RecordWin(int index, int stars)`. It returns true when the caller should show the celebration.
    - `void ResetAll()`
  - Keys: `mtiny.stars.<id>`, `mtiny.celebrated`.

- [ ] **Step 1: Write the failing tests.** Use an in-memory `FakeStore : IProgressStore` (a dictionary) and a 15-mission catalog built with `ScriptableObject.CreateInstance<MissionCatalog>()` in play order E1–H5.
  - `FirstMission_IsUnlocked_SecondIsLocked`
  - `RecordWin_UnlocksNextMission`
  - `RecordWin_LowerScore_KeepsBest`: win E1 with 3 stars, then with 1 → `GetBestStars(0) == 3`.
  - `MediumStage_LockedUntilE5Won`, and `HardStage_LockedUntilM5Won`
  - `MediumFirstMission_UnlockedByE5`: win E1–E5 → `IsMissionUnlocked(5)` is true.
  - `GetStageStars_SumsBestStars`: E1=3, E2=2 → Easy stars = 5.
  - `RecordWin_FinalMissionTwice_CelebratesOnce`: the first `RecordWin(14, 2)` returns true; the second returns false.
  - `RecordWin_NotFinalMission_NeverCelebrates`
  - `ResetAll_ClearsStarsAndCelebration`: after a reset, E2 is locked and winning H5 celebrates again.
  - `RecordWin_ClampsStarsTo1To3`: `RecordWin(0, 7)` stores 3. `RecordWin(0, 0)` is ignored, so the mission stays unfinished.

- [ ] **Step 2: Run the EditMode tests.** Expected: they fail to compile, because `MissionProgress` isn't defined yet.
- [ ] **Step 3: Implement `MissionProgress.cs`.** A mission is unlocked if it's index 0, or the previous index has best stars > 0. A stage is unlocked if its first index is unlocked. `ResetAll` deletes every `mtiny.stars.<id>` key and `mtiny.celebrated`, then calls `Save`.
- [ ] **Step 4: Run the EditMode tests.** Expected: all `MissionProgressTests` pass.
- [ ] **Step 5: Commit** with message `feat(missions): add progress rules with PlayerPrefs store`.

---

### Task 3: The 15-mission catalog asset

**Files:**
- Create: `Assets/Editor/MissionCatalogSeeder.cs`. Menu item `mTiny/Seed Mission Catalog` writes `Assets/Data/MissionCatalog.asset`, overwriting it after an `EditorUtility.DisplayDialog` confirm when it already exists.
- Create: `Assets/Data/MissionCatalog.asset` (by running the menu item)
- Test: `Assets/Tests/EditMode/MissionCatalogTests.cs`

**Interfaces:**
- Consumes: Task 1 types.
- Produces: the asset at `Assets/Data/MissionCatalog.asset`, which Tasks 4–6 reference.

Mission data to seed. Path lists exclude the start and goal. "Req" codes: Mv = movement, Mu = multiplier, Em = emotion. "Visible" codes: Mv = movement blocks, Mu = multiplier blocks, All = every block. E1–E3 are the existing missions snapped to the grid.

| id | stage | title | req | visible | start | path | goal | city tiles | intendedSolution |
|---|---|---|---|---|---|---|---|---|---|
| E1 | Easy | Reach the flag | Mv | Mv | (1,0) | (0,0)(0,1)(0,2)(0,3)(1,3)(2,3)(3,3)(4,3) | (5,3) | — | `L F R F F F R F F F F F` |
| E2 | Easy | Count with multipliers | Mv Mu | Mv Mu | (3,1) | (2,1)(2,0)(1,0)(0,0)(0,1)(0,2)(0,3)(1,3)(2,3)(3,3)(4,3) | (5,3) | — | `L F L F R F 2x R F 3x R F 5x` |
| E3 | Easy | Show a feeling | Mv Mu Em | All | (0,3) | (1,3)(1,2)(1,1)(2,1)(3,1)(4,1)(4,2)(4,3)(5,3)(6,3)(6,2)(6,1)(6,0) | (7,0) | — | `Happy R F R F 2x L F 3x L F 2x R F 2x R F 3x L F` |
| E4 | Easy | Turn right | Mv | Mv | (1,0) | (1,1)(1,2)(2,2) | (3,2) | — | `F F R F F` |
| E5 | Easy | Happy trip | Mv Mu Em | All | (1,0) | (1,1)(1,2) | (1,3) | — | `Happy F 3x` |
| M1 | Medium | Long road | Mv Mu | Mv Mu | (0,1) | (1,1)(2,1)(3,1) | (4,1) | — | `R F 4x` |
| M2 | Medium | Turn left | Mv Mu | Mv Mu | (4,0) | (4,1)(3,1)(2,1) | (1,1) | — | `F L F 3x` |
| M3 | Medium | Up and over | Mv Mu | Mv Mu | (0,0) | (0,1)(0,2)(0,3)(1,3) | (2,3) | — | `F 3x R F 2x` |
| M4 | Medium | Corner | Mv Mu | Mv Mu | (5,0) | (5,1)(5,2)(4,2)(3,2)(2,2) | (1,2) | — | `F 2x L F 4x` |
| M5 | Medium | Climb | Mv Mu | Mv Mu | (0,0) | (0,1)(1,1)(2,1)(3,1)(3,2) | (3,3) | — | `F R F 3x L F 2x` |
| H1 | Hard | Zig-zag | Mv Em | All | (1,0) | (1,1)(2,1) | (2,2) | — | `Happy F R F L F` |
| H2 | Hard | Police help | Mv Em | All | (2,0) | (2,1)(2,2)(1,2) | (0,2) | Police (2,1) | `Surprise F 2x L F 2x` |
| H3 | Hard | Hospital visit | Mv Em | All | (0,0) | (0,1)(0,2)(1,2)(2,2) | (3,2) | Hospital (0,1) | `Happy F 2x R F 3x` |
| H4 | Hard | School crossing | Mv Em | All | (5,0) | (5,1)(4,1)(3,1)(2,1)(2,2) | (2,3) | Crossing (5,1) | `Surprise F L F 3x R F 2x` |
| H5 | Hard | Big city | Mv Em | All | (0,0) | (0,1)(0,2)(0,3)(1,3)(2,3)(3,3)(4,3) | (5,3) | Police (0,2), Hospital (3,3) | `Happy F 3x R Playful F 5x` |

Instructions: E1–E3 keep their current text. E1 is "Reach the finish line and have fun!", E2 is "Use Move and Multiplier blocks", and E3 is "Use Move, Multiplier and Emotion blocks". E4–H5 get one teacher-readable line each in the same tone, for example "Help mTiny turn right to reach the flag!".

- [ ] **Step 1: Write the failing tests.** Load the asset with `AssetDatabase.LoadAssetAtPath<MissionCatalog>("Assets/Data/MissionCatalog.asset")`.
  - `Catalog_Has15Missions_InPlayOrder`: the ids equal `E1..E5, M1..M5, H1..H5`.
  - `Catalog_FivePerStage`
  - `EveryIntendedSolution_ReachesGoal`: a `TestCaseSource` over all 15, with `MissionSimulator.Run(m, m.intendedSolution) == ReachedGoal`.
  - `NewMissions_RespectBlockLimits`: `CountBlocks` ≤ 5 for E4–E5, ≤ 7 for Medium and ≤ 8 for Hard. E1–E3 are exempt; the owner kept them as built.
  - `RequirementsMatchSpec`: Medium missions require a multiplier; Hard missions require an emotion.
  - `CityTiles_AreOnThePath`: every city-tile cell is in `path`.
  - `EveryInstruction_IsNonEmpty`

- [ ] **Step 2: Run the EditMode tests.** Expected: they fail, because the asset doesn't exist yet.
- [ ] **Step 3: Implement `MissionCatalogSeeder.cs`** with the table above, then run `mTiny/Seed Mission Catalog`.
- [ ] **Step 4: Run the EditMode tests.** Expected: all `MissionCatalogTests` pass.
- [ ] **Step 5: Commit** with message `feat(missions): seed 15-mission catalog`.

---

### Task 4: One generated mission scene

**Files:**
- Create: `Assets/Prefab/Tiles/PathTile.prefab`, `StartTile.prefab`, `GoalTile.prefab` (from Mission1's `puzzlepart`, `Start` and `Goal`)
- Create: `Assets/Prefab/Tiles/HospitalTile.prefab`, `CrossingTile.prefab`, `PoliceTile.prefab` (from World's special tiles, with sprites `Layer 8`, `Layer 13` and `Layer 4` and `SpecialTile.tileType` set)
- Create: `Assets/Script/MissionSelection.cs`
- Create: `Assets/Script/MissionBoardBuilder.cs`
- Create: `Assets/Scenes/Mission.unity` (a copy of `Mission1.unity`)
- Modify: Build Settings

**Interfaces:**
- Consumes: `MissionCatalog`, `MissionDefinition`, `CityTileKind`, `BlockGroups` (Task 1); `GameManager.startPoint`; the `MissionManager.require*` fields; `SpecialTile.tileType`.
- Produces:
  - `static class MissionSelection { static int SelectedIndex { get; set; } }`. It defaults to 0.
  - `[DefaultExecutionOrder(-100)] class MissionBoardBuilder : MonoBehaviour` with:
    - Fields: `MissionCatalog catalog`, the tile prefabs, `Transform boardParent`, `Vector2 boardCenter = (-0.5f, -1f)`, `float cellSize = 1.8f`, `GameManager gameManager`, `MissionManager missionManager`, `TMP_Text instructionText`, and `GameObject[] movementBlocks, multiplierBlocks, emotionBlocks`.
    - `MissionDefinition Current { get; }`
    - `int CurrentIndex { get; }`
    - `Vector3 CellToWorld(Vector2Int cell)`

Every prefab keeps its source's local scale (200), its `Walkable` layer and its trigger `BoxCollider2D`. `GoalTile` keeps `GoalZone`.

- [ ] **Step 1: Create the six tile prefabs.** Drag the objects out of the Mission1 and World scenes into `Assets/Prefab/Tiles/`, then check `SpecialTile.tileType` on the three city tiles.
- [ ] **Step 2: Create `Mission.unity`.** Copy `Mission1.unity`, open the copy, and delete the `platforms` children named `Start`, `puzzlepart*` and `Goal`. Keep the panels. Add an empty `Board` (position 0, scale 1) under the scene root.
- [ ] **Step 3: Implement `MissionSelection.cs` and `MissionBoardBuilder.cs`.**
  - In `Awake`:
    1. If `SelectedIndex` is outside `[0, catalog.Count)`, use 0.
    2. Instantiate tiles at `CellToWorld(cell)` with `z = 90` under `boardParent`. City cells get their city prefab instead of `PathTile`.
    3. Set `gameManager.startPoint` to the spawned start tile's transform.
    4. Copy the `require*` flags to `missionManager`, set `instructionText.text`, and turn each block group's buttons on or off by `visibleBlocks`.
  - `CellToWorld` centres the mission's bounding box (start, path and goal) on `boardCenter`.
  - `GameManager.Awake` must run after this. The `-100` execution order ensures that.
- [ ] **Step 4: Wire the scene.** Add `MissionBoardBuilder` to `GameManager`. Assign the catalog, the prefabs, `Board`, `instructionText`, and the block buttons:
  - Movement: `forward`, `left`, `right`
  - Multiplier: `2x multiplier`–`5x multiplier`
  - Emotion: `happy`, `sad`, `angry`, `surprised`, `dizzy`, `sleepy`, `playful`

  Set Build Settings to `MainMenu` (0), `Mission` (1), `World` (2).
- [ ] **Step 5: Verify each mission builds.** In play mode, for each index 0–14, set `MissionSelection.SelectedIndex` and reload the scene (MCP `execute_code`, or a temporary debug key). Expected for every mission:
  - The board is fully on screen and the robot spawns on the start tile.
  - Running `intendedSolution` reaches the goal and shows the finish panel.
  - There are no console errors.
- [ ] **Step 6: Verify the fallback.** Set `SelectedIndex = 99` and open the scene. Expected: E1 builds with no errors.
- [ ] **Step 7: Commit** with message `feat(missions): generate missions in a single Mission scene`.

---

### Task 5: Win recording, Replay, Next and the celebration

**Files:**
- Create: `Assets/Script/MissionRunner.cs`
- Modify: `Assets/Script/CommandManager.cs`. Change `ShowFinishPanel()` to `public int ShowFinishPanel()`, returning the stars awarded, or 0 if the level was already finished.
- Modify: `Assets/Script/GoalZone.cs`. After `ShowFinishPanel`, call `MissionRunner.Instance?.OnMissionWon(stars)`.
- Modify: `Assets/Scenes/Mission.unity`
  - Add a Next button: duplicate `replayButton` into `finishPanel/Canvas/Panel/buttons`.
  - Rewire `replayButton` to `MissionRunner.Replay`.
  - Add `celebrationPanel`, built from a duplicate of `finishPanel`. It contains a mTiny `Image` with an `Animator` that uses the controller holding `mtinyMainmenuAnimation`, a "You did it!" TMP banner, a total-stars TMP, and the Home button wired to `MissionManager.ReturnToHome`.

**Interfaces:**
- Consumes:
  - From Task 2: `MissionProgress`, `PlayerPrefsProgressStore`.
  - From Task 4: `MissionSelection`, `MissionBoardBuilder.CurrentIndex`, `MissionCatalog.NextIndex`.
  - The existing `LoadingManager.LoadScene(string)`.
- Produces: `class MissionRunner : MonoBehaviour` with:
  - Members: `static MissionRunner Instance`, `void OnMissionWon(int stars)`, `void Replay()` and `void Next()`.
  - Fields: `MissionBoardBuilder board`, `MissionCatalog catalog`, `GameObject nextButton`, `GameObject finishPanel`, `GameObject celebrationPanel`, `TMP_Text celebrationStarsText`.

- [ ] **Step 1: Implement the changes.**
  - `OnMissionWon(stars)`:
    1. Ignore `stars <= 0`. Otherwise call `RecordWin(CurrentIndex, stars)`.
    2. Show `nextButton` only if `NextIndex(CurrentIndex) != -1`.
    3. If `RecordWin` returned true: hide `finishPanel`, show `celebrationPanel`, and set `celebrationStarsText` to `"★ " + GetTotalStars() + "/45"`.
  - `Replay` reloads `"Mission"` through `LoadingManager`.
  - `Next` sets `MissionSelection.SelectedIndex` to the next index, then loads `"Mission"`.
  - The celebration panel starts inactive.
- [ ] **Step 2: Check compilation.** Expected: no compile errors (read the console through MCP `read_console`).
- [ ] **Step 3: Play-verify the flow with a clean save.**
  - Win E1. Expected: E1 shows 3 stars when played fast, and Next is visible.
  - Press Next. Expected: E2 opens.
  - Press Replay. Expected: the same mission reloads.
  - Set `SelectedIndex = 14` and win. Expected: the celebration shows, with the animation playing.
  - Win again. Expected: the normal finish panel shows, with no Next button.
- [ ] **Step 4: Commit** with message `feat(missions): record wins, add Next and final celebration`.

---

### Task 6: Stage map on the Main Menu (Option A)

**Files:**
- Create: `Assets/Script/StageMapUI.cs`
- Modify: `Assets/Scenes/MainMenu.unity`
  - Inside `missionPanel/mainMenuMissionPanel`, replace `buttons` (mission1–3) with a `stageCards` row of three card buttons (styled like the current mission buttons).
  - Add a `trailPanel` containing:
    - five node buttons on a winding path over `bg1`, each with a number label, a stars TMP and a padlock image
    - an mTiny marker image
    - a back-arrow button
  - Keep the `close` button.

**Interfaces:**
- Consumes: `MissionCatalog`, `MissionProgress`, `PlayerPrefsProgressStore`, `MissionSelection`, `LoadingManager.LoadScene`.
- Produces: `class StageMapUI : MonoBehaviour` with:
  - Methods: `void Refresh()`, `void OpenStage(int stage)` (0 = Easy, 1 = Medium, 2 = Hard), `void BackToStages()` and `void OnNodePressed(int slot)` (slot 0–4).
  - Serializable `StageCard { Button button; TMP_Text starsText; GameObject lockIcon; }` ×3.
  - Serializable `TrailNode { Button button; TMP_Text starsText; GameObject lockIcon; Image background; }` ×5.
  - Fields: `GameObject stagePanel, trailPanel`, `RectTransform mtinyMarker`, `Color doneColor, nextColor, lockedColor`.

- [ ] **Step 1: Implement `StageMapUI`.**
  - `Refresh` sets each card's stars text to `"★ " + GetStageStars + "/15"` and shows `lockIcon` when the stage is locked.
  - A locked card's press wiggles `lockIcon`: a ±15° z rotation, 3 shakes over 0.4 s using unscaled time. It does nothing else.
  - `OpenStage` shows `trailPanel`. Its nodes map to `IndicesInStage(stage)`:
    - Finished nodes use `doneColor` and show their best stars as "★★☆".
    - The first unlocked unfinished node uses `nextColor`, with `mtinyMarker` moved beside it.
    - Locked nodes use `lockedColor`, show `lockIcon`, and have `button.interactable = false`.
  - `OnNodePressed` ignores a locked node. Otherwise it sets `MissionSelection.SelectedIndex` and calls `LoadingManager.Instance.LoadScene("Mission")`.
  - `Refresh` runs in `OnEnable`.
- [ ] **Step 2: Build the UI and wire the buttons.**
  - The Main Menu's `Mission` button still opens `missionPanel`, through its existing `SetActive` call.
  - Card *i* → `OpenStage(i)`.
  - Node *j* → `OnNodePressed(j)`.
  - Back → `BackToStages()`.
- [ ] **Step 3: Check compilation.** Expected: no compile errors.
- [ ] **Step 4: Play-verify with a clean save.**
  - Only Easy is unlocked, and only E1 is tappable.
  - Medium shows a padlock, and pressing it wiggles the padlock.
  - Win E1 through the menu, then go Home. Expected: Easy shows "★ 3/15", E1 is green with stars, and E2 is the next mission with mTiny beside it.
- [ ] **Step 5: Verify locked and double taps.**
  - Tap a locked node. Expected: no load.
  - Double-tap an open node quickly. Expected: one load (`LoadingManager.isLoading` guard).
- [ ] **Step 6: Commit** with message `feat(menu): stage cards and mission trail map`.

---

### Task 7: Teacher reset

**Files:**
- Create: `Assets/Script/TeacherResetHold.cs`
- Modify: `Assets/Scenes/MainMenu.unity`
  - Add the component to `Canvas/bg/Panel/Image (1)`, the `mtinyTitle` image. Make sure that image has `raycastTarget` on.
  - Add a small `resetConfirmPanel` containing the TMP "Reset all progress?" and Yes and No buttons.

**Interfaces:**
- Consumes: `MissionProgress.ResetAll`, `StageMapUI.Refresh`.
- Produces: `class TeacherResetHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler`. It has the fields `float holdSeconds = 3f`, `GameObject confirmPanel`, `MissionCatalog catalog` and `StageMapUI stageMap`, and the methods `void ConfirmYes()` and `void ConfirmNo()`.

- [ ] **Step 1: Implement it.** The hold timer uses unscaled time and opens `confirmPanel` once it reaches 3 s. Pointer up or exit before then cancels it. `ConfirmYes` resets progress, refreshes `stageMap` if it's assigned, and hides the panel. `ConfirmNo` only hides the panel.
- [ ] **Step 2: Check compilation.** Expected: no compile errors.
- [ ] **Step 3: Play-verify the hold timing.**
  - Hold the title for 3 s. Expected: the prompt appears.
  - Release it at 2 s, or drag off it. Expected: no prompt.
  - Choose Yes. Expected: the stars and unlocks are cleared.
  - Choose No. Expected: nothing changes.
- [ ] **Step 4: Commit** with message `feat(menu): hold-to-reset progress for teachers`.

---

### Task 8: Full playthrough and cleanup

**Files:**
- Modify: Build Settings, which should already be `MainMenu`, `Mission`, `World` from Task 4. Confirm the old `Mission1`–`Mission3` scenes are out.

- [ ] **Step 1: Run all EditMode tests.** Expected: they all pass.
- [ ] **Step 2: Play the full journey from a clean save.** Play from the Main Menu through E1 → H5, using Next. Expected:
  - Each stage unlocks only after its previous stage's fifth mission.
  - The celebration appears after H5, and its Home button returns to the Main Menu.
  - The Explore button still opens World, and World still plays.
  - The console has no errors.
- [ ] **Step 3: Commit** with message `chore: final build settings for single mission scene`.
