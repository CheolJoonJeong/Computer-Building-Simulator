using UnityEngine;

// UI 버튼에 연결 — 케이블 타입 선택 및 소환
public class CableSpawner : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [SerializeField] private GameObject cablePrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0.3f, 0f, 0f);

    public void OnButtonClick()
    {
        if (CableManager.Instance == null) { Debug.LogError("[CableSpawner] CableManager not found."); return; }
        CableManager.Instance.SelectCableType(cableType, this);
    }

    public (CableConnector start, CableConnector end) SpawnAt(Vector3 position)
    {
        if (cablePrefab == null) { Debug.LogError("[CableSpawner] cablePrefab not assigned."); return (null, null); }

        var go = Instantiate(cablePrefab, position + spawnOffset, Quaternion.identity);
        var startConn = go.GetComponent<CableConnector>();
        var endPoint  = go.transform.Find("EndPoint");
        var endConn   = endPoint != null ? endPoint.GetComponent<CableConnector>() : null;

        // CableRenderer 초기화
        var renderer = go.GetComponent<CableRenderer>();
        if (renderer != null && endPoint != null)
            renderer.Init(go.transform, endPoint);

        return (startConn, endConn);
    }
}
