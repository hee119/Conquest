using UnityEngine;

public sealed class BossHpBar : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private HpBar hpBar;

    private void OnEnable()
    {
        BossHealthSource.Changed += RefreshBoss;
        RefreshBoss();
    }

    private void OnDisable()
    {
        BossHealthSource.Changed -= RefreshBoss;
        if (hpBar != null)
            hpBar.BindCombat(null);
        if (panel != null)
            panel.SetActive(false);
    }

    private void RefreshBoss()
    {
        if (panel == null || hpBar == null)
            return;

        EnemyCombat boss = BossHealthSource.CurrentCombat;
        if (boss != null)
        {
            panel.SetActive(true);
            hpBar.BindCombat(boss);
        }
        else
        {
            hpBar.BindCombat(null);
            panel.SetActive(false);
        }
    }
}
