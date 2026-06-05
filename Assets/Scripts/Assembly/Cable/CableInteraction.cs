using UnityEngine;
using System.Collections.Generic;

// 케이블 정리(클린업) 상호작용 — 케이블 루트에 부착
// 케이블 클릭 → 타이포인트 표시 → 타이포인트 클릭 → 해당 파티클 고정
// G키 → 선택한 타이포인트 고정 해제 / B키(번들러) 와 함께 동작
[RequireComponent(typeof(CableComponent))]
public class CableInteraction : MonoBehaviour
{
    [SerializeField] private float clickRadiusPixels = 30f;

    private CableComponent cable;
    private Camera cam;

    private int selectedParticle = -1;                 // 케이블에서 선택한 파티클
    private CableTiePoint selectedTieForRelease;       // G키로 해제할 타이포인트
    private readonly Dictionary<int, CableTiePoint> tieMap = new(); // 파티클 -> 타이포인트

    void Awake()
    {
        cable = GetComponent<CableComponent>();
        cam = Camera.main;
    }

    void Update()
    {
        if (cam == null || !cable.IsInitialized) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Shift+클릭 → 묶기 선택, 일반 클릭 → 정리
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                TryBundleSelect();
            else
                HandleClick();
        }
        if (Input.GetKeyDown(KeyCode.G)) ReleaseSelected();
    }

    void TryBundleSelect()
    {
        int idx = GetClickedParticle();
        if (idx >= 0)
            CableBundler.Instance?.AddSelection(this, idx);
    }

    // ---- 번들러 연동 ----
    public Vector3 GetParticleWorld(int i) => cable.GetParticle(i);

    public void ApplyBundle(int particle, Transform anchor)
    {
        cable.PinParticle(particle, anchor);
    }

    public void HighlightBundle(bool on) => cable.SetColor(on ? Color.magenta : Color.white);

    void HandleClick()
    {
        // 파티클이 선택된 상태 → 타이포인트 클릭 시 고정
        if (selectedParticle >= 0)
        {
            CableTiePoint tie = GetClickedTiePoint();
            if (tie != null)
            {
                if (tie.HasAnyBound && IsBoundToThis(tie))
                    SelectTieForRelease(tie);
                else
                    FixToTiePoint(tie);
                return;
            }
            Deselect();
            return;
        }

        // 케이블 클릭 → 파티클 선택
        int idx = GetClickedParticle();
        if (idx >= 0)
        {
            selectedParticle = idx;
            foreach (var t in CableTiePoint.All) t.ShowIndicator(true);
            cable.SetColor(Color.cyan);
            return;
        }

        // 고정된 타이포인트 직접 클릭 → 해제 선택
        CableTiePoint clicked = GetClickedTiePoint();
        if (clicked != null && clicked.HasAnyBound && IsBoundToThis(clicked))
            SelectTieForRelease(clicked);
    }

    int GetClickedParticle()
    {
        int idx = cable.FindClosestMiddleParticle(cam, Input.mousePosition, out float dist);
        return (idx >= 0 && dist <= clickRadiusPixels) ? idx : -1;
    }

    void FixToTiePoint(CableTiePoint tie)
    {
        // 도달 범위 검증 (늘어남 방지)
        float segLen = cable.SegmentLength;
        float maxFromStart = selectedParticle * segLen;
        float maxFromEnd = (cable.Segments - selectedParticle) * segLen;
        float dStart = Vector3.Distance(cable.GetParticle(0), tie.transform.position);
        float dEnd = Vector3.Distance(cable.GetParticle(cable.Segments), tie.transform.position);

        if (dStart > maxFromStart * 1.02f || dEnd > maxFromEnd * 1.02f)
        {
            Debug.Log("[Cable] Tie point out of reach for this point.");
            Deselect();
            return;
        }

        // 같은 파티클에 이미 고정돼 있으면 교체
        if (tieMap.TryGetValue(selectedParticle, out var old))
        {
            old.Unbind(this);
            cable.UnpinParticle(selectedParticle);
            tieMap.Remove(selectedParticle);
        }

        cable.PinParticle(selectedParticle, tie.transform);
        tie.Bind(this);
        tieMap[selectedParticle] = tie;

        Deselect();
    }

    void SelectTieForRelease(CableTiePoint tie)
    {
        selectedTieForRelease?.SetHighlight(false);
        selectedTieForRelease = tie;
        tie.SetHighlight(true);
    }

    void ReleaseSelected()
    {
        if (selectedTieForRelease == null) return;

        int target = -1;
        foreach (var kv in tieMap)
            if (kv.Value == selectedTieForRelease) { target = kv.Key; break; }

        if (target >= 0)
        {
            selectedTieForRelease.Unbind(this);
            cable.UnpinParticle(target);
            tieMap.Remove(target);
        }
        selectedTieForRelease = null;
    }

    void Deselect()
    {
        selectedParticle = -1;
        foreach (var t in CableTiePoint.All)
            if (!t.HasAnyBound) t.ShowIndicator(false);
        cable.SetColor(Color.white);
    }

    CableTiePoint GetClickedTiePoint()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        int mask = ~LayerMask.GetMask("Ignore Raycast", "AssembledPart");
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mask))
            return hit.collider.GetComponent<CableTiePoint>()
                ?? hit.collider.GetComponentInParent<CableTiePoint>();
        return null;
    }

    bool IsBoundToThis(CableTiePoint tie)
    {
        foreach (var kv in tieMap)
            if (kv.Value == tie) return true;
        return false;
    }

    public int BoundCount => tieMap.Count;
    public void SetColor(Color c) => cable.SetColor(c);
}
