using UnityEngine;

public class Draggable : MonoBehaviour
{
    public GameObject targetSlot;
    private static Draggable selectedPart = null;  // 현재 선택된 부품

    void OnMouseDown()
    {
        // 다른 부품이 선택되어 있었으면 슬롯 끄기
        if (selectedPart != null && selectedPart != this)
            selectedPart.SetRenderers(false);

        // 이 부품 선택
        selectedPart = this;
        if (targetSlot != null)
            SetRenderers(true);

        Debug.Log(gameObject.name + " 선택됨!");
    }

    public void SetRenderers(bool show)
    {
        if (targetSlot == null) return;
        foreach (Renderer r in targetSlot.GetComponentsInChildren<Renderer>())
        {
            r.enabled = show;
        }
    }

    public static Draggable GetSelected()
    {
        return selectedPart;
    }

    public static void ClearSelection()
    {
        if (selectedPart != null)
            selectedPart.SetRenderers(false);
        selectedPart = null;
    }
}