using UnityEngine;

// 파츠에 붙이는 케이블 소켓 (클릭으로 연결)
// Collider는 이 오브젝트에 직접 두거나, 자식에 두고 CableSocketClickRelay로 전달
public class CableSocket : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [SerializeField] private Renderer socketRenderer;
    [SerializeField] private Color idleColor      = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color connectedColor = Color.green;

    public CableType CableType => cableType;
    public bool IsOccupied => connected != null;

    private CableConnector connected;

    void Start() => SetColor(idleColor);

    void Update()
    {
        if (IsOccupied) return;
        bool hl = CableManager.Instance != null && CableManager.Instance.ShouldHighlight(cableType);
        SetColor(hl ? highlightColor : idleColor);
    }

    void OnMouseDown() => HandleClick();

    // 자식 Relay 또는 직접 클릭에서 호출
    public void HandleClick()
    {
        if (IsOccupied) return;
        CableManager.Instance?.OnSocketClicked(this);
    }

    public bool TryConnect(CableConnector connector)
    {
        if (IsOccupied || connector.CableType != cableType) return false;
        connected = connector;
        connector.ConnectTo(this);
        SetColor(connectedColor);
        return true;
    }

    public void Disconnect()
    {
        connected = null;
        SetColor(idleColor);
    }

    void SetColor(Color c)
    {
        if (socketRenderer != null) socketRenderer.material.color = c;
    }
}
