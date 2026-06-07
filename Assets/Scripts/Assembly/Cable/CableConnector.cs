using UnityEngine;

// 케이블 끝점에 붙이는 커넥터
public class CableConnector : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [SerializeField] private bool isEndPoint = false;

    public CableType CableType => cableType;
    public bool IsEndPoint => isEndPoint;
    public CableSocket ConnectedSocket { get; private set; }
    public bool IsConnected => ConnectedSocket != null;

    public void ConnectTo(CableSocket socket)
    {
        ConnectedSocket = socket;
        Transform anchor = socket.AnchorTransform;
        transform.position = anchor.position;
        transform.rotation = anchor.rotation;
    }

    public void Disconnect()
    {
        if (ConnectedSocket != null)
        {
            ConnectedSocket.Disconnect();
            ConnectedSocket = null;
        }
    }
}
