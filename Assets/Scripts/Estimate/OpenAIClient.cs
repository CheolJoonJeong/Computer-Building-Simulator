using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class OpenAIClient : MonoBehaviour
{
    [SerializeField] private string apiKey = ""; // Unity Inspector에서 입력
    private const string API_URL = "https://api.openai.com/v1/chat/completions";

    // GPT 요청용 데이터 구조
    [Serializable]
    private class Message
    {
        public string role;
        public string content;
    }

    [Serializable]
    private class RequestBody
    {
        public string model;
        public Message[] messages;
        public float temperature;
    }

    [Serializable]
    private class Choice
    {
        public Message message;
    }

    [Serializable]
    private class ResponseBody
    {
        public Choice[] choices;
    }

    /// <summary>
    /// GPT에 프롬프트를 보내고 응답을 콜백으로 받는다.
    /// </summary>
    public IEnumerator SendPrompt(string userPrompt, Action<string> onResult, Action<string> onError)
    {
        var body = new RequestBody
        {
            model = "gpt-4o-mini", // 비용 저렴, 충분히 잘 동작
            temperature = 0.7f,
            messages = new Message[]
            {
                new Message { role = "system", content = "너는 한국의 PC 견적 추천 전문가다. 답변은 한국어로 한다." },
                new Message { role = "user", content = userPrompt }
            }
        };

        string jsonBody = JsonUtility.ToJson(body);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest req = new UnityWebRequest(API_URL, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error + " / " + req.downloadHandler.text);
                yield break;
            }

            try
            {
                var res = JsonUtility.FromJson<ResponseBody>(req.downloadHandler.text);
                if (res != null && res.choices != null && res.choices.Length > 0)
                {
                    onResult?.Invoke(res.choices[0].message.content);
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