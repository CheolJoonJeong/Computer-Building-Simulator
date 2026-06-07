using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartViewer : MonoBehaviour
{
    [Header("부품 (세 배열 순서 일치 필수!)")]
    public GameObject[] parts;        // 부품 오브젝트들
    public Button[] buttons;          // 부품 버튼들 (parts와 같은 순서)
    [TextArea(2, 5)]
    public string[] descriptions;     // 부품 설명들 (parts와 같은 순서)

    [Header("UI 참조")]
    public TMP_Text partNameText;     // 부품 이름 (선택)
    public TMP_Text descriptionText;  // 설명 패널 텍스트
    public Transform pivot;           // 회전 중심

    [Header("버튼 색상")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.35f, 0.35f, 0.35f);

    private int currentIndex = 0;

    void Start()
    {
        if (parts.Length > 0)
            ShowPart(0);
    }

    public void ShowPart(int index)
    {
        // 1. 부품 켜고 끄기
        for (int i = 0; i < parts.Length; i++)
            parts[i].SetActive(i == index);
        currentIndex = index;

        // 2. 중심 맞추기 (피벗 보정)
        CenterPart(parts[index]);

        // 3. 버튼 색: 선택된 것만 진하게
        for (int i = 0; i < buttons.Length; i++)
        {
            Image img = buttons[i].GetComponent<Image>();
            if (img != null)
                img.color = (i == index) ? selectedColor : normalColor;
        }

        // 4. 이름 표시
        if (partNameText != null)
            partNameText.text = parts[index].name;

        // 5. 설명 표시
        if (descriptionText != null && index < descriptions.Length)
            descriptionText.text = descriptions[index];
    }

    void CenterPart(GameObject part)
    {
        Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);
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