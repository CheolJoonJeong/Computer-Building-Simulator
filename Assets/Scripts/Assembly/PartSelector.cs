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
        if (IsAssembled())
        {
            if (CableOverlapChecker.Instance != null && CableOverlapChecker.Instance.IsBlocked
                && !CableOverlapChecker.Instance.IsConflictPart(targetPart))
                return;

            if (HasConnectedCable())
            {
                CableOverlapChecker.Instance?.ShowTransientMessage(
                    $"'{targetPart.name}' has connected cables.\nDisconnect the cables first.");
                return;
            }

            Detach();
            return;
        }

        if (CableOverlapChecker.Instance != null && CableOverlapChecker.Instance.IsBlocked)
            return;

        if (PartSelectionManager.SelectedButton == gameObject)
        {
            PartSelectionManager.Clear();
            return;
        }

        PartSelectionManager.Clear();

        if (targetSlot != null)
        {
            // 콜라이더가 슬롯 본체가 아닌 자식에 있는 경우도 처리
            foreach (Collider col in targetSlot.GetComponentsInChildren<Collider>(true))
                col.enabled = true;

            foreach (Renderer r in targetSlot.GetComponentsInChildren<Renderer>())
                r.enabled = true;
        }

        PartSelectionManager.SelectedPart = targetPart;
        PartSelectionManager.SelectedSlot = targetSlot;
        PartSelectionManager.SelectedButton = gameObject;
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
        CableOverlapChecker.Instance?.OnPartDetached(targetPart);
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

    bool HasConnectedCable()
    {
        if (targetPart == null) return false;
        foreach (CableSocket socket in targetPart.GetComponentsInChildren<CableSocket>(true))
            if (socket.IsOccupied) return true;
        return false;
    }

    bool IsAssembled()
    {
        Image img = GetComponent<Image>();
        return img != null && img.color != Color.white;
    }
}
