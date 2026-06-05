using UnityEngine;
using System.Collections.Generic;

// 케이블 정리 상태 검사
public class CableCleanChecker : MonoBehaviour
{
    [System.Serializable]
    public class CableCheckEntry
    {
        public string cableName;
        public CableInteraction cable;
        [Tooltip("최소 고정 타이포인트 수")]
        public int minTiePoints = 2;
    }

    [SerializeField] private List<CableCheckEntry> cables = new();

    [Header("Result")]
    [SerializeField] private bool isClean = false;
    public bool IsClean => isClean;

    public bool Check()
    {
        isClean = true;

        foreach (var entry in cables)
        {
            if (entry.cable == null) continue;

            var lr = entry.cable.GetComponent<LineRenderer>();
            if (lr == null) continue;

            // 타이포인트 고정 수로 정리 여부 판단
            int tieCount = 0;
            foreach (var tie in CableTiePoint.All)
            {
                if (tie.HasAnyBound) tieCount++;
            }

            if (tieCount < entry.minTiePoints)
            {
                Debug.Log($"[CableCleanChecker] '{entry.cableName}' needs {entry.minTiePoints} tie points, has {tieCount}.");
                isClean = false;
            }
            else
            {
                Debug.Log($"[CableCleanChecker] '{entry.cableName}' clean.");
            }
        }

        Debug.Log(isClean ? "[CableCleanChecker] All cables clean!" : "[CableCleanChecker] Some cables need cleanup.");
        return isClean;
    }
}
