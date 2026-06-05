using UnityEngine;
using System.Collections.Generic;

public class CableDragInteraction : MonoBehaviour
{
    [Tooltip("파티클 클릭 감지 반경 (픽셀)")]
    [SerializeField] private float grabRadiusPixels = 25f;

    private CableComponent cable;
    private Camera cam;

    private int selectedParticleIdx = -1;
    private CableTiePoint selectedTieForRelease = null; // 해제할 타이포인트

    // 파티클 인덱스 → (타이포인트, 핸들) 여러 개 고정 가능
    private readonly Dictionary<int, (CableTiePoint tie, GameObject handle)> boundMap = new();

    // CableBundler 호환용
    public CableTiePoint BoundTiePoint => boundMap.Count > 0
        ? boundMap.GetEnumerator().Current.Value.tie
        : null;

    public Vector3? MidParticlePosition
    {
        get
        {
            int mid = cable != null ? cable.Segments / 2 : -1;
            return (mid >= 0 && cable.Points != null) ? cable.Points[mid].Position : (Vector3?)null;
        }
    }

    void Awake()
    {
        cable = GetComponent<CableComponent>();
        if (cable == null)
        {
            Debug.LogError($"[CableDragInteraction] '{gameObject.name}' has no CableComponent.", this);
            enabled = false;
            return;
        }
        cam = Camera.main;
    }

    void OnDestroy()
    {
        foreach (var kv in boundMap)
            if (kv.Value.handle != null) Destroy(kv.Value.handle);
    }

    void Update()
    {
        if (cam == null || cable.Points == null) return;

        if (Input.GetMouseButtonDown(0))
            HandleClick();

        // G키 — 선택된 타이포인트 고정 해제
        if (Input.GetKeyDown(KeyCode.G))
            TryReleaseSelectedTiePoint();
    }

    void HandleClick()
    {
        if (selectedParticleIdx >= 0)
        {
            CableTiePoint clickedTie = GetClickedTiePoint();
            if (clickedTie != null)
            {
                // 고정된 타이포인트 클릭 → 해제 선택
                if (clickedTie.HasAnyBound && IsBoundByThis(clickedTie))
                {
                    SelectTieForRelease(clickedTie);
                    return;
                }
                // 빈 타이포인트 클릭 → 고정
                FixParticleToTiePoint(clickedTie);
                return;
            }
            DeselectParticle();
            return;
        }

        // 파티클 선택 시도
        int closestIdx = FindClosestMiddleParticle(out float pixelDist);
        if (closestIdx >= 0 && pixelDist <= grabRadiusPixels)
        {
            SelectParticle(closestIdx);
            return;
        }

        // 고정된 타이포인트 클릭 (파티클 선택 없이)
        CableTiePoint tie = GetClickedTiePoint();
        if (tie != null && tie.HasAnyBound && IsBoundByThis(tie))
            SelectTieForRelease(tie);
    }

    void SelectParticle(int idx)
    {
        selectedParticleIdx = idx;

        foreach (var tie in CableTiePoint.All)
            tie.ShowIndicator(true);

        SetLineColor(Color.cyan);
    }

    void DeselectParticle()
    {
        selectedParticleIdx = -1;

        foreach (var tie in CableTiePoint.All)
        {
            if (!tie.HasAnyBound)
                tie.ShowIndicator(false);
        }

        SetLineColor(Color.white);
    }

    void FixParticleToTiePoint(CableTiePoint tie)
    {
        // 타이포인트가 해당 파티클의 도달 가능 범위 안에 있는지 체크
        float segLen = cable.CableLength / cable.Segments;
        float maxFromStart = selectedParticleIdx * segLen;
        float maxFromEnd   = (cable.Segments - selectedParticleIdx) * segLen;

        float distFromStart = Vector3.Distance(cable.transform.position, tie.transform.position);
        float distFromEnd   = Vector3.Distance(cable.EndPoint.position,  tie.transform.position);

        // 타원형 체크: 시작+끝 거리 합이 케이블 전체 길이 이하여야 함
        if (distFromStart + distFromEnd > cable.CableLength * 1.1f)
        {
            Debug.Log("Cannot fix: TiePoint out of cable range.");
            DeselectParticle();
            return;
        }

        // 현재 파티클 위치 → 타이포인트 경로에 콜라이더가 있는지 체크
        Vector3 currentPos = cable.Points[selectedParticleIdx].Position;
        Vector3 toTie = tie.transform.position - currentPos;
        float toTieDist = toTie.magnitude;
        int mask = ~LayerMask.GetMask("Ignore Raycast", "Cable");
        if (toTieDist > 0.001f && Physics.SphereCast(currentPos, cable.GetComponent<CableComponent>() != null ? 0.02f : 0.02f, toTie.normalized, out RaycastHit hitInfo, toTieDist, mask))
        {
            if (!hitInfo.collider.isTrigger && !(hitInfo.collider is MeshCollider meshCol && !meshCol.convex))
            {
                Debug.Log("Cannot fix: path to TiePoint blocked by collider.");
                DeselectParticle();
                return;
            }
        }

        // 같은 파티클이 이미 고정돼 있으면 기존 해제 후 재고정
        if (boundMap.TryGetValue(selectedParticleIdx, out var existing))
        {
            existing.tie.ReleaseParticle(cable.Points[selectedParticleIdx]);
            Destroy(existing.handle);
            boundMap.Remove(selectedParticleIdx);
        }

        // 임시로 파티클을 타이포인트 위치로 이동 후 모든 파티클 콜라이더 위반 검사
        Vector3 originalPos = cable.Points[selectedParticleIdx].Position;
        cable.Points[selectedParticleIdx].Position = tie.transform.position;

        if (AnyParticleInCollider())
        {
            // 위반 시 원래 위치로 복구
            cable.Points[selectedParticleIdx].Position = originalPos;
            Debug.Log("Cannot fix: particles would violate collider.");
            DeselectParticle();
            return;
        }

        cable.Points[selectedParticleIdx].Position = originalPos;

        // 새 핸들 생성 (파티클마다 독립 핸들)
        var handleObj = new GameObject($"_Handle_{selectedParticleIdx}");
        handleObj.transform.position = tie.transform.position;

        tie.BindParticle(cable.Points[selectedParticleIdx], handleObj.transform);
        boundMap[selectedParticleIdx] = (tie, handleObj);

        DeselectParticle();
    }

