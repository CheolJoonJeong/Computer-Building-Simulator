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
            int mask = ~LayerMask.GetMask("Cable");
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, mask);
            foreach (RaycastHit h in hits)
            {
                if (h.collider.gameObject == gameObject ||
                    h.collider.transform.IsChildOf(transform))
                {
                    TrySnap();
                    break;
                }
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

                    Collider[] cols = snappedPart.GetComponentsInChildren<Collider>(true);
                    foreach (Collider col in cols)
                        col.enabled = true;

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

        Collider[] cols = selectedPart.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in cols)
            col.enabled = false;

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
}
