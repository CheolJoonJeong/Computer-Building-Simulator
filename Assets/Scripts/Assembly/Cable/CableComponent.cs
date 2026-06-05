using UnityEngine;
using System.Collections.Generic;

// Verlet 물리 기반 케이블 코어
// - 파티클 배열을 Verlet 적분으로 시뮬레이션
// - 핀(고정점) API로 양 끝점/통과점/타이포인트를 고정
// - SphereCast 사전 차단 + 거리 제약으로 콜라이더 통과/늘어남 방지
[RequireComponent(typeof(LineRenderer))]
public class CableComponent : MonoBehaviour
{
    [Header("Cable Shape")]
    [SerializeField] private int segments = 24;
    [SerializeField] private float totalLength = 1.5f;
    [SerializeField] private float cableWidth = 0.02f;
    [SerializeField] private Material cableMaterial;

    [Header("Solver")]
    [SerializeField] private int solverIterations = 30;
    [Range(0f, 1f)]
    [SerializeField] private float damping = 0.92f;
    [SerializeField] private float gravityScale = 1f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.02f;

    // 시뮬레이션 상태
    private Vector3[] pos;
    private Vector3[] prev;
    private LineRenderer line;
    private bool initialized;
    private Transform endVisual;   // 끝점 헤드(EndPoint) 오브젝트

    // 핀: 파티클 인덱스 -> 따라갈 Transform (없으면 자유 파티클)
    private readonly Dictionary<int, Transform> pins = new();
    // 라우팅(통과점)으로 추가된 인덱스 추적
    private readonly List<Transform> routeAnchors = new();
    private readonly HashSet<int> routeIndices = new();

    public int Segments => segments;
    public float TotalLength => totalLength;
    public float SegmentLength => totalLength / segments;
    public Vector3 GetParticle(int i) => pos[Mathf.Clamp(i, 0, segments)];
    public bool IsInitialized => initialized;

    // ---- 초기화 ----

    // start 위치에서 endVisual 방향으로 케이블 파티클 생성. 0번은 start에 고정.
    public void Init(Transform startAnchor, Transform endPointVisual)
    {
        endVisual = endPointVisual;
        Vector3 endPos = endPointVisual.position;

        line = GetComponent<LineRenderer>();
        line.startWidth = cableWidth;
        line.endWidth = cableWidth;
        line.positionCount = segments + 1;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        if (cableMaterial != null) line.material = cableMaterial;

        pos = new Vector3[segments + 1];
        prev = new Vector3[segments + 1];

        Vector3 startPos = startAnchor.position;
        Vector3 dir = (endPos - startPos);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.down;
        dir.Normalize();
        float segLen = SegmentLength;

        for (int i = 0; i <= segments; i++)
        {
            pos[i] = startPos + dir * (segLen * i);
            prev[i] = pos[i];
        }

        pins.Clear();
        routeAnchors.Clear();
        routeIndices.Clear();
        pins[0] = startAnchor;   // 시작점 고정

        initialized = true;
    }

    // ---- 핀 관리 ----

    // 통과점(Pass Through) 추가 — 순서대로 인덱스 분배
    public void AddRouteAnchor(Transform anchor)
    {
        routeAnchors.Add(anchor);
        RebuildRoutePins();
    }

    // 끝점을 소켓에 고정 (마지막 연결)
    public void SetEndAnchor(Transform endAnchor)
    {
        pins[segments] = endAnchor;
    }

    // 끝점을 특정 위치로 텔레포트(자유 상태 유지)
    public void MoveEndTo(Vector3 worldPos)
    {
        pos[segments] = worldPos;
        prev[segments] = worldPos;
    }

    // 임의 인덱스를 Transform에 고정 (타이포인트 정리용)
    public void PinParticle(int index, Transform t)
    {
        index = Mathf.Clamp(index, 0, segments);
        pins[index] = t;
        pos[index] = t.position;
        prev[index] = t.position;
    }

    public void UnpinParticle(int index)
    {
        index = Mathf.Clamp(index, 0, segments);
        if (!routeIndices.Contains(index) && index != 0 && index != segments)
            pins.Remove(index);
    }

    public bool IsPinned(int index) => pins.ContainsKey(index);

    // 화면 클릭 위치에서 가장 가까운 중간 파티클 인덱스 (1..segments-1)
    public int FindClosestMiddleParticle(Camera cam, Vector2 screenPos, out float pixelDist)
    {
        pixelDist = float.MaxValue;
        int best = -1;
        for (int i = 1; i < segments; i++)
        {
            Vector3 sp = cam.WorldToScreenPoint(pos[i]);
            if (sp.z <= 0) continue;
            float d = Vector2.Distance(screenPos, new Vector2(sp.x, sp.y));
            if (d < pixelDist) { pixelDist = d; best = i; }
        }
        return best;
    }

