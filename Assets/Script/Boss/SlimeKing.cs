using System.Collections;
using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyFacing))]
public class SlimeKing : SplitEnemy
{
    public sealed class SplitEncounter
    {
        private readonly HashSet<SlimeKing> members = new HashSet<SlimeKing>();
        private bool interrupted;
        public bool HasLivingMembers => members.Count > 0;
        public bool WasCleared => members.Count == 0 && !interrupted;
        internal void Add(SlimeKing member) => members.Add(member);
        internal void Remove(SlimeKing member)
        {
            if (members.Remove(member) && !member.isDead) interrupted = true;
        }
    }

    private SplitEncounter splitEncounter;

    public SplitEncounter StartSplitEncounter()
    {
        var encounter = new SplitEncounter();
        AttachSplitEncounter(encounter);
        return encounter;
    }

    private void AttachSplitEncounter(SplitEncounter encounter)
    {
        splitEncounter = encounter;
        encounter.Add(this);
    }
    [Header("공통")]
    public float startDelay = 1.5f;
    public float patternInterval = 2f;
    public float hitRadius = 1.2f;          // 슬라이딩 피격 판정 반경

    [Header("패시브 - 분열")]
    public int maxSplitCount = 3;           // 마지막 분열은 일반 슬라임으로 나옴
    public int normalSlimeId;               // 일반 슬라임 풀 id
    public float splitSpread = 0.5f;
    public float splitDamageDiv = 1.5f;     // 세대마다 누적 적용
    public float splitHpDiv = 1.4f;
    public float splitSpeedMul = 1.3f;
    public float splitScaleDiv = 1.3f;

    [Header("패턴1 - 몸 열고 슬라임 생성")]
    public int minionId;                    // 작은 슬라임 풀 id
    public float openDuration = 3f;
    public float minionSpawnInterval = 0.8f;
    public int minionsPerWave = 2;
    public AnimationClip openBodyRaiseClip;
    public AnimationClip openBodyHoldClip;
    public AnimationClip openBodyLowerClip;

    [Header("패턴2 - 점프 내려찍기")]
    public int warningId;                   // 워닝 사인 풀 id
    public int slamCount = 3;
    public float jumpHeight = 8f;
    public float jumpRiseTime = 0.4f;
    public float warningTime = 0.8f;
    public float fallTime = 0.12f;
    public float slamRadius = 1.5f;
    public float slamDamageMul = 1.5f;
    public float slamGap = 0.3f;
    public float slamRecovery = 0.8f;
    public float shadowStartAlpha = 0.15f; 
    public float shadowEndAlpha = 0.8f; 

    [Header("패턴3 - 슬라이딩")]
    public float slideWindup = 0.6f;
    public float slideSpeed = 14f;
    public float slideOvershoot = 3f;
    public float slideDamageMul = 1f;
    public float slideRecovery = 0.8f;
    public AnimationClip slidePrepareClip;
    public AnimationClip slideStartClip;
    public AnimationClip slideLoopClip;
    public AnimationClip slideEndClip;
    public LayerMask obstacleMask;          // 벽 레이어 (비워두면 벽 체크 안 함)

    NavMeshAgent agent;
    BossBodyObstacle bodyObstacle;
    Collider2D[] cols;
    Transform playerTf;
    readonly List<GameObject> warnings = new List<GameObject>();

    Vector3 baseScale = Vector3.one;
    Vector3 airbornePos;
    bool inPattern;
    bool isAirborne;

    protected override bool PlayHitOnNonLethalDamage => false;

    protected override void PrepareDeathAnimation()
    {
        // Attack routines must not move or rescale the dying boss while Hit plays.
        StopAllCoroutines();
        ReleaseAllWarnings();
        if (isAirborne)
        {
            transform.position = airbornePos;
            isAirborne = false;
        }
        if (inPattern)
        {
            transform.localScale = baseScale;
            inPattern = false;
        }
        SetColliders(false);
        SetAI(false);
    }

