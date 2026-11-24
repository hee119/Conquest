using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(TMP_Text))]
public sealed class SurvivalTimeDisplay : MonoBehaviour
{
    private TMP_Text label;
    private float elapsedSeconds;
    private int displayedSecond;
    private EnemyCombat displayedBoss;
    private string displayedLanguage;

    private void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        elapsedSeconds = 0f;
        displayedSecond = -1;
        displayedBoss = null;
        displayedLanguage = null;
        Refresh();
    }

    private void Update()
    {
        if (GameSceneManager.IsLoading || gameObject.scene != SceneManager.GetActiveScene())
            return;

        elapsedSeconds += Time.deltaTime;
        Refresh();
    }

    private void Refresh()
    {
        if (label == null)
            return;

        EnemyCombat boss = BossHealthSource.CurrentCombat;
        string language = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.currentLanguage : "ko";
        if (boss != null)
        {
            if (boss != displayedBoss || language != displayedLanguage)
            {
                displayedBoss = boss;
                displayedLanguage = language;
                displayedSecond = -1;
                label.text = GetBossName(boss, language);
            }
            return;
        }

        displayedBoss = null;
        displayedLanguage = null;
        int seconds = Mathf.FloorToInt(elapsedSeconds);
        if (seconds == displayedSecond)
            return;

        displayedSecond = seconds;
        label.SetText("{0:00}:{1:00}", seconds / 60, seconds % 60);
    }

    private static string GetBossName(EnemyCombat boss, string language)
    {
        int id = boss.enemyInfo != null ? boss.enemyInfo.id : 0;
        string key = $"boss_name_{id}";
        var localization = LocalizationManager.Instance;
        if (localization != null && localization.HasTranslation(key))
            return localization.GetTranslation(key);

        if (id == 150 || boss.GetComponent<OctopusKing>() != null)
            return language == "ko" ? "문어왕" : language == "jp" || language == "ja" ? "タコ王" : "Octopus King";
        if (id == 151 || boss.GetComponent<SlimeKing>() != null)
            return language == "ko" ? "슬라임왕" : language == "jp" || language == "ja" ? "スライム王" : "Slime King";
        return boss.enemyInfo != null && !string.IsNullOrWhiteSpace(boss.enemyInfo.name)
            ? boss.enemyInfo.name : boss.gameObject.name;
    }
}
