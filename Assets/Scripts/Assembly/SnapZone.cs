using UnityEngine;

public class SnapZone : MonoBehaviour
{
    public PartType acceptType;
    public bool isOccupied = false;
    private GameObject snappedPart = null;       // 생성된 인스턴스
    private PartSelector ownerSelector = null;   // 이 슬롯을 채운 버튼

    [Header("시작부터 장착")]
    public bool startOccupied = false;
    public GameObject startPart;                 // 프리팹

    void Start()
    {
        if (startOccupied && startPart != null)
        {
            var instance = Instantiate(startPart, transform.position, transform.rotation, transform.parent);
            SetLayerRecursively(instance, LayerMask.NameToLayer("AssembledPart"));
            snappedPart = instance;
            isOccupied = true;
        }
    }

    void Update()
    {
        // 슬롯 클릭 → 장착
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int mask = ~LayerMask.GetMask("Cable", "Ignore Raycast");
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mask))
            {
                if (hit.collider.gameObject == gameObject ||
                    hit.collider.transform.IsChildOf(transform))
                    TrySnap();
            }
        }

        // R키 → 분리 (장착된 파츠 위에서)
        if (isOccupied && snappedPart != null && Input.GetKeyDown(KeyCode.R))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.IsChildOf(snappedPart.transform) ||
                    hit.transform == snappedPart.transform)
                    DetachPart();
            }
        }
    }

    void TrySnap()
    {
        if (isOccupied) return;

        GameObject prefab = PartSelectionManager.SelectedPart;
        if (prefab == null) return;

        PartInfo part = prefab.GetComponent<PartInfo>();
        if (part == null) { Debug.Log("No PartInfo on prefab"); return; }
        if (part.data.partType != acceptType) { Debug.Log("Type mismatch"); return; }

        // 프리팹에서 인스턴스 생성
        var instance = Instantiate(prefab, transform.position, transform.rotation, transform.parent);
        SetLayerRecursively(instance, LayerMask.NameToLayer("AssembledPart"));

        snappedPart = instance;
        isOccupied = true;

        // 버튼 상태 갱신
        ownerSelector = PartSelectionManager.SelectedButton != null
            ? PartSelectionManager.SelectedButton.GetComponent<PartSelector>() : null;
        ownerSelector?.SetAssembled();

        PartSelectionManager.Clear();
        Debug.Log("Assembled!");
    }

    // 인스턴스 파괴 + 버튼 해제
    public void DetachPart()
    {
        if (snappedPart != null) Destroy(snappedPart);
        ownerSelector?.SetUnassembled();
        snappedPart = null;
        ownerSelector = null;
        isOccupied = false;
        Debug.Log("Detached!");
    }

    public void ForceDetach()
    {
        snappedPart = null;
        ownerSelector = null;
        isOccupied = false;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj.GetComponent<SnapZone>() != null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
