using UnityEngine;

// 지정한 영역(BoxCollider, 트리거)에 케이블이 얼마나 들어와 있는지 검사
// 평가용: 전면 부품 칸 등에 케이블이 지나가면 감점 등에 활용
[RequireComponent(typeof(BoxCollider))]
public class CableAreaChecker : MonoBehaviour
{
    [System.Serializable]
    public class Threshold
    {
        [Tooltip("이 개수 '이상'이면 해당 점수 적용 (큰 값부터 검사)")]
        public int minCount;
        public int score;
        public string label;
    }

    [Tooltip("개수 임계값 목록 — minCount가 큰 순서로 적어두면 자동 정렬됨")]
    [SerializeField] private Threshold[] thresholds;

    private BoxCollider area;

    public int OverlappingParticleCount { get; private set; }
    public bool HasCableInside => OverlappingParticleCount > 0;

    void Awake()
    {
        area = GetComponent<BoxCollider>();
        area.isTrigger = true;
    }

    // 평가 시점에 호출 — 영역 안에 있는 케이블 파티클 개수를 센다
    public int CheckArea()
    {
        OverlappingParticleCount = 0;

        Vector3 center = area.transform.TransformPoint(area.center);
        Vector3 halfExtents = Vector3.Scale(area.size * 0.5f, area.transform.lossyScale);
        Quaternion orientation = area.transform.rotation;

        foreach (var cable in FindObjectsOfType<CableComponent>())
        {
            if (!cable.IsInitialized) continue;

            for (int i = 0; i <= cable.Segments; i++)
            {
                Vector3 p = cable.GetParticle(i);
                if (IsInsideBox(p, center, halfExtents, orientation))
                    OverlappingParticleCount++;
            }
        }

        return OverlappingParticleCount;
    }

    private bool IsInsideBox(Vector3 point, Vector3 center, Vector3 halfExtents, Quaternion orientation)
    {
        Vector3 local = Quaternion.Inverse(orientation) * (point - center);
        return Mathf.Abs(local.x) <= halfExtents.x
            && Mathf.Abs(local.y) <= halfExtents.y
            && Mathf.Abs(local.z) <= halfExtents.z;
    }

    // CheckArea() 호출 후 사용 — 개수에 맞는 임계값 항목 반환 (없으면 null)
    public Threshold Evaluate()
    {
        Threshold best = null;
        foreach (var t in thresholds)
        {
            if (OverlappingParticleCount >= t.minCount)
            {
                if (best == null || t.minCount > best.minCount)
                    best = t;
            }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
