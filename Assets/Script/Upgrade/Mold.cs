using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Mold 증강 관련 동작과 상태를 관리한다.</summary>
public class Mold : MonoBehaviour
{
    public Animator animator;

    [SerializeField]
    public List<Sprite> sprites = new List<Sprite>();

    public SpriteRenderer spriteRenderer;
    public UpgradeInfo upgradeInfo;

    protected int id;

    Coroutine targetMoldCor;

    /// <summary>필요한 컴포넌트와 초기 상태를 준비한다.</summary>
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

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
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

    /// <summary>TargetMushroom 동작을 처리한다.</summary>
    public void TargetMushroom(GameObject target)
    {
        targetMoldCor =
            StartCoroutine(
                TargetMoldCor()
            );
    }

    /// <summary>TargetMoldCor 동작을 처리한다.</summary>
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
            /// <summary>WaitForSeconds 동작을 처리한다.</summary>
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

    /// <summary>Boom 동작을 처리한다.</summary>
    public virtual void Boom()
    {
        SoundManager.Instance?.Play(SoundIds.MoldPop);
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
        // Enemy 탐색
        // =====================================

        ContactFilter2D filter =
            /// <summary>ContactFilter2D 동작을 처리한다.</summary>
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

            enemy.TakeDamage(id, damage,
                hitPoint
            );
        }

        gameObject.SetActive(false);
    }

    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
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