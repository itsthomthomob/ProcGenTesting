using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityTile : MonoBehaviour
{
    public List<GameObject> myNeighbors = new List<GameObject>();
    public bool calculatedHeight;
    public Vector3 GridPos;

    private void OnCollisionEnter(Collision collision)
    {
        if (!myNeighbors.Contains(collision.gameObject)) 
        { 
            myNeighbors.Add(collision.gameObject);
        }
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
