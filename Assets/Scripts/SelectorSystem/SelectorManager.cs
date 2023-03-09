using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorManager : MonoBehaviour
{

    [Header("Selector Properties")]
    public Material selectedMat;
    public Material[] previousMats;
    EntityTile selectedET;

    private void Update()
    {
        OnSelect();
    }

    private void OnSelect()
    {
        if(Input.GetMouseButtonDown(0))
        {
            // Cast a ray from the mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check if the ray hits a gameobject
            if (Physics.Raycast(ray, out hit))
            {

                Debug.Log("GameObject Hit: " + hit.transform.gameObject.name);
                
                // Call the OnSelect function on the gameobject that was hit
                if(hit.transform.parent != null)
                {
                    GameObject entityGO = hit.transform.parent.gameObject;
                    entityGO.TryGetComponent(out EntityTile selectedEntity);

                    // Deselect previously selected entity
                    if(selectedET != selectedEntity && selectedET != null)
                    {
                        selectedET.Deselected();
                    }

                    // Select new entity
                    selectedET = selectedEntity;
                    selectedEntity.Selected();
                }


            }
        }
    }

}
