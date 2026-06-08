using UnityEngine;

public class SnapZone : MonoBehaviour
{
    public PartType acceptType;
    [Tooltip("RAM 슬롯인 경우의 슬롯 번호 (1~4). 평가 시 RAM 장착 조합(2,4 / 1,3 / 1,2,3,4) 검증에 사용")]
    public int ramSlotIndex = 0;
    public bool isOccupied = false;
    private GameObject snappedPart = null;

    public bool startOccupied = false;
    public GameObject startPart;

    void Start()
    {
        bool playerDetached = AssemblyProgress.DetachedSlots.Contains(gameObject.name);

        if (startOccupied && startPart != null && !playerDetached)
        {
            startPart.SetActive(true);
            startPart.transform.position = transform.position;
            startPart.transform.rotation = transform.rotation;
            startPart.transform.SetParent(transform.parent);

            // 콜라이더 유지, 레이어만 변경 (클릭은 막고 케이블 등 물리 충돌은 유지)
            // — TrySnap()의 장착 처리와 동일하게 맞춤
            SetLayerRecursively(startPart, LayerMask.NameToLayer("AssembledPart"));

            snappedPart = startPart;
            isOccupied = true;
            return;
        }

        // 처음부터 장착된 슬롯이지만 플레이어가 분리한 적이 있으면 분리 상태 유지
        if (startOccupied && startPart != null && playerDetached)
        {
            startPart.transform.SetParent(null);
            SetLayerRecursively(startPart, LayerMask.NameToLayer("Default"));
            startPart.SetActive(false);

            foreach (PartSelector ps in FindObjectsOfType<PartSelector>(true))
                if (ps.targetPart == startPart) { ps.SetUnassembled(); break; }

            snappedPart = null;
            isOccupied = false;
            return;
        }

        // 뷰어 등 다른 씬에 갔다 돌아온 경우 — 이전에 장착했던 슬롯이면 복원
        if (AssemblyProgress.SnappedSlots.Contains(gameObject.name))
            RestoreFromProgress();
    }

    // 이 슬롯에 연결된 PartSelector의 targetPart를 찾아 즉시 장착 (체크 없이)
    void RestoreFromProgress()
    {
        foreach (PartSelector ps in FindObjectsOfType<PartSelector>(true))
        {
            if (ps.targetSlot == gameObject && ps.targetPart != null)
            {
                GameObject part = ps.targetPart;
                part.SetActive(true);
                part.transform.position = transform.position;
                part.transform.rotation = transform.rotation;
                part.transform.SetParent(transform.parent);
                SetLayerRecursively(part, LayerMask.NameToLayer("AssembledPart"));

                ps.SetAssembled();

                snappedPart = part;
                isOccupied = true;
                return;
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int mask = ~LayerMask.GetMask("Cable", "Ignore Raycast");
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
            {
                if (hit.collider.gameObject == gameObject ||
                    hit.collider.transform.IsChildOf(transform))
                    TrySnap();
            }
        }

        if (isOccupied && snappedPart != null && Input.GetKeyDown(KeyCode.R))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.IsChildOf(snappedPart.transform)
                    || hit.transform == snappedPart.transform)
                {
                    snappedPart.transform.SetParent(null);
                    SetLayerRecursively(snappedPart, LayerMask.NameToLayer("Default"));
                    snappedPart.SetActive(false);

                    foreach (PartSelector ps in FindObjectsOfType<PartSelector>(true))
                    {
                        if (ps.targetPart == snappedPart)
                        {
                            ps.SetUnassembled();
                            break;
                        }
                    }

                    snappedPart = null;
                    isOccupied = false;
                    AssemblyProgress.SnappedSlots.Remove(gameObject.name);
                    AssemblyProgress.DetachedSlots.Add(gameObject.name);
                    Debug.Log("Detached!");
                }
            }
        }
    }

    void TrySnap()
    {
        if (CableOverlapChecker.Instance != null && CableOverlapChecker.Instance.IsBlocked)
            return;

        if (isOccupied)
        {
            Debug.Log("Already occupied");
            return;
        }

        GameObject selectedPart = PartSelectionManager.SelectedPart;
        if (selectedPart == null)
        {
            Debug.Log("No part selected");
            return;
        }

        PartInfo part = selectedPart.GetComponent<PartInfo>();
        if (part == null)
        {
            Debug.Log($"No PartInfo on '{selectedPart.name}'");
            return;
        }

        if (part.data == null)
        {
            Debug.LogError($"PartInfo on '{selectedPart.name}' has no PartData (data == null)");
            return;
        }

        if (part.data.partType != acceptType)
        {
            Debug.Log($"Type mismatch: selected part '{selectedPart.name}' " +
                      $"(partType={part.data.partType}, data='{part.data.name}') " +
                      $"!= slot '{gameObject.name}' acceptType={acceptType}");
            return;
        }

        selectedPart.SetActive(true);
        selectedPart.transform.position = transform.position;
        selectedPart.transform.rotation = transform.rotation;
        selectedPart.transform.SetParent(transform.parent);

        // 콜라이더 유지, 레이어만 변경 (클릭은 막고 물리 충돌 유지)
        SetLayerRecursively(selectedPart, LayerMask.NameToLayer("AssembledPart"));

        if (PartSelectionManager.SelectedButton != null)
            PartSelectionManager.SelectedButton.GetComponent<PartSelector>()?.SetAssembled();

        snappedPart = selectedPart;
        isOccupied = true;
        AssemblyProgress.SnappedSlots.Add(gameObject.name);
        AssemblyProgress.DetachedSlots.Remove(gameObject.name);
        PartSelectionManager.Clear();
        Debug.Log("Assembled!");

        CableOverlapChecker.Instance?.RunCheckForPart(snappedPart);
        AssemblyCompletionChecker.Instance?.CheckCompletion();
    }

    public void ForceDetach()
    {
        snappedPart = null;
        isOccupied = false;
        AssemblyProgress.SnappedSlots.Remove(gameObject.name);
        AssemblyProgress.DetachedSlots.Add(gameObject.name);
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj.GetComponent<SnapZone>() != null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
