using Broccoli.Pipe;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static TreeEditor.TreeEditorHelper;
using static Unity.Burst.Intrinsics.X86.Avx;

/// <summary>
/// 
/// World Generator Mark II
/// 
/// Primary Goals:
/// - Integrate an accurate height map
///   - Generated from a form of perlin noise
/// - Integrate a biome generation system
///   - Heat map (adjusted by the height map, higher = colder)
///   - Rain/Perceptation map
///   - Biome = Percepatation / Temperature
/// - Integrate an accurate river map
///   - Voronoi outlines?
/// - Integrate an accurate tree map
///   - Gaussian noise, effected by height map
/// 
/// </summary>

public enum NOISE_TYPE { PERLIN, GAUSSIAN, VALUE, SIMPLEX, VORONOI, WORLEY }

public class GeneratorMarkII : MonoBehaviour
{
    [Header("World Settings")]
    public GameObject World;
    public int worldSizeX;
    public int worldSizeY;
    public float tileWidth = 1.0f;
    public float tileLength = 1.0f;

    [Header("Tile Prefabs")]
    public GameObject Grass;
    public GameObject Sand;

    [Header("Noise Settings")]
    public int seed;
    public bool generateSeed;
    public float worldFrequency;
    public float worldAmp;
    public float rainFrequency;
    public float treeFrequency;
    public float treeAmp;
    public float treeSpawnChance;

    [Header("Noise Maps")]
    public RawImage heatMapUI;
    public RawImage worldMapUI;
    public RawImage rainMapUI;
    public RawImage treeMapUI;
    [Space(6)]
    public Texture2D worldMap;
    public Texture2D heatMap;
    public Texture2D rainMap;
    public Texture2D riverMap;
    public Texture2D treeMap;

    [Header("Noise Details")]

    // Keeps track of the temperature for each position
    // TODO: change so entity tile manages its heat
    public Dictionary<Vector2Int, float> heatPositions;

    // Stores the pixels of worldMap that has either grass tile or sand tile (for beaches and sea floor)
    public Dictionary<Vector2Int, EntityType> landPositions;

    [Header("World Details")]
    public GameObject[,] WorldGrid = new GameObject[0,0];


    private void Start()
    {
        // Create noise maps
        BuildNoiseMaps();

        // Set UI images for debugging
        SetCanvasImages();

        // Generate tiles
        BuildWorld();
    }

    public void BuildWorld()
    {
        foreach (KeyValuePair<Vector2Int,EntityType> entry in landPositions)
        {
            
            GameObject tile = new GameObject();

            switch (entry.Value)
            {
                case EntityType.Grass:
                    
                    GameObject grass = Instantiate(Grass);
                    tile = grass;
                    break;

                case EntityType.Sand:

                    GameObject sand = Instantiate(Sand);
                    tile = sand;
                    break;
            }

            // Set entity information
            EntityTile curTile = tile.GetComponent<EntityTile>();
            curTile.SetEntityCategory(EntityCategory.Surface);

            // Set world position of game object
            curTile.SetWorldPos(calcWorldCoord(entry.Key));
            curTile.SetGridPos(calcWorldCoord(entry.Key));

            tile.transform.parent = World.transform;

        }
    }

    /// <summary>
    /// Calls the generate functions for each map.
    /// </summary>
    public void BuildNoiseMaps()
    {
        // Generate seed
        var rand = new System.Random();
        seed = rand.Next();
        Debug.Log("Seed: " + seed);

        // Create initial heat map
        heatMap = GenerateHeatMap();

        // Generate the world map
        worldMap = GenerateWorldMap(seed, worldFrequency, worldAmp);

        // Generate the rain map
        rainMap = GenerateRainMap(seed, worldFrequency, worldAmp);

        // Generate the tree map
        float treeChance = UnityEngine.Random.Range(worldFrequency + 1.5f, worldFrequency + 3.5f);
        treeMap = GenerateTreeMap(seed, treeChance, treeFrequency);
    }

