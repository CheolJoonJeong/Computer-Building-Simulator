using UnityEngine;

// 케이블 양 끝점 오브젝트에 붙이는 커넥터
public class CableConnector : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [SerializeField] private bool isEndPoint = false; // EndPoint 오브젝트면 true
    public CableType CableType => cableType;
    public bool IsEndPoint => isEndPoint;

    public CableSocket ConnectedSocket { get; private set; }
    public bool IsConnected => ConnectedSocket != null;

    public void ConnectTo(CableSocket socket)
    {
        ConnectedSocket = socket;
        transform.position = socket.transform.position;
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
