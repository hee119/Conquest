
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyCombat))]
public sealed class BossHealthSource : MonoBehaviour
{
    private static readonly List<BossHealthSource> activeBosses = new();
    public static event Action Changed;

    public readonly struct HealthSegment
    {
        public readonly EnemyCombat Combat;
        public readonly float Min;
        public readonly float Max;

        public HealthSegment(
            EnemyCombat combat,
            float min,
            float max)
        {
            Combat = combat;
            Min = min;
            Max = max;
        }
    }

    private static readonly List<HealthSegment> segments = new();
    public static IReadOnlyList<HealthSegment> Segments => segments;

    // 씬에 배치된 원본 HP바
    private static RectTransform originalBar;
    private static Vector3 originalScale;

    // 원본과 복제된 HP바
    private static readonly List<RectTransform> bars = new();

    private static int lastRefreshFrame = -1;

    private EnemyCombat combat;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSignals()
    {
        activeBosses.Clear();
        segments.Clear();
        bars.Clear();

        originalBar = null;
        originalScale = Vector3.one;

        lastRefreshFrame = -1;
        Changed = null;
    }

    private void OnEnable()
    {
        combat = GetComponent<EnemyCombat>();
    }

    // ─────────────────────────────
    // 씬의 기존 HP바 검색
    // ─────────────────────────────

    private static bool FindOriginalBar()
    {
        if (originalBar != null)
            return true;

        GameObject canvas = GameObject.Find("Screen Canvas");

        if (canvas == null)
        {
            Debug.LogWarning(
                "[BossHealthSource] Screen Canvas를 찾을 수 없습니다."
            );
            return false;
        }

        Transform bossUI = canvas.transform.Find("Boss");

        if (bossUI == null)
        {
            Debug.LogWarning(
                "[BossHealthSource] Boss UI를 찾을 수 없습니다."
            );
            return false;
        }

        originalBar = bossUI.Find("HpBar") as RectTransform;

        if (originalBar == null)
        {
            Debug.LogWarning(
                "[BossHealthSource] HpBar를 찾을 수 없습니다."
            );
            return false;
        }

        originalScale = originalBar.localScale;

        bars.Clear();
        bars.Add(originalBar);

        // HorizontalLayoutGroup 설정은 변경하지 않는다.

        return true;
    }

    // ─────────────────────────────
    // 분열 시 HP바 구간 교체
    // ─────────────────────────────

    public static void ReplaceHealthSegment(
        EnemyCombat parent,
        IReadOnlyList<SlimeKing> children)
    {
        if (parent == null ||
            children == null ||
            children.Count == 0)
            return;

        int index = segments.FindIndex(
            segment => segment.Combat == parent
        );

        if (index < 0)
            return;

        HealthSegment previous = segments[index];

        segments.RemoveAt(index);

        float range = previous.Max - previous.Min;

        for (int i = 0; i < children.Count; i++)
        {
            float min =
                previous.Min +
                range * i / children.Count;

            float max =
                previous.Min +
                range * (i + 1) / children.Count;

            segments.Insert(
                index + i,
                new HealthSegment(children[i], min, max)
            );
        }

        RebuildHealthBars();

        Changed?.Invoke();
    }

    // ─────────────────────────────
    // 필요한 만큼 HP바 복제
    // ─────────────────────────────

    private static void RebuildHealthBars()
    {
        if (!FindOriginalBar())
            return;

        int count = segments.Count;

        // 부족한 HP바만 추가 생성
        while (bars.Count < count)
        {
            RectTransform newBar = Instantiate(
                originalBar,
                originalBar.parent
            );

            newBar.name = "HpBar_Split_" + bars.Count;

            newBar.SetSiblingIndex(
                originalBar.GetSiblingIndex() + bars.Count
            );

            bars.Add(newBar);
        }

        // 기존 씬의 레이아웃 설정은 수정하지 않는다.
        for (int i = 0; i < bars.Count; i++)
        {
            RectTransform bar = bars[i];

            if (bar == null)
                continue;

            bool hasSegment = i < count;

            if (!hasSegment)
            {
                if (bar.gameObject.activeSelf)
                    bar.gameObject.SetActive(false);

                continue;
            }

            HealthSegment segment = segments[i];

            float widthRatio = Mathf.Max(
                0f,
                segment.Max - segment.Min
            );

            // X축 스케일만 변경
            bar.localScale = new Vector3(
                originalScale.x * widthRatio,
                originalScale.y,
                originalScale.z
            );
        }

        UpdateHealthValues();
    }

    // ─────────────────────────────
    // 각 자식의 HP 및 사망 여부 갱신
    // ─────────────────────────────

