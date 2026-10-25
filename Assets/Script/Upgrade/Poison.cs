using System.Collections;
using UnityEngine;

public class Poison : HomingProjectile
{
    [SerializeField]
    private float targetDistance = 0.1f;

    private bool dotStarted = false;
    private Coroutine distanceCoroutine;
    [SerializeField]
    private AudioSource travelAudioSource;
    public bool isDamage = true;

    protected override void OnEnable()
    {
        base.OnEnable();

        dotStarted = false;
        distanceCoroutine = null;


    }

    public override void SetTarget(GameObject target)
    {
        base.SetTarget(target);
        PlayTravelAudioIfFlying();
    }

    public override void SetTarget(Vector3 targetPosition)
    {
        base.SetTarget(targetPosition);
        PlayTravelAudioIfFlying();
    }

    private void PlayTravelAudioIfFlying()
    {
        if (isDamage && travelAudioSource != null)
            travelAudioSource.Play();
    }
    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            pooledObject.DestroyObj();
            return;
        }

        if (!other.CompareTag("Enemy"))
            return;

        if (dotStarted)
            return;
        
        EnemyCombat enemy = other.GetComponent<EnemyCombat>();

        if (enemy == null || !gameObject.activeInHierarchy)
            return;

        if (distanceCoroutine == null)
        {
            distanceCoroutine =
                StartCoroutine(CheckDistance(enemy));
        }
    }

    private IEnumerator CheckDistance(EnemyCombat enemy)
    {
        if (isDamage)
        {
            Collider2D myCollider = GetComponent<Collider2D>();
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();

            if (myCollider == null || enemyCollider == null)
            {
                distanceCoroutine = null;
                yield break;
            }

            float prevDistance = float.MaxValue;
            float elapsed = 0f;
            const float timeout = 0.5f; // 이 시간 안에 조건 못 채우면 포기

            while (!dotStarted)
            {
                if (enemy == null || enemy.isDead)
                    break;

                Vector2 myCenter = myCollider.bounds.center;
                Vector2 closestPoint = enemyCollider.ClosestPoint(myCenter);
                float distance = Vector2.Distance(myCenter, closestPoint);

                if (distance <= targetDistance)
                {
                    dotStarted = true;

                    AudioSource poisonAudio = GetComponent<AudioSource>();
                    if (poisonAudio != null)
                        poisonAudio.Play();

                    DotDamage dotDamage = GetComponent<DotDamage>();
                    if (dotDamage != null)
                        dotDamage.SetTarget(enemy.gameObject);

                    break;
                }

                // 이미 스쳐 지나가서 다시 멀어지는 중이면 포기
                if (distance > prevDistance)
                    break;

                prevDistance = distance;

                elapsed += Time.deltaTime;
                if (elapsed >= timeout)
                    break;

                yield return null;
            }
        }

        distanceCoroutine = null; // 성공/실패 상관없이 반드시 초기화 → 다음 적 체크 가능
    }
    
    protected override void OnDisable()
    {
        if (travelAudioSource != null)
            travelAudioSource.Stop();

        rb.linearVelocity = Vector2.zero;

        // 타겟 초기화
        target = null;
        
        isDamage = true;
    }
}