    bool AnyParticleInCollider()
    {
        int mask = ~LayerMask.GetMask("Ignore Raycast", "Cable");
        foreach (var particle in cable.Points)
        {
            Collider[] hits = Physics.OverlapSphere(particle.Position, 0.02f, mask);
            foreach (var hit in hits)
            {
                if (hit.isTrigger) continue;
                if (hit is MeshCollider mc && !mc.convex) continue;
                if (hit.transform == cable.transform || hit.transform.IsChildOf(cable.transform)) continue;
                return true;
            }
        }
        return false;
    }

    void SelectTieForRelease(CableTiePoint tie)
    {
        // 기존 선택 해제
        selectedTieForRelease?.SetHighlight(false);
        selectedTieForRelease = tie;
        tie.SetHighlight(true);
    }

    void TryReleaseSelectedTiePoint()
    {
        if (selectedTieForRelease == null) return;

        // boundMap에서 해당 타이포인트와 연결된 파티클 찾아서 해제
        int targetIdx = -1;
        foreach (var kv in boundMap)
        {
            if (kv.Value.tie == selectedTieForRelease)
            {
                targetIdx = kv.Key;
                break;
            }
        }

        if (targetIdx < 0) return;

        var entry = boundMap[targetIdx];
        entry.tie.ReleaseParticle(cable.Points[targetIdx]);
        if (!entry.tie.HasAnyBound) entry.tie.ShowIndicator(false);
        Destroy(entry.handle);
        boundMap.Remove(targetIdx);

        selectedTieForRelease = null;
    }

    bool IsBoundByThis(CableTiePoint tie)
    {
        foreach (var kv in boundMap)
            if (kv.Value.tie == tie) return true;
        return false;
    }

    CableTiePoint GetClickedTiePoint()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        int mask = ~LayerMask.GetMask("Ignore Raycast", "AssembledPart");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mask))
        {
            return hit.collider.GetComponent<CableTiePoint>()
                ?? hit.collider.GetComponentInParent<CableTiePoint>();
        }
        return null;
    }

    // CableBundler에서 호출
    public void SnapMidToTiePoint(CableTiePoint tie)
    {
        int mid = cable.Segments / 2;

        if (boundMap.TryGetValue(mid, out var existing))
        {
            existing.tie.ReleaseParticle(cable.Points[mid]);
            Destroy(existing.handle);
            boundMap.Remove(mid);
        }

        var handleObj = new GameObject($"_Handle_{mid}");
        handleObj.transform.position = tie.transform.position;
        tie.BindParticle(cable.Points[mid], handleObj.transform);
        boundMap[mid] = (tie, handleObj);
    }

    public void SnapMidToPosition(Vector3 pos)
    {
        int mid = cable.Segments / 2;

        if (boundMap.TryGetValue(mid, out var existing))
        {
            existing.tie.ReleaseParticle(cable.Points[mid]);
            Destroy(existing.handle);
            boundMap.Remove(mid);
        }

        var handleObj = new GameObject($"_Handle_{mid}");
        handleObj.transform.position = pos;
        cable.Points[mid].Bind(handleObj.transform);
        boundMap[mid] = (null, handleObj);
    }

    public void SetLineColor(Color color)
    {
        var lr = GetComponent<LineRenderer>();
        if (lr != null) lr.material.color = color;
    }

    int FindClosestMiddleParticle(out float closestPixelDist)
    {
        closestPixelDist = float.MaxValue;
        int closestIdx = -1;
        Vector2 mousePos = Input.mousePosition;

        for (int i = 1; i < cable.Segments; i++)
        {
            Vector3 screenPt = cam.WorldToScreenPoint(cable.Points[i].Position);
            if (screenPt.z <= 0) continue;

            float dist = Vector2.Distance(mousePos, new Vector2(screenPt.x, screenPt.y));
            if (dist < closestPixelDist)
            {
                closestPixelDist = dist;
                closestIdx = i;
            }
        }

        return closestIdx;
    }
}
