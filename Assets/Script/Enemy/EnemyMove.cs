using System.Collections;
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

    [SerializeField] protected float pathUpdateInterval = 0.35f;
    [SerializeField, Min(0f)] protected float targetMoveThreshold = 0.1f;
    [SerializeField] protected float knockbackCooldown = 0.3f;

    [Header("Knockback")]
    [SerializeField] protected float knockbackPower = 5f;
    [SerializeField] protected float knockbackDuration = 0.15f;

    protected bool canKnockback = true;
    protected Coroutine knockbackCoroutine;

    private float nextPathUpdateTime;
    private Vector3 lastDestinationTargetPosition;
    private bool hasDestinationTargetPosition;

    protected virtual void Awake()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player");

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateUpAxis = false;
            agent.updateRotation = false;
            agent.speed = moveSpeed;
        }
    }

    protected virtual void OnEnable()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player");

        canKnockback = true;
        nextPathUpdateTime = 0f;
        hasDestinationTargetPosition = false;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    protected virtual void Update()
    {
        if (Time.time < nextPathUpdateTime)
            return;

        nextPathUpdateTime = Time.time + Mathf.Max(0.01f, pathUpdateInterval);
        Move();
    }

    protected virtual void Move()
    {
        if (target == null || !target.activeInHierarchy ||
            agent == null || !agent.enabled || !agent.isOnNavMesh ||
            agent.isStopped)
        {
            return;
        }

        Vector3 targetPosition = target.transform.position;
        if (!HasTargetMoved(targetPosition))
            return;

        if (agent.SetDestination(targetPosition))
            RememberTargetPosition(targetPosition);
    }

    protected bool HasTargetMoved(Vector3 targetPosition)
    {
        if (!hasDestinationTargetPosition)
            return true;

        float threshold = Mathf.Max(0f, targetMoveThreshold);
        return (targetPosition - lastDestinationTargetPosition).sqrMagnitude > threshold * threshold;
    }

    protected void RememberTargetPosition(Vector3 targetPosition)
    {
        lastDestinationTargetPosition = targetPosition;
        hasDestinationTargetPosition = true;
    }

    public void MoveSpeedRenewal()
    {
        if (agent != null)
            agent.speed = moveSpeed;
    }

    protected virtual void OnDisable()
    {
        if (knockbackCoroutine != null)
        {
            StopCoroutine(knockbackCoroutine);
            knockbackCoroutine = null;
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Upgrade") || other.CompareTag("Skill"))
        {
            Vector2 direction =
                (Vector2)transform.position - (Vector2)other.transform.position;
            StartKnockback(direction, knockbackPower);
        }
    }

    public bool CanKnockback()
    {
        return canKnockback;
    }

    public void StartKnockback(Vector2 direction, float power)
    {
        if (!gameObject.activeInHierarchy || !canKnockback ||
            agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        canKnockback = false;

        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(
            KnockBack(direction.normalized, power));
    }

    private IEnumerator KnockBack(Vector2 direction, float power)
    {
        agent.isStopped = true;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            float strength = 1f - elapsed / knockbackDuration;
            Vector3 delta =
                (Vector3)(direction * power * strength) * Time.deltaTime;

            agent.Warp(transform.position + delta);

            elapsed += Time.deltaTime;
            yield return null;
        }

        agent.isStopped = false;
        hasDestinationTargetPosition = false;
        Move();

        yield return new WaitForSeconds(knockbackCooldown);

        canKnockback = true;
        knockbackCoroutine = null;
    }
}
