using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BasicAttackChainUtility
{
    public static void Apply(
        Vector2 origin,
        EnemyCombat primaryTarget,
        int damage,
        int chainCount,
        float chainRange)
    {
        if (chainCount <= 0 || chainRange <= 0f || damage <= 0)
            return;

        int enemyLayer = LayerMask.GetMask("Enemy");
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, chainRange, enemyLayer);
        IEnumerable<EnemyCombat> targets = hits
            .Select(hit => hit != null ? hit.GetComponentInParent<EnemyCombat>() : null)
            .Where(enemy => enemy != null && enemy != primaryTarget &&
                            enemy.gameObject.activeInHierarchy)
            .Distinct()
            .OrderBy(enemy => ((Vector2)enemy.transform.position - origin).sqrMagnitude)
            .Take(chainCount);

        foreach (EnemyCombat enemy in targets)
            enemy.TakeDamage(damage, enemy.transform.position);
    }
}

/// <summary>
/// 풀링되는 검사 기본 공격 이펙트에 런타임 강화값을 적용한다.
/// 프리팹 수정 없이 SkillSpawner가 자동으로 부착한다.
/// </summary>
public sealed class BasicAttackInstanceState : MonoBehaviour
{
    private Vector3 baseScale;
    private int baseDamage;
    private SkillInfo skillInfo;
    private bool initialized;

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        baseScale = transform.localScale;
        skillInfo = GetComponent<SkillInfo>();
        baseDamage = skillInfo != null ? skillInfo.skillDamage : 0;
    }

    public void Apply(BasicAttackModifiers modifiers)
    {
        Initialize();
        transform.localScale = baseScale * modifiers.rangeMultiplier;

        if (skillInfo != null)
        {
            skillInfo.skillDamage = Mathf.Max(
                1,
                Mathf.RoundToInt(baseDamage * modifiers.damageMultiplier));
        }

        BasicAttackChainRelay relay = GetComponent<BasicAttackChainRelay>();
        if (relay == null)
            relay = gameObject.AddComponent<BasicAttackChainRelay>();

        relay.Configure(
            skillInfo != null ? skillInfo.skillDamage : 0,
            modifiers.chainCount,
            modifiers.chainRange);
    }

    private void OnDisable()
    {
        if (!initialized)
            return;

        transform.localScale = baseScale;
        if (skillInfo != null)
            skillInfo.skillDamage = baseDamage;
    }
}

public sealed class BasicAttackChainRelay : MonoBehaviour
{
    private readonly HashSet<int> triggeredTargets = new HashSet<int>();
    private int damage;
    private int chainCount;
    private float chainRange;

    public void Configure(int hitDamage, int count, float range)
    {
        damage = hitDamage;
        chainCount = Mathf.Max(0, count);
        chainRange = Mathf.Max(0f, range);
        triggeredTargets.Clear();
        enabled = chainCount > 0 && chainRange > 0f && damage > 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyCombat primary = other.GetComponentInParent<EnemyCombat>();
        if (primary == null || !triggeredTargets.Add(primary.GetInstanceID()))
            return;

        BasicAttackChainUtility.Apply(
            primary.transform.position,
            primary,
            damage,
            chainCount,
            chainRange);
    }
}
