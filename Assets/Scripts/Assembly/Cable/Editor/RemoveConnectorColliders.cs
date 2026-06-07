using UnityEngine;
using UnityEditor;

// 케이블 StartPoint/EndPoint(CableConnector) 콜라이더 제거
// — CableComponent의 OverlapSphere 자기충돌(EndPoint와의 끼임 진동) 방지용
public class RemoveConnectorColliders : EditorWindow
{
    [MenuItem("Tools/Remove Cable Connector Colliders")]
    static void Run()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/Cable" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;

            foreach (var connector in root.GetComponentsInChildren<CableConnector>(true))
            {
                var col = connector.GetComponent<Collider>();
                if (col != null)
                {
                    Object.DestroyImmediate(col, true);
                    count++;
                    changed = true;
                }
            }

            if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log($"Removed {count} colliders from cable connectors (StartPoint/EndPoint).");
    }
}
