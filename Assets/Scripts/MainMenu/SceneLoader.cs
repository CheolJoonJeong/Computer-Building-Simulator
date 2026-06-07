using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 버튼 OnClick에서 씬 이름을 직접 넣어 호출 (재사용 가능)
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}