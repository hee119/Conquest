using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCombat : MonoBehaviour
{
    public float maxHp = 0;
    public float hp;

    // 흰색 HP 잔상용
    public float delayedHp;

    public bool isDead = false;

    PooledObject pooledObject;
    PlayerInfo _playerInfo;

    private SpriteRenderer[] spriteRenderers;
    private PlayerMove playerMove;
    private ScreenHitFlash screenHitFlash;

    public event Action hpUpdate;

    [Header("체력 재생")]
    public float regenDelay = 5f;
    public float regenInterval = 1f;

    float noDamageTimer;
    float regenTimer;

    // ============================
    // HP 잔상
    // ============================

    [Header("HP 잔상")]
    [Tooltip("피격 후 잔상이 멈춰있는 시간")]
    public float hpTrailDelay = 0.5f;

    [Tooltip("잔상이 실제 HP를 따라가는 속도 (초당 HP 감소량)")]
    public float hpTrailSpeed = 300f;

    private float hpTrailTimer;

    // ============================
    // HP 색상
    // ============================

    [Header("HP 색상")]
    public Color startHpColor = Color.green;
    public Color endHpColor = Color.red;

    public Color currentHpColor { get; private set; }

    [Header("이미지 색상 변경")]
    [Tooltip("HP가 줄어들수록 색이 바뀔 이미지")]
    [SerializeField] private Image targetImage;

    [Tooltip("HP가 0일 때 이미지가 도달할 목표 색")]
    [SerializeField] private Color targetColor = Color.red;

    private Color originColor;

    // ============================
    // 피격 색상
    // ============================

    [Header("피격 색상")]

    [SerializeField]
    private Color hitColor = Color.red;

    [SerializeField]
    private Color normalColor = Color.white;

    [SerializeField]
    private float hitFlashTime = 0.1f;

    private Coroutine hitFlashCoroutine;

    // ============================
    // Awake
    // ============================

    public void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        playerMove = GetComponent<PlayerMove>();
        screenHitFlash = FindFirstObjectByType<ScreenHitFlash>();

        spriteRenderers =
            GetComponentsInChildren<SpriteRenderer>();

        _playerInfo =
            GetComponent<PlayerInfo>();

        maxHp = _playerInfo.hp;

        hp = maxHp;
        delayedHp = maxHp;

        // 이미지 원래 색 저장
        if (targetImage != null)
            originColor = targetImage.color;

        UpdateHpColor();
    }

    // ============================
    // Update
    // ============================

    void Update()
    {
        UpdateHpTrail();

        if (isDead || hp >= maxHp)
            return;

        noDamageTimer += Time.deltaTime;

        if (noDamageTimer < regenDelay)
            return;

        regenTimer += Time.deltaTime;

        if (regenTimer >= regenInterval)
        {
            regenTimer -= regenInterval;

            Regen();
        }
    }

    // ============================
    // HP 잔상
    // ============================

    void UpdateHpTrail()
    {
        if (isDead)
            return;

        if (delayedHp > hp)
        {
            // 피격 후 대기 시간 (연속 피격 시 TakeDamage에서 리셋됨)
            if (hpTrailTimer > 0f)
            {
                hpTrailTimer -= Time.deltaTime;
                return;
            }

            delayedHp =
                Mathf.MoveTowards(
                    delayedHp,
                    hp,
                    hpTrailSpeed * Time.deltaTime
                );

            hpUpdate?.Invoke();
        }
        else if (delayedHp < hp)
        {
            delayedHp = hp;

            hpUpdate?.Invoke();
        }
    }

    // ============================
    // 체력 회복
    // ============================

    void Regen()
    {
        float rate =
            DataManager.Instance
                .playerDatasDic[
                    CharacterManager.Instance.currentCharacter
                ]
                .hpRegenRate;

        if (rate <= 0f)
            return;

        int amount =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    maxHp * rate / 100f
                )
            );

        hp =
            Mathf.Min(
                maxHp,
                hp + amount
            );

        if (delayedHp < hp)
        {
            delayedHp = hp;
        }

        UpdateHpColor();

        hpUpdate?.Invoke();
    }

    // ============================
    // 데미지
    // ============================

    public void TakeDamage(int damage)
    {
        if (isDead || (playerMove != null && playerMove.IsDashing))
            return;

        float defense =
            DataManager.Instance
                .playerDatasDic[
                    CharacterManager.Instance.currentCharacter
                ]
                .defense;

        float finalDamage =
            damage * (1f - defense / 100f);

        hp -= finalDamage;

        hp =
            Mathf.Max(
                0f,
                hp
            );

        // 잔상 멈춤 시간 초기화
        hpTrailTimer = hpTrailDelay;

        // 체력 재생 초기화
        noDamageTimer = 0f;
        regenTimer = 0f;

        UpdateHpColor();

        hpUpdate?.Invoke();

        // 플레이어 SpriteRenderer 색상 변경
        HitFlash();

        if (hp <= 0)
        {
            Die();
        }

    }

    // ============================
    // 활성화
    // ============================

    public void OnEnable()
    {
        isDead = false;

        hp = maxHp;
        delayedHp = maxHp;

        hpTrailTimer = 0f;

        noDamageTimer = 0f;
        regenTimer = 0f;

        UpdateHpColor();

        // 모든 SpriteRenderer 원래 색으로 복구
        if (spriteRenderers != null)
        {
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = normalColor;
                }
            }
        }

        hpUpdate?.Invoke();
    }

    // ============================
    // 죽음
    // ============================

    public void Die()
    {
        isDead = true;

        gameObject.SetActive(false);
    }

    // ============================
    // 피격 Flash
    // ============================

    public void HitFlash()
    {
        if (isDead)
            return;

        if (screenHitFlash == null)
            screenHitFlash = FindFirstObjectByType<ScreenHitFlash>();

        screenHitFlash?.Play();

        // 연속으로 맞았을 때 기존 코루틴 제거
        if (hitFlashCoroutine != null)
        {
            StopCoroutine(hitFlashCoroutine);
        }

        hitFlashCoroutine =
            StartCoroutine(HitFlashCor());
    }

    public IEnumerator HitFlashCor()
    {
        // 모든 SpriteRenderer를 피격 색상으로
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hitColor;
            }
        }

        yield return new WaitForSeconds(hitFlashTime);

        // 다시 원래 색상으로
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }

        hitFlashCoroutine = null;
    }

    // ============================
    // UI Update
    // ============================

    public void UpdateHpUI()
    {
        UpdateHpColor();

        hpUpdate?.Invoke();
    }

    // ============================
    // HP Color
    // ============================

    public void UpdateHpColor()
    {
        if (maxHp <= 0f)
        {
            currentHpColor = endHpColor;

            if (targetImage != null)
                targetImage.color = targetColor;

            return;
        }

        float hpRatio =
            Mathf.Clamp01(
                hp / maxHp
            );

        currentHpColor =
            Color.Lerp(
                endHpColor,
                startHpColor,
                hpRatio
            );

        // HP가 줄어들수록 원래 색 → 목표 색으로 변화
        if (targetImage != null)
        {
            targetImage.color =
                Color.Lerp(
                    targetColor,
                    originColor,
                    hpRatio
                );
        }
    }
}
