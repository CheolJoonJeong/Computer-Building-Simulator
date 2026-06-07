using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// 모든 부품/케이블이 장착·연결되었는지 검사하고, 완료 시 결과 패널 표시
public class AssemblyCompletionChecker : MonoBehaviour
{
    public static AssemblyCompletionChecker Instance { get; private set; }

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private string completionMessage = "Assembly Complete!\nGreat job.";

    [Header("Evaluation (optional)")]
    [SerializeField] private CableAreaChecker areaChecker;

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

    private void ShowResult()
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText == null) return;

        string message = completionMessage;

        if (areaChecker != null)
        {
            areaChecker.CheckArea();
            var grade = areaChecker.Evaluate();
            message += grade != null
                ? $"\n[Front Panel] {grade.label} (score {grade.score}, count {areaChecker.OverlappingParticleCount})"
                : $"\n[Front Panel] No matching grade (count {areaChecker.OverlappingParticleCount})";
        }

        resultText.text = message;
    }

    // Home 버튼 OnClick 에 연결
    public void OnHomeButtonClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }
}
