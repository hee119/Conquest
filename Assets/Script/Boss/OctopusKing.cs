using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyFacing))]
public class OctopusKing : MonoBehaviour
{
    [Header("WaterSpawnPoint")]
    public GameObject waterSpawnPoint;
    [Header("WaterStream")]
    public int rotationSpeed = 200;
    public int rotationCount = 1;
    public int waterStreamObjectId = 1000;
    public AnimationClip spinAnimation;
    [Header("PredictiveWater")]
    public int bulletSpawnCount = 5;
    public float bulletSpawnInterval = .2f;
    public int bulletId = 601;
    public AnimationClip waterShotAnimation;
    [Min(0f)] public float waterShotReleaseDelay = .225f;
    [Header("GroundAttack")]
    public int groundAttackSkillId = 1001;
    public string groundAttackAnimTrigger = "GroundCrack";
    [Min(0f)] public float groundAttackImpactDelay = 1f / 3f;
    public AnimationClip groundAttackAnimation;
    [Min(1)] public int groundStrikeCount = 1;
    [Min(.1f)] public float groundAttackRadius = 4f;
    [SerializeField] private Vector2 groundOriginOffset = new Vector2(0, -.705357f);
    [Header("AlternatingFan")]
    [Min(1)] public int fanBulletCount = 10;
    [Min(1)] public int fanBurstCount = 5;
    [Range(1f, 180f)] public float fanSpreadDegrees = 120f;
    [Header("RadialWater")]
    [Min(1)] public int radialBulletCount = 10;
    [Min(1)] public int radialBurstCount = 5;
    public Animator animator;
    public float patternInterval = 6f;
    public bool runPatternsAutomatically = true;

    private GameObject target;
    private EnemyCombat combat;
    private NavMeshAgent agent;
    private bool attacking;
    private GameObject activeBeam;
    private enum ShotPattern { Predictive, Fan, Radial }
    private ShotPattern pendingShot;
    private int pendingBurst;
    private bool shotArmed;
    private static readonly int SpinWaterHash = Animator.StringToHash("SpinWater");
    private static readonly int SpinSpeedHash = Animator.StringToHash("SpinSpeed");
    private bool Alive => isActiveAndEnabled && (combat == null || (!combat.isDead && combat.hp > 0f));
    private Transform Mouth => waterSpawnPoint != null ? waterSpawnPoint.transform : transform;

