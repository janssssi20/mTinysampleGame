using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Writes the 15 starting missions into Assets/Data/MissionCatalog.asset.
// After seeding, edit missions in the asset's Inspector instead.
public static class MissionCatalogSeeder
{
    private const string CatalogPath = "Assets/Data/MissionCatalog.asset";

    private const BlockGroups Move = BlockGroups.Movement;
    private const BlockGroups MoveMulti = BlockGroups.Movement | BlockGroups.Multiplier;
    private const BlockGroups All = BlockGroups.All;

    [MenuItem("mTiny/Seed Mission Catalog")]
    public static void Seed()
    {
        MissionCatalog existing = AssetDatabase.LoadAssetAtPath<MissionCatalog>(CatalogPath);

        if (existing != null && !Application.isBatchMode &&
            !EditorUtility.DisplayDialog("Seed Mission Catalog",
                "This replaces every mission in the catalog with the starting 15. Continue?",
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
            // ---------------- EASY ----------------
            Mission("E1", MissionStage.Easy, "Reach the flag",
                "Reach the finish line and have fun!",
                false, false, Move,
                C(1, 0), Cells(0, 0, 0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3), C(5, 3),
                "L F R F F F R F F F F F"),

            Mission("E2", MissionStage.Easy, "Count with multipliers",
                "Use Move and Multiplier blocks.",
                true, false, MoveMulti,
                C(3, 1), Cells(2, 1, 2, 0, 1, 0, 0, 0, 0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3), C(5, 3),
                "L F L F R F 2x R F 3x R F 5x"),

            Mission("E3", MissionStage.Easy, "Show a feeling",
                "Use Move, Multiplier and Emotion blocks.",
                true, true, All,
                C(0, 3), Cells(1, 3, 1, 2, 1, 1, 2, 1, 3, 1, 4, 1, 4, 2, 4, 3, 5, 3, 6, 3, 6, 2, 6, 1, 6, 0), C(7, 0),
                "Happy R F R F 2x L F 3x L F 2x R F 2x R F 3x L F"),

            Mission("E4", MissionStage.Easy, "Turn right",
                "Help mTiny turn right to reach the flag!",
                false, false, Move,
                C(1, 0), Cells(1, 1, 1, 2, 2, 2), C(3, 2),
                "F F R F F"),

            Mission("E5", MissionStage.Easy, "Happy trip",
                "Make mTiny happy, then go up to the flag!",
                true, true, All,
                C(1, 0), Cells(1, 1, 1, 2), C(1, 3),
                "Happy F 3x"),

            // ---------------- MEDIUM ----------------
            Mission("M1", MissionStage.Medium, "Long road",
                "Turn right and use a multiplier to go far!",
                true, false, MoveMulti,
                C(0, 1), Cells(1, 1, 2, 1, 3, 1), C(4, 1),
                "R F 4x"),

            Mission("M2", MissionStage.Medium, "Turn left",
                "Go up, turn left, then zoom to the flag!",
                true, false, MoveMulti,
                C(4, 0), Cells(4, 1, 3, 1, 2, 1), C(1, 1),
                "F L F 3x"),

            Mission("M3", MissionStage.Medium, "Up and over",
                "Climb up high, then turn to the flag!",
                true, false, MoveMulti,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3), C(2, 3),
                "F 3x R F 2x"),

            Mission("M4", MissionStage.Medium, "Corner",
                "Go up, turn left, and use a big multiplier!",
                true, false, MoveMulti,
                C(5, 0), Cells(5, 1, 5, 2, 4, 2, 3, 2, 2, 2), C(1, 2),
                "F 2x L F 4x"),

            Mission("M5", MissionStage.Medium, "Climb",
                "Two turns this time. Can you reach the top?",
                true, false, MoveMulti,
                C(0, 0), Cells(0, 1, 1, 1, 2, 1, 3, 1, 3, 2), C(3, 3),
                "F R F 3x L F 2x"),

            // ---------------- HARD ----------------
            Mission("H1", MissionStage.Hard, "Zig-zag",
                "Show a feeling, then zig-zag to the flag!",
                false, true, All,
                C(1, 0), Cells(1, 1, 2, 1), C(2, 2),
                "Happy F R F L F"),

            Mission("H2", MissionStage.Hard, "Police help",
                "Say hello to the police, then turn left!",
                true, true, All,
                C(2, 0), Cells(2, 1, 2, 2, 1, 2), C(0, 2),
                "Surprise F 2x L F 2x",
                City(2, 1, CityTileKind.PoliceStation)),

            Mission("H3", MissionStage.Hard, "Hospital visit",
                "Pass the hospital and reach the flag!",
                true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 1, 2, 2, 2), C(3, 2),
                "Happy F 2x R F 3x",
                City(0, 1, CityTileKind.Hospital)),

            Mission("H4", MissionStage.Hard, "School crossing",
                "Watch out for the children crossing!",
                true, true, All,
                C(5, 0), Cells(5, 1, 4, 1, 3, 1, 2, 1, 2, 2), C(2, 3),
                "Surprise F L F 3x R F 2x",
                City(5, 1, CityTileKind.ChildrenCrossing)),

            Mission("H5", MissionStage.Hard, "Big city",
                "A big trip across the city. You can do it!",
                true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3), C(5, 3),
                "Happy F 3x R Playful F 5x",
                City(0, 2, CityTileKind.PoliceStation),
                City(3, 3, CityTileKind.Hospital)),
        };
    }

    private static MissionDefinition Mission(
        string id, MissionStage stage, string title, string instruction,
        bool requireMultiplier, bool requireEmotion, BlockGroups visible,
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
            visibleBlocks = visible,
            start = start,
            path = path,
            goal = goal,
            cityTiles = new List<CityTilePlacement>(cityTiles),
            intendedSolution = solution
        };
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
