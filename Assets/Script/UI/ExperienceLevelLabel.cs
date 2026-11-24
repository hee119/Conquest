using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class ExperienceLevelLabel : MonoBehaviour
{
    private TMP_Text label;
    private LevelManager levelManager;
    private Coroutine binding;

    private void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        binding = StartCoroutine(Bind());
    }

    private IEnumerator Bind()
    {
        while (LevelManager.Instance == null)
            yield return null;

        levelManager = LevelManager.Instance;
        levelManager.levelUpdate += Refresh;
        Refresh();
        binding = null;
    }

    private void Refresh()
    {
        if (label != null && levelManager != null)
            label.SetText("LV {0}", levelManager.currentLevel);
    }

    private void OnDisable()
    {
        if (binding != null)
        {
            StopCoroutine(binding);
            binding = null;
        }

        if (levelManager != null)
            levelManager.levelUpdate -= Refresh;
        levelManager = null;
    }
}
