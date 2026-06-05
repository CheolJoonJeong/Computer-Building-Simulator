using UnityEngine;

// 파츠에 붙이는 케이블 소켓 (클릭으로 연결)
// 구조: Socket(CableSocket + Collider) -> Visual(메시만)
// 평소엔 비주얼 숨김, 해당 타입 케이블 선택 시에만 표시 (파츠 슬롯과 동일)
[RequireComponent(typeof(Collider))]
public class CableSocket : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [Tooltip("소켓 비주얼 (켜고 끔으로 하이라이트)")]
    [SerializeField] private GameObject socketVisual;

    public CableType CableType => cableType;
    public bool IsOccupied => connected != null;

    private CableConnector connected;

    void Start() => ShowVisual(false);

    void Update()
    {
        if (IsOccupied) { ShowVisual(false); return; }

        bool match = CableManager.Instance != null &&
                     CableManager.Instance.ShouldHighlight(cableType);
        ShowVisual(match);
    }

    void OnMouseDown()
    {
        if (IsOccupied) return;
        CableManager.Instance?.OnSocketClicked(this);
    }

    public bool TryConnect(CableConnector connector)
    {
        if (IsOccupied || connector.CableType != cableType) return false;
        connected = connector;
        connector.ConnectTo(this);
        ShowVisual(false);
        return true;
    }

    public void Disconnect()
    {
        connected = null;
        ShowVisual(false);
    }

    void ShowVisual(bool show)
    {
        if (socketVisual != null && socketVisual.activeSelf != show)
            socketVisual.SetActive(show);
    }
}
