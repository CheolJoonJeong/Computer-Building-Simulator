using UnityEngine;

// 케이블 연결 상태 관리 싱글톤
// 씬에 빈 GameObject 하나에 붙여두면 됨
public class CableManager : MonoBehaviour
{
    public static CableManager Instance { get; private set; }

    public CableType? SelectedType { get; private set; }

    private enum State { Idle, TypeSelected, PendingEnd }
    private State state = State.Idle;

    private CableSpawner activeSpawner;
    private CableConnector pendingConnector;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool HasPending => pendingConnector != null && state == State.PendingEnd;

    // 소켓이 하이라이트를 켜야 하는지 여부
    public bool ShouldHighlight(CableType type) =>
        state != State.Idle && SelectedType == type;

    // CableSpawner 버튼에서 호출 — 타입 선택 및 소켓 하이라이트 시작
    public void SelectCableType(CableType type, CableSpawner spawner)
    {
        // 같은 타입 다시 누르면 취소
        if (state == State.TypeSelected && SelectedType == type)
        {
            Cancel();
            return;
        }

        SelectedType = type;
        activeSpawner = spawner;
        pendingConnector = null;
        state = State.TypeSelected;
    }

    // CableSocket.OnMouseDown에서 호출
    public void OnSocketClicked(CableSocket socket)
    {
        if (state == State.TypeSelected)
        {
            // 첫 번째 소켓 클릭 — 케이블 소환 후 연결
            (CableConnector start, CableConnector end) = activeSpawner.SpawnAt(socket.transform.position);
            if (start == null) return;

            socket.TryConnect(start);
            pendingConnector = end;
            state = State.PendingEnd;
        }
        else if (state == State.PendingEnd)
        {
            // 두 번째 소켓 클릭 — 끝점 연결
            if (socket.TryConnect(pendingConnector))
                Cancel();
        }
    }

    // CablePassThrough에서 호출 — 끝점을 구멍 위치로 이동 후 대기 유지
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