    // ───────────── 생명주기 ─────────────

    protected override void OnEnable()
    {
        base.OnEnable();    // SplitEnemy: 스케일 1로 초기화
        splitEncounter = null;

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (bodyObstacle == null) bodyObstacle = GetComponent<BossBodyObstacle>();
        bodyObstacle?.SetSkillBodyActive(true);
        if (cols == null || cols.Length == 0) cols = GetComponents<Collider2D>();

        splitCount = maxSplitCount;     // 풀 재사용 시 복구하고 분열체는 스폰 직후 덮어쓴다.
        baseScale = transform.localScale;
        inPattern = false;
        isAirborne = false;

        // Pooled split generations must start in Move, never the previous death pose.
        if (bossAnimator != null && bossAnimator.runtimeAnimatorController != null)
        {
            bossAnimator.Rebind();
            bossAnimator.Update(0f);
        }

        StartCoroutine(PatternLoop());
    }

    protected override void OnDisable()
    {
        splitEncounter?.Remove(this);
        splitEncounter = null;
        base.OnDisable();   // 스탯 초기화 유지

        ReleaseAllWarnings();
        bodyObstacle?.SetSkillBodyActive(false);
        inPattern = false;
        isAirborne = false;
        SetColliders(true);
        if (agent != null) agent.updatePosition = true;
        if (enemyMove != null) enemyMove.enabled = true;
    }

    public override void Die()
    {
        StopAllCoroutines();
        bodyObstacle?.SetSkillBodyActive(false);
        ReleaseAllWarnings();

        if (isAirborne) { transform.position = airbornePos; isAirborne = false; }
        if (inPattern) { transform.localScale = baseScale; inPattern = false; }
        SetColliders(true);
        SetAI(true);

        base.Die();         // EnemySplit() → EnemyCombat.Die()
    }

    // ───────────── 패시브: 분열 ─────────────

    public override void EnemySplit()
    {
        if (splitCount <= 0 || splitSpawnCount <= 0) return;

        SoundManager.Instance?.Play(SoundIds.SlimeSplit);

        bool toNormal = splitCount <= 1;
        var children = new List<SlimeKing>(splitSpawnCount);
        float parentHp = maxHp;
        float parentSpeed = enemyInfo.speed;
        int parentDamage = enemyInfo.damage;
        int parentDefense = enemyInfo.defense;
        int parentRegen = enemyInfo.hpRegenRate;
        int parentExplosionDamage = enemyInfo.explosionDamage;
        int parentExplosionRange = enemyInfo.explosionRange;

        for (int i = 0; i < splitSpawnCount; i++)
        {
            Vector3 pos = transform.position + (Vector3)(Random.insideUnitCircle * splitSpread);
            if (toNormal)
            {
                PoolManager.Instance.Spawn(normalSlimeId, pos);
                continue;
            }

            GameObject obj = PoolManager.Instance.Spawn(enemyInfo.id, pos);
            if (obj == null || !obj.TryGetComponent(out SlimeKing child)) continue;
            if (splitEncounter != null) child.AttachSplitEncounter(splitEncounter);
            children.Add(child);
        }

        int childCount = children.Count;
        for (int i = 0; i < childCount; i++)
        {
            SlimeKing child = children[i];
            child.enemyInfo.hp = parentHp / childCount;
            child.enemyInfo.damage = DivideStat(parentDamage, childCount, i);
            child.enemyInfo.defense = DivideStat(parentDefense, childCount, i);
            child.enemyInfo.hpRegenRate = DivideStat(parentRegen, childCount, i);
            child.enemyInfo.explosionDamage = DivideStat(parentExplosionDamage, childCount, i);
            child.enemyInfo.explosionRange = DivideStat(parentExplosionRange, childCount, i);
            child.ResetHealth(child.enemyInfo.hp);
            child.enemyInfo.speed = parentSpeed / childCount;
            child.enemyMove.moveSpeed = child.enemyInfo.speed;
            child.enemyMove.MoveSpeedRenewal();
            obj.transform.localScale = new Vector3(
                transform.localScale.x / splitScaleDiv, transform.localScale.y / splitScaleDiv, 1f);
            child.splitCount = splitCount - 1;
        }
    }

