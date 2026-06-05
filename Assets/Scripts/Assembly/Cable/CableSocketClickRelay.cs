using UnityEngine;

// 자식 Collider에 붙여서 클릭을 부모 CableSocket으로 전달
// 구조: Socket(CableSocket) -> 24pin(BoxCollider + 이 스크립트)
[RequireComponent(typeof(Collider))]
public class CableSocketClickRelay : MonoBehaviour
{
    private CableSocket socket;

    void Awake()
    {
        socket = GetComponentInParent<CableSocket>();
    }

    void OnMouseDown()
    {
        if (socket != null) socket.HandleClick();
    }
}
