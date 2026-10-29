using TMPro;
using UnityEngine;

public sealed class RunRewardView : MonoBehaviour
{
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text fragmentText;
    [SerializeField] private UnityEngine.UI.Image portrait;
    private RunReward reward;
    private LocalizationManager localization;

    private void OnEnable()
    {
        localization = LocalizationManager.Instance;
        if (localization != null) localization.OnLanguageChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (localization != null) localization.OnLanguageChanged -= Refresh;
    }
    public void Bind(RunReward value) { reward = value; Refresh(); }
    private void Refresh()
    {
        if (reward == null) return;
        if (heading != null) heading.text = CharacterProgression.Copy("획득 보상 · 자동 저장", "REWARDS · SAVED", "獲得報酬 · 保存済み");
        if (goldText != null) goldText.text = CharacterProgression.Copy("골드", "GOLD", "ゴールド") + $"  +{reward.gold:N0}";
        if (fragmentText != null) fragmentText.text = CharacterProgression.CharacterName(reward.characterId) +
            CharacterProgression.Copy(" 파편", " Shards", "の欠片") + $"  +{reward.fragments}";
        if (portrait != null)
        {
            portrait.sprite = CharacterProgression.Definition(reward.characterId)?.portrait;
            portrait.enabled = portrait.sprite != null;
        }
    }
}
