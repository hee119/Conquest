using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCombat))]
public sealed class BossHealthSource : MonoBehaviour
{
    private static readonly List<BossHealthSource> activeBosses = new List<BossHealthSource>();
    public static event Action Changed;

    public readonly struct HealthSegment
    {
        public readonly EnemyCombat Combat;
        public readonly float Min;
        public readonly float Max;

        public HealthSegment(EnemyCombat combat, float min, float max)
        {
            Combat = combat;
            Min = min;
            Max = max;
        }
    }

    private static readonly List<HealthSegment> segments = new List<HealthSegment>();
    public static IReadOnlyList<HealthSegment> Segments => segments;

    public static void ReplaceHealthSegment(EnemyCombat parent, IReadOnlyList<SlimeKing> children)
    {
        int index = segments.FindIndex(segment => segment.Combat == parent);
        if (index < 0 || children.Count == 0) return;

        HealthSegment previous = segments[index];
        segments.RemoveAt(index);
        for (int i = 0; i < children.Count; i++)
        {
            float min = Mathf.Lerp(previous.Min, previous.Max, (float)i / children.Count);
            float max = Mathf.Lerp(previous.Min, previous.Max, (float)(i + 1) / children.Count);
            segments.Insert(index + i, new HealthSegment(children[i], min, max));
        }
        Changed?.Invoke();
    }

    private EnemyCombat combat;

    public static bool IsRegisteredBoss(EnemyCombat enemy)
    {
        if (enemy == null) return false;
        foreach (BossHealthSource source in activeBosses)
            if (source != null && source.isActiveAndEnabled && source.combat == enemy)
                return true;
        return false;
    }

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
        segments.Clear();
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
        segments.Clear();
        segments.Add(new HealthSegment(combat, 0f, 1f));
        Changed?.Invoke();
    }

    private void OnDisable()
    {
        bool removed = activeBosses.Remove(this);
        for (int i = 0; i < segments.Count; i++)
        {
            HealthSegment segment = segments[i];
            if (segment.Combat != combat) continue;
            segments[i] = new HealthSegment(null, segment.Min, segment.Max);
            removed = true;
        }
        if (removed) Changed?.Invoke();
    }
}
