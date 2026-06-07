using UnityEngine;
using UnityEditor;

// 씬의 Tie_Point* / Pass_Through_Point* 오브젝트에 컴포넌트 일괄 부착
public static class CableSetup
{
    [MenuItem("Tools/Cable/Setup Tie & Pass-Through Points")]
    static void Run()
    {
        int count = 0;
        foreach (var go in Object.FindObjectsOfType<GameObject>(true))
        {
            if (go.name.StartsWith("Tie_Point") && go.name != "Tie_Points")
            {
                if (go.GetComponent<CableTiePoint>() == null)
                {
                    go.AddComponent<CableTiePoint>();
                    count++;
                }
            }
            else if (go.name.StartsWith("Pass_Through_Point") && go.name != "Pass_Through_Points")
            {
                if (go.GetComponent<CablePassThrough>() == null)
                {
                    if (go.GetComponent<Collider>() == null)
                    {
                        var c = go.AddComponent<SphereCollider>();
                        c.isTrigger = true;
                        c.radius = 0.08f;
                    }
                    go.AddComponent<CablePassThrough>();
                    count++;
                }
            }
        }
        Debug.Log($"[CableSetup] {count} components added.");
    }
}
