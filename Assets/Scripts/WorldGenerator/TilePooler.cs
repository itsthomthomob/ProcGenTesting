using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilePooler : MonoBehaviour
{
    [Header("World Settings")]
    public float sandElevation;

    [Header("Tile Prefabs")]
    public GameObject grassPrefab;
    public GameObject sandPrefab;

    [Header("Pool Details")]
    public int poolSize;
    private List<GameObject> poolObjects = new List<GameObject>();
    private List<GameObject> grassObjects = new List<GameObject>();
    private List<GameObject> sandObjects = new List<GameObject>();


    public void InitializePool()
    {
        GeneratorMarkII GEN = FindObjectOfType<GeneratorMarkII>();
        poolSize = GEN.worldPixels.Length;

        for (int i = 0; i < poolSize; i++)
        {
            // The R, G, and B in worldPixels is constantly the same
            if (GEN.worldPixels[i].r > GEN.sandElevation)
            {
                GameObject grass = Instantiate(grassPrefab);
                grassObjects.Add(grass);
                grass.SetActive(false);
            }
            else
            {
                GameObject sand = Instantiate(sandPrefab);
                sandObjects.Add(sand);
                sand.SetActive(false);
            }
        }
    }

    public GameObject GetGrassTile()
    {
        if (grassObjects.Count == 0)
        {
            GameObject tile = Instantiate(grassPrefab);
            grassObjects.Add(tile);
        }

        GameObject newTile = grassObjects[0];
        grassObjects.RemoveAt(0);
        newTile.SetActive(true);
        return newTile;
    }

    public GameObject GetSandTile()
    {
        if (grassObjects.Count == 0)
        {
            GameObject tile = Instantiate(sandPrefab);
            sandObjects.Add(tile);
        }

        GameObject newTile = sandObjects[0];
        sandObjects.RemoveAt(0);
        newTile.SetActive(true);
        return newTile;
    }

    public void ReturnTile(GameObject tile)
    {
        tile.SetActive(false);
        poolObjects.Add(tile);
    }
}
