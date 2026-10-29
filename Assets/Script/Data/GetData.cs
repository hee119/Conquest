using System.IO;
using System.Text.RegularExpressions;
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

            string resourcesPath = Path.Combine(
                Application.dataPath,
                "Resources"
            );

            string folderPath = Path.Combine(
                resourcesPath,
                folderName
            );

            string filePath = Path.Combine(
                folderPath,
                saveFileName
            );

            Directory.CreateDirectory(folderPath);

            // CSV 가져오기
            string csvData = request.downloadHandler.text;

            if (Regex.IsMatch(csvData, @"^\s*<(?:!DOCTYPE\s+html|html)\b", RegexOptions.IgnoreCase))
            {
                Debug.LogError($"CSV 다운로드 응답이 HTML입니다. 공개 CSV 링크를 확인하세요: {dataLink}");
                request.Dispose();
                return;
            }

            if ((saveFileName == "StageData.csv" || saveFileName == "PlayerData.csv") &&
                Regex.IsMatch(csvData, @"#(?:VALUE!|REF!|ERROR!|N/A|DIV/0!|NAME\?)"))
            {
                Debug.LogWarning($"공개 {saveFileName}에 수식 오류가 남아 있어 기존 데이터를 유지합니다. 시트 수식과 공개본 갱신 후 다시 다운로드하세요.");
                request.Dispose();
                return;
            }

            // 첫 번째 열이 비어있는 행 제거
            csvData = Regex.Replace(
                csvData,
                @"(?m)^[ \t]*,[^\r\n]*(?:\r?\n|$)",
                ""
            );

            // 저장
            File.WriteAllText(filePath, csvData);

            Debug.Log($"다운로드 완료!\n{filePath}");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            if (saveFileName == "PlayerData.csv") CharacterProgression.ReloadEnhancements();
#endif

            request.Dispose();
        };
    }
}