    private static void UpdateHealthValues()
    {
        if (originalBar == null)
            return;

        for (int i = 0; i < bars.Count; i++)
        {
            RectTransform bar = bars[i];

            if (bar == null)
                continue;

            if (i >= segments.Count)
            {
                if (bar.gameObject.activeSelf)
                    bar.gameObject.SetActive(false);

                continue;
            }

            EnemyCombat enemy = segments[i].Combat;

            bool alive =
                enemy != null &&
                enemy.isActiveAndEnabled &&
                enemy.gameObject.activeInHierarchy &&
                !enemy.isDead &&
                enemy.hp > 0f;

            // 죽었으면 HP바 전체를 비활성화
            if (!alive)
            {
                if (bar.gameObject.activeSelf)
                    bar.gameObject.SetActive(false);

                continue;
            }

            // 살아 있는 자식의 HP바만 활성화
            if (!bar.gameObject.activeSelf)
                bar.gameObject.SetActive(true);

            Slider slider =
                bar.GetComponentInChildren<Slider>(true);

            if (slider == null)
                continue;

            float ratio = enemy.maxHp > 0f
                ? Mathf.Clamp01(enemy.hp / enemy.maxHp)
                : 0f;

            slider.interactable = false;

            slider.SetValueWithoutNotify(
                Mathf.Lerp(
                    slider.minValue,
                    slider.maxValue,
                    ratio
                )
            );
        }
    }

    // ─────────────────────────────
    // 자식 슬라임 체력 지속 갱신
    // ─────────────────────────────

    private void LateUpdate()
    {
        if (segments.Count == 0)
            return;

        // 프레임당 한 번만 갱신
        if (lastRefreshFrame == Time.frameCount)
            return;

        // 현재 HP 구간에 등록된 보스 또는 자식만 갱신
        bool registered = false;

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].Combat == combat)
            {
                registered = true;
                break;
            }
        }

        if (!registered)
            return;

        lastRefreshFrame = Time.frameCount;

        if (originalBar == null || bars.Count < segments.Count)
        {
            RebuildHealthBars();
            return;
        }

        UpdateHealthValues();
    }

    // ─────────────────────────────
    // 보스 등록 확인
    // ─────────────────────────────

    public static bool IsRegisteredBoss(EnemyCombat enemy)
    {
        if (enemy == null)
            return false;

        foreach (BossHealthSource source in activeBosses)
        {
            if (source != null &&
                source.isActiveAndEnabled &&
                source.combat == enemy)
            {
                return true;
            }
        }

        return false;
    }

    public static EnemyCombat CurrentCombat
    {
        get
        {
            for (int i = activeBosses.Count - 1; i >= 0; i--)
            {
                BossHealthSource source = activeBosses[i];

                if (source != null &&
                    source.isActiveAndEnabled &&
                    source.combat != null &&
                    !source.combat.isDead)
                {
                    return source.combat;
                }
            }

            // 최초 보스가 분열한 경우
            // 살아 있는 자식 중 하나를 반환
            foreach (HealthSegment segment in segments)
            {
                EnemyCombat enemy = segment.Combat;

                if (enemy != null &&
                    enemy.isActiveAndEnabled &&
                    !enemy.isDead)
                {
                    return enemy;
                }
            }

            return null;
        }
    }

    // ─────────────────────────────
    // 최초 보스 등록
    // ─────────────────────────────

    public void RegisterBoss()
    {
        if (!isActiveAndEnabled)
            return;

        combat = GetComponent<EnemyCombat>();

        activeBosses.Remove(this);
        activeBosses.Add(this);

        segments.Clear();
        segments.Add(
            new HealthSegment(combat, 0f, 1f)
        );

        lastRefreshFrame = -1;

        RebuildHealthBars();

        Changed?.Invoke();
    }

    // ─────────────────────────────
    // 사망 / 풀 반환 시 HP바 제거
    // ─────────────────────────────

    private void OnDisable()
    {
        bool removed = activeBosses.Remove(this);

        for (int i = 0; i < segments.Count; i++)
        {
            HealthSegment segment = segments[i];

            if (segment.Combat != combat)
                continue;

            // 구간 자체는 남겨서 다른 자식들의
            // HP바 크기 비율이 바뀌지 않게 한다.
            segments[i] = new HealthSegment(
                null,
                segment.Min,
                segment.Max
            );

            // 해당 구간의 HP바 비활성화
            if (i < bars.Count && bars[i] != null)
            {
                bars[i].gameObject.SetActive(false);
            }

            removed = true;
        }

        if (removed)
        {
            UpdateHealthValues();
            Changed?.Invoke();
        }
    }
}
