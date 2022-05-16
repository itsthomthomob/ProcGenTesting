using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// Thank you to TBS Development for sharing his methods on hexagonal terrain generation.
/// Link here: https://tbswithunity3d.wordpress.com/about/
/// 
/// This is mostly a playground project so I can mess with terrain generation and editor scripts.
/// 
/// Modification Log
/// 
/// 05-12: Initialization of the project. Implemented basic hexagonal grid generation with hex calculations and editor controls
/// 
/// </summary>

[ExecuteInEditMode]
public class Generator : MonoBehaviour
{
    [Header("Generator Controls")]
    public float sizeX;
    public float sizeY;
    public float tileWidth = 1.0f;
    public float tileLength = 1.0f;

    [Header("Tiles")]
    public GameObject Grass;
    public GameObject Dirt;
    public GameObject Water;
    public GameObject World;
    public List<GameObject> allTiles = new List<GameObject>();

    public void GenerateWorld() 
    {
        for (int i = 0; i < allTiles.Count; i++)
        {
            DestroyImmediate(allTiles[i]);
        }
        allTiles.Clear();

        for (float y = 0; y < sizeY; y++)
        {
            for (float x = 0; x < sizeX; x++)
            {
                //GameObject assigned to Hex public variable is cloned
                GameObject tile = (GameObject)Instantiate(Grass);

                //Current position in grid
                Vector2 gridPos = new Vector2(x, y);
                tile.transform.position = calcWorldCoord(gridPos);
                //tile.transform.position = new Vector3(tile.transform.position.x, 0, tile.transform.position.y);
                tile.transform.parent = World.transform;
                allTiles.Add(tile);
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
        //Position of the first tile tile
        Vector3 initPos = CalcInit();
        //Every second row is offset by half of the tile width
        float offset = 0;
        if (gridPos.y % 2 != 0)
            offset = tileWidth / 2;

        float x = initPos.x + offset + gridPos.x * tileWidth;
        //Every new line is offset in z direction by 3/4 of the tileagon height
        float z = initPos.z - gridPos.y * tileLength * 0.75f;

        float randomHeight = Random.Range(0.0f, 1.0f);

        return new Vector3(x, randomHeight, z);
    }

    // Simple X-Y grid
    public void TestGrid() 
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                Vector3 newPos = new Vector3(x, 0, y);
                GameObject newTile = Instantiate(Grass, newPos, Quaternion.identity);
                newTile.transform.SetParent(World.transform);
                allTiles.Add(newTile);
            }
        }
    }
}
