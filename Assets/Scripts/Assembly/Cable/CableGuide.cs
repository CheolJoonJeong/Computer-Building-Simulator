using UnityEngine;

// 소켓 자식으로 배치 — 케이블 연결 시 근처 파티클을 이 위치에 자동 고정
// 소켓에서 바깥 방향으로 조금 앞에 배치하면 자연스러운 꺾임 연출 가능
public class CableGuide : MonoBehaviour
{
    [Tooltip("소켓 끝점에서 몇 번째 파티클을 고정할지 (1 = 바로 옆)")]
    [SerializeField] private int particleOffset = 1;

    private GameObject dragHandleObj;
    private Transform dragHandle;

    void Awake()
    {
        dragHandleObj = new GameObject("_GuideHandle");
        dragHandle = dragHandleObj.transform;
        dragHandle.position = this.transform.position;
        dragHandleObj.SetActive(false);
    }

    void OnDestroy()
    {
        if (dragHandleObj != null)
            Destroy(dragHandleObj);
    }

    // CableSocket.TryConnect 성공 후 호출
    // cable: 연결된 케이블, isEndPoint: true면 EndPoint 쪽, false면 Start 쪽
    public void ApplyGuide(CableComponent cable, bool isEndPoint)
    {
        int idx = isEndPoint
            ? cable.Segments - particleOffset
            : particleOffset;

        idx = Mathf.Clamp(idx, 1, cable.Segments - 1);

        dragHandle.position = this.transform.position;
        dragHandleObj.SetActive(true);
        cable.Points[idx].Bind(dragHandle);
    }

    public void ReleaseGuide(CableComponent cable, bool isEndPoint)
    {
        int idx = isEndPoint
            ? cable.Segments - particleOffset
            : particleOffset;

        idx = Mathf.Clamp(idx, 1, cable.Segments - 1);

        cable.Points[idx].UnBind();
        dragHandleObj.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.02f);
        // 부모 소켓과의 연결선
        if (transform.parent != null)
        {
            Gizmos.DrawLine(transform.parent.position, transform.position);
        }
    }
}
