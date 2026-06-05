using UnityEngine;

// 케이스 구멍 통과 포인트 — 클릭하면 케이블 끝점이 이 위치를 경유
[RequireComponent(typeof(Collider))]
public class CablePassThrough : MonoBehaviour
{
    [SerializeField] private Renderer holeRenderer;
    [SerializeField] private Color idleColor      = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0f, 0.6f);
    [SerializeField] private Color passedColor    = new Color(0f, 1f, 0.4f, 0.6f);

    private bool isPassed = false;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (holeRenderer == null || isPassed) return;
        bool pending = CableManager.Instance != null && CableManager.Instance.HasPending;
        SetColor(pending ? highlightColor : idleColor);
    }

    void OnMouseDown()
    {
        if (CableManager.Instance == null) return;
        if (!CableManager.Instance.HasPending) return;

        CableManager.Instance.MoveEndPointTo(transform.position);
        isPassed = true;
        SetColor(passedColor);
    }

    void SetColor(Color c)
    {
        if (holeRenderer != null) holeRenderer.material.color = c;
    }
}
