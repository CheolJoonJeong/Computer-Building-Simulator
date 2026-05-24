using UnityEngine;

public class PartSelectionManager : MonoBehaviour
{
    public static GameObject SelectedPart;
    public static GameObject SelectedSlot;
    public static GameObject SelectedButton;

    public static void Clear()
    {
        // ¸ðµç ½½·Ô Collider ²ô±â + ·»´õ·¯ ²ô±â
        foreach (SnapZone slot in FindObjectsOfType<SnapZone>(true))
        {
            Collider col = slot.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            foreach (Renderer r in slot.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        SelectedPart = null;
        SelectedSlot = null;
        SelectedButton = null;
    }
}