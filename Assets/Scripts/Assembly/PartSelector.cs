using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartSelector : MonoBehaviour
{
    public GameObject targetPart;
    public GameObject targetSlot;
    public bool startAssembled = false;

    void Start()
    {
        if (startAssembled)
            SetAssembled();
    }

    public void SelectPart()
    {
        // 조립된 상태면 해체
        if (IsAssembled())
        {
            Detach();
            return;
        }

        if (PartSelectionManager.SelectedButton == gameObject)
        {
            PartSelectionManager.Clear();
            Debug.Log("선택 해제됨");
            return;
        }

        PartSelectionManager.Clear();

        if (targetSlot != null)
        {
            Collider col = targetSlot.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            foreach (Renderer r in targetSlot.GetComponentsInChildren<Renderer>())
                r.enabled = true;
        }

        PartSelectionManager.SelectedPart = targetPart;
        PartSelectionManager.SelectedSlot = targetSlot;
        PartSelectionManager.SelectedButton = gameObject;
        Debug.Log(targetPart.name + " 선택됨!");
    }

    void Detach()
    {
        if (targetPart == null) return;

        if (targetSlot != null)
        {
            SnapZone snapZone = targetSlot.GetComponent<SnapZone>();
            if (snapZone != null)
                snapZone.ForceDetach();
        }

        targetPart.transform.SetParent(null);
        targetPart.SetActive(false);

        Collider[] cols = targetPart.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in cols)
            col.enabled = true;

        SetUnassembled();
        Debug.Log(targetPart.name + " 해체됨!");
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