using UnityEngine;
using System.Collections.Generic;

// 케이스 구멍 통과점 — 라우팅 중 클릭하면 케이블이 이 위치를 경유
[RequireComponent(typeof(Collider))]
public class CablePassThrough : MonoBehaviour
{
    public static readonly List<CablePassThrough> All = new();

    [SerializeField] private float indicatorScale = 0.05f;
    [SerializeField] private Color idleColor      = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0f, 0.6f);
    [SerializeField] private Color passedColor    = new Color(0f, 1f, 0.4f, 0.6f);

    [Tooltip("이 통과점을 클릭하면 함께 추가될 강제 경유점들 (순서대로, 이 통과점 자신 포함하지 않음)")]
    [SerializeField] private Transform[] forcedRoute;

    public Transform[] ForcedRoute => forcedRoute;

    private bool passed = false;
    private bool selected = false;   // 케이블 클릭(정리 모드) 시 타이포인트처럼 표시

    private GameObject indicator;
    private Material indicatorMat;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        All.Add(this);

        // 씬에 미리 배치된 구멍 시각 메쉬가 있다면 평소엔 꺼둠 (우리가 만든 인디케이터로 대체)
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        CreateIndicator();
        SetColor(idleColor);
        ShowIndicator(false);
    }

    public void ShowIndicator(bool show)
    {
        if (indicator != null) indicator.SetActive(show);
    }

    void OnDestroy() => All.Remove(this);

    void CreateIndicator()
    {
        indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "_PassThroughIndicator";
        indicator.transform.SetParent(transform, false);
        indicator.transform.localScale = Vector3.one * indicatorScale * 2f;
        Destroy(indicator.GetComponent<Collider>());

        indicatorMat = new Material(Shader.Find("Standard"));
        indicatorMat.SetFloat("_Mode", 3);
        indicatorMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        indicatorMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        indicatorMat.SetInt("_ZWrite", 0);
        indicatorMat.EnableKeyword("_ALPHABLEND_ON");
        indicatorMat.renderQueue = 3000;
        indicator.GetComponent<Renderer>().material = indicatorMat;
    }

    // 케이블 선택/해제 시 CableInteraction에서 호출 — 타이포인트처럼 색만 전환
    public void SetSelected(bool on)
    {
        selected = on;
        ShowIndicator(on);
        if (on) SetColor(passed ? passedColor : highlightColor);
    }

    // 클릭 처리는 CableManager.Update()에서 RaycastAll로 중앙 처리 (소켓과 동일한 방식)
    public bool Passed => passed;

    public void MarkPassed()
    {
        passed = true;
        SetColor(passedColor);
    }

    // 라우팅 되돌리기 시 통과 표시 해제
    public void UnmarkPassed()
    {
        passed = false;
        SetColor(selected ? highlightColor : idleColor);
    }

    void SetColor(Color c)
    {
        if (indicatorMat != null) indicatorMat.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.04f);
    }
}
