using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Writes the 50 missions (5 stages of 10) into Assets/Data/MissionCatalog.asset.
// After seeding, edit missions in the asset's Inspector instead.
public static class MissionCatalogSeeder
{
    private const string CatalogPath = "Assets/Data/MissionCatalog.asset";

    private const BlockGroups Move = BlockGroups.Movement;
    private const BlockGroups MoveMulti = BlockGroups.Movement | BlockGroups.Multiplier;
    private const BlockGroups MoveMultiFeel = BlockGroups.Movement | BlockGroups.Multiplier | BlockGroups.Emotion;
    private const BlockGroups All = BlockGroups.All;

    [MenuItem("mTiny/Seed Mission Catalog")]
    public static void Seed()
    {
        MissionCatalog existing = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);

        if (existing != null && !Application.isBatchMode &&
            !EditorUtility.DisplayDialog("Seed Mission Catalog",
                "This replaces every mission in the catalog with the 50 built-in missions. Continue?",
                "Replace", "Cancel"))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }

        MissionCatalog catalog = existing;

        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<MissionCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.missions = BuildMissions();

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();

        Debug.Log($"Mission catalog seeded with {catalog.Count} missions.");
    }

    private static List<MissionDefinition> BuildMissions()
    {
        return new List<MissionDefinition>
        {
            // Five stages of ten. Board tiles (start + path + goal):
            // Stage 1: 6-8, Stage 2: 8-10, Stage 3: 10-12, Stage 4: 12-15, Stage 5: 15-20.
            // Boards stay within 7 x 4 tiles so they fit the Top view.
            // The robot starts facing up; L turns counter-clockwise.

            // ---------------- STAGE 1 ----------------
            Mission("S1-01", MissionStage.Stage1, "First steps",
                "Turn right, then walk to the finish!",
                false, false, false, Move,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0), C(5, 0),
                "R F F F F F"),

            Mission("S1-02", MissionStage.Stage1, "Turn right",
                "Go up, turn right, and reach the finish!",
                false, false, false, Move,
                C(0, 0), Cells(0, 1, 0, 2, 1, 2, 2, 2), C(3, 2),
                "F F R F F F"),

            Mission("S1-03", MissionStage.Stage1, "Turn left",
                "Go up, turn left, and reach the finish!",
                false, false, false, Move,
                C(2, 0), Cells(2, 1, 2, 2, 2, 3, 1, 3), C(0, 3),
                "F F F L F F"),

            Mission("S1-04", MissionStage.Stage1, "Long walk",
                "A long walk. Count every step!",
                false, false, false, Move,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0), C(6, 0),
                "R F F F F F F"),

            Mission("S1-05", MissionStage.Stage1, "Right first",
                "Turn right first, then go up!",
                false, false, false, Move,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 3, 1, 3, 2), C(3, 3),
                "R F F F L F F F"),

            Mission("S1-06", MissionStage.Stage1, "Zig",
                "Right, up, then right again!",
                false, false, false, Move,
                C(0, 0), Cells(1, 0, 2, 0, 2, 1, 2, 2, 3, 2), C(4, 2),
                "R F F L F F R F F"),

            Mission("S1-07", MissionStage.Stage1, "Zag",
                "Left, up, then left again!",
                false, false, false, Move,
                C(4, 0), Cells(3, 0, 2, 0, 2, 1, 2, 2, 1, 2), C(0, 2),
                "L F F R F F L F F"),

            Mission("S1-08", MissionStage.Stage1, "Corner hop",
                "Right, up, then left to the finish!",
                false, false, false, Move,
                C(0, 0), Cells(1, 0, 2, 0, 2, 1, 2, 2, 2, 3, 1, 3), C(0, 3),
                "R F F L F F F L F F"),

            Mission("S1-09", MissionStage.Stage1, "Stairs",
                "Step, turn, step, turn. Climb the stairs!",
                false, false, false, Move,
                C(0, 0), Cells(0, 1, 1, 1, 1, 2, 2, 2), C(2, 3),
                "F R F L F R F L F"),

            Mission("S1-10", MissionStage.Stage1, "Big turn",
                "Go up, over, and back down!",
                false, false, false, Move,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 2, 2), C(2, 1),
                "F F F R F F R F F"),

            // ---------------- STAGE 2 ----------------
            Mission("S2-01", MissionStage.Stage2, "Count to three",
                "Use a x3 block to go up, then a x4 to go right!",
                true, false, false, MoveMulti,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3), C(4, 3),
                "F 3x R F 4x"),

            Mission("S2-02", MissionStage.Stage2, "Zoom!",
                "Go up two, turn right, and zoom five!",
                true, false, false, MoveMulti,
                C(0, 0), Cells(0, 1, 0, 2, 1, 2, 2, 2, 3, 2, 4, 2), C(5, 2),
                "F 2x R F 5x"),

            Mission("S2-03", MissionStage.Stage2, "Left and zoom",
                "Climb up, turn left, then zoom to the finish!",
                true, false, false, MoveMulti,
                C(5, 0), Cells(5, 1, 5, 2, 5, 3, 4, 3, 3, 3, 2, 3, 1, 3), C(0, 3),
                "F 3x L F 5x"),

            Mission("S2-04", MissionStage.Stage2, "Up and over",
                "Up, over, and back down to the finish!",
                true, false, false, MoveMulti,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 3, 2), C(3, 1),
                "F 3x R F 3x R F 2x"),

            Mission("S2-05", MissionStage.Stage2, "Big step",
                "Zoom right, then climb up!",
                true, false, false, MoveMulti,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 5, 1, 5, 2), C(5, 3),
                "R F 5x L F 3x"),

            Mission("S2-06", MissionStage.Stage2, "Two turns",
                "Right, up, then right again!",
                true, false, false, MoveMulti,
                C(0, 0), Cells(1, 0, 2, 0, 2, 1, 2, 2, 2, 3, 3, 3, 4, 3), C(5, 3),
                "R F 2x L F 3x R F 3x"),

            Mission("S2-07", MissionStage.Stage2, "Stairs up",
                "Climb the big stairs all the way to the finish!",
                true, false, false, MoveMulti,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 3, 1, 3, 2, 3, 3, 4, 3, 5, 3), C(6, 3),
                "R F 3x L F 3x R F 3x"),

            Mission("S2-08", MissionStage.Stage2, "Hook",
                "Go left, climb up, then turn right!",
                true, false, false, MoveMulti,
                C(4, 0), Cells(3, 0, 2, 0, 1, 0, 0, 0, 0, 1, 0, 2, 0, 3, 1, 3), C(2, 3),
                "L F 4x R F 3x R F 2x"),

            Mission("S2-09", MissionStage.Stage2, "Snake",
                "Up, over, and all the way down!",
                true, false, false, MoveMulti,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 2, 2, 2, 1), C(2, 0),
                "F 3x R F 2x R F 3x"),

            Mission("S2-10", MissionStage.Stage2, "Long road",
                "Up, a long road right, then down!",
                true, false, false, MoveMulti,
                C(0, 0), Cells(0, 1, 0, 2, 1, 2, 2, 2, 3, 2, 4, 2, 5, 2, 5, 1), C(5, 0),
                "F 2x R F 5x R F 2x"),

            // ---------------- STAGE 3 ----------------
            WithCues(Mission("S3-01", MissionStage.Stage3, "Happy start",
                "Zoom to the finish. Show a feeling on the smiley tile!",
                true, true, false, MoveMultiFeel,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 5, 1, 5, 2, 5, 3), C(6, 3),
                "R F 5x Happy L F 3x R F"),
                Cue(5, 0, CueKind.Feeling)),

            WithCues(Mission("S3-02", MissionStage.Stage3, "Sleepy road",
                "Up and over. Stop on the smiley tile and show a feeling!",
                true, true, false, MoveMultiFeel,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 3, 2, 3, 1), C(3, 0),
                "F 2x Surprise F R F 3x R F 3x"),
                Cue(0, 2, CueKind.Feeling)),

            WithCues(Mission("S3-03", MissionStage.Stage3, "Surprise!",
                "Go left, then climb. Show a feeling on the smiley tile!",
                true, true, false, MoveMultiFeel,
                C(5, 0), Cells(4, 0, 3, 0, 2, 0, 1, 0, 0, 0, 0, 1, 0, 2, 0, 3, 1, 3), C(2, 3),
                "L F 5x R F Playful F 2x R F 2x"),
                Cue(0, 1, CueKind.Feeling)),

            WithCues(Mission("S3-04", MissionStage.Stage3, "Dizzy loop",
                "A big loop! Show a feeling on the smiley tile.",
                true, true, false, MoveMultiFeel,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 4, 2, 4, 1), C(4, 0),
                "F 2x Sleepy F R F 4x R F 3x"),
                Cue(0, 2, CueKind.Feeling)),

            WithCues(Mission("S3-05", MissionStage.Stage3, "Playful hook",
                "Up, over and down. Be playful on the smiley tile!",
                true, true, false, MoveMultiFeel,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 5, 2), C(5, 1),
                "F 3x R F 5x R F Angry F"),
                Cue(5, 2, CueKind.Feeling)),

            WithCues(Mission("S3-06", MissionStage.Stage3, "Grumpy wall",
                "Zoom right, then climb. Show a feeling on the smiley tile!",
                true, true, false, MoveMultiFeel,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 5, 1, 5, 2, 5, 3, 4, 3), C(3, 3),
                "R F 5x L F 2x Dizzy F L F 2x"),
                Cue(5, 2, CueKind.Feeling)),

            WithCues(Mission("S3-07", MissionStage.Stage3, "Two feelings",
                "Find the smiley tile and show a feeling there!",
                true, true, false, MoveMultiFeel,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 3, 1, 3, 2, 3, 3, 2, 3, 1, 3), C(0, 3),
                "R F 2x Sad F L F 3x L F 3x"),
                Cue(2, 0, CueKind.Feeling)),

            WithCues(Mission("S3-08", MissionStage.Stage3, "Police hello",
                "Say hello to the police. Show a feeling on the smiley tile!",
                true, true, false, MoveMultiFeel,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 5, 2, 5, 1), C(5, 0),
                "F 3x R F 5x R F Happy F 2x",
                City(2, 3, CityTileKind.PoliceStation)),
                Cue(5, 2, CueKind.Feeling)),

            WithCues(Mission("S3-09", MissionStage.Stage3, "Hospital visit",
                "Pass the hospital. Show a feeling on the smiley tile!",
                true, true, false, MoveMultiFeel,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 5, 1, 5, 2, 5, 3, 4, 3, 3, 3), C(2, 3),
                "R F 5x L F 2x Surprise F L F 3x",
                City(3, 0, CityTileKind.Hospital)),
                Cue(5, 2, CueKind.Feeling)),

            WithCues(Mission("S3-10", MissionStage.Stage3, "School crossing",
                "Watch the crossing! Show a feeling on the smiley tile.",
                true, true, false, MoveMultiFeel,
                C(0, 0), Cells(0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 5, 2, 5, 3, 4, 3), C(3, 3),
                "F R F 5x L F 2x Playful L F 2x",
                City(3, 1, CityTileKind.ChildrenCrossing)),
                Cue(5, 3, CueKind.Feeling)),

            // ---------------- STAGE 4 ----------------
            WithCues(Mission("S4-01", MissionStage.Stage4, "First song",
                "Sing on the music tile and show a feeling on the smiley tile!",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 5, 2, 5, 1), C(5, 0),
                "F 2x Do F R F 5x R F 2x Sleepy F"),
                Cue(5, 1, CueKind.Feeling), Cue(0, 2, CueKind.Music)),

            WithCues(Mission("S4-02", MissionStage.Stage4, "Do Re Mi",
                "Sing on the music tile. Smile on the smiley tile!",
                true, true, true, All,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 5, 1, 5, 2, 5, 3, 4, 3, 3, 3), C(2, 3),
                "R F 5x L F Re F 2x L F Angry F 2x"),
                Cue(4, 3, CueKind.Feeling), Cue(5, 1, CueKind.Music)),

            WithCues(Mission("S4-03", MissionStage.Stage4, "Music walk",
                "A long road! Sing on the music tile, feel on the smiley tile.",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 6, 3, 6, 2), C(6, 1),
                "F 3x Dizzy R F 5x F R F Mi F"),
                Cue(0, 3, CueKind.Feeling), Cue(6, 2, CueKind.Music)),

            WithCues(Mission("S4-04", MissionStage.Stage4, "Echo",
                "Go left and come back. Sing and feel on the right tiles!",
                true, true, true, All,
                C(5, 0), Cells(4, 0, 3, 0, 2, 0, 1, 0, 0, 0, 0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3), C(5, 3),
                "L F 5x R F Fa F 2x R F 4x Sad F"),
                Cue(4, 3, CueKind.Feeling), Cue(0, 1, CueKind.Music)),

            WithCues(Mission("S4-05", MissionStage.Stage4, "Climb and sing",
                "Climb and sing on the music tile. Feel on the smiley tile!",
                true, true, true, All,
                C(0, 0), Cells(1, 0, 2, 0, 2, 1, 2, 2, 2, 3, 3, 3, 4, 3, 5, 3, 6, 3, 6, 2, 6, 1), C(6, 0),
                "R F 2x Happy L F Sol F 2x R F 4x R F 3x"),
                Cue(2, 0, CueKind.Feeling), Cue(2, 1, CueKind.Music)),

            WithCues(Mission("S4-06", MissionStage.Stage4, "Big U",
                "A big U! Find the music tile and the smiley tile.",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 6, 3, 6, 2, 6, 1), C(6, 0),
                "F 3x Surprise R F 4x La F 2x R F 3x"),
                Cue(0, 3, CueKind.Feeling), Cue(4, 3, CueKind.Music)),

            WithCues(Mission("S4-07", MissionStage.Stage4, "Zigzag song",
                "Zigzag, and stop on the music and smiley tiles!",
                true, true, true, All,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 3, 1, 3, 2, 3, 3, 4, 3, 5, 3, 6, 3, 6, 2, 6, 1), C(6, 0),
                "R F 3x Playful L F 3x R F 3x Si R F 3x"),
                Cue(3, 0, CueKind.Feeling), Cue(6, 3, CueKind.Music)),

            WithCues(Mission("S4-08", MissionStage.Stage4, "Concert",
                "Up and down like a song. Stop on both special tiles!",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 2, 2, 2, 1, 3, 1, 4, 1, 4, 2), C(4, 3),
                "F 2x Sleepy F R F Do F R F 2x L F 2x L F 2x"),
                Cue(0, 2, CueKind.Feeling), Cue(1, 3, CueKind.Music)),

            WithCues(Mission("S4-09", MissionStage.Stage4, "Police song",
                "Sing for the police on the music tile. Smile on the smiley tile!",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 6, 3, 6, 2, 6, 1), C(6, 0),
                "F 3x R F Re F 5x R F 2x Angry F",
                City(2, 3, CityTileKind.PoliceStation),
                City(5, 3, CityTileKind.Hospital)),
                Cue(6, 1, CueKind.Feeling), Cue(1, 3, CueKind.Music)),

            WithCues(Mission("S4-10", MissionStage.Stage4, "Busy street",
                "Busy street! Stop on the music tile and the smiley tile.",
                true, true, true, All,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 6, 0, 6, 1, 6, 2, 6, 3, 5, 3, 4, 3, 3, 3, 2, 3), C(1, 3),
                "R F 4x Dizzy F 2x L F 3x L F 4x Mi F",
                City(3, 0, CityTileKind.ChildrenCrossing),
                City(3, 3, CityTileKind.PoliceStation)),
                Cue(4, 0, CueKind.Feeling), Cue(2, 3, CueKind.Music)),

            // ---------------- STAGE 5 ----------------
            WithCues(Mission("S5-01", MissionStage.Stage5, "City tour",
                "A big city tour. Stop on the music tile and the smiley tile!",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 6, 3, 6, 2, 6, 1, 6, 0, 5, 0), C(4, 0),
                "F 3x R F Fa F 2x Sad F 3x R F 3x R F 2x",
                City(2, 3, CityTileKind.PoliceStation)),
                Cue(3, 3, CueKind.Feeling), Cue(1, 3, CueKind.Music)),

            WithCues(Mission("S5-02", MissionStage.Stage5, "Hospital run",
                "Hurry past the hospital. Don't miss the special tiles!",
                true, true, true, All,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 6, 0, 6, 1, 6, 2, 6, 3, 5, 3, 4, 3, 3, 3, 2, 3, 1, 3), C(0, 3),
                "R F 4x Sol F 2x L F 3x L F 3x Happy F 3x",
                City(3, 0, CityTileKind.Hospital),
                City(2, 3, CityTileKind.ChildrenCrossing)),
                Cue(3, 3, CueKind.Feeling), Cue(4, 0, CueKind.Music)),

            WithCues(Mission("S5-03", MissionStage.Stage5, "Twisty town",
                "A twisty town. Sing and feel on the right tiles!",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 2, 2, 2, 1, 2, 0, 3, 0, 4, 0, 4, 1, 4, 2, 4, 3), C(5, 3),
                "F 3x La R F 2x R F 3x L F 2x L F 3x Surprise R F",
                City(1, 3, CityTileKind.PoliceStation),
                City(3, 0, CityTileKind.Hospital)),
                Cue(4, 3, CueKind.Feeling), Cue(0, 3, CueKind.Music)),

            WithCues(Mission("S5-04", MissionStage.Stage5, "Long song",
                "A long road. Sing on the music tile, feel on the smiley tile!",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 1, 2, 2, 2, 3, 2, 4, 2, 5, 2, 6, 2, 6, 1, 6, 0, 5, 0, 4, 0, 3, 0), C(2, 0),
                "F 2x R F 5x F R F Playful F R F 2x Si F 2x",
                City(3, 2, CityTileKind.ChildrenCrossing)),
                Cue(6, 1, CueKind.Feeling), Cue(4, 0, CueKind.Music)),

            WithCues(Mission("S5-05", MissionStage.Stage5, "Big loop",
                "All the way around! Stop on both special tiles.",
                true, true, true, All,
                C(6, 0), Cells(5, 0, 4, 0, 3, 0, 2, 0, 1, 0, 0, 0, 0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 6, 3), C(6, 2),
                "L F 5x F R F 3x R F 3x Sleepy F 3x Do R F",
                City(3, 0, CityTileKind.Hospital),
                City(4, 3, CityTileKind.PoliceStation)),
                Cue(3, 3, CueKind.Feeling), Cue(6, 3, CueKind.Music)),

            WithCues(Mission("S5-06", MissionStage.Stage5, "Mountains",
                "Two mountains! Sing and feel on the right tiles.",
                true, true, true, All,
                C(0, 0), Cells(1, 0, 1, 1, 1, 2, 1, 3, 2, 3, 3, 3, 3, 2, 3, 1, 3, 0, 4, 0, 5, 0, 5, 1, 5, 2), C(5, 3),
                "R F L F 2x Angry F R F 2x R F 3x L F 2x Re L F 3x",
                City(2, 3, CityTileKind.ChildrenCrossing),
                City(4, 0, CityTileKind.Hospital)),
                Cue(1, 2, CueKind.Feeling), Cue(5, 0, CueKind.Music)),

            WithCues(Mission("S5-07", MissionStage.Stage5, "Snake city",
                "A snake through the city. Find both special tiles!",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 2, 2, 2, 1, 2, 0, 3, 0, 4, 0, 4, 1, 4, 2, 4, 3, 5, 3), C(6, 3),
                "F 3x R F 2x R F 3x L F 2x L F 2x Dizzy F Mi R F 2x",
                City(1, 3, CityTileKind.PoliceStation),
                City(3, 0, CityTileKind.Hospital),
                City(5, 3, CityTileKind.ChildrenCrossing)),
                Cue(4, 2, CueKind.Feeling), Cue(4, 3, CueKind.Music)),

            WithCues(Mission("S5-08", MissionStage.Stage5, "Music city",
                "Zoom around town. Sing on the music tile, smile on the smiley tile!",
                true, true, true, All,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 6, 0, 6, 1, 6, 2, 6, 3, 5, 3, 4, 3, 3, 3, 2, 3), C(1, 3),
                "R F 2x Fa F 4x L F 3x L F 2x Sad F 3x",
                City(3, 0, CityTileKind.Hospital),
                City(3, 3, CityTileKind.PoliceStation)),
                Cue(4, 3, CueKind.Feeling), Cue(2, 0, CueKind.Music)),

            WithCues(Mission("S5-09", MissionStage.Stage5, "Round the block",
                "Round the block! Stop on both special tiles.",
                true, true, true, All,
                C(2, 0), Cells(1, 0, 0, 0, 0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 5, 2, 5, 1, 5, 0), C(6, 0),
                "L F 2x Sol R F Happy F 2x R F 5x R F 3x L F",
                City(1, 0, CityTileKind.ChildrenCrossing),
                City(3, 3, CityTileKind.PoliceStation)),
                Cue(0, 1, CueKind.Feeling), Cue(0, 0, CueKind.Music)),

            WithCues(Mission("S5-10", MissionStage.Stage5, "Champion!",
                "The ultimate challenge! Sing and feel on the right tiles, champion!",
                true, true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 2, 2, 2, 1, 2, 0, 3, 0, 4, 0, 4, 1, 4, 2, 4, 3, 5, 3, 6, 3, 6, 2, 6, 1), C(6, 0),
                "F 3x R F 2x R F 3x L F 2x La L F 3x R F 2x Surprise R F 3x",
                City(1, 3, CityTileKind.PoliceStation),
                City(3, 0, CityTileKind.ChildrenCrossing),
                City(5, 3, CityTileKind.Hospital)),
                Cue(6, 3, CueKind.Feeling), Cue(4, 0, CueKind.Music))
        };
    }

    private static MissionDefinition Mission(
        string id, MissionStage stage, string title, string instruction,
        bool requireMultiplier, bool requireEmotion, bool requireSound, BlockGroups visible,
        Vector2Int start, List<Vector2Int> path, Vector2Int goal,
        string solution, params CityTilePlacement[] cityTiles)
    {
        return new MissionDefinition
        {
            id = id,
            stage = stage,
            title = title,
            instruction = instruction,
            requireMovement = true,
            requireMultiplier = requireMultiplier,
            requireEmotion = requireEmotion,
            requireSound = requireSound,
            visibleBlocks = visible,
            start = start,
            path = path,
            goal = goal,
            cityTiles = new List<CityTilePlacement>(cityTiles),
            intendedSolution = solution
        };
    }

    // Stages 3-5: a marked tile where mTiny must show a feeling or play music
    private static MissionDefinition WithCues(MissionDefinition mission, params MissionCue[] cues)
    {
        mission.cues = new List<MissionCue>(cues);
        return mission;
    }

    private static MissionCue Cue(int x, int y, CueKind kind)
    {
        return new MissionCue { cell = new Vector2Int(x, y), kind = kind };
    }

    private static Vector2Int C(int x, int y)
    {
        return new Vector2Int(x, y);
    }

    // Pairs of numbers -> cells: Cells(0,0, 0,1) = (0,0) (0,1)
    private static List<Vector2Int> Cells(params int[] xy)
    {
        List<Vector2Int> cells = new List<Vector2Int>();

        for (int i = 0; i + 1 < xy.Length; i += 2)
        {
            cells.Add(new Vector2Int(xy[i], xy[i + 1]));
        }

        return cells;
    }

    private static CityTilePlacement City(int x, int y, CityTileKind kind)
    {
        return new CityTilePlacement { cell = new Vector2Int(x, y), kind = kind };
    }
}
