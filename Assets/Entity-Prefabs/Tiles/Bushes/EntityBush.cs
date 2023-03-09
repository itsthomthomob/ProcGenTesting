using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityBush : EntityTile
{
    private void Awake()
    {
        worldGenerator = FindObjectOfType<Generator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<EntityTile>(out EntityTile curCollider))
        {
            if (curCollider == null)
            {
                // No grass tile below
                if (worldGenerator.allTiles.Contains(gameObject))
                {
                    worldGenerator.allTiles.Remove(gameObject);
                    DestroyImmediate(gameObject);
                    Debug.Log("Destroyed invalid bush");
                }
            }
        }
    }
}
