using UnityEngine;

// UI 버튼에 연결 — 케이블 타입 선택 및 소환
public class CableSpawner : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [SerializeField] private GameObject cablePrefab;
    [Tooltip("케이블이 기본으로 지나갈 통과점들 (순서대로). 비워두면 직선)")]
    [SerializeField] private Transform[] defaultRoute;
    [Tooltip("지정하면 버튼 클릭 시 바로 이 소켓에 연결되어 라우팅 시작 (출발 소켓 직접 클릭 불필요)")]
    [SerializeField] private CableSocket sourceSocket;

    // UI 버튼 OnClick 에 연결
    public void OnButtonClick()
    {
        if (CableManager.Instance == null) { Debug.LogError("[CableSpawner] CableManager not found."); return; }

        if (sourceSocket != null)
            CableManager.Instance.StartFromSpawner(cableType, this, sourceSocket);
        else
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
        {
            cable.Init(start.transform, endPoint);

            // 기본 경로(통과점) 자동 적용
            if (defaultRoute != null)
                foreach (var anchor in defaultRoute)
                    if (anchor != null) cable.AddRouteAnchor(anchor);
        }

        return (start, end, cable);
    }
}
