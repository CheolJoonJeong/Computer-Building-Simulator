using UnityEngine;
using System.Collections.Generic;

// 케이블 묶기 — Shift+클릭으로 여러 케이블의 한 점을 선택, B키로 한 점에 묶음
// 묶인 뒤에도 각 케이블의 나머지는 자유롭게 움직임
public class CableBundler : MonoBehaviour
{
    public static CableBundler Instance { get; private set; }

    private readonly List<(CableInteraction cable, int particle)> selected = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && selected.Count >= 2)
            Bundle();
    }

    // CableInteraction 에서 Shift+클릭 시 호출
    public void AddSelection(CableInteraction cable, int particle)
    {
        // 같은 케이블 중복 선택 방지 (마지막 클릭 지점으로 갱신)
        for (int i = 0; i < selected.Count; i++)
        {
            if (selected[i].cable == cable)
            {
                selected[i] = (cable, particle);
                return;
            }
        }
        selected.Add((cable, particle));
        cable.HighlightBundle(true);
    }

    void Bundle()
    {
        // 선택된 지점들의 평균 위치에 공유 앵커 생성
        Vector3 avg = Vector3.zero;
        foreach (var s in selected) avg += s.cable.GetParticleWorld(s.particle);
        avg /= selected.Count;

        var anchor = new GameObject("_BundleAnchor");
        anchor.transform.position = avg;

        // 각 케이블의 선택 지점을 공유 앵커에 고정
        foreach (var s in selected)
        {
            s.cable.ApplyBundle(s.particle, anchor.transform);
            s.cable.HighlightBundle(false);
        }

        selected.Clear();
        Debug.Log("[CableBundler] Bundled cables at one point.");
    }
}
