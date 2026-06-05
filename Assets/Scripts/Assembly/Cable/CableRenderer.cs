using UnityEngine;
using System.Collections.Generic;

// Bezier 곡선으로 케이블을 그리는 컴포넌트
// 끝점 두 개 + 타이포인트(중간 제어점)로 자연스러운 곡선 표현
[RequireComponent(typeof(LineRenderer))]
public class CableRenderer : MonoBehaviour
{
    [SerializeField] private int resolution = 20;   // 곡선 분할 수
    [SerializeField] private float sag = 0.3f;      // 처짐 정도
    [SerializeField] private float cableWidth = 0.02f;
    [SerializeField] private Material cableMaterial;

    private LineRenderer line;
    private Transform startPoint;
    private Transform endPoint;

    // 타이포인트 순서대로 등록 (인덱스 = 제어점 순서)
    private readonly List<Transform> controlPoints = new();

    public void Init(Transform start, Transform end)
    {
        startPoint = start;
        endPoint = end;

        line = GetComponent<LineRenderer>();
        line.startWidth = cableWidth;
        line.endWidth = cableWidth;
        if (cableMaterial != null) line.material = cableMaterial;
        line.alignment = LineAlignment.TransformZ;
    }

    public void AddControlPoint(Transform point)
    {
        controlPoints.Add(point);
    }

    public void RemoveControlPoint(Transform point)
    {
        controlPoints.Remove(point);
    }

    void LateUpdate()
    {
        if (startPoint == null || endPoint == null) return;
        DrawCable();
    }

    void DrawCable()
    {
        // 제어점 목록: start → controlPoints → end
        var pts = new List<Vector3>();
        pts.Add(startPoint.position);
        foreach (var cp in controlPoints)
            if (cp != null) pts.Add(cp.position);
        pts.Add(endPoint.position);

        // 각 구간을 Bezier로 연결
        var positions = new List<Vector3>();
        for (int seg = 0; seg < pts.Count - 1; seg++)
        {
            Vector3 p0 = pts[seg];
            Vector3 p1 = pts[seg + 1];

            // 처짐 제어점 (중간에서 아래로)
            Vector3 mid = (p0 + p1) * 0.5f + Vector3.down * sag;

            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Vector3 pos = QuadraticBezier(p0, mid, p1, t);
                if (seg == 0 || i > 0) // 중복 제거
                    positions.Add(pos);
            }
        }

        line.positionCount = positions.Count;
        line.SetPositions(positions.ToArray());
    }

    Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    public void SetColor(Color color)
    {
        if (line != null) line.material.color = color;
    }
}
