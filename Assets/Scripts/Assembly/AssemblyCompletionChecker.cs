using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Text;

// 모든 부품/케이블이 장착·연결되었는지 검사하고, 완료 시 결과 패널 표시
public class AssemblyCompletionChecker : MonoBehaviour
{
    public static AssemblyCompletionChecker Instance { get; private set; }

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private string completionMessage = "Assembly Complete!\nGreat job.";

    [Header("Home Button")]
    [SerializeField] private string homeSceneName = "Home";

    private bool completed = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    // 부품 장착/해체, 케이블 연결/해체 시 호출
    public void CheckCompletion()
    {
        if (completed) return;

        foreach (SnapZone zone in FindObjectsOfType<SnapZone>(true))
        {
            if (!zone.isOccupied) return;
        }

        foreach (CableSpawner spawner in FindObjectsOfType<CableSpawner>(true))
        {
            if (!spawner.IsAssembled) return;
        }

        completed = true;
        ShowResult();
    }

    // "Complete" 버튼 OnClick 에 연결 — 장착/연결 완료 여부와 무관하게
    // 언제든 눌러서 현재 상태에 대한 평가 결과를 확인할 수 있도록 함
    public void OnCompleteButtonClick()
    {
        ShowResult();
    }

    private void ShowResult()
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText == null) return;

        var zones = FindObjectsOfType<SnapZone>(true);

        int total = 0, ok = 0;
        var lines = new StringBuilder();

        // 부품별 O/X 라인
        AppendPartLine(lines, zones, PartType.CPU,       "cpu",       ref total, ref ok);
        AppendPartLine(lines, zones, PartType.GPU,       "gpu",       ref total, ref ok);
        AppendPartLine(lines, zones, PartType.Mainboard, "mainboard", ref total, ref ok);
        AppendPartLine(lines, zones, PartType.PSU,       "psu",       ref total, ref ok);
        AppendPartLine(lines, zones, PartType.SSD,       "ssd",       ref total, ref ok);
        AppendPartLine(lines, zones, PartType.HDD,       "hdd",       ref total, ref ok);
        AppendPartLine(lines, zones, PartType.Cooler,    "cooler",    ref total, ref ok);
        AppendEvalLine(lines, "ram",   EvaluateRamLine(zones),   ref total, ref ok);
        AppendEvalLine(lines, "cable", EvaluateCableLine(),      ref total, ref ok);

        int score = (total > 0) ? Mathf.RoundToInt(100f * ok / total) : 0;

        var sb = new StringBuilder();
        sb.Append(completionMessage).Append('\n').Append('\n');
        sb.Append($"Score : {score} / 100  ({ok}/{total})").Append('\n').Append('\n');
        sb.Append(lines);

        resultText.text = sb.ToString();
    }

    // 평가 결과 한 줄을 추가하면서 O/X 개수를 집계 ("-"는 채점 대상에서 제외)
    private void AppendEvalLine(StringBuilder sb, string label, string evalResult, ref int total, ref int ok)
    {
        sb.Append(label).Append(" : ").Append(evalResult).Append('\n');
        if (evalResult == "-") return;
        total++;
        if (evalResult == "O" || evalResult.StartsWith("O")) ok++;
    }

    // 일반 부품 한 줄 평가 — 해당 타입의 슬롯이 모두 채워졌으면 O, 하나라도 비었으면 X
    private void AppendPartLine(StringBuilder sb, SnapZone[] zones, PartType type, string label, ref int total, ref int ok)
    {
        bool any = false, allOccupied = true;
        foreach (var zone in zones)
        {
            if (zone.acceptType != type) continue;
            any = true;
            if (!zone.isOccupied) allOccupied = false;
        }
        if (!any) return; // 씬에 해당 타입 슬롯이 없으면 표시/채점하지 않음

        string result = allOccupied ? "O" : "X";
        sb.Append(label).Append(" : ").Append(result).Append('\n');
        total++;
        if (allOccupied) ok++;
    }

    // RAM 평가 — 장착 자체뿐 아니라 슬롯 조합(2,4 / 1,3 / 1,2,3,4)까지 확인
    private string EvaluateRamLine(SnapZone[] zones)
    {
        var occupied = new List<int>();
        bool any = false;
        foreach (var zone in zones)
        {
            if (zone.acceptType != PartType.RAM) continue;
            any = true;
            if (zone.isOccupied && zone.ramSlotIndex > 0) occupied.Add(zone.ramSlotIndex);
        }
        if (!any) return "-";
        if (occupied.Count == 0) return "X (장착 안 됨)";

        occupied.Sort();
        bool validCombo = SequenceMatches(occupied, 2, 4)
                       || SequenceMatches(occupied, 1, 3)
                       || SequenceMatches(occupied, 1, 2, 3, 4);

        return validCombo ? "O" : $"X (적절하지 않은 슬롯 조합: {string.Join(", ", occupied)})";
    }

    private bool SequenceMatches(List<int> list, params int[] expected)
    {
        if (list.Count != expected.Length) return false;
        for (int i = 0; i < list.Count; i++)
            if (list[i] != expected[i]) return false;
        return true;
    }

    // 케이블 평가 — 모든 케이블이 양쪽 소켓에 연결되었으면 O, 하나라도 미흡하면 X
    private string EvaluateCableLine()
    {
        var spawners = FindObjectsOfType<CableSpawner>(true);
        if (spawners.Length == 0) return "-";

        var bad = new List<string>();
        foreach (var spawner in spawners)
            if (!spawner.BothEndsConnected) bad.Add(spawner.name.Replace("Button", "").Trim());

        return bad.Count == 0 ? "O" : $"X (미연결: {string.Join(", ", bad)})";
    }

    // Home 버튼 OnClick 에 연결
    public void OnHomeButtonClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }
}
