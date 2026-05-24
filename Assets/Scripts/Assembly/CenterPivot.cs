using UnityEngine;

public class CenterPivot : MonoBehaviour
{
    [ContextMenu("Center Pivot")]
    void Center()
    {
        // 자식 오브젝트들의 중앙 계산
        Vector3 center = Vector3.zero;
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            center += child.position;
        }
        center /= GetComponentsInChildren<Transform>().Length;

        // Empty Root를 중앙으로 이동
        Vector3 offset = transform.position - center;
        foreach (Transform child in transform)
        {
            child.position += offset;
        }
        transform.position = center;
    }
}