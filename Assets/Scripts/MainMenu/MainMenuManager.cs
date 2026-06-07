using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // ⚠️ 아래 큰따옴표 안의 이름을 실제 씬 파일명과 똑같이 맞춰야 함
    [SerializeField] private string assemblySceneName = "Assembly";
    [SerializeField] private string quoteSceneName = "Estimate";

    // 조립 버튼이 호출할 함수
    public void GoToAssembly()
    {
        SceneManager.LoadScene(assemblySceneName);
    }

    // 견적추천 버튼이 호출할 함수
    public void GoToQuote()
    {
        SceneManager.LoadScene(quoteSceneName);
    }

    // (선택) 게임 종료용 — 나중에 종료 버튼 추가하면 연결
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료");
    }
}