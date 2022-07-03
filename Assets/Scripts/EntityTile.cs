using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityTile : MonoBehaviour
{
    public Generator worldGenerator;

    public List<GameObject> myNeighbors = new List<GameObject>();
    [SerializeField]private string TileType;
    public bool calculatedHeight;
    public Vector3 GridPos;

    private void Awake()
    {
        worldGenerator = FindObjectOfType<Generator>();
    }

    public string GetTileType() 
    {
        return TileType;
    }

    public void SetTileType(string newType) 
    {
        TileType = newType;
    }



    public List<GameObject> GetNeighbors() 
    {
        return myNeighbors;
    }

    public void SetWorldPos(Vector3 newPos) 
    { 
        gameObject.transform.position = newPos;
    }

    public Vector3 GetWorldPos(Vector3 newPos)
    {
        return gameObject.transform.position;
    }

    public void SetGridPos(Vector3 newPos)
    {
        GridPos = newPos;
    }

    public Vector3 GetWorldPos()
    {
        return GridPos;
    }

}
