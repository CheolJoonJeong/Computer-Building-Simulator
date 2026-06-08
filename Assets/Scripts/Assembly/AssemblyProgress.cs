using System.Collections.Generic;

// Assembly <-> Viewer 씬 전환 시에도 조립/케이블 연결 상태를 유지하기 위한 순수 static 저장소
// (PartSelectionManager / ViewerState와 동일한 패턴)
public static class AssemblyProgress
{
    // 장착된 슬롯의 GameObject 이름 집합
    public static readonly HashSet<string> SnappedSlots = new();
    // 플레이어가 직접 분리한 슬롯 — startOccupied 슬롯이라도 씬 재로드 시 자동 재장착하지 않도록 함
    public static readonly HashSet<string> DetachedSlots = new();

    public static void Clear()
    {
        SnappedSlots.Clear();
        DetachedSlots.Clear();
    }
}
