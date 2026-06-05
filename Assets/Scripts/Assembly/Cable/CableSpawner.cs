using UnityEngine;

// UI 버튼에 연결 — 케이블 타입 선택 및 소환
public class CableSpawner : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [SerializeField] private GameObject cablePrefab;

    // UI 버튼 OnClick 에 연결
    public void OnButtonClick()
    {
        if (CableManager.Instance == null) { Debug.LogError("[CableSpawner] CableManager not found."); return; }
        CableManager.Instance.SelectCableType(cableType, this);
    }

    public (CableConnector start, CableConnector end, CableComponent cable) SpawnAt(Vector3 position)
    {
        if (cablePrefab == null)
        {
            Debug.LogError("[CableSpawner] cablePrefab not assigned.");
            return (null, null, null);
        }

        var go = Instantiate(cablePrefab, position, Quaternion.identity);
        var cable = go.GetComponent<CableComponent>();

        // 루트의 커넥터 = start, EndPoint 자식의 커넥터 = end
        CableConnector start = null, end = null;
        foreach (var c in go.GetComponentsInChildren<CableConnector>())
        {
            if (c.IsEndPoint) end = c;
            else start = c;
        }

        Transform endPoint = end != null ? end.transform : go.transform;

        if (cable != null && start != null)
            cable.Init(start.transform, endPoint);

        return (start, end, cable);
    }
}
