using UnityEngine;
using System.Collections.Generic;

// 케이블 정리 상태 검사
// 케이스 내부 박스 범위 밖에 있는 파티클을 노출로 판단
public class CableCleanChecker : MonoBehaviour
{
    [System.Serializable]
    public class CableCheckEntry
    {
        public string cableName;
        public CableComponent cable;
        [Tooltip("박스 밖에 허용되는 최대 파티클 수")]
        public int maxAllowedParticles = 2;
    }

    [Header("케이스 내부 범위 (박스)")]
    [Tooltip("케이스 내부 박스의 중심")]
    [SerializeField] private Vector3 boxCenter = Vector3.zero;
    [Tooltip("케이스 내부 박스의 크기")]
    [SerializeField] private Vector3 boxSize = new Vector3(1f, 1f, 1f);

    [Header("케이블 목록")]
    [SerializeField] private List<CableCheckEntry> cables = new();

    [Header("결과")]
    [SerializeField] private bool isClean = false;
    public bool IsClean => isClean;

    // 검사 실행 — UI 버튼이나 외부에서 호출
    public bool Check()
    {
        isClean = true;

        Bounds box = new Bounds(boxCenter, boxSize);

        foreach (var entry in cables)
        {
            if (entry.cable == null) continue;
            if (entry.cable.Points == null) continue;

            int exposedCount = 0;
            foreach (var particle in entry.cable.Points)
            {
                if (!box.Contains(particle.Position))
                    exposedCount++;
            }

            if (exposedCount > entry.maxAllowedParticles)
            {
                Debug.Log($"[CableCleanChecker] '{entry.cableName}' exposed particles: {exposedCount} (allowed: {entry.maxAllowedParticles}) — needs cleanup");
                isClean = false;
            }
            else
            {
                Debug.Log($"[CableCleanChecker] '{entry.cableName}' clean ({exposedCount} exposed)");
            }
        }

        if (isClean)
            Debug.Log("[CableCleanChecker] All cables clean!");
        else
            Debug.Log("[CableCleanChecker] Some cables need cleanup.");

        return isClean;
    }

    // 에디터에서 박스 범위 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawCube(boxCenter, boxSize);
        Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}
