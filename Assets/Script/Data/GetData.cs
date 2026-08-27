using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class GetData : MonoBehaviour
{
    [SerializeField]
    private string dataLink =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vQEZhIo1VT7czDDgYnBXL_gBZ746-6LIXT1KlKfehY4lBLxO0-2ge94_1b7AVREicu6y2gWqSh-xebh/pub?gid=0&single=true&output=csv";

    [SerializeField]
    private string folderName = "TextData";

    [SerializeField]
    private string saveFileName = "StageData.csv";

    [ContextMenu("Data Download")]
    public void DownloadStageData()
    {
        UnityWebRequest request = UnityWebRequest.Get(dataLink);

        var operation = request.SendWebRequest();

        operation.completed += _ =>
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"다운로드 실패: {request.error}");
                request.Dispose();
                return;
            }

            // Assets/Resources
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");

            // Assets/Resources/TextData
            string folderPath = Path.Combine(resourcesPath, folderName);

            // Assets/Resources/TextData/StageData.csv
            string filePath = Path.Combine(folderPath, saveFileName);

            // 폴더가 없으면 자동 생성
            Directory.CreateDirectory(folderPath);

            // CSV 저장
            File.WriteAllText(filePath, request.downloadHandler.text);

            Debug.Log($"다운로드 완료!\n{filePath}");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            request.Dispose();
        };
    }
}