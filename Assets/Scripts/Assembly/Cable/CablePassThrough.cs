using UnityEngine;

// 케이스 구멍에 배치 — 클릭하면 대기 중인 케이블 끝점이 이 위치를 통과
// Collider가 필요함 (Is Trigger 체크)
public class CablePassThrough : MonoBehaviour
{
    [Tooltip("통과 후 끝점이 이 위치에 고정됨")]
    [SerializeField] private bool highlightOnPending = true;

    [Header("Visual")]
    [SerializeField] private Renderer holeRenderer;
    [SerializeField] private Color idleColor   = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0f, 0.6f);
    [SerializeField] private Color passedColor = new Color(0f, 1f, 0.4f, 0.6f);

    private bool isPassed = false;

    void Update()
    {
        if (holeRenderer == null) return;

        if (isPassed)
        {
            SetColor(passedColor);
            return;
        }

        // 케이블 대기 중일 때 하이라이트
        bool pending = highlightOnPending &&
                       CableManager.Instance != null &&
                       CableManager.Instance.HasPending;
        SetColor(pending ? highlightColor : idleColor);
    }

    void OnMouseDown()
    {
        if (CableManager.Instance == null) return;
        if (!CableManager.Instance.HasPending) return;

        // 대기 중인 끝점을 이 위치로 이동 후 다시 대기 상태 유지
        CableManager.Instance.MoveEndPointTo(transform.position);
        isPassed = true;
    }

    private void SetColor(Color color)
    {
        if (holeRenderer != null)
            holeRenderer.material.color = color;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, 0.03f);
    }
}
