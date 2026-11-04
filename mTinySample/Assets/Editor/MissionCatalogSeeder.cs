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
            // Board tiles (start + path + goal): Easy 5-8, Medium 8-12, Hard 12-15.
            // The robot starts facing up; L turns counter-clockwise.

            // ---------------- EASY ----------------
            Mission("E1", MissionStage.Easy, "Reach the flag",
                "Walk up, turn right, and reach the flag!",
                false, false, Move,
                C(0, 0), Cells(0, 1, 1, 1, 2, 1), C(3, 1),
                "F R F F F"),

            Mission("E2", MissionStage.Easy, "Count with multipliers",
                "Go up, turn right, and use multipliers!",
                true, false, MoveMulti,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3), C(4, 3),
                "F 3x R F 4x"),

            Mission("E3", MissionStage.Easy, "Show a feeling",
                "Show a feeling, then race to the flag!",
                true, true, All,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0), C(5, 0),
                "Happy R F 5x"),

            Mission("E4", MissionStage.Easy, "Turn right",
                "Help mTiny turn right to reach the flag!",
                false, false, Move,
                C(1, 0), Cells(1, 1, 1, 2, 2, 2), C(3, 2),
                "F F R F F"),

            Mission("E5", MissionStage.Easy, "Happy trip",
                "Make mTiny happy, then climb up and over!",
                true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3), C(2, 3),
                "Happy F 3x R F 2x"),

            // ---------------- MEDIUM ----------------
            Mission("M1", MissionStage.Medium, "Long road",
                "Go up, turn right, and zoom down the long road!",
                true, false, MoveMulti,
                C(0, 0), Cells(0, 1, 0, 2, 1, 2, 2, 2, 3, 2, 4, 2), C(5, 2),
                "F 2x R F 5x"),

            Mission("M2", MissionStage.Medium, "Turn left",
                "Climb up, turn left, then zoom to the flag!",
                true, false, MoveMulti,
                C(5, 0), Cells(5, 1, 5, 2, 5, 3, 4, 3, 3, 3, 2, 3, 1, 3), C(0, 3),
                "F 3x L F 5x"),

            Mission("M3", MissionStage.Medium, "Up and over",
                "Up, over, and back down to the flag!",
                true, false, MoveMulti,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 3, 2, 3, 1), C(3, 0),
                "F 3x R F 3x R F 3x"),

            Mission("M4", MissionStage.Medium, "Corner",
                "Turn left, go far, then turn right and climb!",
                true, false, MoveMulti,
                C(7, 0), Cells(6, 0, 5, 0, 4, 0, 3, 0, 2, 0, 2, 1, 2, 2), C(2, 3),
                "L F 5x R F 3x"),

            Mission("M5", MissionStage.Medium, "Climb",
                "Climb the stairs all the way to the flag!",
                true, false, MoveMulti,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 3, 1, 3, 2, 3, 3, 4, 3, 5, 3), C(6, 3),
                "R F 3x L F 3x R F 3x"),

            // ---------------- HARD ----------------
            Mission("H1", MissionStage.Hard, "U-turn",
                "Show a feeling and make a big U-turn!",
                false, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 5, 2, 5, 1), C(5, 0),
                "Happy F 3x R F 5x R F 3x"),

            // City tiles sit on east-west stretches: their road art runs that way
            Mission("H2", MissionStage.Hard, "Police help",
                "Turn right and say hello to the police!",
                true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 5, 2, 5, 1), C(5, 0),
                "Surprise F 3x R F 5x R F 3x",
                City(2, 3, CityTileKind.PoliceStation)),

            Mission("H3", MissionStage.Hard, "Hospital visit",
                "Pass the hospital and reach the flag!",
                true, true, All,
                C(0, 0), Cells(1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 5, 1, 5, 2, 5, 3, 4, 3, 3, 3), C(2, 3),
                "Happy R F 5x L F 3x L F 3x",
                City(3, 0, CityTileKind.Hospital)),

            Mission("H4", MissionStage.Hard, "School crossing",
                "Watch out for the children crossing!",
                true, true, All,
                C(0, 0), Cells(0, 1, 1, 1, 2, 1, 3, 1, 4, 1, 5, 1, 5, 2, 5, 3, 4, 3, 3, 3, 2, 3), C(1, 3),
                "Surprise F R F 5x L F 2x L F 4x",
                City(3, 1, CityTileKind.ChildrenCrossing)),

            Mission("H5", MissionStage.Hard, "Big city",
                "A big trip across the city. You can do it!",
                true, true, All,
                C(0, 0), Cells(0, 1, 0, 2, 0, 3, 1, 3, 2, 3, 3, 3, 4, 3, 5, 3, 6, 3, 7, 3, 7, 2, 7, 1), C(7, 0),
                "Happy F 3x R Playful F 5x F 2x R F 3x",
                City(2, 3, CityTileKind.PoliceStation),
                City(5, 3, CityTileKind.Hospital)),
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
