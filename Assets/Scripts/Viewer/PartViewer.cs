using UnityEngine;
using TMPro;

public class PartViewer : MonoBehaviour
{
    public GameObject[] parts;
    public TMP_Text partNameText;
    public Transform pivot;          // ★ 추가: 회전 중심(0,0,0)인 Pivot 지정

    private int currentIndex = 0;

    void Start()
    {
        if (parts.Length > 0)
            ShowPart(0);
    }

    public void ShowPart(int index)
    {
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].SetActive(i == index);
        }
        currentIndex = index;

        CenterPart(parts[index]);    // ★ 추가: 메시 중심을 Pivot에 맞춤

        if (partNameText != null)
            partNameText.text = parts[index].name;
    }

    // ★ 추가: 부품 메시의 실제 중심을 Pivot 위치로 이동
    void CenterPart(GameObject part)
    {
        Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 모든 Renderer를 감싸는 전체 bounds 계산
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        // 메시 중심과 Pivot 위치의 차이만큼 부품을 이동
        Vector3 targetCenter = (pivot != null) ? pivot.position : Vector3.zero;
        Vector3 offset = bounds.center - targetCenter;
        part.transform.position -= offset;
    }

    public void NextPart()
    {
        ShowPart((currentIndex + 1) % parts.Length);
    }

    public void PrevPart()
    {
        ShowPart((currentIndex - 1 + parts.Length) % parts.Length);
    }
}