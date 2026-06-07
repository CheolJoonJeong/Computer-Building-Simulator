using UnityEngine;

// 케이스 구멍 통과점 — 라우팅 중 클릭하면 케이블이 이 위치를 경유
[RequireComponent(typeof(Collider))]
public class CablePassThrough : MonoBehaviour
{
    [SerializeField] private Renderer holeRenderer;
    [SerializeField] private Color idleColor      = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0f, 0.6f);
    [SerializeField] private Color passedColor    = new Color(0f, 1f, 0.4f, 0.6f);

    [Tooltip("이 통과점을 클릭하면 함께 추가될 강제 경유점들 (순서대로, 이 통과점 자신 포함하지 않음)")]
    [SerializeField] private Transform[] forcedRoute;

    public Transform[] ForcedRoute => forcedRoute;

    private bool passed = false;

    void Awake() => GetComponent<Collider>().isTrigger = true;

    void Update()
    {
        if (holeRenderer == null || passed) return;
        bool routing = CableManager.Instance != null && CableManager.Instance.IsRouting;
        SetColor(routing ? highlightColor : idleColor);
    }

    void OnMouseDown()
    {
        if (CableManager.Instance == null || !CableManager.Instance.IsRouting) return;
        CableManager.Instance.OnPassThroughClicked(this);
        passed = true;
        SetColor(passedColor);
    }

    void SetColor(Color c)
    {
        if (holeRenderer != null) holeRenderer.material.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.04f);
    }
}
