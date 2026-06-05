using UnityEngine;
using UnityEditor;

// 케이블 GameObject를 컴포넌트 구조까지 자동 생성
// Tools/Cable/Create Cable Object 메뉴로 생성 후 프리팹화하면 됨
public static class CableBuilder
{
    [MenuItem("Tools/Cable/Create Cable Object (24pin)")]
    static void Create24() => Build("Cable_24pin", CableType.ATX24Pin, 0.012f);

    [MenuItem("Tools/Cable/Create Cable Object (CPU 8pin)")]
    static void CreateCPU() => Build("Cable_CPU_8pin", CableType.CPU8Pin, 0.008f);

    [MenuItem("Tools/Cable/Create Cable Object (PCIe 8pin)")]
    static void CreatePCIe() => Build("Cable_PCIe_8pin", CableType.PCIe8Pin, 0.008f);

    [MenuItem("Tools/Cable/Create Cable Object (Fan)")]
    static void CreateFan() => Build("Cable_Fan", CableType.FanHeader, 0.005f);

    static void Build(string name, CableType type, float headSize)
    {
        var root = new GameObject(name);

        var lr = root.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;

        root.AddComponent<CableComponent>();
        root.AddComponent<CableInteraction>();

        // StartPoint
        var start = MakeHead("StartPoint", root.transform, headSize, type, false);
        // EndPoint (오른쪽으로 약간 떨어뜨려 초기 방향 제공)
        var end = MakeHead("EndPoint", root.transform, headSize, type, true);
        end.transform.localPosition = new Vector3(0.3f, 0f, 0f);

        Selection.activeGameObject = root;
        Debug.Log($"[CableBuilder] Created {name}. Assign cable material on LineRenderer, then drag into Assets/Prefab/Cable.");
    }

    static GameObject MakeHead(string name, Transform parent, float size, CableType type, bool isEnd)
    {
        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = name;
        head.transform.SetParent(parent, false);
        head.transform.localScale = new Vector3(size * 2f, size, size * 1.5f);
        // 헤드 콜라이더는 클릭/물리 간섭 방지 위해 제거
        Object.DestroyImmediate(head.GetComponent<Collider>());

        var conn = head.AddComponent<CableConnector>();
        var so = new SerializedObject(conn);
        so.FindProperty("cableType").enumValueIndex = (int)type;
        so.FindProperty("isEndPoint").boolValue = isEnd;
        so.ApplyModifiedProperties();

        return head;
    }
}
