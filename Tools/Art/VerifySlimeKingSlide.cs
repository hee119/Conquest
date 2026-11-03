if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Runtime check required.");
UnityEngine.Time.timeScale = 0f;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var obj = PoolManager.Instance.Spawn(151, UnityEngine.Vector2.zero);
var king = obj.GetComponent<SlimeKing>();
king.StopAllCoroutines();
obj.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
king.enemyMove.enabled = false;
king.splitSpawnCount = 0;
var target = new UnityEngine.GameObject("SlimeSlideTestTarget");
target.transform.position = new UnityEngine.Vector3(-100, -100, 0);
typeof(SlimeKing).GetField("playerTf", flags).SetValue(king, target.transform);
var method = typeof(SlimeKing).GetMethod("SlidePattern", flags);
var animator = obj.GetComponent<UnityEngine.Animator>();
animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
var scale = obj.transform.localScale;
var oldMask = king.obstacleMask;
var oldOvershoot = king.slideOvershoot;
int burn = GameManager.Instance.burnChance, mold = GameManager.Instance.moldChance;
GameManager.Instance.burnChance = 0; GameManager.Instance.moldChance = 0;
UnityEngine.GameObject wall = null;
try
{
    var routine = (System.Collections.IEnumerator)method.Invoke(king, null);
    if (!routine.MoveNext() || !(routine.Current is UnityEngine.WaitForSeconds)) throw new System.Exception("Preparation wait missing.");
    animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("SlidePrepare")) throw new System.Exception("Preparation state missing.");
    float expectedSpeed = king.slidePrepareClip.length / UnityEngine.Mathf.Max(.01f, king.slideWindup);
    if (UnityEngine.Mathf.Abs(animator.GetFloat("SlidePrepareSpeed") - expectedSpeed) > .001f) throw new System.Exception("Preparation duration does not follow slideWindup.");
    if (!routine.MoveNext()) throw new System.Exception("Slide did not start.");
    animator.Update(0f); animator.Update(king.slideStartClip.length + .01f); animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("SlideLoop")) throw new System.Exception("Start did not enter afterimage loop.");
    animator.Update(king.slideLoopClip.length * 3.2f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("SlideLoop") || animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 3f)
        throw new System.Exception("Long slide exited loop too early.");
    if (obj.transform.localScale != scale) throw new System.Exception("Sprite animation applied an extra body squash.");
    var sprite = obj.GetComponent<UnityEngine.SpriteRenderer>().sprite;
    if (sprite == null || !sprite.name.StartsWith("SlimeKingSlide_")) throw new System.Exception("Slide has an empty or incorrect sprite.");
    int frame = int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1));
    if (frame < 12 || frame > 19) throw new System.Exception("Loop is not using central afterimage frames.");

    wall = new UnityEngine.GameObject("SlimeSlideTestWall"); wall.layer = 30;
    wall.AddComponent<UnityEngine.BoxCollider2D>().size = new UnityEngine.Vector2(.2f, .2f);
    wall.transform.position = obj.transform.position + new UnityEngine.Vector3(-1,-1,0).normalized * (king.hitRadius * scale.x * .5f);
    king.obstacleMask = 1 << 30;
    UnityEngine.Physics2D.SyncTransforms();
    if (!routine.MoveNext() || !(routine.Current is UnityEngine.WaitForSeconds)) throw new System.Exception("Wall stop did not enter ending wait.");
    animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("SlideEnd")) throw new System.Exception("Wall stop did not enter ending animation.");
    routine.MoveNext(); animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Move")) throw new System.Exception("Ending did not restore Move.");
    if (routine.MoveNext()) throw new System.Exception("Ending did not finish once.");

    // A short movement may finish before SlideStart's frames have all played.
    king.obstacleMask = 0; king.slideOvershoot = 0;
    target.transform.position = obj.transform.position;
    var shortSlide = (System.Collections.IEnumerator)method.Invoke(king, null);
    shortSlide.MoveNext(); shortSlide.MoveNext(); animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("SlideEnd")) throw new System.Exception("Short slide failed to end immediately.");
    shortSlide.MoveNext(); shortSlide.MoveNext();

    // Existing lethal Hit must still interrupt the central slide loop.
    animator.Play("Base Layer.SlideLoop", 0, 0f); animator.Update(0f);
    king.ResetHealth(100); king.TakeDamage(101, UnityEngine.Vector2.zero); animator.Update(.01f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Hit")) throw new System.Exception("Death did not interrupt slide.");
    var report = "PASS: preparation follows slideWindup; slide start enters central afterimage loop; loop persists beyond 3 cycles; fixed transform scale; no blank frames; wall stop and short travel enter ending; ending returns to Move; lethal Hit still interrupts.\n";
    System.IO.File.WriteAllText("Tools/Art/SlimeKingSlideVerification.txt", report);
    return report;
}
finally
{
    king.StopAllCoroutines(); king.obstacleMask = oldMask; king.slideOvershoot = oldOvershoot; king.splitSpawnCount = 2;
    GameManager.Instance.burnChance = burn; GameManager.Instance.moldChance = mold;
    obj.GetComponent<PooledObject>().DestroyObj();
    UnityEngine.Object.Destroy(target); if (wall != null) UnityEngine.Object.Destroy(wall);
    UnityEngine.Time.timeScale = 1f;
}
