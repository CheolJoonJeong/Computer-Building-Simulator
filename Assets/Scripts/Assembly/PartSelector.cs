using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartSelector : MonoBehaviour
{
    [Tooltip("장착할 파츠 '프리팹' (씬 오브젝트 아님)")]
    public GameObject targetPart;       // 프리팹
    public GameObject targetSlot;       // 씬의 슬롯
    public bool startAssembled = false;

    void Start()
    {
        if (startAssembled)
            SetAssembled();
    }

    public void SelectPart()
    {
        if (IsAssembled())
        {
            Detach();
            return;
        }

        if (PartSelectionManager.SelectedButton == gameObject)
        {
            PartSelectionManager.Clear();
            return;
        }

        PartSelectionManager.Clear();

        // 슬롯 고스트 표시
        if (targetSlot != null)
        {
            Collider col = targetSlot.GetComponent<Collider>();
            if (col != null) col.enabled = true;
            foreach (Renderer r in targetSlot.GetComponentsInChildren<Renderer>())
                r.enabled = true;
        }

        PartSelectionManager.SelectedPart = targetPart;   // 프리팹 전달
        PartSelectionManager.SelectedSlot = targetSlot;
        PartSelectionManager.SelectedButton = gameObject;
    }

    void Detach()
    {
        if (targetSlot != null)
        {
            SnapZone sz = targetSlot.GetComponent<SnapZone>();
            if (sz != null) sz.DetachPart();   // 인스턴스 파괴
        }
        SetUnassembled();
    }

    public void SetAssembled()
    {
        Image img = GetComponent<Image>();
        if (img != null) img.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>())
            t.color = new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    public void SetUnassembled()
    {
        Image img = GetComponent<Image>();
        if (img != null) img.color = Color.white;
        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>())
            t.color = Color.black;
    }

    bool IsAssembled()
    {
        Image img = GetComponent<Image>();
        return img != null && img.color != Color.white;
    }
}
