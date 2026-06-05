using UnityEngine;
using System.Collections.Generic;

// 케이블 클릭/타이포인트 고정/해제 상호작용
public class CableInteraction : MonoBehaviour
{
    [SerializeField] private float clickRadiusPixels = 30f;

    private CableRenderer cableRenderer;
    private Camera cam;

    // 타이포인트 고정 맵: 제어점 인덱스 → 타이포인트
    // 인덱스는 CableRenderer controlPoints 리스트 순서
    private readonly Dictionary<int, CableTiePoint> tieMap = new();
    private readonly List<Transform> controlHandles = new();

    // 현재 선택된 제어점 인덱스 (-1 = 없음)
    private int selectedIdx = -1;
    // G키로 해제할 타이포인트 선택
    private CableTiePoint selectedTieForRelease = null;

    public bool IsSelected => selectedIdx >= 0;

    void Awake()
    {
        cableRenderer = GetComponent<CableRenderer>();
        cam = Camera.main;
    }

    void Update()
    {
        if (cam == null) return;

        if (Input.GetMouseButtonDown(0))
            HandleClick();

        if (Input.GetKeyDown(KeyCode.G))
            TryReleaseSelected();
    }

    void HandleClick()
    {
        if (selectedIdx >= 0)
        {
            // 타이포인트 클릭 체크
            CableTiePoint clicked = GetClickedTiePoint();
            if (clicked != null)
            {
                if (clicked.HasAnyBound && IsBoundToThis(clicked))
                    SelectTieForRelease(clicked);
                else
                    FixToTiePoint(clicked);
                return;
            }
            Deselect();
            return;
        }

        // 케이블 라인 클릭 체크
        if (IsClickingCable())
        {
            selectedIdx = GetBestControlPointIdx();
            foreach (var tie in CableTiePoint.All)
                tie.ShowIndicator(true);
            cableRenderer.SetColor(Color.cyan);
            return;
        }

        // 고정된 타이포인트 클릭 (케이블 선택 없이)
        CableTiePoint tie2 = GetClickedTiePoint();
        if (tie2 != null && tie2.HasAnyBound && IsBoundToThis(tie2))
            SelectTieForRelease(tie2);
    }

    // 케이블 라인을 화면상 픽셀 거리로 클릭 감지
    bool IsClickingCable()
    {
        if (cableRenderer == null) return false;
        var lr = GetComponent<LineRenderer>();
        if (lr == null) return false;

        Vector2 mousePos = Input.mousePosition;
        for (int i = 0; i < lr.positionCount; i++)
        {
            Vector3 screenPt = cam.WorldToScreenPoint(lr.GetPosition(i));
            if (screenPt.z <= 0) continue;
            if (Vector2.Distance(mousePos, new Vector2(screenPt.x, screenPt.y)) < clickRadiusPixels)
                return true;
        }
        return false;
    }

    // 클릭 지점과 가장 가까운 제어점 인덱스 반환
    int GetBestControlPointIdx()
    {
        var lr = GetComponent<LineRenderer>();
        if (lr == null || lr.positionCount == 0) return 0;

        Vector2 mousePos = Input.mousePosition;
        float bestDist = float.MaxValue;
        int bestIdx = 0;

        // 라인 전체에서 가장 가까운 점의 비율로 제어점 인덱스 추정
        for (int i = 0; i < lr.positionCount; i++)
        {
            Vector3 screenPt = cam.WorldToScreenPoint(lr.GetPosition(i));
            if (screenPt.z <= 0) continue;
            float dist = Vector2.Distance(mousePos, new Vector2(screenPt.x, screenPt.y));
            if (dist < bestDist)
            {
                bestDist = dist;
                // 제어점 인덱스로 매핑 (중간 구역)
                bestIdx = Mathf.Clamp(i * controlHandles.Count / Mathf.Max(lr.positionCount, 1),
                                      0, Mathf.Max(controlHandles.Count - 1, 0));
            }
        }
        return bestIdx;
    }

    void FixToTiePoint(CableTiePoint tie)
    {
        // 이미 이 인덱스에 고정된 게 있으면 해제
        if (tieMap.TryGetValue(selectedIdx, out var existing))
        {
            existing.Unbind(this);
            if (selectedIdx < controlHandles.Count)
            {
                cableRenderer.RemoveControlPoint(controlHandles[selectedIdx]);
                Destroy(controlHandles[selectedIdx].gameObject);
                controlHandles.RemoveAt(selectedIdx);
            }
            tieMap.Remove(selectedIdx);
        }

        // 새 제어점 핸들 생성
        var handleGO = new GameObject($"_TieHandle_{selectedIdx}");
        handleGO.transform.position = tie.transform.position;
        var handle = handleGO.transform;

        // controlHandles에 삽입
        int insertIdx = Mathf.Min(selectedIdx, controlHandles.Count);
        controlHandles.Insert(insertIdx, handle);
        cableRenderer.AddControlPoint(handle);

        // tieMap 업데이트
        tieMap[insertIdx] = tie;
        tie.Bind(this);

        Deselect();
    }

    void SelectTieForRelease(CableTiePoint tie)
    {
        selectedTieForRelease?.SetHighlight(false);
        selectedTieForRelease = tie;
        tie.SetHighlight(true);
    }

    void TryReleaseSelected()
    {
        if (selectedTieForRelease == null) return;

        // tieMap에서 해당 타이포인트 찾아서 해제
        int targetIdx = -1;
        foreach (var kv in tieMap)
        {
            if (kv.Value == selectedTieForRelease) { targetIdx = kv.Key; break; }
        }
        if (targetIdx < 0) return;

        selectedTieForRelease.Unbind(this);
        if (targetIdx < controlHandles.Count)
        {
            cableRenderer.RemoveControlPoint(controlHandles[targetIdx]);
            Destroy(controlHandles[targetIdx].gameObject);
            controlHandles.RemoveAt(targetIdx);
        }
        tieMap.Remove(targetIdx);
        selectedTieForRelease = null;
    }

    void Deselect()
    {
        selectedIdx = -1;
        foreach (var tie in CableTiePoint.All)
            if (!tie.HasAnyBound) tie.ShowIndicator(false);
        cableRenderer.SetColor(Color.white);
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

    bool IsBoundToThis(CableTiePoint tie)
    {
        foreach (var kv in tieMap)
            if (kv.Value == tie) return true;
        return false;
    }

    // CableBundler용
    public void SetColor(Color color) => cableRenderer?.SetColor(color);
}
