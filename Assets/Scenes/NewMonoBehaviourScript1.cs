using UnityEditor;
using UnityEngine;
using TMPro;

public static class ReplaceTMPFonts
{
    [MenuItem("Tools/Conquest/Replace All Fonts")]
    private static void ReplaceAllFonts()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        if (font == null)
        {
            UnityEngine.Debug.LogError("TMP 기본 폰트가 설정되지 않았습니다.");
            return;
        }

        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Undo.RecordObjects(texts, "Replace TMP Fonts");

        foreach (TMP_Text text in texts)
        {
            text.font = font;
            EditorUtility.SetDirty(text);
        }

        UnityEngine.Debug.Log($"{texts.Length}개의 TMP 텍스트 폰트를 변경했습니다.");
    }
}