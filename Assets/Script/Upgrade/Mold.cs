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
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        upgradeInfo = GetComponent<UpgradeInfo>();
        id = upgradeInfo.id;
    }
    
    private void OnEnable()
    {
        if (animator != null)
        {
            animator.enabled = true;
            animator.ResetTrigger("Boom");
            animator.Play("Empty", 0, 0f);
        }

        if (spriteRenderer != null && sprites.Count > 0)
        {
            spriteRenderer.sprite = sprites[0];
        }
    }

    public void TargetMushroom(GameObject target)
    {
        targetMoldCor = StartCoroutine(TargetMoldCor());
    }

    public virtual IEnumerator TargetMoldCor()
    {
        animator.enabled = false;
        int i = 1;

        Debug.Log("Mushroom 시작 / Sprite 개수 : " + sprites.Count);

        while (i < sprites.Count)
        {
            yield return new WaitForSeconds(3f);

            Debug.Log(
                $"변경 전 : {spriteRenderer.sprite?.name} → 변경 : {sprites[i]?.name}"
            );

            spriteRenderer.sprite = sprites[i];

            Debug.Log(
                $"변경 후 : {spriteRenderer.sprite?.name}"
            );

            i++;
        }

        Debug.Log("Boom!");
        animator.enabled = true;
        animator.SetTrigger("Boom");
    }

    public virtual void Boom()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col == null)
            return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(LayerMask.GetMask("Enemy"));
        filter.useTriggers = true;

        Collider2D[] enemies = new Collider2D[50];

        int count = col.Overlap(filter, enemies);

        for (int i = 0; i < count; i++)
        {
            EnemyCombat enemy = enemies[i].GetComponent<EnemyCombat>();

            if (enemy == null)
                continue;

            enemy.TakeDamage(DataManager.Instance.UpgradeDatasDic[id].damage[DataManager.Instance.UpgradeDatasDic[id].upgradeLevel], enemies[i].ClosestPoint(transform.position));
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

        if (spriteRenderer != null && sprites.Count > 0)
        {
            spriteRenderer.sprite = sprites[0];
        }
    }
}
