using System.Collections.Generic;
using UnityEngine;

// Cheap lookup grid so fish only check nearby stuff instead of everything.
// Spatial grid so fish do not scan every object every time.
public class EcosystemSpatialGrid : MonoBehaviour
{
    public float CellSize = 12f;
    public bool DrawDebugGrid;

    private readonly Dictionary<Vector3Int, List<MarineCreatureAgent>> creatureCells = new Dictionary<Vector3Int, List<MarineCreatureAgent>>();
    private readonly Dictionary<Vector3Int, List<FoodSource>> foodCells = new Dictionary<Vector3Int, List<FoodSource>>();
    private readonly Dictionary<Vector3Int, List<CarrionSource>> carrionCells = new Dictionary<Vector3Int, List<CarrionSource>>();
    private readonly Dictionary<Vector3Int, List<FishEggCluster>> eggCells = new Dictionary<Vector3Int, List<FishEggCluster>>();

    private int lastCreatureCount;
    private int lastFoodCount;
    private int lastCarrionCount;
    private int lastEggCount;

    // Rebuilds nearby lookup cells for fish/resources.
    public void Rebuild(
        List<MarineCreatureAgent> creatures,
        List<FoodSource> food,
        List<CarrionSource> carrion,
        List<FishEggCluster> eggs)
    {
        creatureCells.Clear();
        foodCells.Clear();
        carrionCells.Clear();
        eggCells.Clear();

        float safeCellSize = Mathf.Max(1f, CellSize);

        if (creatures != null)
        {
            for (int i = 0; i < creatures.Count; i++)
            {
                MarineCreatureAgent creature = creatures[i];
                if (creature == null)
                {
                    continue;
                }

                AddToCell(creatureCells, GetCell(creature.transform.position, safeCellSize), creature);
            }

            lastCreatureCount = creatures.Count;
        }
        else
        {
            lastCreatureCount = 0;
        }

        if (food != null)
        {
            for (int i = 0; i < food.Count; i++)
            {
                FoodSource source = food[i];
                if (source == null || source.IsConsumed)
                {
                    continue;
                }

                AddToCell(foodCells, GetCell(source.transform.position, safeCellSize), source);
            }

            lastFoodCount = food.Count;
        }
        else
        {
            lastFoodCount = 0;
        }

        if (carrion != null)
        {
            for (int i = 0; i < carrion.Count; i++)
            {
                CarrionSource source = carrion[i];
                if (source == null || source.IsConsumed)
                {
                    continue;
                }

                AddToCell(carrionCells, GetCell(source.transform.position, safeCellSize), source);
            }

            lastCarrionCount = carrion.Count;
        }
        else
        {
            lastCarrionCount = 0;
        }

        if (eggs != null)
        {
            for (int i = 0; i < eggs.Count; i++)
            {
                FishEggCluster cluster = eggs[i];
                if (cluster == null)
                {
                    continue;
                }

                AddToCell(eggCells, GetCell(cluster.transform.position, safeCellSize), cluster);
            }

            lastEggCount = eggs.Count;
        }
        else
        {
            lastEggCount = 0;
        }
    }

    // Returns creatures near a point by checking only nearby grid cells
    public List<MarineCreatureAgent> QueryCreatures(Vector3 position, float radius)
    {
        List<MarineCreatureAgent> results = new List<MarineCreatureAgent>(24);
        QueryCells(creatureCells, position, radius, results);
        return results;
    }

    // Returns food near a point by checking only nearby grid cells
    public List<FoodSource> QueryFood(Vector3 position, float radius)
    {
        List<FoodSource> results = new List<FoodSource>(24);
        QueryCells(foodCells, position, radius, results);
        return results;
    }

    // Returns carrion near a point by checking only nearby grid cells
    public List<CarrionSource> QueryCarrion(Vector3 position, float radius)
    {
        List<CarrionSource> results = new List<CarrionSource>(12);
        QueryCells(carrionCells, position, radius, results);
        return results;
    }

    // Returns egg clusters near a point by checking only nearby grid cells
    public List<FishEggCluster> QueryEggClusters(Vector3 position, float radius)
    {
        List<FishEggCluster> results = new List<FishEggCluster>(8);
        QueryCells(eggCells, position, radius, results);
        return results;
    }

    // Builds a short debug string for the current state
    public string GetDebugSummary()
    {
        return "Grid C:" + lastCreatureCount + " F:" + lastFoodCount + " K:" + lastCarrionCount + " E:" + lastEggCount + " Cell:" + CellSize.ToString("F1");
    }

    // Handles the add to cell step.
    private static void AddToCell<T>(Dictionary<Vector3Int, List<T>> dictionary, Vector3Int cell, T value)
    {
        if (!dictionary.TryGetValue(cell, out List<T> list))
        {
            list = new List<T>();
            dictionary.Add(cell, list);
        }

        list.Add(value);
    }

    // Handles the query cells step.
    private void QueryCells<T>(Dictionary<Vector3Int, List<T>> dictionary, Vector3 position, float radius, List<T> results) where T : Component
    {
        float safeCellSize = Mathf.Max(1f, CellSize);
        Vector3Int centre = GetCell(position, safeCellSize);
        int cellRadius = Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0.01f, radius) / safeCellSize));
        float radiusSqr = radius * radius;

        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector3Int cell = new Vector3Int(centre.x + x, centre.y + y, centre.z + z);
                    if (!dictionary.TryGetValue(cell, out List<T> list))
                    {
                        continue;
                    }

                    for (int i = 0; i < list.Count; i++)
                    {
                        T item = list[i];
                        if (item == null)
                        {
                            continue;
                        }

                        if ((item.transform.position - position).sqrMagnitude <= radiusSqr)
                        {
                            results.Add(item);
                        }
                    }
                }
            }
        }
    }

    // Converts a world position into a spatial grid cell
    private static Vector3Int GetCell(Vector3 position, float cellSize)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize));
    }

    // Draws selected-only gizmos so setup can be checked without clutter
    private void OnDrawGizmosSelected()
    {
        if (!DrawDebugGrid)
        {
            return;
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, Vector3.one * Mathf.Max(1f, CellSize));
    }
}
