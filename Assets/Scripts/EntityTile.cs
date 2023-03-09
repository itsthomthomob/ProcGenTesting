using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

// Surface Types:
// - Dirt, Sand, Grass, Stone, etc.
// Vegetation Type:
// - Cactus, Bushes, Plants, etc.
// Quercus:
// - Based on the scientific name for oak, maple, and other tree types
// - Oak trees, birch trees, maple trees

public enum EntityCategory
{
    Surface, 
    Vegetation,
    Quercus
}

public enum EntityType
{
    Grass,
    Sand,
    MapleTree
}

public class EntityTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Generator worldGenerator;

    [Header("Tile Information")]
    public List<GameObject> myNeighbors = new List<GameObject>();
    [SerializeField] private string TileType;
    public bool calculatedHeight;
    public Vector3 GridPos;
    EntityCategory EntityCategory;
    EntityType EntityType;

    [Header("Interactable Vars")]
    public Material highlighted;
    Color defaultColor;
    Material[] allMaterials;
    Texture defaultTexture;
    MeshRenderer MR;

    private void Awake()
    {
        worldGenerator = FindObjectOfType<Generator>();
    }


    public void Selected()
    {
        // Get the mesh renderer
        MR = transform.GetChild(0).GetComponent<MeshRenderer>();

        // Set default texture to be the same texture as current tile
        SelectorManager SM = FindObjectOfType<SelectorManager>();
        SM.selectedMat.mainTexture = MR.material.mainTexture;

        // Clear materials and apply highlight material
        SM.previousMats = MR.materials;
        MR.materials = new Material[0];
        MR.material = SM.selectedMat;
    }

    public void Deselected()
    {
        // Set back to old materials
        SelectorManager SM = FindObjectOfType<SelectorManager>();
        MR.materials = SM.previousMats;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Selected();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        Deselected();
    }

    public void SetEntityType(EntityType entityType)
    {
        EntityType = entityType;
    }
    public EntityType GetEntityType()
    {
        return EntityType;
    }
    public void SetEntityCategory(EntityCategory type)
    {
        EntityCategory = type;
    }
    public EntityCategory GetEntityCategory()
    {
        return EntityCategory;
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
