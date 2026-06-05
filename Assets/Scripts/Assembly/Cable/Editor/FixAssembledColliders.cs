using UnityEngine;
using UnityEditor;

public class FixAssembledColliders : EditorWindow
{
    [MenuItem("Tools/Fix Assembled Part Colliders")]
    static void Run()
    {
        int count = 0;
        foreach (var col in FindObjectsOfType<Collider>(true))
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("AssembledPart"))
            {
                col.enabled = true;
                count++;
            }
        }
        Debug.Log($"Fixed {count} colliders on AssembledPart layer.");
    }
}
