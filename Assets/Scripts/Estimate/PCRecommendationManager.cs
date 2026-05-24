using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PCRecommendationManager : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button finalQuoteButton;
    [SerializeField] private TMP_Dropdown cpuDropdown;
    [SerializeField] private TMP_Dropdown gpuDropdown;
    [SerializeField] private TMP_Dropdown ramDropdown;
    [SerializeField] private TMP_Text outputText;

    [Header("API")]
    [SerializeField] private GeminiClient gemini;

    // 사용자가 처음 입력한 정보 저장
    private string userBudget = "";
    private string userPurpose = "";
    private bool hasFirstResponse = false;

    private void Start()
    {
        sendButton.onClick.AddListener(OnSendClicked);
        finalQuoteButton.onClick.AddListener(OnFinalQuoteClicked);
        inputField.onSubmit.AddListener(_ => { if (sendButton.interactable) OnSendClicked(); });

        finalQuoteButton.interactable = false;

        // 드롭다운에 None 옵션 추가 후 비활성화 (1차 응답 전)
        InsertNoneOption(cpuDropdown);
        InsertNoneOption(gpuDropdown);
        InsertNoneOption(ramDropdown);
        cpuDropdown.interactable = false;
        gpuDropdown.interactable = false;
        ramDropdown.interactable = false;

        outputText.text = "사용 목적과 예산을 입력해주세요.\n예) 150만원 게임용 PC 추천";
    }

    private void InsertNoneOption(TMP_Dropdown dropdown)
    {
        dropdown.options.Insert(0, new TMP_Dropdown.OptionData("None"));
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    private void RemoveNoneOption(TMP_Dropdown dropdown)
    {
        if (dropdown.options.Count > 0 && dropdown.options[0].text == "None")
        {
            dropdown.options.RemoveAt(0);
            dropdown.value = 0;
            dropdown.RefreshShownValue();
        }
    }

    [Serializable]
    private class PickData
    {
        public string[] cpu;
        public string[] gpu;
        public string[] ram_capacity;
    }

    private void AutoSelectDropdowns(string responseText)
    {
        int start = responseText.LastIndexOf('{');
        int end   = responseText.LastIndexOf('}');
        if (start < 0 || end < start) return;

        string json = responseText.Substring(start, end - start + 1);
        try
        {
            var pick = JsonUtility.FromJson<PickData>(json);
            if (pick.cpu         != null && pick.cpu.Length         > 0) SetDropdownOptions(cpuDropdown, pick.cpu);
            if (pick.gpu         != null && pick.gpu.Length         > 0) SetDropdownOptions(gpuDropdown, pick.gpu);
            if (pick.ram_capacity != null && pick.ram_capacity.Length > 0) SetDropdownOptions(ramDropdown, pick.ram_capacity);
        }
        catch (Exception e)
        {
            Debug.LogWarning("추천 JSON 파싱 실패: " + e.Message + "\n원문: " + json);
        }
    }

    private void SetDropdownOptions(TMP_Dropdown dropdown, string[] options)
    {
        dropdown.ClearOptions();
        foreach (var opt in options)
            dropdown.options.Add(new TMP_Dropdown.OptionData(opt));
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }


    // ====== 1차 요청 (사용자 입력 → 방향 추천) ======
    private void OnSendClicked()
    {
        string userInput = inputField.text.Trim();
        if (string.IsNullOrEmpty(userInput))
        {
            outputText.text = "입력값이 비어있습니다.";
            return;
        }

        // 단순히 통째로 저장 (정교한 파싱은 GPT에 맡김)
        // 필요하면 정규식으로 "150만원" / "게임용" 분리 가능
        ParseBudgetAndPurpose(userInput);

        outputText.text = "AI가 추천을 생성 중입니다...";
        sendButton.interactable = false;

        string prompt =
    $"사용자의 요청: \"{userInput}\"\n\n" +
    "위 요청을 분석해서 예산과 목적에 맞는 CPU, GPU, RAM을 각각 3개씩 추천해줘.\n\n" +
    "출력 형식은 다음을 따라:\n\n" +
    "## CPU 추천\n" +
    "예산과 목적에 맞는 CPU 3개를 1순위~3순위로 추천해줘.\n" +
    "각 모델에 대해:\n" +
    "- **모델명**: 대략 가격대\n" +
    "  - 장점 / 단점 / 적합한 사용자\n" +
    "비교 문장 2~3개 포함.\n\n" +
    "## GPU 추천\n" +
    "동일하게 GPU 3개를 1순위~3순위로 비교해줘. 해상도/프레임 관점 포함.\n\n" +
    "## RAM 추천\n" +
    "추천 용량 3가지를 1순위~3순위로 설명해줘.\n\n" +
    "## 종합 추천\n" +
    "가장 균형 잡힌 조합(1순위 기준)을 추천하고 이유를 2~3줄로 설명해줘.\n\n" +
    "답변 맨 마지막 줄에 반드시 아래 형식의 JSON 한 줄을 출력해라. 다른 내용은 넣지 마라.\n" +
    "{\"cpu\":[\"1순위모델\",\"2순위모델\",\"3순위모델\"],\"gpu\":[\"1순위모델\",\"2순위모델\",\"3순위모델\"],\"ram_capacity\":[\"1순위용량\",\"2순위용량\",\"3순위용량\"]}\n\n" +
    "답변을 끝까지 완성해서 작성하라. 한국어로 답변해라.";

        StartCoroutine(gemini.SendPrompt(prompt,
            onResult: (text) =>
            {
                outputText.text = "[1차 추천]\n" + text +
                    "\n\n위 내용을 참고해 왼쪽에서 CPU/GPU/RAM을 선택한 뒤 [최종견적 추천]을 눌러주세요.";
                hasFirstResponse = true;
                finalQuoteButton.interactable = true;
                sendButton.interactable = true;

                // None 제거 후 드롭다운 활성화 및 추천 항목 자동 선택
                RemoveNoneOption(cpuDropdown);
                RemoveNoneOption(gpuDropdown);
                RemoveNoneOption(ramDropdown);
                AutoSelectDropdowns(text);
                cpuDropdown.interactable = true;
                gpuDropdown.interactable = true;
                ramDropdown.interactable = true;
            },
            onError: (err) =>
            {
                outputText.text = "오류: " + err;
                sendButton.interactable = true;
            }));
    }

    // 간단한 파싱 — "150만원 게임용" 같은 입력에서 budget/purpose 추출
    private void ParseBudgetAndPurpose(string input)
    {
        // 가장 단순하게 통째로 저장. 더 정교한 처리가 필요하면 정규식 사용.
        userBudget = input;  // GPT가 알아서 해석함
        userPurpose = input;
    }

    // ====== 2차 요청 (선택 부품 → 최종 견적) ======
    private void OnFinalQuoteClicked()
    {
        if (!hasFirstResponse)
        {
            outputText.text = "먼저 사용 목적과 예산을 입력해 1차 추천을 받아주세요.";
            return;
        }

        // 사용자가 선택한 부품 수집
        string selectedCPU = cpuDropdown.options[cpuDropdown.value].text;
        string selectedGPU = gpuDropdown.options[gpuDropdown.value].text;
        string selectedRAM = ramDropdown.options[ramDropdown.value].text;

        // JSON 데이터 생성
        var data = new QuoteData
        {
            budget = userBudget,
            purpose = userPurpose,
            selected_parts = new SelectedParts
            {
                cpu = selectedCPU,
                gpu = selectedGPU,
                ram = selectedRAM
            }
        };

        string json = JsonUtility.ToJson(data, true);
        Debug.Log("전송 JSON:\n" + json);

        outputText.text = "최종 견적을 생성 중입니다...";
        finalQuoteButton.interactable = false;

        // GPT에 보낼 최종 프롬프트
        string prompt =
    "다음 JSON은 사용자가 입력한 예산/목적과 직접 선택한 핵심 부품 정보다.\n\n" +
    json + "\n\n" +
    "[매우 중요한 안내]\n" +
    "1. 사용자의 RAM 선택값은 '제품명'이 아니라 '용량'(16GB / 32GB / 64GB)이다.\n" +
    "2. 너는 이 용량에 맞는 실제 RAM 제품을 직접 골라서 추천해야 한다.\n" +
    "   예: 사용자가 32GB를 골랐다면 'G.Skill Trident Z5 DDR5-6000 32GB(16GB x 2)' 같은 구체적 제품을 추천.\n" +
    "3. CPU와 GPU는 사용자 선택값을 그대로 유지하라. 절대 변경하지 마라.\n\n" +
    "[필수 조건]\n" +
    "1. CPU/GPU: 사용자 선택값 그대로.\n" +
    "2. RAM: 선택된 용량에 맞는 실제 제품을 추천. 선택된 CPU와 호환되는 규격(DDR4/DDR5)으로.\n" +
    "3. 메인보드: CPU 소켓과 RAM 규격에 호환되는 모델 추천.\n" +
    "4. SSD, 파워서플라이, CPU 쿨러, 케이스 모두 추천하고 호환성/전력/크기 고려.\n" +
    "5. 예산을 최대한 고려하라.\n\n" +
    "[출력 형식]\n" +
    "## 최종 견적\n" +
    "- **CPU**: (사용자 선택값 그대로)\n" +
    "- **GPU**: (사용자 선택값 그대로)\n" +
    "- **RAM**: 사용자가 선택한 용량에 맞는 실제 제품 + 모델명 + 간단한 이유\n" +
    "- **메인보드**: 모델명 + 칩셋 + 이유\n" +
    "- **SSD**: 모델명 + 용량 + 이유\n" +
    "- **파워서플라이**: 정격 출력 + 80PLUS 등급 + 모델명 + 이유\n" +
    "- **CPU 쿨러**: 공랭/수랭 + 모델명 + 이유\n" +
    "- **케이스**: 모델명 + 특징 + 이유\n\n" +
    "## 총평\n" +
    "예상 총 가격, 전체 구성의 장점, 향후 업그레이드 여지를 3~4줄로 정리.\n\n" +
    "반드시 답변을 끝까지 완성해라. 중간에 끊지 마라. 한국어로 답변해라.";

        StartCoroutine(gemini.SendPrompt(prompt,
            onResult: (text) =>
            {
                outputText.text = "[최종 견적]\n" + text;
                finalQuoteButton.interactable = true;
            },
            onError: (err) =>
            {
                outputText.text = "오류: " + err;
                finalQuoteButton.interactable = true;
            }));
    }
}