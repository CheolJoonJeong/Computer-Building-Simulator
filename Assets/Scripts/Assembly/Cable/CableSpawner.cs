using UnityEngine;

// UI 버튼에 연결 — 케이블 타입 선택 및 소환 담당
public class CableSpawner : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [SerializeField] private GameObject cablePrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0.3f, 0f, 0f);

    // UI 버튼 OnClick에 연결
    public void OnButtonClick()
    {
        if (CableManager.Instance == null)
        {
            Debug.LogError("[CableSpawner] CableManager not found in scene.");
            return;
        }
        CableManager.Instance.SelectCableType(cableType, this);
    }

    // CableManager에서 첫 번째 소켓 클릭 시 호출
    public (CableConnector start, CableConnector end) SpawnAt(Vector3 position)
    {
        if (cablePrefab == null)
        {
            Debug.LogError("[CableSpawner] cablePrefab is not assigned.");
            return (null, null);
        }

        var go = Instantiate(cablePrefab, position + spawnOffset, Quaternion.identity);
        var cable     = go.GetComponent<CableComponent>();
        var startConn = go.GetComponent<CableConnector>();
        var endConn   = cable.EndPoint.GetComponent<CableConnector>();

        return (startConn, endConn);
    }
}
