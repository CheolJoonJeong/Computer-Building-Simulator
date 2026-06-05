using UnityEngine;

public class Draggable : MonoBehaviour
{
    public GameObject targetSlot;
    private static Draggable selectedPart = null;  // ���� ���õ� ��ǰ

    void OnMouseDown()
    {
        // �ٸ� ��ǰ�� ���õǾ� �־����� ���� ����
        if (selectedPart != null && selectedPart != this)
            selectedPart.SetRenderers(false);

        // �� ��ǰ ����
        selectedPart = this;
        if (targetSlot != null)
            SetRenderers(true);

        Debug.Log(gameObject.name + " ���õ�!");
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