    private static int DivideStat(int total, int count, int index)
    {
        total = Mathf.Max(0, total);
        return total / count + (index < total % count ? 1 : 0);
    }

    // ───────────── 패턴 루프 ─────────────

    IEnumerator PatternLoop()
    {
        yield return new WaitForSeconds(startDelay);

        int last = -1;
        while (true)
        {
            yield return new WaitForSeconds(patternInterval);

            int next;
            do { next = Random.Range(0, 3); } while (next == last);
            last = next;

            baseScale = transform.localScale;
            inPattern = true;
            bodyObstacle?.SetSkillBodyActive(true);
            SetAI(false);

            switch (next)
            {
                case 0: yield return OpenBodyPattern(); break;
                case 1: yield return JumpSlamPattern(); break;
                case 2: yield return SlidePattern(); break;
            }

            transform.localScale = baseScale;
            SetAI(true);
            inPattern = false;
        }
    }

    // 패턴1: 몸을 열어 작은 슬라임 생성
    IEnumerator OpenBodyPattern()
    {
        bodyObstacle?.SetSummoning(true);
        Rigidbody2D bossBody = GetComponent<Rigidbody2D>();
        if (bossBody != null)
            bossBody.linearVelocity = Vector2.zero;

        bool hasOpenAnimation = bossAnimator != null && openBodyRaiseClip != null &&
            openBodyHoldClip != null && openBodyLowerClip != null &&
            bossAnimator.HasState(0, Animator.StringToHash("Base Layer.OpenBodyRaise")) &&
            bossAnimator.HasState(0, Animator.StringToHash("Base Layer.OpenBodyHold")) &&
            bossAnimator.HasState(0, Animator.StringToHash("Base Layer.OpenBodyLower"));

        if (hasOpenAnimation)
        {
            bossAnimator.Play("Base Layer.OpenBodyRaise", 0, 0f);
            yield return new WaitForSeconds(openBodyRaiseClip.length);
            bossAnimator.Play("Base Layer.OpenBodyHold", 0, 0f);
        }
        else
        {
            yield return ScaleTo(new Vector3(baseScale.x * 1.15f, baseScale.y * 0.8f, 1f), 0.3f);
        }

        float elapsed = 0f;
        float timer = minionSpawnInterval;      // 열자마자 첫 웨이브
        while (elapsed < openDuration)
        {
            timer += Time.deltaTime;
            elapsed += Time.deltaTime;
            if (timer >= minionSpawnInterval)
            {
                timer = 0f;
                for (int i = 0; i < minionsPerWave; i++)
                {
                    Vector3 offset;
                    if (hasOpenAnimation)
                    {
                        // Emerge from the visible opening at the front of the body.
                        float side = minionsPerWave <= 1 ? 0f :
                            Mathf.Lerp(-0.25f, 0.25f, i / (float)(minionsPerWave - 1));
                        offset = new Vector3(side * baseScale.x, -0.8f * baseScale.y, 0f);
                    }
                    else
                    {
                        offset = (Vector3)(Random.insideUnitCircle.normalized * baseScale.x * 0.8f);
                    }
                    Vector3 pos = transform.position + offset;
                    PoolManager.Instance.Spawn(minionId, pos);
                }
            }
            yield return null;
        }

        if (hasOpenAnimation)
        {
            bossAnimator.Play("Base Layer.OpenBodyLower", 0, 0f);
            yield return new WaitForSeconds(openBodyLowerClip.length);
            bossAnimator.Play("Base Layer.Move", 0, 0f);
        }
        else
        {
            yield return ScaleTo(baseScale, 0.2f);
        }

        bodyObstacle?.SetSummoning(false);
    }

