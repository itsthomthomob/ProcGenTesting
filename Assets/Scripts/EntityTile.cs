using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityTile : MonoBehaviour
{
    public List<GameObject> myNeighbors = new List<GameObject>();
    public bool calculatedHeight;

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

    public void SetPos(Vector3 newPos) 
    { 
        gameObject.transform.position = newPos;
    }

    public Vector3 GetPos(Vector3 newPos)
    {
        return gameObject.transform.position;
    }

}
