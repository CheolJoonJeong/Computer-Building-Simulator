using UnityEngine;

// 파츠에 붙이는 케이블 소켓 (클릭으로 연결)
// 구조: Socket(CableSocket + Collider) -> Visual(메시만)
// 평소엔 비주얼 숨김, 해당 타입 케이블 선택 시에만 표시 (파츠 슬롯과 동일)
[RequireComponent(typeof(Collider))]
public class CableSocket : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [Tooltip("출발(시작) 소켓이면 체크. 도착 소켓이면 해제")]
    [SerializeField] private bool isSource = true;
    [Tooltip("소켓 비주얼 (켜고 끔으로 하이라이트)")]
    [SerializeField] private GameObject socketVisual;
    [Tooltip("도착 소켓일 때, 연결 직전 케이블이 경유할 포인트들 (순서대로)")]
    [SerializeField] private Transform[] endRoute;

    public CableType CableType => cableType;
    public bool IsSource => isSource;
    public bool IsOccupied => connected != null;
    public Transform[] EndRoute => endRoute;

    // 케이블 헤더가 붙을 실제 지점. socketVisual이 있으면 그 위치를 사용
    // (소켓 pivot이 슬롯 구멍과 어긋나 있어도 비주얼 위치에 정확히 붙음)
    public Transform AnchorTransform => socketVisual != null ? socketVisual.transform : transform;

    private CableConnector connected;

    void Start() => ShowVisual(false);

    void Update()
    {
        if (IsOccupied) { ShowVisual(false); return; }

        bool match = CableManager.Instance != null &&
                     CableManager.Instance.ShouldHighlight(this);
        ShowVisual(match);
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