    /// <summary>
    /// The GenerateHeatMap is different from GenerateNoiseMaps because 
    /// Heat Map uses red and blue to determine hot and cold places
    /// </summary>
    /// <returns> Texture2D </returns>
    public Texture2D GenerateHeatMap()
    {
        // Init texture
        Texture2D texture = new Texture2D(worldSizeX, worldSizeY);

        // Init dictionary
        heatPositions = new Dictionary<Vector2Int, float>();

        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                // Gets the X and Y from the world sizes
                float normalizedX = (float)x / worldSizeX;
                float normalizedY = (float)y / worldSizeY;

                // Calculate the current distance from the center
                float imageOffset = 0.5f;
                float distanceFromCenterY = Mathf.Abs(normalizedY - imageOffset) * 2;
                float temperature = imageOffset;

                // Determine temperature change from the center
                if (distanceFromCenterY < 0.3f)
                {
                    temperature = 1f;
                }
                else if (distanceFromCenterY < 0.3f)
                {
                    temperature = 0.8f;
                }
                else if (distanceFromCenterY < 0.5f)
                {
                    temperature = 0.6f;
                }
                else if (distanceFromCenterY < 0.7f)
                {
                    temperature = 0.4f;
                }
                else if (distanceFromCenterY < 0.8f)
                {
                    temperature = 0.2f;
                }
                else
                {
                    temperature = 0.0f;
                }


                Color color;

                // Determine the color of each pixel based on distance from center
                // red, light red, yellow, light blue, white
                if (temperature < 0.2f)
                {
                    // white
                    color = Color.white;
                }
                else if (temperature < 0.45f)
                {
                    // light blue
                    float blueComponent = Mathf.Lerp(0.45f, 1.0f, (temperature - 0.55f) / 0.2f);
                    color = new Color(0f, 0f, blueComponent);
                }
                else if (temperature < 0.8f)
                {
                    // light red
                    float redComponent = Mathf.Lerp(0.85f, 1.0f, (temperature - 0.4f) / 0.2f);
                    float greenComponent = Mathf.Lerp(0.0f, 0.4f, (temperature - 0.4f) / 0.2f);
                    color = new Color(redComponent, greenComponent, 0f);
                }
                else
                {
                    // red
                    float redComponent = Mathf.Lerp(0.15f, 1.0f, (temperature - 0.85f) / 0.2f);
                    color = new Color(redComponent, 0f, 0f);
                }

                Vector2Int pos = new Vector2Int(x, y);
                heatPositions[pos] = temperature;

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Generates the world map using Simplex Noise.
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="frequency"></param>
    /// <param name="amp"></param>
    /// <returns></returns>
    public Texture2D GenerateWorldMap(int seed, float frequency, float amp)
    {
        Texture2D texture = new Texture2D(worldSizeX, worldSizeY);

        SimplexNoise simplexNoise = new SimplexNoise(seed, frequency, amp);

        float[,] worldArray = new float[worldSizeX, worldSizeY];

        // Sample the 2D noise and add it into the worldArray
        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                float fx = x / (worldSizeX - 1.0f);
                float fy = y / (worldSizeY - 1.0f);

                worldArray[x, y] = simplexNoise.Sample2D(fx, fy);
            }
        }

        landPositions = new Dictionary<Vector2Int, EntityType>();

        // Set the pixels for each texture
        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                float n = worldArray[x, y];
                texture.SetPixel(x, y, new Color(n, n, n, 1));

                // Create land map for fast lookup when generating tiles
                Vector2Int pos = new Vector2Int(x, y);

                if (n >= 0.90f)
                {
                    landPositions[pos] = landPositions.GetValueOrDefault(pos, EntityType.Grass);
                }
                else
                {
                    landPositions[pos] = landPositions.GetValueOrDefault(pos, EntityType.Sand);
                }
            }
        }

        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Generates the rain map using Perlin Noise.
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="frequency"></param>
    /// <param name="amp"></param>
    /// <returns></returns>
    public Texture2D GenerateRainMap(int seed, float frequency, float amp)
    {
        Texture2D texture = new Texture2D(worldSizeX, worldSizeY);

        PerlinNoise voronoiNoise = new PerlinNoise(seed, frequency, amp);

        float[,] worldArray = new float[worldSizeX, worldSizeY];

        // Sample the 2D noise and add it into the worldArray
        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                float fx = x / (worldSizeX - 1.0f);
                float fy = y / (worldSizeY - 1.0f);

                worldArray[x, y] = voronoiNoise.Sample2D(fx, fy);
            }
        }

        // Set the pixels for each texture
        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                float n = worldArray[x, y];
                texture.SetPixel(x, y, new Color(n, n, n, 1));
            }
        }

        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Generates the tree map using Voronoi Noise.
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="frequency"></param>
    /// <param name="amp"></param>
    /// <returns></returns>
    public Texture2D GenerateTreeMap(int seed, float frequency, float amp)
    {
        Texture2D texture = new Texture2D(worldSizeX, worldSizeY);

        PerlinNoise voronoiNoise = new PerlinNoise(seed, frequency, amp);

        float[,] worldArray = new float[worldSizeX, worldSizeY];

        // Sample the 2D noise and add it into the worldArray
        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                float fx = x / (worldSizeX - 1.0f);
                float fy = y / (worldSizeY + heatPositions[new Vector2Int(x, y)]);

                worldArray[x, y] = voronoiNoise.Sample2D(fx, fy);
            }
        }

        // Set the pixels for each texture
        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                float n = worldArray[x, y];
                texture.SetPixel(x, y, new Color(n, n, n, 1));
            }
        }

        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Calculate the initial position for the world, so hexagonal tiles can be accuratly spawned in.
    /// </summary>
    /// <returns></returns>
    Vector3 CalcInit()
    {
        Vector3 initPos;

        initPos = new Vector3(-tileWidth * worldSizeX / 2f + tileWidth / 2, 0, worldSizeY / 2f * tileLength - tileLength / 2);

        return initPos;
    }

    // Grid to World converter
    public Vector3 calcWorldCoord(Vector2 gridPos)
    {
        // Position of the first tile
        Vector3 initPos = CalcInit();

        // Every second row is offset by half of the tile sizeX
        float offset = 0;

        if (gridPos.y % 2 != 0)
            offset = tileWidth / 2;

        float x = initPos.x + offset + gridPos.x * tileWidth;

        // Every new line is offset in z direction by 3/4 of the tileagon sizeY
        float z = initPos.z - gridPos.y * tileLength * 0.75f;

        return new Vector3(x, 0, z);
    }

    /// <summary>
    /// Sets UI textures to maps.
    /// </summary>
    public void SetCanvasImages()
    {
        heatMapUI.texture = heatMap;
        worldMapUI.texture = worldMap;
        rainMapUI.texture = rainMap;
        treeMapUI.texture = treeMap;
    }
}
