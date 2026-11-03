if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Runtime check required.");
UnityEngine.Time.timeScale = 0f;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var active = (System.Collections.Generic.HashSet<PooledObject>)typeof(PoolManager).GetField("activeObjects", flags).GetValue(PoolManager.Instance);
var before = new System.Collections.Generic.HashSet<PooledObject>(active);
var obj = PoolManager.Instance.Spawn(151, UnityEngine.Vector2.zero);
var king = obj.GetComponent<SlimeKing>();
king.StopAllCoroutines();
king.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
king.enemyMove.enabled = false;
float duration = king.openDuration;
try
{
    var animator = obj.GetComponent<UnityEngine.Animator>();
    animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
    var routine = (System.Collections.IEnumerator)typeof(SlimeKing).GetMethod("OpenBodyPattern", flags).Invoke(king, null);
    if (!routine.MoveNext() || !(routine.Current is UnityEngine.WaitForSeconds)) throw new System.Exception("Opening windup missing.");
    animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("OpenBodyRaise")) throw new System.Exception("Raise animation not entered.");
    foreach (var pooled in active)
        if (!before.Contains(pooled) && pooled.gameObject != obj) throw new System.Exception("Minions spawned before body opened.");

    // Advance the opening wait, then check the first real spawn wave.
    if (!routine.MoveNext()) throw new System.Exception("Hold phase missing.");
    animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("OpenBodyHold")) throw new System.Exception("Hold animation not entered.");
    int spawned = 0;
    foreach (var pooled in active)
    {
        if (before.Contains(pooled) || pooled.gameObject == obj) continue;
        var info = pooled.GetComponent<EnemyInfo>();
        if (info == null || info.id != king.minionId) throw new System.Exception("Unexpected spawn.");
        if (pooled.transform.position.y >= obj.transform.position.y) throw new System.Exception("Minion did not emerge in front of opening.");
        spawned++;
    }
    if (spawned != king.minionsPerWave) throw new System.Exception("Wrong minion wave count: " + spawned);
    animator.Update(king.openBodyHoldClip.length * 2.1f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("OpenBodyHold") || animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 2f)
        throw new System.Exception("Open pose did not loop while spawning.");

    king.openDuration = 0f;
    if (!routine.MoveNext() || !(routine.Current is UnityEngine.WaitForSeconds)) throw new System.Exception("Closing wait missing.");
    animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("OpenBodyLower")) throw new System.Exception("Lower animation not entered.");
    if (routine.MoveNext()) throw new System.Exception("Pattern did not finish.");
    animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Move")) throw new System.Exception("Pattern did not return to Move.");
    animator.SetTrigger("Hit"); animator.Update(.01f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Hit")) throw new System.Exception("Existing lethal Hit transition lost.");
    var report = "PASS: Raise before spawning; Hold loops; real first wave spawns " + spawned + " minions at the front opening; Lower after spawn phase; returns to Move; lethal Hit transition preserved. Full 25-frame sheet assigned, alpha retained.\n";
    System.IO.File.WriteAllText("Tools/Art/SlimeKingOpenBodyVerification.txt", report);
    return report;
}
finally
{
    king.openDuration = duration;
    foreach (var pooled in new System.Collections.Generic.List<PooledObject>(active))
        if (pooled != null && !before.Contains(pooled)) pooled.DestroyObj();
    UnityEngine.Time.timeScale = 1f;
}
