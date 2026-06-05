using UnityEngine;
using System.Collections.Generic;

// 케이블 정리 상태 검사 — 각 케이블이 최소 고정 개수를 만족하는지
public class CableCleanChecker : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string cableName;
        public CableInteraction cable;
        [Tooltip("최소 타이포인트 고정 수")]
        public int minTiePoints = 2;
    }

    [SerializeField] private List<Entry> cables = new();

    [Header("Result")]
    [SerializeField] private bool isClean = false;
    public bool IsClean => isClean;

    // UI 버튼에서 호출
    public bool Check()
    {
        isClean = true;
        foreach (var e in cables)
        {
            if (e.cable == null) continue;
            if (e.cable.BoundCount < e.minTiePoints)
            {
                Debug.Log($"[CleanCheck] '{e.cableName}' needs {e.minTiePoints} ties, has {e.cable.BoundCount}.");
                isClean = false;
            }
            else
            {
                Debug.Log($"[CleanCheck] '{e.cableName}' OK.");
            }
        }
        Debug.Log(isClean ? "[CleanCheck] All cables clean!" : "[CleanCheck] Some cables need cleanup.");
        return isClean;
    }
}
