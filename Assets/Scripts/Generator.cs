using Broccoli.Factory;
using Broccoli.Generator;
using Broccoli.Pipe;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
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

public enum NOISE_TYPE_I { PERLIN, VALUE, SIMPLEX, VORONOI, WORLEY }

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
    public NOISE_TYPE_I noiseType;
    public RawImage mapTexture;
    public RawImage TreeMapTexture;
    private int seed = 0;
    public float frequency = 1.0f;
    public float amp = 1.0f;
    public float TreeAmpModifer;
    public float TreeFreqModifier;
    public float TreeThreshold;
    public float treePlacementThreshold;

    [Header("Tree Parameters")]
    [Header("Structure")]
    public int maxFrequencyUL;
    public int maxFrequencyLL;

    public int minFrequencyLL;
    public int minFrequencyUL;

    public bool randomizeTwirlOffsetParam;

    public float lengthAtTopUL;
    public float lengthAtTopLL;

    public float girthScaleUL;
    public float girthScaleLL;

    public float rangeUL;
    public float rangeLL;


    // BASE TREE PARAMETERS
    // --------------------
    // STRUCTURE
    // maxFrequency 3
    // minFrequency 2
    // Probability = 1
    // Distribution = Alternative
    // Distribution Spacing = 0.98
    // Distribution Curve ?
    // Randomize Twirl Offset = True
    // Twirl = 0.17/0.17
    // Length at Top = 1.31/2.07
    // Length at Curve ?
    // Girth Scale = 0.86/0.86
    // Use FixedSeed = false
    // ---------------------
    // ALIGNMENT
    // Parallel Align at Top = 0.94/0.94
    // Parallel Align at Base 0.41/0.41
    // Parallel Align Curve = ?
    // Gravity Algin at Top = 0.04 / 0.23
    // Gravity Align at Base = 0.02 / 0.30
    // Gravity Align Curve = ?
    // Horizontal Align at Top = 0.4/0.4
    // Horizontal Align at Base = 0.4/0.4
    // Horizontal Align curve = ?
    // ---------------------
    // RANGE
    // Range = 0.58/0.79
    // Mask 0/1

    [Header("Entity Pipelines")]
    TreeFactory treeFactory;
    Pipeline mapleTreePipeline;
    public static string mapleTreePipelinePath = "MapleTreePipeline";

    Texture2D WorldNoiseTexture;
    Texture2D TreeNoiseTexture;
    private Color[] pix;

    [Header("Tiles")]
    public GameObject Grass;
    public GameObject Water;
    public GameObject Sand;
    public GameObject Tree;
    public GameObject World;
    public Dictionary<Vector3Int, EntityTile> tilePositions;
    public GameObject[,] hexGrid = new GameObject[0,0];
    public List<GameObject> allTiles = new List<GameObject>();

    public void GenerateWorld() 
    {
        
        // Generate the noise texture
        hexGrid = new GameObject[sizeX, sizeY];
        tilePositions = new Dictionary<Vector3Int, EntityTile>();

        // Initialize tree factory, load pipelines, etc.
        // - The current tree factory is a maple tree pipeline variant
        // - Might have to make prefabs or instances for different vegetations
        treeFactory = FindObjectOfType<TreeFactory>();
        mapleTreePipeline = treeFactory.localPipeline;
        Debug.Log("Successfully loaded tree pipelines.");

        // Generates the noise based on input params
        ConstructWorldNoise();
        Debug.Log("Generated world noise map.");

        // Generates a noise from ConstructWorldNoise for different Tree groups
        ConstructTreeNoise();
        Debug.Log("Generated tree noise map.");

        // Create the base tiles of the world, to be replaced
        ConstructBaseTiles();
        Debug.Log("Generated world noise map.");

        // Replace the base tiles in the world with grass, sand, etc.
        ConstructWorldTiles();
        Debug.Log("Replaced world tiles with correct tiles.");


        // Spawn the trees from the tree noise map
        ConstructTreeTiles();
        Debug.Log("Generated trees.");

    }

    private void ConstructBaseTiles()
    {
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
                EntityTile curTile = tile.GetComponentInChildren<EntityTile>();
                curTile.SetEntityCategory(EntityCategory.Surface);
                curTile.SetGridPos(calcWorldCoord(gridPos));
                curTile.SetWorldPos(calcWorldCoord(gridPos));
                tile.transform.parent = World.transform;
                allTiles.Add(tile);
            }
        }
    }

    /// <summary>
    /// Implements the Perlin Noise method from Scrawk's example.
    /// </summary>
    public void ConstructWorldNoise() 
    {
        switch (noiseType)
        {
            case NOISE_TYPE_I.PERLIN:
                // Set new noise
                WorldNoiseTexture = new Texture2D(sizeX, sizeY);
                pix = new Color[WorldNoiseTexture.width * WorldNoiseTexture.height];

                seed = UnityEngine.Random.Range(0, 1000);

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
    public void ConstructTreeNoise()
    {
        switch (noiseType)
        {
            case NOISE_TYPE_I.PERLIN:
                // Set new noise
                TreeNoiseTexture = new Texture2D(sizeX, sizeY);
                pix = new Color[TreeNoiseTexture.width * TreeNoiseTexture.height];

                PerlinNoise perlin = new PerlinNoise(seed, frequency - TreeFreqModifier, amp - TreeAmpModifer);

                float[,] arr = new float[sizeX, sizeY];

                //Sample the 2D noise and add it into a array.
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        float fx = x / (sizeX - 1.0f);
                        float fy = y / (sizeY - 1.0f);

                        arr[x, y] = perlin.Sample2D(fx, fy);
                        float n = arr[x, y];
                        TreeNoiseTexture.SetPixel(x, y, new Color(n, n, n, 0.8f));
                    }
                }

                TreeNoiseTexture.Apply();
                TreeMapTexture.texture = TreeNoiseTexture;
            break;
        }
    }

    /// <summary>
    /// Instantiates all grass, stone, and sand tiles
    /// </summary>
    public void ConstructWorldTiles() 
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Color curPixel = WorldNoiseTexture.GetPixel(x, y);
                EntityTile curTile = hexGrid[x, y].GetComponentInChildren<EntityTile>();
                GameObject curObject = hexGrid[x, y];
                float step = UnityEngine.Random.Range(0.0f, 0.25f);
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
                    curTile.SetEntityType(EntityType.Sand);
                    EntityTile newTile = seaSand.GetComponentInChildren<EntityTile>();
                    newTile.SetWorldPos(newPos);
                    newTile.SetGridPos(newPos);
                    DestroyImmediate(seaSand);
                }

                // Check if the current pixel meets sand conditions
                if (curPixel.grayscale >= SandLevelMin && curPixel.grayscale <= SandLevelMax)
                {
                    // Destroy the grass tile
                    DestroyImmediate(curObject);
                    hexGrid[x, y] = null;

                    // Replace with the sand tile
                    GameObject sand = Instantiate(Sand);
                    allTiles.Add(sand);
                    sand.transform.SetParent(World.transform);
                    hexGrid[x, y] = sand;

                    // Set the position of the sand tile
                    Vector3 newPos = new Vector3(curTile.GetWorldPos().x, 0 - WaterElevation, curTile.GetWorldPos().z);
                    curTile.SetEntityCategory(EntityCategory.Surface);
                    EntityTile newTile = sand.GetComponentInChildren<EntityTile>();
                    newTile.SetWorldPos(newPos);
                    newTile.SetGridPos(newPos);
                }

            }
        }
    }

    /// <summary>
    /// TODO: Instantiate all vegetation types
    /// </summary>
    public void ConstructTreeTiles() 
    {
        // Reads each pixel of the Tree nosie texture
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                // The current pixel of the tree noise map
                Color curPixel = TreeNoiseTexture.GetPixel(x, y);

                float placeTree = UnityEngine.Random.Range(0f, 1f);

                // If the x, y of hexGrid has a tile 
                if (hexGrid[x, y] != null && placeTree > treePlacementThreshold)
                {
                    // Get the entity tile
                    EntityTile curTile = hexGrid[x, y].GetComponentInChildren<EntityTile>();
                    GameObject curObject = hexGrid[x, y];
                    float step = UnityEngine.Random.Range(0.0f, 0.25f);
                    curTile.transform.localScale = new Vector3(1, curTile.transform.localScale.y + step, 1);

                    // If grayscale is completely white and below tree threshold
                    if (curPixel.grayscale > TreeThreshold) 
                    { 
                        // Trees spawn on grass types
                        if (curTile.GetEntityCategory() == EntityCategory.Surface && curTile.GetEntityType() == EntityType.Grass)
                        {

                            // BASE TREE PARAMETERS
                            // --------------------
                            // STRUCTURE
                            // maxFrequency 3
                            // minFrequency 2
                            // Probability = 1
                            // Distribution = Alternative
                            // Distribution Spacing = 0.98
                            // Distribution Curve ?
                            // Randomize Twirl Offset = True
                            // Twirl = 0.17/0.17
                            // Length at Top = 1.31/2.07
                            // Length at Curve ?
                            // Girth Scale = 0.86/0.86
                            // Use FixedSeed = false
                            // ---------------------
                            // ALIGNMENT
                            // Parallel Align at Top = 0.94/0.94
                            // Parallel Align at Base 0.41/0.41
                            // Parallel Align Curve = ?
                            // Gravity Algin at Top = 0.04 / 0.23
                            // Gravity Align at Base = 0.02 / 0.30
                            // Gravity Align Curve = ?
                            // Horizontal Align at Top = 0.4/0.4
                            // Horizontal Align at Base = 0.4/0.4
                            // Horizontal Align curve = ?
                            // ---------------------
                            // RANGE
                            // Range = 0.58/0.79
                            // Mask 0/1


                            //// Set new pipeline details for each tree, so each tree is unique and procedural
                            //PipelineElement structureElement = mapleTreePipeline.GetElement(PipelineElement.ClassType.StructureGenerator);

                            //StructureGeneratorElement structure = (StructureGeneratorElement)mapleTreePipeline.GetElement(PipelineElement.ClassType.StructureGenerator);
                            ////PositionerElement positioner = (PositionerElement)mapleTreePipeline.GetElement(PipelineElement.ClassType.Positioner);
                            //List<StructureGenerator.StructureLevel> levels = structure.structureLevels;

                            //// For each level in the pipeline
                            //// - if it's not a sprout (has to be branch then)
                            //// - change parameters accordingly
                            //foreach (StructureGenerator.StructureLevel level in levels)
                            //{
                            //    if (level.isSprout == false)
                            //    {
                            //        // Set parameters of the tree to a random value between defined params above
                            //        level.maxFrequency = UnityEngine.Random.Range(maxFrequencyLL, maxFrequencyUL);
                            //        level.minFrequency = UnityEngine.Random.Range(minFrequencyLL, minFrequencyUL);
                            //        level.probability = 1.0f;
                            //        level.randomTwirlOffsetEnabled = true;
                            //        level.lengthAtTop = UnityEngine.Random.Range(lengthAtTopLL, lengthAtTopUL);
                            //        level.maxGirthScale = UnityEngine.Random.Range(girthScaleLL, girthScaleUL);
                            //        level.minGirthScale = UnityEngine.Random.Range(girthScaleLL, girthScaleUL);
                            //        level.maxRange = UnityEngine.Random.Range(rangeLL, rangeUL);

                            //        level.distributionCurve = AnimationCurve.Linear(0.1f, 0.1f, 1f, 1f);
                            //        level.lengthCurve = AnimationCurve.Linear(0.1f, 0.1f, 1f, 1f);
                            //        level.parallelAlignCurve = AnimationCurve.Linear(0.1f, 0f, 1f, 1f);
                            //        level.gravityAlignCurve = AnimationCurve.Linear(0.1f, 0.1f, 1f, 1f);
                            //        level.horizontalAlignCurve = AnimationCurve.Linear(0.1f, 0.1f, 1f, 1f);
                            //    }
                            //}

                            // Create the actual tree
                            // TODO: Contact BTC owner about beizer error
                            GameObject newTree = treeFactory.Spawn();

                            // Guarantee each tree has an EntityTile component
                            newTree.TryGetComponent(out EntityTile ET);
                            if (ET == null)
                            {
                                ET = newTree.AddComponent<EntityTile>();
                            }

                            // Set EntityTile details
                            // TODO: Maybe create a "build" script for each entity type?
                            ET.SetEntityCategory(EntityCategory.Quercus);
                            ET.SetEntityType(EntityType.MapleTree);
                            ET.worldGenerator = this;

                            // Set position of tree
                            Vector3 pos = new Vector3(
                                curObject.transform.position.x,
                                curObject.transform.position.y + 1.25f,
                                curObject.transform.position.z);

                            newTree.transform.position = pos;

                            // Add position to positioner so tree orientation is correct?
                            //positioner.positions.Add(new Position(Vector3.zero, pos.normalized, true));

                            // Set parent
                            ET.transform.SetParent(World.transform);

                            // Add tree to all tiles
                            allTiles.Add(newTree);


                        }

                        // Adjust local position
                        curTile.transform.localScale = new Vector3(1, 1, 1);
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
