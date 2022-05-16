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
        // Clear and destroy previous grid
        for (int i = 0; i < allTiles.Count; i++)
        {
            DestroyImmediate(allTiles[i]);
        }

        allTiles.Clear();

        // Create new grid
        for (float y = 0; y < sizeY; y++)
        {
            for (float x = 0; x < sizeX; x++)
            {
                //GameObject assigned to Hex public variable is cloned
                GameObject tile = Instantiate(Grass);

                //Current position in grid
                Vector2 gridPos = new Vector2(x, y);
                tile.transform.position = calcWorldCoord(gridPos);
                tile.transform.parent = World.transform;
                allTiles.Add(tile);
            }
        }

        // Randomize and Interpolate Height
        // random tile
        float randomHeight = Random.Range(0.0f, 1.0f);
        int index = Random.Range(0, allTiles.Count);
        GameObject highestPoint = allTiles[index];
        
        // Set height
        if (TryGetComponent(out EntityTile curTile))
        {
            SetHeight(curTile);
        }
        
    }

    public void SetHeight(EntityTile curTile) 
    {
        if (curTile.calculatedHeight == false)
        {
            Vector3 newPos = new Vector3(curTile.gameObject.transform.position.x, curTile.gameObject.transform.position.y, curTile.gameObject.transform.position.z - 0.1f);
            curTile.SetPos(newPos);
            curTile.calculatedHeight = true;
        }
        else 
        {
            // do next tile
            for (int i = 0; i < curTile.GetNeighbors().Count; i++)
            {
                if (curTile.GetNeighbors()[i].gameObject.TryGetComponent<EntityTile>(out EntityTile newTile)) 
                {
                    if (newTile.calculatedHeight == false)
                    {
                        SetHeight(newTile);
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
        //Position of the first tile tile
        Vector3 initPos = CalcInit();

        //Every second row is offset by half of the tile width
        float offset = 0;
        if (gridPos.y % 2 != 0)
            offset = tileWidth / 2;

        float x = initPos.x + offset + gridPos.x * tileWidth;

        //Every new line is offset in z direction by 3/4 of the tileagon height
        float z = initPos.z - gridPos.y * tileLength * 0.75f;

        return new Vector3(x, 0, z);
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
