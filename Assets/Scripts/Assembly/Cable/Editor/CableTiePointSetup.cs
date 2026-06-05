using UnityEngine;
using UnityEditor;

public class CableTiePointSetup : EditorWindow
{
    [MenuItem("Tools/Setup Cable Tie Points")]
    static void Run()
    {
        int count = 0;
        foreach (var go in FindObjectsOfType<GameObject>(true))
        {
            if (go.name.StartsWith("Tie_Point") || go.name.StartsWith("Pass_Through_Point"))
            {
                bool isTie = go.name.StartsWith("Tie_Point");

                if (isTie)
                {
                    if (go.GetComponent<CableTiePoint>() == null)
                    {
                        go.AddComponent<CableTiePoint>();
                        count++;
                        Debug.Log($"Added CableTiePoint to {go.name}");
                    }
                }
                else
                {
                    if (go.GetComponent<CablePassThrough>() == null)
                    {
                        go.AddComponent<CablePassThrough>();
                        // Collider 추가
                        if (go.GetComponent<Collider>() == null)
                        {
                            var col = go.AddComponent<SphereCollider>();
                            col.isTrigger = true;
                            col.radius = 0.05f;
                        }
                        count++;
                        Debug.Log($"Added CablePassThrough to {go.name}");
                    }
                }
            }
        }

        Debug.Log($"Setup complete. {count} components added.");
    }
}
