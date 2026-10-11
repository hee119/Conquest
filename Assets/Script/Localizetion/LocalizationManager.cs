using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    private Dictionary<string, Dictionary<string, string>> localizationData =
        new Dictionary<string, Dictionary<string, string>>();

    public string currentLanguage = "ko";

    // CSV 파싱용 정규식 (쉼표 구분 처리)
    private readonly string CSV_REGEX = @",(?=(?:[^""]*""[^""]*"")*[^""]*$)";

    public event Action OnLanguageChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadCSV();

        // 저장된 언어 설정 불러오기
        string savedLanguage = PlayerPrefs.GetString("Language", "ko");
        SetLanguage(savedLanguage);
    }

    public void LoadCSV()
    {
        // 파일명과 Resources 경로를 확인하세요.
        TextAsset csvFile = Resources.Load<TextAsset>("TextData/Localization");
        if (csvFile == null)
        {
            Debug.LogError(
                "CSV 파일을 찾을 수 없습니다! Resources/Localization.csv 파일을 확인하세요."
            );
            return;
        }

        localizationData.Clear();

        // UTF-8 인코딩으로 파일 내용 읽기
        byte[] utf8Bytes = csvFile.bytes;
        string content = System.Text.Encoding.UTF8.GetString(utf8Bytes);

        // 줄바꿈 형식 처리
        content = content.Replace("\r\n", "\n").Replace("\r", "\n");

        string[] lines = content.Split(
            new[] { '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (lines.Length == 0)
            return;

        // 헤더 파싱 후 소문자로 변환 (ko, en, ja)
        string[] headers = Regex.Split(lines[0], CSV_REGEX);

        for (int i = 0; i < headers.Length; i++)
        {
            headers[i] = headers[i].Trim().ToLower();
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] row = Regex.Split(lines[i], CSV_REGEX);

            if (row.Length < headers.Length)
                continue;

            string key = row[0].Trim();

            var translations = new Dictionary<string, string>();

            for (int j = 1; j < headers.Length; j++)
            {
                string lang = headers[j];

                // 값의 앞뒤 공백 및 따옴표 제거
                string value = row[j]
                    .Trim()
                    .Trim('"')
                    .Replace("\"\"", "\"");

                translations[lang] = value;
            }

            localizationData[key] = translations;
        }

        Debug.Log(
            $"[Localization] {localizationData.Count}개의 번역 데이터를 로드 완료."
        );
    }

    public string GetTranslation(string key)
    {
        if (localizationData.TryGetValue(key, out var languages))
        {
            if (languages.TryGetValue(currentLanguage, out string result))
            {
                return result;
            }
        }

        return key;
    }

    public void SetLanguage(string langCode)
    {
        currentLanguage = langCode.ToLower();

        if (GameManager.Instance != null)
            GameManager.Instance.currentLanguage = currentLanguage;

        PlayerPrefs.SetString("Language", currentLanguage);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke();

        Debug.Log($"[Localization] 언어 설정: {currentLanguage}");
    }
}
