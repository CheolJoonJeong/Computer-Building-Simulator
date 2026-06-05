using UnityEngine;

// 파츠에 붙이는 케이블 소켓
public class CableSocket : MonoBehaviour
{
    [SerializeField] public CableType cableType;
    [SerializeField] private Renderer socketRenderer;
    [SerializeField] private Color idleColor      = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color connectedColor = Color.green;

    public bool IsOccupied => connectedConnector != null;
    private CableConnector connectedConnector;

    void Start() => SetColor(idleColor);

    void Update()
    {
        if (IsOccupied) return;
        bool highlight = CableManager.Instance != null &&
                         CableManager.Instance.ShouldHighlight(cableType);
        SetColor(highlight ? highlightColor : idleColor);
    }

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
        return true;
    }

    public void Disconnect()
    {
        connectedConnector = null;
        SetColor(idleColor);
    }

    private void SetColor(Color color)
    {
        if (socketRenderer != null)
            socketRenderer.material.color = color;
    }
}
