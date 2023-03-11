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
    string name;

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

    //public void BuildTile()
    //{
    //    // Hexagonal sides
    //    // 1001 = top
    //    // 1002 - 1007 = tides

    //    // Get current materials
    //    MeshRenderer MR = GetComponentInChildren<MeshRenderer>();

    //    // Find new materials to replace EmptyTile's materials
    //    Material[] loadedMaterials = new Material[MR.materials.Length];
    //    Material topMat = Resources.Load<Material>("Entities/Tiles/" + name + "/1001/Mat_1001");
    //    loadedMaterials[0] = topMat;

    //    for (int i = 1; i < loadedMaterials.Length; i++)
    //    {
    //        loadedMaterials[i] = Resources.Load<Material>("Entities/Tiles/" + name + "/100" + i + "/Mat_100" + i);
    //    }

    //    // Update and set the new materials
    //    MR.materials = loadedMaterials;
    //}

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

    public void SetWorldPos(Vector3 newPos, float heightOffset) 
    { 
        gameObject.transform.position = new Vector3(newPos.x, newPos.y + heightOffset, newPos.z);
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