    void RebuildRoutePins()
    {
        // 기존 route 핀 제거
        foreach (int idx in routeIndices) pins.Remove(idx);
        routeIndices.Clear();

        int count = routeAnchors.Count;
        for (int i = 0; i < count; i++)
        {
            int idx = Mathf.RoundToInt((i + 1) * segments / (float)(count + 1));
            idx = Mathf.Clamp(idx, 1, segments - 1);
            // 중복 방지
            while (routeIndices.Contains(idx) && idx < segments - 1) idx++;
            routeIndices.Add(idx);
            pins[idx] = routeAnchors[i];
            pos[idx] = routeAnchors[i].position;
            prev[idx] = routeAnchors[i].position;
        }
    }

    // ---- 시뮬레이션 ----

    void FixedUpdate()
    {
        if (!initialized) return;

        // 1. 핀 위치 갱신
        foreach (var kv in pins)
        {
            if (kv.Value != null)
            {
                pos[kv.Key] = kv.Value.position;
                prev[kv.Key] = kv.Value.position;
            }
        }

        // 2. Verlet 적분 (자유 파티클만, SphereCast로 통과 차단)
        Vector3 gravity = Physics.gravity * gravityScale * Time.fixedDeltaTime * Time.fixedDeltaTime;
        for (int i = 0; i <= segments; i++)
        {
            if (pins.ContainsKey(i)) continue;

            Vector3 velocity = (pos[i] - prev[i]) * damping;
            Vector3 target = pos[i] + velocity + gravity;

            Vector3 moveDir = target - pos[i];
            float moveDist = moveDir.magnitude;
            if (moveDist > 0.0001f)
            {
                if (Physics.SphereCast(pos[i], collisionRadius, moveDir / moveDist,
                                       out RaycastHit hit, moveDist, collisionMask,
                                       QueryTriggerInteraction.Ignore))
                {
                    if (!(hit.collider is MeshCollider mc && !mc.convex))
                        target = pos[i] + (moveDir / moveDist) * Mathf.Max(0f, hit.distance - 0.001f);
                }
            }

            prev[i] = pos[i];
            pos[i] = target;
        }

        // 3. 거리 제약 + 충돌 밀어내기 (인터리브)
        for (int iter = 0; iter < solverIterations; iter++)
        {
            DistanceConstraintPass();
            CollisionPushoutPass();
        }
    }

    void DistanceConstraintPass()
    {
        float segLen = SegmentLength;
        // 앞 -> 뒤
        for (int i = 0; i < segments; i++) SolvePair(i, i + 1, segLen);
        // 뒤 -> 앞 (양방향 수렴)
        for (int i = segments - 1; i >= 0; i--) SolvePair(i, i + 1, segLen);
    }

    void SolvePair(int a, int b, float segLen)
    {
        bool pa = pins.ContainsKey(a);
        bool pb = pins.ContainsKey(b);
        if (pa && pb) return;

        Vector3 delta = pos[b] - pos[a];
        float dist = delta.magnitude;
        if (dist < 0.0001f) return;

        // 늘어남만 막음(압축은 허용) — 자연스러운 처짐 유지하며 길이 초과 방지
        if (dist <= segLen) return;

        float diff = (dist - segLen) / dist;
        if (!pa && !pb)
        {
            pos[a] += delta * 0.5f * diff;
            pos[b] -= delta * 0.5f * diff;
        }
        else if (!pa)
        {
            pos[a] += delta * diff;
        }
        else
        {
            pos[b] -= delta * diff;
        }
    }

    void CollisionPushoutPass()
    {
        for (int i = 0; i <= segments; i++)
        {
            if (pins.ContainsKey(i)) continue;

            Collider[] hits = Physics.OverlapSphere(pos[i], collisionRadius, collisionMask,
                                                    QueryTriggerInteraction.Ignore);
            foreach (var col in hits)
            {
                if (col is MeshCollider mc && !mc.convex) continue;

                Vector3 closest = col.ClosestPoint(pos[i]);
                Vector3 push = pos[i] - closest;
                float d = push.magnitude;
                if (d < 0.0001f) { push = Vector3.up; d = 0f; }
                float penetration = collisionRadius - d;
                if (penetration > 0f)
                    pos[i] += push.normalized * penetration;
            }
        }
    }

    // ---- 렌더 ----

    void LateUpdate()
    {
        if (!initialized) return;
        for (int i = 0; i <= segments; i++)
            line.SetPosition(i, pos[i]);

        // 끝점 헤드를 마지막 파티클 위치로
        if (endVisual != null)
            endVisual.position = pos[segments];
    }

    public void SetColor(Color c)
    {
        if (line != null && line.material != null) line.material.color = c;
    }
}
