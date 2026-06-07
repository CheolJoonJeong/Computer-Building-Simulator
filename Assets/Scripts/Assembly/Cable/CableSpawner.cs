using UnityEngine;
using UnityEngine.UI;
using TMPro;

// UI 버튼에 연결 — 케이블 타입 선택, 소환, 해체
public class CableSpawner : MonoBehaviour
{
    [SerializeField] private CableType cableType;
    [SerializeField] private GameObject cablePrefab;
    [Tooltip("케이블이 기본으로 지나갈 통과점들 (순서대로). 비워두면 직선)")]
    [SerializeField] private Transform[] defaultRoute;
    [Tooltip("지정하면 버튼 클릭 시 바로 이 소켓에 연결되어 라우팅 시작 (출발 소켓 직접 클릭 불필요)")]
    [SerializeField] private CableSocket sourceSocket;
    [Tooltip("스폰 직후 끝점이 초기 배치될 위치를 가리키는 오브젝트. 비워두면 시작점 기준 기본 오프셋 사용 (주변 소켓과 안 겹치는 곳에 빈 오브젝트를 두고 등록)")]
    [SerializeField] private Transform initialEndPoint;
    [Tooltip("이 케이블이 연결되는 목적지 파츠 — 장착(AssembledPart 레이어) 전엔 케이블 생성 불가 " +
             "(예: Fan 케이블은 Cooler가 장착되어야 함. sourceSocket만으론 판단 불가한 경우에 사용)")]
    [SerializeField] private PartInfo requiredAssembledPart;

    private GameObject connectedCableInstance;
    private CableConnector connectedStart;
    private CableConnector connectedEnd;
    private bool isAssembled;
    public bool IsAssembled => isAssembled;

    // 평가용 — 케이블 양쪽 끝(시작/도착 소켓)이 모두 실제로 연결되었는지
    public bool BothEndsConnected =>
        connectedStart != null && connectedStart.IsConnected &&
        connectedEnd != null && connectedEnd.IsConnected;

    // UI 버튼 OnClick 에 연결
    public void OnButtonClick()
    {
        if (CableManager.Instance == null) { Debug.LogError("[CableSpawner] CableManager not found."); return; }

        if (isAssembled)
        {
            Detach();
            return;
        }

        // 명시적으로 지정된 목적지 파츠가 아직 장착되지 않았다면 생성 불가
        // (예: Fan 케이블은 쿨러가 장착되어야 하므로 requiredAssembledPart에 쿨러를 직접 지정)
        if (!IsPartAssembled(requiredAssembledPart))
        {
            Debug.Log("[CableSpawner] Required destination part not assembled yet — cable spawn blocked.");
            return;
        }

        if (sourceSocket != null)
            CableManager.Instance.StartFromSpawner(cableType, this, sourceSocket);
        else
            CableManager.Instance.SelectCableType(cableType, this);
    }

    // info가 null이면 검사 대상 없음(통과). 지정된 경우 AssembledPart 레이어인지로 장착 여부 판단
    private bool IsPartAssembled(PartInfo info)
    {
        if (info == null) return true;
        // 케이스는 SnapZone을 거치지 않고 처음부터 씬에 존재하는 베이스 파츠 — 항상 장착된 것으로 간주
        // (레이어를 AssembledPart로 바꾸면 레이캐스트 차단 등 부작용이 있어 별도 판단)
        if (info.data != null && info.data.partType == PartType.Case) return true;
        return info.gameObject.layer == LayerMask.NameToLayer("AssembledPart");
    }

    public (CableConnector start, CableConnector end, CableComponent cable) SpawnAt(Vector3 position)
    {
        if (cablePrefab == null)
        {
            Debug.LogError("[CableSpawner] cablePrefab not assigned.");
            return (null, null, null);
        }

        var go = Instantiate(cablePrefab, position, Quaternion.identity);
        var cable = go.GetComponent<CableComponent>();

        // 루트의 커넥터 = start, EndPoint 자식의 커넥터 = end
        CableConnector start = null, end = null;
        foreach (var c in go.GetComponentsInChildren<CableConnector>())
        {
            if (c.IsEndPoint) end = c;
            else start = c;
        }

        Transform endPoint = end != null ? end.transform : go.transform;

        if (cable != null && start != null)
        {
            // 끝점을 지정된 초기 위치에 스폰 (시작점과 겹치면 파티클이 뭉쳐서
            // 주변 헤더 소켓 콜라이더와 같이 겹쳐 튕겨 나가는 문제가 있었음)
            if (initialEndPoint != null)
                endPoint.position = initialEndPoint.position;
            else
                endPoint.position = start.transform.position + Vector3.down * 0.3f;

            cable.Init(start.transform, endPoint);

            // 라우팅 시작 전까지 끝점을 시작점에 고정 — 자유 파티클 상태로 몇 프레임 방치되며
            // 중력/충돌에 떠밀려 튀어 오르는 것을 방지 (라우팅 시작 시 CableManager가 해제)
            cable.HoldEndInPlace();

            // 기본 경로(통과점) 자동 적용
            if (defaultRoute != null)
                foreach (var anchor in defaultRoute)
                    if (anchor != null) cable.AddRouteAnchor(anchor);
        }

        connectedCableInstance = go;
        connectedStart = start;
        connectedEnd = end;

        return (start, end, cable);
    }

    // 케이블 연결 완료 시 CableManager가 호출
    public void OnConnected()
    {
        isAssembled = true;
        SetAssembled();
        AssemblyCompletionChecker.Instance?.CheckCompletion();
    }

    private void Detach()
    {
        if (connectedStart != null) connectedStart.Disconnect();
        if (connectedEnd != null) connectedEnd.Disconnect();

        if (connectedCableInstance != null)
            Destroy(connectedCableInstance);

        connectedCableInstance = null;
        connectedStart = null;
        connectedEnd = null;
        isAssembled = false;

        SetUnassembled();
        Debug.Log($"[CableSpawner] '{cableType}' cable detached.");
    }

    private void SetAssembled()
    {
        ApplyButtonColor(new Color(0.4f, 0.4f, 0.4f, 1f));

        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>())
            t.color = new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    private void SetUnassembled()
    {
        ApplyButtonColor(Color.white);

        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>())
            t.color = Color.black;
    }

    // Button의 Color Tint 트랜지션이 Image.color를 매 프레임 덮어쓰므로,
    // Button.colors 자체를 변경해야 색이 유지된다.
    private void ApplyButtonColor(Color color)
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = color;
            cb.selectedColor = color;
            btn.colors = cb;
        }

        Image img = GetComponent<Image>();
        if (img != null) img.color = color;
    }
}
