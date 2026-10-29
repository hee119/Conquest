using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCombat))]
public sealed class BossHealthSource : MonoBehaviour
{
    private static readonly List<BossHealthSource> activeBosses = new List<BossHealthSource>();
    public static event Action Changed;

    private EnemyCombat combat;

    public static EnemyCombat CurrentCombat
    {
        get
        {
            for (int i = activeBosses.Count - 1; i >= 0; i--)
            {
                BossHealthSource source = activeBosses[i];
                if (source != null && source.isActiveAndEnabled &&
                    source.combat != null && !source.combat.isDead)
                    return source.combat;
            }
            return null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSignals()
    {
        activeBosses.Clear();
        Changed = null;
    }

    private void OnEnable()
    {
        combat = GetComponent<EnemyCombat>();
    }

    // Only the stage's boss spawn registers here. Split children also carry
    // this prefab component, but remain ordinary enemies when enabled.
    public void RegisterBoss()
    {
        if (!isActiveAndEnabled)
            return;

        combat = GetComponent<EnemyCombat>();
        activeBosses.Remove(this);
        activeBosses.Add(this);
        Changed?.Invoke();
    }

    private void OnDisable()
    {
        if (activeBosses.Remove(this))
            Changed?.Invoke();
    }
}
