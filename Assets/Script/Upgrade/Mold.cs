using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mold : MonoBehaviour
{
    public Animator animator;

    [SerializeField]
    public List<Sprite> sprites = new List<Sprite>();

    public SpriteRenderer spriteRenderer;
    public UpgradeInfo upgradeInfo;

    protected int id;

    Coroutine targetMoldCor;

    public void Awake()
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        animator =
            GetComponent<Animator>();

        upgradeInfo =
            GetComponent<UpgradeInfo>();

        id =
            upgradeInfo.id;
    }

    private void OnEnable()
    {
        if (animator != null)
        {
            animator.enabled = true;
            animator.ResetTrigger("Boom");
            animator.Play("Empty", 0, 0f);
        }

        if (spriteRenderer != null &&
            sprites.Count > 0)
        {
            spriteRenderer.sprite =
                sprites[0];
        }
    }

    public void TargetMushroom(GameObject target)
    {
        targetMoldCor =
            StartCoroutine(
                TargetMoldCor()
            );
    }

    public virtual IEnumerator TargetMoldCor()
    {
        animator.enabled = false;

        int i = 1;

        UnityEngine.Debug.Log(
            "Mushroom 시작 / Sprite 개수 : " +
            sprites.Count
        );

        while (i < sprites.Count)
        {
            yield return new WaitForSeconds(3f);

            UnityEngine.Debug.Log(
                $"변경 전 : {spriteRenderer.sprite?.name} → 변경 : {sprites[i]?.name}"
            );

            spriteRenderer.sprite =
                sprites[i];

            UnityEngine.Debug.Log(
                $"변경 후 : {spriteRenderer.sprite?.name}"
            );

            i++;
        }

        UnityEngine.Debug.Log("Boom!");

        animator.enabled = true;
        animator.SetTrigger("Boom");
    }

    public virtual void Boom()
    {
        Collider2D col =
            GetComponent<Collider2D>();

        if (col == null)
            return;

        // =====================================
        // 기본 데미지
        // =====================================

        UpgradeData upgradeData =
            DataManager.Instance
                .upgradeDatasDic[id];

        int level =
            upgradeData.UpgradeLevel;

        float damage =
            upgradeData.damage[level];

        // =====================================
        // 현재 캐릭터 스탯
        // =====================================

        int characterId =
            CharacterManager.Instance.currentCharacter;

        PlayerData playerData =
            DataManager.Instance
                .playerDatasDic[characterId];

        int playerDamage =
            playerData.damage;

        int critChance =
            playerData.criticalChance;

        float critDamage =
            playerData.criticalDamage;

        // =====================================
        // 공격력 적용
        // =====================================

        damage =
            (int)(
                damage *
                (1f + playerDamage / 100f)
            );

        // =====================================
        // 크리티컬 판정
        // 폭발 전체에 한 번만 판정
        // =====================================

        bool isCritical =
            UnityEngine.Random.Range(
                0,
                100
            ) < critChance;

        if (isCritical)
        {
            damage =
                (int)(
                    damage *
                    (2f + critDamage / 100f)
                );
        }

        // =====================================
        // Enemy 탐색
        // =====================================

        ContactFilter2D filter =
            new ContactFilter2D();

        filter.SetLayerMask(
            LayerMask.GetMask("Enemy")
        );

        filter.useTriggers = true;

        Collider2D[] enemies =
            new Collider2D[50];

        int count =
            col.Overlap(
                filter,
                enemies
            );

        // =====================================
        // 범위 내 모든 Enemy에게 동일 데미지
        // =====================================

        for (int i = 0; i < count; i++)
        {
            EnemyCombat enemy =
                enemies[i].GetComponent<EnemyCombat>();

            if (enemy == null)
                continue;

            if (enemy.isDead)
                continue;

            Vector2 hitPoint =
                enemies[i].ClosestPoint(
                    transform.position
                );

            enemy.TakeDamage(
                damage,
                hitPoint
            );
        }

        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (targetMoldCor != null)
        {
            StopCoroutine(targetMoldCor);
            targetMoldCor = null;
        }

        if (spriteRenderer != null &&
            sprites.Count > 0)
        {
            spriteRenderer.sprite =
                sprites[0];
        }
    }
}