using UnityEngine;
using System.Collections.Generic;

// B키로 여러 케이블 묶기
public class CableBundler : MonoBehaviour
{
    public static CableBundler Instance { get; private set; }

    private readonly List<CableInteraction> selected = new();

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

    public void Toggle(CableInteraction cable)
    {
        if (selected.Contains(cable)) { selected.Remove(cable); cable.SetColor(Color.white); }
        else { selected.Add(cable); cable.SetColor(Color.cyan); }
    }

    void Bundle()
    {
        // 선택된 케이블들의 중간 위치로 모으기
        Vector3 avg = Vector3.zero;
        foreach (var c in selected)
        {
            var lr = c.GetComponent<LineRenderer>();
            if (lr != null && lr.positionCount > 0)
                avg += lr.GetPosition(lr.positionCount / 2);
        }
        avg /= selected.Count;

        // 가장 가까운 타이포인트 찾기
        CableTiePoint best = null;
        float bestDist = float.MaxValue;
        foreach (var tie in CableTiePoint.All)
        {
            float d = Vector3.Distance(avg, tie.transform.position);
            if (d < bestDist) { bestDist = d; best = tie; }
        }

        foreach (var c in selected) c.SetColor(Color.white);
        selected.Clear();
    }
}
