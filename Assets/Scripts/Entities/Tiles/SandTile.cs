using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SandTile : EntityTile
{
    public string name;

    private void Start()
    {
        SetEntityCategory(EntityCategory.Surface);
        SetEntityType(EntityType.Grass);
        
        name = "Sand";

        //BuildTile();
    }

    public void BuildTile()
    {
        // Get current materials
        MeshRenderer MR = GetComponentInChildren<MeshRenderer>();

        // Hexagonal sides
        // 1001 = top
        // 1002 - 1007 = tides

        // Find new materials to replace EmptyTile's materials
        Material[] loadedMaterials = new Material[MR.materials.Length];
        loadedMaterials[MR.materials.Length - 1] = Resources.Load<Material>("Entities/Tiles/" + name + "/1001/Mat_1001");

        // Skip the top 
        for (int i = 0; i < MR.materials.Length - 1; i++)
        {
            loadedMaterials[i] = Resources.Load<Material>("Entities/Tiles/" + name + "/1002" + "/Mat_1002");
        }

        MR.materials = loadedMaterials;
    }
}