    // 패턴2: 점프 후 워닝 사인 → 플레이어 위치 연속 내려찍기
    IEnumerator JumpSlamPattern()
    {
        float s = baseScale.x;
        Vector3 ground = transform.position;

        SetColliders(false);
        isAirborne = true;
        airbornePos = ground;
        yield return MoveTo(ground + Vector3.up * jumpHeight, jumpRiseTime);

        for (int i = 0; i < slamCount; i++)
        {
            Transform player = GetPlayer();
            Vector3 target = player != null ? player.position : ground;
            target.z = ground.z;
            airbornePos = target;               // 공중에서 죽으면 여기로 내려서 분열

            GameObject warning = SpawnWarning(target, slamRadius * s * 2f);
            StartCoroutine(FadeShadow(warning, warningTime + fallTime));   // 착지 순간까지 진해짐
            yield return new WaitForSeconds(warningTime);

            transform.position = target + Vector3.up * jumpHeight;
            yield return MoveTo(target, fallTime, true);

            TryHitPlayer(target, slamRadius * s, slamDamageMul);
            ReleaseWarning(warning);

            if (i < slamCount - 1)
            {
                yield return new WaitForSeconds(slamGap);
                yield return MoveTo(target + Vector3.up * jumpHeight, jumpRiseTime);
            }
        }

        isAirborne = false;
        SetColliders(true);
        yield return new WaitForSeconds(slamRecovery);
    }

    // 패턴3: 플레이어 위치로 빠르게 슬라이딩
    IEnumerator SlidePattern()
    {
        float s = baseScale.x;
        bool hasSlideAnimation = bossAnimator != null && slidePrepareClip != null &&
            slideStartClip != null && slideLoopClip != null && slideEndClip != null &&
            bossAnimator.HasState(0, Animator.StringToHash("Base Layer.SlidePrepare")) &&
            bossAnimator.HasState(0, Animator.StringToHash("Base Layer.SlideStart")) &&
            bossAnimator.HasState(0, Animator.StringToHash("Base Layer.SlideLoop")) &&
            bossAnimator.HasState(0, Animator.StringToHash("Base Layer.SlideEnd"));

        if (hasSlideAnimation)
        {
            bossAnimator.SetFloat("SlidePrepareSpeed", slidePrepareClip.length / Mathf.Max(0.01f, slideWindup));
            bossAnimator.Play("Base Layer.SlidePrepare", 0, 0f);
            yield return new WaitForSeconds(Mathf.Max(0.01f, slideWindup));
        }
        else
        {
            yield return ScaleTo(new Vector3(baseScale.x * 1.2f, baseScale.y * 0.8f, 1f), slideWindup);
        }

        Transform player = GetPlayer();
        if (player == null)
        {
            if (hasSlideAnimation) bossAnimator.Play("Base Layer.Move", 0, 0f);
            yield break;
        }

        Vector3 start = transform.position;
        Vector2 dir = (Vector2)(player.position - start);
        float dist = dir.magnitude + slideOvershoot;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

        float travelled = 0f;
        bool hit = false;
        if (hasSlideAnimation) bossAnimator.Play("Base Layer.SlideStart", 0, 0f);
        while (travelled < dist)
        {
            float step = Mathf.Min(slideSpeed * Time.deltaTime, dist - travelled);

            if (obstacleMask.value != 0)
            {
                RaycastHit2D wall = Physics2D.CircleCast(transform.position, hitRadius * s * 0.5f, dir, step + 0.05f, obstacleMask);
                if (wall.collider != null) break;
            }

            transform.position += (Vector3)(dir * step);
            travelled += step;

            if (!hit) hit = TryHitPlayer(transform.position, hitRadius * s, slideDamageMul);
            yield return null;
        }

        if (hasSlideAnimation)
        {
            // SlideStart transitions to the afterimage loop. Only actual movement
            // completion (or a wall) may leave that loop and play the ending.
            bossAnimator.Play("Base Layer.SlideEnd", 0, 0f);
            yield return new WaitForSeconds(slideEndClip.length);
            bossAnimator.Play("Base Layer.Move", 0, 0f);
            if (slideRecovery > slideEndClip.length)
                yield return new WaitForSeconds(slideRecovery - slideEndClip.length);
        }
        else
        {
            yield return ScaleTo(baseScale, 0.15f);
            yield return new WaitForSeconds(slideRecovery);
        }
    }

