using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 
/// Thank you to TBS Development for sharing his methods on hexagonal terrain generation.
/// Link here: https://tbswithunity3d.wordpress.com/about/
/// 
/// Thank you to Scrawk for his straight-forward implemenation of different noise types.
/// Link here: https://github.com/Scrawk/Procedural-Noise
/// 
/// Thank you Halis Avakis for providing a great, free-to-use water shader as a baseline
/// 
/// This is mostly a playground project so I can mess with terrain generation and editor scripts.
/// 
/// Modification Log
/// 
/// 05-12: Initialization of the project. Implemented basic hexagonal grid generation with hex calculations and editor controls
/// 05-16: Implemented basic noise manipulation thanks to Scrawk, link abve
/// 05-18: Painted a new texture in Photoshop for the grass tile
/// 05-19 to 05-21: Experimentation with normals on the grass tile cap
/// 06-02: Created the Sand tile
/// --   : Drew textures for sand, grass, water, and trees 
/// 06-30: Created water shaders
/// 
/// </summary>
/// 

public enum NOISE_TYPE { PERLIN, VALUE, SIMPLEX, VORONOI, WORLEY }

[ExecuteInEditMode]
public class Generator : MonoBehaviour
{
    [Header("Generator Controls")]
    public float WaterElevation;
    public float SandLevelMin;
    public float SandLevelMax;
    public int sizeX;
    public int sizeY;
    public float tileWidth = 1.0f;
    public float tileLength = 1.0f;

    [Header("Map Controls")]
    public NOISE_TYPE noiseType;
    public RawImage mapTexture;
    public RawImage bushMapTexture;
    private int seed = 0;
    public float frequency = 1.0f;
    public float amp = 1.0f;
    public float bushAmpModifer;
    public float bushFreqModifier;
    public float BushThreshold;

    Texture2D WorldNoiseTexture;
    Texture2D BushNoiseTexture;
    private Color[] pix;

    [Header("Tiles")]
    public GameObject Grass;
    public GameObject Water;
    public GameObject Sand;
    public GameObject Bush;
    public GameObject World;
    public GameObject[,] hexGrid = new GameObject[0,0];
    public List<GameObject> allTiles = new List<GameObject>();

    public void GenerateWorld() 
    {
        
        // Generate the noise texture
        hexGrid = new GameObject[sizeX, sizeY];

        ConstructWorldNoise();
        ConstructBushNoise();

        // Clear and destroy previous grid
        for (int i = 0; i < allTiles.Count; i++)
        {
            DestroyImmediate(allTiles[i]);
        }

        allTiles.Clear();

        // Create new grid
        for (int y = 0; y < sizeX; y++)
        {
            for (int x = 0; x < sizeY; x++)
            {
                //GameObject assigned to Hex public variable is cloned
                GameObject tile = Instantiate(Grass);

                //Set tile to grid
                hexGrid[y, x] = tile;

                //Current position in grid
                Vector2 gridPos = new Vector2(y, x);
                EntityTile curTile = tile.GetComponent<EntityTile>();
                curTile.SetTileType("Grass");
                curTile.SetGridPos(calcWorldCoord(gridPos));
                curTile.SetWorldPos(calcWorldCoord(gridPos));
                tile.transform.parent = World.transform;
                allTiles.Add(tile);
            }
        }

        ConstructWorldTiles();
        ConstructBushTiles();
    }

