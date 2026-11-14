using UnityEngine;

/// <summary>Poison 증강 관련 동작과 상태를 관리한다.</summary>
public class Poison : HomingProjectile
{
    private bool dotStarted = false;
    private AudioSource travelAudioSource;
    public bool isDamage = true;

    /// <summary>활성화될 때 필요한 이벤트와 동작을 시작한다.</summary>
    protected override void OnEnable()
    {
        base.OnEnable();

        dotStarted = false;


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

        if (!isDamage)
            return;

        DotDamage dotDamage = GetComponent<DotDamage>();
        if (dotDamage == null)
            return;

        dotStarted = true;
        SoundManager.Instance?.Play(SoundIds.PoisonApply, enemy: enemy);
        dotDamage.SetTarget(enemy.gameObject);
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
