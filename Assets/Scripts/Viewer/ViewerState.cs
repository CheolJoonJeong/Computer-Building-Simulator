using UnityEngine;

// Assembly <-> Viewer 씬 전환 시에도 유지되는 뷰어 상태 (PartSelectionManager와 동일한 순수 static 패턴)
public static class ViewerState
{
    public static int LastViewedIndex = 0;

    public static void Clear()
    {
        LastViewedIndex = 0;
    }
}
