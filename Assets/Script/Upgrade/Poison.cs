using System.Collections;
using UnityEngine;

/// <summary>Poison 증강 관련 동작과 상태를 관리한다.</summary>
public class Poison : HomingProjectile
{
    [SerializeField]
    private float targetDistance = 0.1f;

    private bool dotStarted = false;
    private Coroutine distanceCoroutine;
    private AudioSource travelAudioSource;
    public bool isDamage = true;

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    protected override void OnEnable()
    {
        base.OnEnable();

        dotStarted = false;
        distanceCoroutine = null;


    }

    /// <summary>Target 관련 상태를 설정한다.</summary>
    public override void SetTarget(GameObject target)
    {
        base.SetTarget(target);
        PlayTravelAudioIfFlying();
    }

    /// <summary>Target 관련 상태를 설정한다.</summary>
    public override void SetTarget(Vector3 targetPosition)
    {
        base.SetTarget(targetPosition);
        PlayTravelAudioIfFlying();
    }

    /// <summary>PlayTravelAudioIfFlying 동작을 처리한다.</summary>
    private void PlayTravelAudioIfFlying()
    {
        if (isDamage && (travelAudioSource == null || !travelAudioSource.isPlaying))
            travelAudioSource = SoundManager.Instance?.Play(SoundIds.PoisonFlight, loop: true);
    }
    /// <summary>다른 콜라이더가 진입했을 때 적중 효과를 처리한다.</summary>
    public override void HandleEnemyContact(Collider2D other)
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

    /// <summary>CheckDistance 동작을 처리한다.</summary>
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

                    SoundManager.Instance?.Play(SoundIds.PoisonApply, enemy: enemy);

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
    
    /// <summary>비활성화될 때 이벤트와 진행 중인 동작을 정리한다.</summary>
    protected override void OnDisable()
    {
        SoundManager.Instance?.Stop(travelAudioSource);
        travelAudioSource = null;

        rb.linearVelocity = Vector2.zero;

        // 타겟 초기화
        target = null;
        
        isDamage = true;
    }
}
