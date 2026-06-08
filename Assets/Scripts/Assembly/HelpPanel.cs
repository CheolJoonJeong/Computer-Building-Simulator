using UnityEngine;
using TMPro;

public class HelpPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text contentText;

    [TextArea(10, 30)]
    [SerializeField] private string helpContent =
@"카메라 (FreeFlyCamera / OrbitCamera)

- 마우스 우클릭 드래그: 시점 회전
- W/A/S/D: 전후좌우 이동
- E / Q: 위 / 아래 이동
- 마우스 휠: 줌(전진/후진)
- Shift (좌/우): 이동 속도 가속 (Free-fly 모드)

부품 조립 (SnapZone)

- 마우스 좌클릭: 슬롯 클릭 → 선택된 부품 장착
- R 또는 활성화된 버튼 클릭: 장착된 부품 분리(detach)

케이블 시스템

- 마우스 좌클릭: [라우팅 중] 소켓/패스스루 클릭 → 경로 진행 (한 구멍은 한 번만 통과 가능) / [정리 모드] 케이블 파티클 클릭(선택) → 타이포인트 클릭(고정 또는 해제 선택)
- G: [라우팅 중] 마지막에 클릭한 통과점 되돌리기 / [정리 모드] 선택된 파티클을 타이포인트에서 해제";

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void ToggleHelp()
    {
        if (panel == null) return;

        bool show = !panel.activeSelf;
        panel.SetActive(show);

        if (show && contentText != null)
            contentText.text = helpContent;
    }
}
