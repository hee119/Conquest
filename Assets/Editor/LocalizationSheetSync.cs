using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Pulls the published Master tab into the runtime resource file.
/// The Google Sheet remains the source of truth; the CSV is a build-time cache.
/// </summary>
public static class LocalizationSheetSync
{
    private const string SheetId = "14_lKzmecH_o7RLOVoGifOkc1kWEbT16ONS5YNvABhSI";
    private const string MasterGid = "1762728301";
    private const string AssetPath = "Assets/Resources/TextData/Localization.csv";

    [MenuItem("Conquest/Localization/Sync Google Sheet")]
    public static void Sync()
    {
        string url = $"https://docs.google.com/spreadsheets/d/{SheetId}/export?format=csv&gid={MasterGid}";
        using UnityWebRequest request = UnityWebRequest.Get(url);
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone)
            System.Threading.Thread.Sleep(10);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Localization] Google Sheet 동기화 실패: {request.error}\n{url}");
            return;
        }

        string csv = request.downloadHandler.text.Replace("\r\n", "\n").Replace("\r", "\n");
        if (!csv.StartsWith("key,", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("[Localization] 내보낸 파일이 CSV 키 테이블이 아닙니다. 시트 공개 범위와 Master 탭을 확인하세요.");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
        File.WriteAllText(AssetPath, csv, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[Localization] Google Sheet에서 {csv.Split('\n').Length - 1}개 키를 동기화했습니다.");
    }
}
