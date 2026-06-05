using UnityEngine;

// 케이블 연결 흐름 관리 싱글톤
// Idle -> TypeSelected(버튼) -> Routing(첫 소켓 연결, 끝점 라우팅) -> Idle
public class CableManager : MonoBehaviour
{
    public static CableManager Instance { get; private set; }

    private enum State { Idle, TypeSelected, Routing }
    private State state = State.Idle;

    public CableType? SelectedType { get; private set; }
    private CableSpawner activeSpawner;

    private CableComponent activeCable;
    private CableConnector activeEndConnector;

    public bool IsRouting => state == State.Routing;

    // 단계별로 표시할 소켓: TypeSelected → 출발 소켓, Routing → 도착 소켓
    public bool ShouldHighlight(CableSocket socket)
    {
        if (SelectedType != socket.CableType) return false;
        if (state == State.TypeSelected) return socket.IsSource;
        if (state == State.Routing)      return !socket.IsSource;
        return false;
    }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // 케이블 버튼 클릭
    public void SelectCableType(CableType type, CableSpawner spawner)
    {
        // 라우팅 중엔 무시
        if (state == State.Routing) return;
        // 같은 타입 재클릭 → 취소
        if (state == State.TypeSelected && SelectedType == type) { Cancel(); return; }

        SelectedType = type;
        activeSpawner = spawner;
        state = State.TypeSelected;
    }

    // 소켓 클릭
    public void OnSocketClicked(CableSocket socket)
    {
        if (state == State.TypeSelected)
        {
            if (socket.CableType != SelectedType || !socket.IsSource) return;

            var spawned = activeSpawner.SpawnAt(socket.transform.position);
            if (spawned.cable == null) return;

            // 첫 끝점 연결
            socket.TryConnect(spawned.start);
            // index 0 은 start 커넥터(소켓 위치)를 따라가도록 초기화됨

            activeCable = spawned.cable;
            activeEndConnector = spawned.end;
            state = State.Routing;
        }
        else if (state == State.Routing)
        {
            if (socket.CableType != SelectedType || socket.IsSource) return;
            if (socket.TryConnect(activeEndConnector))
            {
                activeCable.SetEndAnchor(socket.transform);
                Finish();
            }
        }
    }

    // 통과점 클릭
    public void OnPassThroughClicked(CablePassThrough pt)
    {
        if (state != State.Routing || activeCable == null) return;
        activeCable.AddRouteAnchor(pt.transform);
        activeCable.MoveEndTo(pt.transform.position);
    }

    void Finish()
    {
        state = State.Idle;
        SelectedType = null;
        activeSpawner = null;
        activeCable = null;
        activeEndConnector = null;
    }

    public void Cancel()
    {
        // 라우팅 중 취소 시 케이블 제거
        if (state == State.Routing && activeCable != null)
            Destroy(activeCable.gameObject);
        Finish();
    }
}
