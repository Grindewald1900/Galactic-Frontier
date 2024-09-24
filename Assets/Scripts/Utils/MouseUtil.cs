using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MouseUtil
{
    public static bool IsPointerOverUIElement(GameObject uiElement, Canvas canvas)
    {
        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogError("Canvas doesn't have a GraphicRaycaster component!");
            return false;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("No active EventSystem found!");
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == uiElement)
            {
                return true;
            }
        }

        return false;
    }
}