    /// <summary>
    /// Implements the Voronoi Noise method from Scrawk's example.
    /// </summary>
    public void ConstructWorldNoise() 
    {
        switch (noiseType)
        {
            case NOISE_TYPE.PERLIN:
                // Set new noise
                WorldNoiseTexture = new Texture2D(sizeX, sizeY);
                pix = new Color[WorldNoiseTexture.width * WorldNoiseTexture.height];

                seed = Random.Range(0, 1000);

                PerlinNoise perlin = new PerlinNoise(seed, frequency, amp);

                float[,] arr = new float[sizeX, sizeY];

                //Sample the 2D noise and add it into a array.
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        float fx = x / (sizeX - 1.0f);
                        float fy = y / (sizeY - 1.0f);

                        arr[x, y] = perlin.Sample2D(fx, fy);
                    }
                }

                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        float n = arr[x, y];
                        WorldNoiseTexture.SetPixel(x, y, new Color(n, n, n, 1));
                    }
                }

                WorldNoiseTexture.Apply();
                mapTexture.texture = WorldNoiseTexture;
                break;
        }
    }
    public void ConstructBushNoise()
    {
        switch (noiseType)
        {
            case NOISE_TYPE.PERLIN:
                // Set new noise
                BushNoiseTexture = new Texture2D(sizeX, sizeY);
                pix = new Color[BushNoiseTexture.width * BushNoiseTexture.height];

                //seed = seed + 1;

                PerlinNoise perlin = new PerlinNoise(seed, frequency - bushFreqModifier, amp - bushAmpModifer);

                float[,] arr = new float[sizeX, sizeY];

                //Sample the 2D noise and add it into a array.
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        float fx = x / (sizeX - 1.0f);
                        float fy = y / (sizeY - 1.0f);

                        arr[x, y] = perlin.Sample2D(fx, fy);
                    }
                }

                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        float n = arr[x, y];
                        BushNoiseTexture.SetPixel(x, y, new Color(n, n, n, 1));
                    }
                }

                BushNoiseTexture.Apply();
                bushMapTexture.texture = BushNoiseTexture;
                break;
        }
    }

    public void ConstructWorldTiles() 
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Color curPixel = WorldNoiseTexture.GetPixel(x, y);
                EntityTile curTile = hexGrid[x, y].GetComponent<EntityTile>();
                GameObject curObject = hexGrid[x, y];
                float step = Random.Range(0.0f, 0.25f);
                curTile.transform.localScale = new Vector3(1, curTile.transform.localScale.y + step, 1);

                // If at ground level
                if (curPixel.grayscale == 1)
                {
                    curTile.transform.localScale = new Vector3(1, 2, 1);
                }

                if (curPixel.grayscale < WaterElevation)
                {
                    DestroyImmediate(curObject);
                    hexGrid[x, y] = null;

                    GameObject seaSand = Instantiate(Sand);
                    hexGrid[x, y] = seaSand;

                    Vector3 newPos = new Vector3(curTile.GetWorldPos().x, 0 - 0.99f, curTile.GetWorldPos().z);
                    curTile.SetTileType("Sand");
                    EntityTile newTile = seaSand.GetComponent<EntityTile>();
                    newTile.SetWorldPos(newPos);
                    newTile.SetGridPos(newPos);
                    DestroyImmediate(seaSand);
                }

                if (curPixel.grayscale >= SandLevelMin && curPixel.grayscale <= SandLevelMax)
                {
                    DestroyImmediate(curObject);
                    hexGrid[x, y] = null;

                    GameObject sand = Instantiate(Sand);
                    allTiles.Add(sand);
                    sand.transform.SetParent(World.transform);
                    hexGrid[x, y] = sand;

                    Vector3 newPos = new Vector3(curTile.GetWorldPos().x, 0 - WaterElevation, curTile.GetWorldPos().z);
                    curTile.SetTileType("Sand");
                    EntityTile newTile = sand.GetComponent<EntityTile>();
                    newTile.SetWorldPos(newPos);
                    newTile.SetGridPos(newPos);
                }

            }
        }
    }

    public void ConstructBushTiles() 
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Color curPixel = BushNoiseTexture.GetPixel(x, y);
                if (hexGrid[x, y] != null)
                {
                    EntityTile curTile = hexGrid[x, y].GetComponent<EntityTile>();
                    GameObject curObject = hexGrid[x, y];
                    float step = Random.Range(0.0f, 0.25f);
                    curTile.transform.localScale = new Vector3(1, curTile.transform.localScale.y + step, 1);

                    // If grayscale is completely white and 
                    if (curPixel.grayscale > BushThreshold) 
                    { 
                        if (curTile.GetTileType() == "Grass")
                        {
                            GameObject newBush = Instantiate(Bush);
                            EntityTile bush = newBush.GetComponent<EntityTile>();
                            bush.SetTileType("Bush");
                            bush.worldGenerator = this;
                            Bush.transform.position = new Vector3(curObject.transform.position.x, curObject.transform.position.y + 2.1f, curObject.transform.position.z);
                            newBush.transform.SetParent(World.transform);
                            allTiles.Add(newBush);
                        }
                        curTile.transform.localScale = new Vector3(1, 2, 1);
                    }
                }
            }
        }
    }

    Vector3 CalcInit() 
    {
        Vector3 initPos;

        initPos = new Vector3(-tileWidth * sizeX / 2f + tileWidth / 2, 0,
            sizeY / 2f * tileLength - tileLength / 2);

        return initPos;
    }

    //Grid to World converter
    public Vector3 calcWorldCoord(Vector2 gridPos)
    {
        //Position of the first tile
        Vector3 initPos = CalcInit();

        //Every second row is offset by half of the tile sizeX
        float offset = 0;
        if (gridPos.y % 2 != 0)
            offset = tileWidth / 2;

        float x = initPos.x + offset + gridPos.x * tileWidth;

        //Every new line is offset in z direction by 3/4 of the tileagon sizeY
        float z = initPos.z - gridPos.y * tileLength * 0.75f;

        return new Vector3(x, 0, z);
    }
}