    // ───────────── 유틸 ─────────────

    bool TryHitPlayer(Vector3 pos, float radius, float mul)
    {
        Transform p = GetPlayer();
        if (p == null || Vector2.Distance(pos, p.position) > radius) return false;

        // TODO: 실제 플레이어 피격 함수로 교체
        p.gameObject.SendMessage("TakeDamage", Mathf.RoundToInt(enemyInfo.damage * mul), SendMessageOptions.DontRequireReceiver);
        return true;
    }

    Transform GetPlayer()
    {
        if (playerTf == null || !playerTf.gameObject.activeInHierarchy)
        {
            GameObject p = GameObject.FindWithTag("Player");
            playerTf = p != null ? p.transform : null;
        }
        return playerTf;
    }

    void SetAI(bool on)
    {
        if (on)
        {
            if (agent != null)
            {
                agent.updatePosition = true;
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                if (agent.isOnNavMesh) agent.isStopped = false;
            }
            if (enemyMove != null) enemyMove.enabled = true;
        }
        else
        {
            if (enemyMove != null) enemyMove.enabled = false;
            if (agent != null)
            {
                if (agent.isOnNavMesh) agent.isStopped = true;
                agent.updatePosition = false;
            }
        }
    }

    void SetColliders(bool on)
    {
        if (cols == null) return;
        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null) cols[i].enabled = on;
    }

    GameObject SpawnWarning(Vector3 pos, float diameter)
    {
        GameObject w = PoolManager.Instance.Spawn(warningId, pos);
        if (w != null)
        {
            w.transform.localScale = new Vector3(diameter, diameter, 1f);
            SpriteRenderer sr = w.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) SetAlpha(sr, shadowStartAlpha);   // 풀 재사용 시 알파 초기화
            warnings.Add(w);
        }
        return w;
    }

    void ReleaseWarning(GameObject w)
    {
        if (w == null) return;
        warnings.Remove(w);
        if (PoolManager.Instance == null) return;

        PooledObject po = w.GetComponent<PooledObject>();
        if (po != null) po.DestroyObj();
        else w.SetActive(false);
    }

    void ReleaseAllWarnings()
    {
        for (int i = warnings.Count - 1; i >= 0; i--)
            ReleaseWarning(warnings[i]);
        warnings.Clear();
    }

    IEnumerator ScaleTo(Vector3 target, float time)
    {
        Vector3 from = transform.localScale;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(from, target, t / time);
            yield return null;
        }
        transform.localScale = target;
    }

    IEnumerator MoveTo(Vector3 to, float time, bool easeIn = false)
    {
        Vector3 from = transform.position;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            k = easeIn ? k * k : Mathf.SmoothStep(0f, 1f, k);
            transform.position = Vector3.Lerp(from, to, k);
            yield return null;
        }
        transform.position = to;
    }
    
    IEnumerator FadeShadow(GameObject w, float time)
    {
        if (w == null) yield break;
        SpriteRenderer sr = w.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) yield break;

        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            SetAlpha(sr, Mathf.Lerp(shadowStartAlpha, shadowEndAlpha, t / time));
            yield return null;
        }
        SetAlpha(sr, shadowEndAlpha);
    }

    void SetAlpha(SpriteRenderer sr, float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
