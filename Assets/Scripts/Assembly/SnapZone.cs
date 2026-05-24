using UnityEngine;

public class SnapZone : MonoBehaviour
{
    public PartType acceptType;
    public bool isOccupied = false;
    private GameObject snappedPart = null;

    public bool startOccupied = false;
    public GameObject startPart; // 처음부터 조립할 부품 연결

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
        // 마우스 클릭
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    Debug.Log("슬롯 클릭됨: " + gameObject.name);
                    TrySnap();
                }
            }
        }

        // 분리 (R키)
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

                    Collider[] cols =
                        snappedPart.GetComponentsInChildren<Collider>(true);
                    foreach (Collider col in cols)
                        col.enabled = true;

                    snappedPart.SetActive(false);

                    foreach (PartSelector ps in FindObjectsOfType<PartSelector>(true))
                    {
                        if (ps.targetPart == snappedPart)
                        {
                            ps.SetUnassembled(); // 버튼 다시 밝게
                            break;
                        }
                    }

                    snappedPart = null;
                    isOccupied = false;
                    Debug.Log("분리 완료!");
                }
            }
        }
    }

    void TrySnap()
    {
        if (isOccupied)
        {
            Debug.Log("이미 장착됨");
            return;
        }

        GameObject selectedPart = PartSelectionManager.SelectedPart;
        if (selectedPart == null)
        {
            Debug.Log("선택된 부품 없음");
            return;
        }

        PartInfo part = selectedPart.GetComponent<PartInfo>();
        if (part == null)
        {
            Debug.Log("PartInfo 없음");
            return;
        }

        if (part.data.partType != acceptType)
        {
            Debug.Log("타입 불일치");
            return;
        }

        // 부품 활성화
        selectedPart.SetActive(true);

        // 위치 이동
        selectedPart.transform.position = transform.position;
        selectedPart.transform.rotation = transform.rotation;
        selectedPart.transform.SetParent(transform.parent);

        // 모든 Collider 끄기
        Collider[] cols =
            selectedPart.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in cols)
            col.enabled = false;

        // 버튼 어둡게
        if (PartSelectionManager.SelectedButton != null)
        {
            PartSelectionManager.SelectedButton
                .GetComponent<PartSelector>()?.SetAssembled();
        }

        snappedPart = selectedPart;
        isOccupied = true;
        PartSelectionManager.Clear();
        Debug.Log("장착 완료!");
    }
    public void ForceDetach()
    {
        snappedPart = null;
        isOccupied = false;
    }
}