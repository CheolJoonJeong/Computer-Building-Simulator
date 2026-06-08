using UnityEngine;
using UnityEngine.EventSystems;

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

    // 통과점 클릭 되돌리기용 — (통과점, 그 클릭으로 추가된 anchor 개수) 스택
    private readonly System.Collections.Generic.Stack<(CablePassThrough pt, int anchorCount)> passThroughHistory = new();

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

    void Update()
    {
        // 타입 선택/라우팅 중일 때만 소켓 클릭을 감지
        if (state == State.Idle) return;

        // G키 → 마지막 통과점 라우팅 되돌리기
        if (state == State.Routing && Input.GetKeyDown(KeyCode.G))
        {
            UndoLastPassThrough();
            return;
        }

        if (!Input.GetMouseButtonDown(0)) return;
        // UI 버튼 위 클릭은 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (Camera.main == null) return;

        // 케이스 벽 등에 가려진 소켓도 찾도록 RaycastAll → 가장 가까운 소켓 선택
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            CableSocket sock = h.collider.GetComponentInParent<CableSocket>();
            if (sock != null) { OnSocketClicked(sock); return; }

            CablePassThrough pt = h.collider.GetComponentInParent<CablePassThrough>();
            if (pt != null) { OnPassThroughClicked(pt); return; }
        }
    }

    // 케이블 버튼 클릭
    public void SelectCableType(CableType type, CableSpawner spawner)
    {
        if (CableOverlapChecker.Instance != null && CableOverlapChecker.Instance.IsBlocked) return;

        // 라우팅 중엔 무시
        if (state == State.Routing) return;
        // 같은 타입 재클릭 → 취소
        if (state == State.TypeSelected && SelectedType == type) { Cancel(); return; }

        SelectedType = type;
        activeSpawner = spawner;
        state = State.TypeSelected;
    }

    // 버튼 클릭만으로 지정된 출발 소켓에 즉시 연결 후 라우팅 시작
    public void StartFromSpawner(CableType type, CableSpawner spawner, CableSocket sourceSocket)
    {
        if (CableOverlapChecker.Instance != null && CableOverlapChecker.Instance.IsBlocked) return;
        if (state == State.Routing) return;
        if (state == State.TypeSelected && SelectedType == type) { Cancel(); return; }

        if (sourceSocket.IsOccupied || sourceSocket.CableType != type || !sourceSocket.IsSource)
        {
            Debug.Log("[CableManager] Source socket not available.");
            return;
        }

        var spawned = spawner.SpawnAt(sourceSocket.AnchorTransform.position);
        if (spawned.cable == null) return;

        sourceSocket.TryConnect(spawned.start);

        SelectedType = type;
        activeSpawner = spawner;
        activeCable = spawned.cable;
        activeEndConnector = spawned.end;
        state = State.Routing;
    }

    // 소켓 클릭
    public void OnSocketClicked(CableSocket socket)
    {
        if (state == State.TypeSelected)
        {
            if (socket.CableType != SelectedType || !socket.IsSource) return;

            if (activeSpawner == null) return;
            var spawned = activeSpawner.SpawnAt(socket.AnchorTransform.position);
            if (spawned.cable == null) return;

            // 첫 끝점 연결
            socket.TryConnect(spawned.start);
            // index 0 은 start 커넥터(소켓 위치)를 따라가도록 초기화됨

            spawned.cable.ReleaseEnd();

            activeCable = spawned.cable;
            activeEndConnector = spawned.end;
            state = State.Routing;
        }
        else if (state == State.Routing)
        {
            if (socket.CableType != SelectedType || socket.IsSource) return;
            if (socket.TryConnect(activeEndConnector))
            {
                if (socket.EndRoute != null)
                    foreach (var anchor in socket.EndRoute)
                        if (anchor != null) activeCable.AddRouteAnchor(anchor);

                activeCable.SetEndAnchor(socket.AnchorTransform);
                activeSpawner?.OnConnected();
                Finish();
            }
        }
    }

    // 통과점 클릭
    public void OnPassThroughClicked(CablePassThrough pt)
    {
        if (state != State.Routing || activeCable == null) return;
        if (pt.Passed) return; // 한 구멍은 한 번만 통과 가능

        pt.MarkPassed();
        int added = 1;
        activeCable.AddRouteAnchor(pt.transform);

        if (pt.ForcedRoute != null)
            foreach (var anchor in pt.ForcedRoute)
                if (anchor != null) { activeCable.AddRouteAnchor(anchor); added++; }

        passThroughHistory.Push((pt, added));
        activeCable.MoveEndTo(pt.transform.position);
    }

    // 마지막 통과점 클릭 되돌리기 (우클릭)
    void UndoLastPassThrough()
    {
        if (activeCable == null || passThroughHistory.Count == 0) return;

        var (pt, count) = passThroughHistory.Pop();
        for (int i = 0; i < count; i++)
            activeCable.RemoveLastRouteAnchor();

        pt.UnmarkPassed();
    }

    void Finish()
    {
        passThroughHistory.Clear();
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