    public void Awake()
    {
        animator = GetComponent<Animator>();
        combat = GetComponent<EnemyCombat>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable() => StartCoroutine(PatternLoop());

    private void OnDisable()
    {
        StopAllCoroutines();
        StopBeam();
        shotArmed = false;
        attacking = false;
        SetSpinning(false);
    }

    private IEnumerator PatternLoop()
    {
        yield return null; // Health and pooled dependencies finish OnEnable first.
        target = GameObject.FindGameObjectWithTag("Player");
        while (Alive)
        {
            if (runPatternsAutomatically && !attacking && target != null && target.activeInHierarchy &&
                PoolManager.Instance != null && PoolManager.Instance.prefabIsLoaded)
            {
                switch (Random.Range(0, 5))
                {
                    case 0: yield return RunAttack(SpinWaterStreamAttackCor()); break;
                    case 1: yield return RunAttack(PredictiveWaterCannonAttackCor()); break;
                    case 2: yield return RunAttack(GroundCrackAttackCor()); break;
                    case 3: yield return RunAttack(WaterCannonSequence(ShotPattern.Fan, fanBurstCount)); break;
                    case 4: yield return RunAttack(WaterCannonSequence(ShotPattern.Radial, radialBurstCount)); break;
                }
                yield return new WaitForSeconds(Mathf.Max(.1f, patternInterval));
            }
            else yield return null;
        }
        StopBeam();
    }

    private void StartAttack(IEnumerator attack)
    {
        if (!Alive || attacking || PoolManager.Instance == null) return;
        if (target == null) target = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(RunAttack(attack));
    }

    private IEnumerator RunAttack(IEnumerator attack)
    {
        attacking = true;
        bool stopped = agent != null && agent.enabled && agent.isOnNavMesh && agent.isStopped;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
        try { yield return attack; }
        finally
        {
            shotArmed = false;
            StopBeam();
            SetSpinning(false);
            attacking = false;
            if (agent != null && agent.enabled && agent.isOnNavMesh && Alive) agent.isStopped = stopped;
        }
    }

    public void SpinWaterStreamAttack() => StartAttack(SpinWaterStreamAttackCor());
    public void PredictiveWaterCannonAttack() => StartAttack(PredictiveWaterCannonAttackCor());
    public void GroundCrackAttack() => StartAttack(GroundCrackAttackCor());
    public void FanWaterCannonAttack() => StartAttack(WaterCannonSequence(ShotPattern.Fan, fanBurstCount));
    public void RadialWaterCannonAttack() => StartAttack(WaterCannonSequence(ShotPattern.Radial, radialBurstCount));

    private void SetSpinning(bool value)
    {
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetBool(SpinWaterHash, value);
    }

    private void PlayState(string state)
    {
        if (animator != null && animator.runtimeAnimatorController != null &&
            animator.HasState(0, Animator.StringToHash("Base Layer." + state))) animator.Play(state, 0, 0f);
    }

    private IEnumerator WindUpShot()
    {
        PlayState("WaterShot");
        yield return new WaitForSeconds(Mathf.Max(0f, waterShotReleaseDelay));
        // Sample the mouth after the Animator has applied the release pose.
        yield return new WaitForEndOfFrame();
    }

    private IEnumerator SpinWaterStreamAttackCor()
    {
        if (rotationSpeed <= 0 || rotationCount <= 0) yield break;
        yield return WindUpShot();
        if (!Alive) yield break;
        SetSpinning(true);
        if (animator != null)
            animator.SetFloat(SpinSpeedHash, (spinAnimation != null ? spinAnimation.length : 2f) * rotationSpeed / 360f);
        PlayState("SpinWater");
        yield return new WaitForEndOfFrame();
        activeBeam = PoolManager.Instance.Spawn(waterStreamObjectId, Mouth.position);
        var pump = activeBeam != null ? activeBeam.GetComponent<HydroPump>() : null;
        if (pump == null) yield break;
        pump.FollowMouth(Mouth);
        pump.StartCoroutine(pump.BeamBodyScale(1));
        float duration = 360f * rotationCount / rotationSpeed;
        float elapsed = 0f;
        while (Alive && activeBeam != null && activeBeam.activeInHierarchy && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (pump != null && pump.gameObject.activeInHierarchy)
            yield return pump.BeamBodyScale(-1);
        StopBeam();
        SetSpinning(false);
        PlayState("Move");
    }

    private IEnumerator PredictiveWaterCannonAttackCor()
    {
        yield return WaterCannonSequence(ShotPattern.Predictive, bulletSpawnCount);
    }

    private IEnumerator WaterCannonSequence(ShotPattern pattern, int repeatCount)
    {
        for (int i = 0; i < Mathf.Max(1, repeatCount) && Alive; i++)
        {
            if (target == null || !target.activeInHierarchy) yield break;
            pendingShot = pattern;
            pendingBurst = i;
            shotArmed = true;
            PlayState("WaterShot");
            float duration = waterShotAnimation != null ? waterShotAnimation.length : .625f;
            yield return new WaitForSeconds(Mathf.Max(bulletSpawnInterval,
                duration / Mathf.Max(.01f, animator != null ? animator.speed : 1f)));
            if (shotArmed)
                Debug.LogError("OctopusKing WaterShot is missing its FireWaterCannon animation event.", this);
            shotArmed = false;
        }
        PlayState("Move");
    }

    // Called only by the mouth-release frame of the WaterShot animation.
    public void FireWaterCannon()
    {
        if (!shotArmed || !attacking || !Alive) return;
        shotArmed = false;
        StartCoroutine(ReleaseWaterCannon(pendingShot, pendingBurst));
    }

    private IEnumerator ReleaseWaterCannon(ShotPattern pattern, int burst)
    {
        // Animation events precede final pose application; use the sampled mouth.
        yield return new WaitForEndOfFrame();
        if (!Alive || !attacking || target == null || !target.activeInHierarchy) yield break;
        if (pattern == ShotPattern.Predictive)
        {
            var bullet = PoolManager.Instance.Spawn(bulletId, Mouth.position);
            var move = bullet != null ? bullet.GetComponent<PredictionBulletMove>() : null;
            if (move != null) { move.target = target; move.Move(); }
            else ReturnToPool(bullet);
            yield break;
        }
        int count = Mathf.Max(1, pattern == ShotPattern.Fan ? fanBulletCount : radialBulletCount);
        Vector2 aim = (Vector2)target.transform.position - (Vector2)Mouth.position;
        float center = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        for (int i = 0; i < count; i++)
        {
            float angle;
            if (pattern == ShotPattern.Fan)
            {
                float step = count > 1 ? fanSpreadDegrees / (count - 1) : 0f;
                angle = center + (i - (count - 1) * .5f) * step + (burst % 2 == 0 ? -.25f : .25f) * step;
            }
            else angle = (i + (burst % 2) * .5f) * 360f / count;
            float radians = angle * Mathf.Deg2Rad;
            var bullet = PoolManager.Instance.Spawn(bulletId, Mouth.position);
            var move = bullet != null ? bullet.GetComponent<BulletMove>() : null;
            if (move != null)
            {
                move.target = target;
                move.MoveInDirection(new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)));
            }
            else ReturnToPool(bullet);
        }
    }

    private IEnumerator GroundCrackAttackCor()
    {
        for (int i = 0; i < Mathf.Max(1, groundStrikeCount) && Alive; i++)
        {
            PlayState("GroundCrack");
            yield return new WaitForSeconds(Mathf.Max(0f, groundAttackImpactDelay));
            if (!Alive) yield break;
            var effect = PoolManager.Instance.Spawn(groundAttackSkillId, transform.TransformPoint(groundOriginOffset));
            if (effect != null) effect.GetComponent<GroundCrack>()?.SetRadius(groundAttackRadius);
            float length = groundAttackAnimation != null ? groundAttackAnimation.length : 1.9f;
            yield return new WaitForSeconds(Mathf.Max(.1f, length - groundAttackImpactDelay));
        }
    }

    private void StopBeam()
    {
        ReturnToPool(activeBeam);
        activeBeam = null;
    }

    private static void ReturnToPool(GameObject obj)
    {
        if (obj == null) return;
        var pooled = obj.GetComponent<PooledObject>();
        if (pooled != null) pooled.DestroyObj();
        else obj.SetActive(false);
    }
}
