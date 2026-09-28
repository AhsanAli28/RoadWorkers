using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickHandler : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            //To close the current popup when clicking elsewhere
            if (!UIManager.Instance.IsMouseOverPopup())
                UIManager.Instance.CloseCurrentPopup();

            if (IsPointerOverUI()) return; // Prevent clicks when over UI

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Interactable interactable = hit.collider.GetComponent<Interactable>();
                if (interactable != null)
                {
                    interactable.Interact(); // Call OnClick if it's a building
                }
            }

                  
        }
    }

    bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }
}
