using System.Collections.Generic;
using UnityEngine;

// Every mission in play order. The stage map, the Next button and
// progress tracking all read from this one list.
[CreateAssetMenu(menuName = "mTiny/Mission Catalog", fileName = "MissionCatalog")]
public class MissionCatalog : ScriptableObject
{
    public List<MissionDefinition> missions = new List<MissionDefinition>();

    public int Count => missions.Count;

    public MissionDefinition Get(int index)
    {
        return missions[index];
    }

    public int IndexOf(string id)
    {
        for (int i = 0; i < missions.Count; i++)
        {
            if (missions[i] != null && missions[i].id == id)
            {
                return i;
            }
        }

        return -1;
    }

    public List<int> IndicesInStage(MissionStage stage)
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < missions.Count; i++)
        {
            if (missions[i] != null && missions[i].stage == stage)
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    public int NextIndex(int index)
    {
        int next = index + 1;

        return next < missions.Count ? next : -1;
    }
}
