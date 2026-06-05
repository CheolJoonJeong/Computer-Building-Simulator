using UnityEngine;

// 케이블 연결 상태 관리 싱글톤
public class CableManager : MonoBehaviour
{
    public static CableManager Instance { get; private set; }

    public CableType? SelectedType { get; private set; }

    private enum State { Idle, TypeSelected, PendingEnd }
    private State state = State.Idle;

    private CableSpawner activeSpawner;
    private CableConnector pendingConnector;

    public bool HasPending => pendingConnector != null && state == State.PendingEnd;

    public bool ShouldHighlight(CableType type) =>
        state != State.Idle && SelectedType == type;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SelectCableType(CableType type, CableSpawner spawner)
    {
        if (state == State.TypeSelected && SelectedType == type) { Cancel(); return; }
        SelectedType = type;
        activeSpawner = spawner;
        pendingConnector = null;
        state = State.TypeSelected;
    }

    public void OnSocketClicked(CableSocket socket)
    {
        if (state == State.TypeSelected)
        {
            var (start, end) = activeSpawner.SpawnAt(socket.transform.position);
            if (start == null) return;
            socket.TryConnect(start);
            pendingConnector = end;
            state = State.PendingEnd;
        }
        else if (state == State.PendingEnd)
        {
            if (socket.TryConnect(pendingConnector))
                Cancel();
        }
    }

    public void MoveEndPointTo(Vector3 position)
    {
        if (pendingConnector == null) return;
        pendingConnector.transform.position = position;
    }

    public void Cancel()
    {
        SelectedType = null;
        activeSpawner = null;
        pendingConnector = null;
        state = State.Idle;
    }
}
