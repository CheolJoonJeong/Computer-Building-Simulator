using UnityEngine;

// 파츠(메인보드, PSU, GPU 등)에 붙이는 케이블 소켓
public class CableSocket : MonoBehaviour
{
    [SerializeField] public CableType cableType;
    [SerializeField] private float snapRadius = 0.15f;

    [Header("Visual")]
    [SerializeField] private Renderer socketRenderer;
    [SerializeField] private Color idleColor      = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color connectedColor = Color.green;

    public bool IsOccupied => connectedConnector != null;
    public float SnapRadius => snapRadius;

    private CableConnector connectedConnector;
    private CableGuide[] guides;

    void Awake()
    {
        guides = GetComponentsInChildren<CableGuide>();
    }

    void Start()
    {
        SetColor(idleColor);
    }

    // 선택된 케이블 타입과 맞으면 하이라이트
    void Update()
    {
        if (CableManager.Instance == null || IsOccupied) return;

        bool highlight = CableManager.Instance.ShouldHighlight(cableType);
        SetColor(highlight ? highlightColor : idleColor);
    }

    // 클릭 시 CableManager에 전달 → 연결 처리
    void OnMouseDown()
    {
        if (IsOccupied) return;
        CableManager.Instance?.OnSocketClicked(this);
    }

    public bool TryConnect(CableConnector connector)
    {
        if (IsOccupied || connector.CableType != cableType) return false;

        connectedConnector = connector;
        connector.ConnectTo(this);
        SetColor(connectedColor);

        // 가이드 포인트 자동 적용 (여러 개 지원)
        if (guides != null && guides.Length > 0)
        {
            var cable = connector.GetComponentInParent<CableComponent>()
                     ?? connector.GetComponentInChildren<CableComponent>();
            if (cable != null)
                foreach (var g in guides)
                    g.ApplyGuide(cable, connector.IsEndPoint);
        }

        return true;
    }

    public void Disconnect()
    {
        // 가이드 해제
        if (guides != null && connectedConnector != null)
        {
            var cable = connectedConnector.GetComponentInParent<CableComponent>()
                     ?? connectedConnector.GetComponentInChildren<CableComponent>();
            if (cable != null)
                foreach (var g in guides)
                    g.ReleaseGuide(cable, connectedConnector.IsEndPoint);
        }

        connectedConnector = null;
        SetColor(idleColor);
    }

    public void SetHighlight(bool on)
    {
        if (!IsOccupied)
            SetColor(on ? highlightColor : idleColor);
    }

    private void SetColor(Color color)
    {
        if (socketRenderer != null)
            socketRenderer.material.color = color;
    }
}
