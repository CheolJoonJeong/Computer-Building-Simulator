using UnityEngine;
using System.Collections.Generic;

// 케이블 묶기 관리자 — 씬에 하나만 배치
// 클릭으로 케이블 선택, B키로 묶기
public class CableBundler : MonoBehaviour
{
    public static CableBundler Instance { get; private set; }

    [SerializeField] private Color selectedColor = Color.cyan;

    private readonly List<CableDragInteraction> selectedCables = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && selectedCables.Count >= 2)
            BundleSelected();
    }

    // CableDragInteraction에서 클릭(드래그 없이) 시 호출
    public void ToggleSelect(CableDragInteraction cable)
    {
        if (selectedCables.Contains(cable))
        {
            selectedCables.Remove(cable);
            cable.SetLineColor(Color.white);
        }
        else
        {
            selectedCables.Add(cable);
            cable.SetLineColor(selectedColor);
        }
    }

    void BundleSelected()
    {
        // 선택된 케이블들 중 타이포인트에 고정된 것 찾기
        CableTiePoint targetTie = null;
        foreach (var cable in selectedCables)
        {
            var tie = cable.BoundTiePoint;
            if (tie != null) { targetTie = tie; break; }
        }

        // 타이포인트가 없으면 선택 케이블들의 중간 평균 위치로 모으기
        if (targetTie == null)
        {
            Vector3 avg = Vector3.zero;
            int count = 0;
            foreach (var cable in selectedCables)
            {
                if (cable.MidParticlePosition.HasValue)
                {
                    avg += cable.MidParticlePosition.Value;
                    count++;
                }
            }
            if (count == 0) return;
            avg /= count;

            foreach (var cable in selectedCables)
                cable.SnapMidToPosition(avg);
        }
        else
        {
            foreach (var cable in selectedCables)
                cable.SnapMidToTiePoint(targetTie);
        }

        // 선택 해제
        foreach (var cable in selectedCables)
            cable.SetLineColor(Color.white);
        selectedCables.Clear();
    }
}
