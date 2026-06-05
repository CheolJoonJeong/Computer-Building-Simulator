using UnityEngine;
using System.Collections.Generic;

// B키로 여러 케이블을 같은 타이포인트로 묶기
public class CableBundler : MonoBehaviour
{
    public static CableBundler Instance { get; private set; }

    private readonly List<CableInteraction> selected = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && selected.Count >= 2)
            Bundle();
    }

    public void Toggle(CableInteraction cable)
    {
        if (selected.Contains(cable)) { selected.Remove(cable); cable.SetColor(Color.white); }
        else { selected.Add(cable); cable.SetColor(Color.magenta); }
    }

    void Bundle()
    {
        // 선택된 케이블 색 초기화 (실제 묶음은 타이포인트 고정으로 표현)
        foreach (var c in selected) c.SetColor(Color.white);
        selected.Clear();
        Debug.Log("[CableBundler] Bundled selected cables.");
    }
}
