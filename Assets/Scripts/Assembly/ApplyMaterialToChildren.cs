using UnityEngine;

public class ApplyMaterialToChildren : MonoBehaviour
{
    public Material material;

    [ContextMenu("Apply Material To All Children")]
    void Apply()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            r.material = material;
        }
    }
}