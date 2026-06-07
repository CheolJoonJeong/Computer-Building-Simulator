using UnityEngine;

public class PartSelectionManager : MonoBehaviour
{
    public static GameObject SelectedPart;
    public static GameObject SelectedSlot;
    public static GameObject SelectedButton;

    public static void Clear()
    {
        // ��� ���� Collider ���� + ������ ����
        foreach (SnapZone slot in FindObjectsOfType<SnapZone>(true))
        {
            // 콜라이더가 슬롯 본체가 아닌 자식에 있는 경우도 처리
            foreach (Collider col in slot.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            foreach (Renderer r in slot.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        SelectedPart = null;
        SelectedSlot = null;
        SelectedButton = null;
    }
}