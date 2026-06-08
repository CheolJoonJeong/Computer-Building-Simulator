using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiClient : MonoBehaviour
{
    [SerializeField] private string apiKey = ""; // 비워두면 GeminiApiKey.txt에서 로드 (키를 깃에 커밋하지 않기 위함)
    [SerializeField] private string model = "gemini-2.5-flash";

    private const string KeyFileName = "GeminiApiKey.txt";

    void Awake()
    {
        if (string.IsNullOrEmpty(apiKey))
            apiKey = LoadKeyFromFile();
    }

    // 프로젝트 루트의 GeminiApiKey.txt (.gitignore에 등록됨)에서 키를 읽음
    private string LoadKeyFromFile()
    {
        string path = System.IO.Path.Combine(Application.dataPath, "..", KeyFileName);
        if (System.IO.File.Exists(path))
            return System.IO.File.ReadAllText(path).Trim();

        Debug.LogWarning($"[GeminiClient] API key not set. Create '{KeyFileName}' next to the project folder, or assign it in the Inspector.");
        return "";
    }

    // ====== 요청 구조 ======
    [Serializable]
    private class Part
    {
        public string text;
    }

    [Serializable]
    private class Content
    {
        public string role;
        public Part[] parts;
    }

    [Serializable]
    private class SystemInstruction
    {
        public Part[] parts;
    }

    [Serializable]
    private class GenerationConfig
    {
        public float temperature;
        public int maxOutputTokens;
    }

    [Serializable]
    private class RequestBody
    {
        public Content[] contents;
        public SystemInstruction systemInstruction;
        public GenerationConfig generationConfig;
    }

    // ====== 응답 구조 ======
    [Serializable]
    private class ResponseContent
    {
        public Part[] parts;
        public string role;
    }

    [Serializable]
    private class Candidate
    {
        public ResponseContent content;
        public string finishReason;
    }

    [Serializable]
    private class ResponseBody
    {
        public Candidate[] candidates;
    }

    /// <summary>
    /// Gemini에 프롬프트를 보내고 응답을 콜백으로 받는다.
    /// </summary>
    public IEnumerator SendPrompt(string userPrompt, Action<string> onResult, Action<string> onError)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var body = new RequestBody
        {
            contents = new Content[]
            {
                new Content
                {
                    role = "user",
                    parts = new Part[] { new Part { text = userPrompt } }
                }
            },
            systemInstruction = new SystemInstruction
            {
                parts = new Part[]
                {
                    new Part { text = "너는 한국의 PC 견적 추천 전문가다. 답변은 반드시 한국어로 한다." }
                }
            },
            generationConfig = new GenerationConfig
            {
                temperature = 0.7f,
                maxOutputTokens = 16384
            }
        };

        string jsonBody = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error + " / " + req.downloadHandler.text);
                yield break;
            }

            try
            {
                var res = JsonUtility.FromJson<ResponseBody>(req.downloadHandler.text);

                if (res != null && res.candidates != null && res.candidates.Length > 0
                    && res.candidates[0].content != null
                    && res.candidates[0].content.parts != null
                    && res.candidates[0].content.parts.Length > 0)
                {
                    // 모든 parts를 하나의 문자열로 합치기
                    var sb = new StringBuilder();
                    foreach (var part in res.candidates[0].content.parts)
                    {
                        sb.Append(part.text);
                    }

                    // finishReason 확인 (디버깅용)
                    string finishReason = res.candidates[0].finishReason;
                    Debug.Log("Gemini finishReason: " + finishReason);

                    if (finishReason == "MAX_TOKENS")
                    {
                        Debug.LogWarning("응답이 토큰 한도에 걸려 잘렸습니다. maxOutputTokens를 더 늘리세요.");
                    }

                    onResult?.Invoke(sb.ToString());
                }
                else
                {
                    onError?.Invoke("응답 파싱 실패: " + req.downloadHandler.text);
                }
            }
            catch (Exception e)
            {
                onError?.Invoke("예외: " + e.Message);
            }
        }
    }
}