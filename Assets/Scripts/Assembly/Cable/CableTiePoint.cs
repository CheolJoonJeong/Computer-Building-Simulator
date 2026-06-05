using UnityEngine;
using System.Collections.Generic;

// 케이스 내부 케이블 고정 포인트
[RequireComponent(typeof(SphereCollider))]
public class CableTiePoint : MonoBehaviour
{
    public static readonly List<CableTiePoint> All = new();

    [SerializeField] private float indicatorScale = 0.06f;
    [SerializeField] private Color idleColor      = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0f, 0.5f);
    [SerializeField] private Color boundColor     = new Color(0f, 1f, 0.4f, 0.5f);

    // 이 타이포인트에 고정된 케이블 목록
    private readonly List<CableInteraction> boundCables = new();
    public bool HasAnyBound => boundCables.Count > 0;

    private GameObject indicator;
    private Material indicatorMat;

    void Awake()
    {
        All.Add(this);
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = indicatorScale;
        CreateIndicator();
        ShowIndicator(false);
    }

    void OnDestroy() => All.Remove(this);

    void CreateIndicator()
    {
        indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "_TieIndicator";
        indicator.transform.SetParent(transform, false);
        indicator.transform.localScale = Vector3.one * indicatorScale;
        Destroy(indicator.GetComponent<Collider>());

        indicatorMat = new Material(Shader.Find("Standard"));
        indicatorMat.SetFloat("_Mode", 3);
        indicatorMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        indicatorMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        indicatorMat.SetInt("_ZWrite", 0);
        indicatorMat.EnableKeyword("_ALPHABLEND_ON");
        indicatorMat.renderQueue = 3000;
        indicator.GetComponent<Renderer>().material = indicatorMat;
        SetColor(idleColor);
    }

    public void ShowIndicator(bool show)
    {
        if (indicator != null) indicator.SetActive(show);
    }

    public void SetHighlight(bool on)
    {
        if (!HasAnyBound) SetColor(on ? highlightColor : idleColor);
    }

    public void Bind(CableInteraction cable)
    {
        if (!boundCables.Contains(cable))
            boundCables.Add(cable);
        SetColor(boundColor);
        ShowIndicator(true);
    }

    public void Unbind(CableInteraction cable)
    {
        boundCables.Remove(cable);
        if (!HasAnyBound)
        {
            SetColor(idleColor);
            ShowIndicator(false);
        }
    }

    void SetColor(Color c)
    {
        if (indicatorMat != null) indicatorMat.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, indicatorScale);
    }
}
