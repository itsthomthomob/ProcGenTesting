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
    public TilePooler TP;
    public int worldSizeX;
    public int worldSizeY;

    // Keep at 1.75, 2
    public float tileWidth = 1.75f;
    public float tileLength = 2.0f;
    public float sandElevation = 0.20f;
    public float tileStep = 10;

    [Header("Noise Settings")]
    public int seed;
    public bool generateSeed;
    public float worldFrequency;
    public float worldAmp;
    public float rainFrequency;

    [Range(0.01f, 0.30f)]
    public float treeFalloff;
    
    //public float treeSpawnChance;

    [Header("Noise Maps")]
    public RawImage heatMapUI;
    public RawImage worldMapUI;
    public RawImage rainMapUI;
    public RawImage treeMapUI;
    [Space(6)]
    public Color[] worldPixels;
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
    public Dictionary<Vector2Int, float> landPositions;

    [Header("World Details")]
    public GameObject[,] WorldGrid = new GameObject[0,0];


    private void Start()
    {
        // Create noise maps
        BuildNoiseMaps();

        // Set UI images for debugging
        SetCanvasImages();

        // Generate the world structure
        BuildWorld();
    }

    public void BuildWorld()
    {

        Debug.Log("Land Position Count:" + landPositions.Count);

        foreach (KeyValuePair<Vector2Int, float> entry in landPositions)
        {

            GameObject tile;

            // Set tile according to land elevation
            if (entry.Value > sandElevation)
            {
                tile = TP.GetGrassTile();
            }
            else
            {
                tile = TP.GetSandTile();
            }

            // Set tiles world positions
            EntityTile curTile = tile.GetComponent<EntityTile>();

            // Calculate height placement
            float heightOffset = entry.Value * tileStep;

            // Set tile position
            curTile.SetWorldPos(calcWorldCoord(entry.Key), heightOffset);
            curTile.SetGridPos(calcWorldCoord(entry.Key));

            // Set parent
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

        // Initialize the tile pool after world map is generated
        TP = FindObjectOfType<TilePooler>();
        TP.InitializePool();

        // Generate the rain map
        rainMap = GenerateRainMap(seed, worldFrequency, worldAmp);

        // Generate the tree map
        treeMap = GenerateTreeMap(seed, worldFrequency, worldAmp - treeFalloff);
    }

    /// <summary>
    /// Heat Map uses red and blue to determine hot and cold places
    /// </summary>
    /// <returns> Texture2D </returns>
    public Texture2D GenerateHeatMap()
    {
        Texture2D texture = new Texture2D(worldSizeX, worldSizeY);
        heatPositions = new Dictionary<Vector2Int, float>();

        Color[] pixels = new Color[worldSizeX * worldSizeY];
        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % worldSizeX;
            int y = i / worldSizeX;

            float normalizedX = (float)x / worldSizeX;
            float normalizedY = (float)y / worldSizeY;

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

            pixels[i] = color;
        }

        texture.SetPixels(pixels);
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

        landPositions = new Dictionary<Vector2Int, float>();
        Color[] pixels = new Color[worldSizeX * worldSizeY];
        Vector2Int pos = Vector2Int.zero;

        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                float fx = x / (worldSizeX - 1.0f);
                float fy = y / (worldSizeY - 1.0f);

                float n = simplexNoise.Sample2D(fx, fy);

                pixels[x + y * worldSizeX] = new Color(n, n, n, 1);
                pos.x = x;
                pos.y = y;

                landPositions[pos] = n;
            }
        }

        worldPixels = pixels;

        texture.SetPixels(pixels);
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

        // Sample the 2D noise and add it into the worldArray
        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                float fx = x / (worldSizeX - 1.0f);
                float fy = y / (worldSizeY - 1.0f);

                float n = voronoiNoise.Sample2D(fx, fy);
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

        SimplexNoise simplexNoise = new SimplexNoise(seed, frequency, amp);

        // Sample the 2D noise and set the pixels for each texture
        for (int y = 0; y < worldSizeY; y++)
        {
            for (int x = 0; x < worldSizeX; x++)
            {
                float fx = x / (worldSizeX - 1.0f);
                float fy = y / (worldSizeY + heatPositions[new Vector2Int(x, y)]);

                float n = simplexNoise.Sample2D(fx, fy);
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
