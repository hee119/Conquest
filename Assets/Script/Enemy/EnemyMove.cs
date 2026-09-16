using System;
using System.Collections;
using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMove : MonoBehaviour
{
    public GameObject target;
    public NavMeshAgent agent;

    public float moveSpeed;
    public float stoppingDistance;

    public bool isSlowDown;
    public int slowCount;

    [SerializeField] protected float pathUpdateInterval = 0.2f;
    [SerializeField] protected float knockbackCooldown = 0.3f;

    [Header("Knockback")]
    [SerializeField] protected float knockbackPower = 5f;
    [SerializeField] protected float knockbackDuration = 0.15f;

    protected bool canKnockback = true;

    public Coroutine moveCoroutine;
    protected Coroutine knockbackCoroutine;


    void Awake()
    {
        target = GameObject.FindGameObjectWithTag("Player");

        agent = GetComponent<NavMeshAgent>();

        agent.updateUpAxis = false;
        agent.updateRotation = false;
        agent.speed = moveSpeed;
    }


    void OnEnable()
    {
        canKnockback = true;
        agent.isStopped = false; // 풀 재사용 시 이전 넉백 상태가 남아 추적 불능이 되는 것 방지

        moveCoroutine = StartCoroutine(Move());
    }


    protected virtual IEnumerator Move()
    {
        while (true)
        {
            // 넉백 중(isStopped)에는 추격 명령을 내리지 않음 → Warp 이동을 덮어쓰지 않도록 보호
            if (target != null && !agent.isStopped)
            {
                agent.SetDestination(target.transform.position);
            }

            yield return new WaitForSeconds(pathUpdateInterval);
        }
    }


    public void MoveSpeedRenewal()
    {
        agent.speed = moveSpeed;
    }


    public void OnDisable()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }
    }


    public void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Upgrade") || other.CompareTag("Skill"))
        {
            Vector2 dir = (Vector2)transform.position - (Vector2)other.transform.position;
            StartKnockback(dir, knockbackPower);
        }
    }


    public bool CanKnockback()
    {
        return canKnockback;
    }


    // 외부(EnemyCombat 등 피격 처리 쪽)에서도 이 시그니처로 호출 가능
    public void StartKnockback(Vector2 direction, float power)
    {
        if (!gameObject.activeInHierarchy || !canKnockback)
            return;

        canKnockback = false;

        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(KnockBack(direction.normalized, power));
    }


    private IEnumerator KnockBack(Vector2 dir, float power)
    {
        // agent.enabled = false 대신 isStopped만 사용 → 비활성 에이전트에 SetDestination 호출하는
        // 런타임 에러(InvalidOperationException)를 원천 차단
        agent.isStopped = true;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            float t = 1f - (elapsed / knockbackDuration); // 시간에 따라 힘 감쇠
            Vector3 delta = (Vector3)(dir * power * t) * Time.deltaTime;

            // Rigidbody 없이 agent.Warp()로 직접 이동 → 항상 NavMesh 표면에 스냅되어
            // 바닥 뚫림이나 NavMesh 이탈이 발생하지 않음
            agent.Warp(transform.position + delta);

            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = false;

        yield return new WaitForSeconds(knockbackCooldown);

        canKnockback = true;
        knockbackCoroutine = null;
    }
}