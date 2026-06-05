using UnityEngine;
using System.Collections.Generic;

// 케이스 내부 케이블 정리 고정 포인트 — 여러 케이블이 동시에 고정 가능
public class CableTiePoint : MonoBehaviour
{
    // 씬 내 모든 타이포인트 캐시 (FindObjectsOfType 대체)
    public static readonly List<CableTiePoint> All = new();

    [SerializeField] private float snapRadius    = 0.12f;
    [SerializeField] private float indicatorScale = 0.06f;

    [Header("Colors")]
    [SerializeField] private Color idleColor      = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.8f, 0f, 0.5f);
    [SerializeField] private Color boundColor     = new Color(0f, 1f, 0.4f, 0.5f);

    public float SnapRadius => snapRadius;
    public bool HasAnyBound => boundEntries.Count > 0;

    private readonly List<(CableParticle particle, Transform handle)> boundEntries = new();
    private GameObject indicator;
    private Material indicatorMat;

    void Awake()
    {
        All.Add(this);
        CreateIndicator();
        ShowIndicator(false);
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    public void ShowIndicator(bool show)
    {
        if (indicator != null)
            indicator.SetActive(show);
    }

    void CreateIndicator()
    {
        indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.name = "_TiePointIndicator";
        indicator.transform.SetParent(this.transform, false);
        indicator.transform.localScale = Vector3.one * indicatorScale;
        Destroy(indicator.GetComponent<Collider>());

        indicatorMat = new Material(Shader.Find("Standard"));
        indicatorMat.SetFloat("_Mode", 3);
        indicatorMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        indicatorMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        indicatorMat.SetInt("_ZWrite", 0);
        indicatorMat.DisableKeyword("_ALPHATEST_ON");
        indicatorMat.EnableKeyword("_ALPHABLEND_ON");
        indicatorMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        indicatorMat.renderQueue = 3000;

        indicator.GetComponent<Renderer>().material = indicatorMat;
        SetColor(idleColor);
    }

    public void SetHighlight(bool on)
    {
        if (!HasAnyBound)
            SetColor(on ? highlightColor : idleColor);
    }

    // 케이블 파티클 고정 (여러 개 가능)
    public void BindParticle(CableParticle particle, Transform handle)
    {
        handle.position = this.transform.position;
        particle.Bind(handle);
        boundEntries.Add((particle, handle));
        SetColor(boundColor);
    }

    // 특정 파티클 해제
    public void ReleaseParticle(CableParticle particle)
    {
        for (int i = boundEntries.Count - 1; i >= 0; i--)
        {
            if (boundEntries[i].particle == particle)
            {
                particle.UnBind();
                boundEntries.RemoveAt(i);
                break;
            }
        }

        if (!HasAnyBound)
        {
            SetColor(idleColor);
            ShowIndicator(false);
        }
    }

    private void SetColor(Color color)
    {
        if (indicatorMat != null)
            indicatorMat.color = color;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, snapRadius);
    }
}
