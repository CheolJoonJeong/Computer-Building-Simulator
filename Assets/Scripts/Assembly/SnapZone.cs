using UnityEngine;

public class SnapZone : MonoBehaviour
{
    public PartType acceptType;
    public bool isOccupied = false;
    private GameObject snappedPart = null;

    public bool startOccupied = false;
    public GameObject startPart;

    void Start()
    {
        if (startOccupied && startPart != null)
        {
            startPart.SetActive(true);
            startPart.transform.position = transform.position;
            startPart.transform.rotation = transform.rotation;
            startPart.transform.SetParent(transform.parent);

            Collider[] cols = startPart.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in cols)
                col.enabled = false;

            snappedPart = startPart;
            isOccupied = true;
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
                    Debug.Log("Detached!");
                }
            }
        }
    }

    void TrySnap()
    {
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
            Debug.Log("No PartInfo");
            return;
        }

        if (part.data.partType != acceptType)
        {
            Debug.Log("Type mismatch");
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
        PartSelectionManager.Clear();
        Debug.Log("Assembled!");
    }

    public void ForceDetach()
    {
        snappedPart = null;
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
