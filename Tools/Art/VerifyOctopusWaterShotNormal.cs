if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Runtime check required.");
UnityEngine.Time.timeScale = 0f;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var active = (System.Collections.Generic.HashSet<PooledObject>)typeof(PoolManager).GetField("activeObjects", flags).GetValue(PoolManager.Instance);
var before = new System.Collections.Generic.HashSet<PooledObject>(active);
var player = UnityEngine.GameObject.FindWithTag("Player");
if (player == null || player.GetComponent<PlayerCombat>() == null) throw new System.Exception("Active player required.");
var obj = PoolManager.Instance.Spawn(108, player.transform.position + UnityEngine.Vector3.right * 2f);
var combat = obj.GetComponent<EnemyCombat>();
var attack = obj.GetComponent<RamgedEnemyAttack>();
attack.StopAllCoroutines();
obj.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
obj.GetComponent<EnemyMove>().enabled = false;
var animator = obj.GetComponent<UnityEngine.Animator>();
animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
animator.Rebind(); animator.Update(0f);
var moveSprite = obj.GetComponent<UnityEngine.SpriteRenderer>().sprite;
UnityEngine.Sprite releaseSprite = null;
try
{
    if (!UnityEditor.AssetDatabase.GetAssetPath(moveSprite).EndsWith("OctopusUncrownedMove.png")) throw new System.Exception("Move sheet not assigned.");
    var method = typeof(RamgedEnemyAttack).GetMethod("AttackCoolTimeCor", flags);
    var routine = (System.Collections.IEnumerator)method.Invoke(attack, null);
    if (!routine.MoveNext() || !(routine.Current is UnityEngine.WaitUntil)) throw new System.Exception("Range wait missing.");
    if (!routine.MoveNext() || !(routine.Current is UnityEngine.WaitForSeconds)) throw new System.Exception("Release anticipation missing.");
    float delay = (float)typeof(RamgedEnemyAttack).GetField("attackReleaseDelay", flags).GetValue(attack);
    if (UnityEngine.Mathf.Abs(delay - .375f) > .001f) throw new System.Exception("Incorrect release delay.");
    foreach (var pooled in active)
        if (!before.Contains(pooled) && pooled.gameObject != obj) throw new System.Exception("Projectile spawned before mouth opened.");
    animator.Update(0f); animator.Update(.001f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) throw new System.Exception("Attack trigger failed.");
    animator.Update(delay);
    releaseSprite = obj.GetComponent<UnityEngine.SpriteRenderer>().sprite;
    if (!releaseSprite.name.EndsWith("_09")) throw new System.Exception("Release not aligned to O-mouth frame: " + releaseSprite.name);
    if (!routine.MoveNext()) throw new System.Exception("Shot did not complete.");
    int bullets = 0;
    foreach (var pooled in active)
        if (!before.Contains(pooled) && pooled.gameObject != obj && pooled.GetComponent<BulletMove>() != null) bullets++;
    if (bullets != 1) throw new System.Exception("Expected exactly one water projectile, got " + bullets);
    animator.Update(1f); animator.Update(0f);
    if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Run")) throw new System.Exception("Attack did not return to movement.");
    int count = active.Count;
    var interrupted = (System.Collections.IEnumerator)method.Invoke(attack, null);
    interrupted.MoveNext(); interrupted.MoveNext(); combat.hp = 0;
    if (interrupted.MoveNext() || active.Count != count) throw new System.Exception("Dying enemy fired after anticipation.");

    // Render the actual imported sprites on grey to inspect transparency and crowns.
    var preview = new UnityEngine.GameObject("OctopusSpritePreview");
    var cameraObject = new UnityEngine.GameObject("OctopusPreviewCamera");
    var camera = cameraObject.AddComponent<UnityEngine.Camera>();
    camera.enabled = false; camera.transform.position = new UnityEngine.Vector3(0,0,-10);
    camera.orthographic = true; camera.orthographicSize = .8f; camera.cullingMask = 1 << 31;
    camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor; camera.backgroundColor = new UnityEngine.Color(.16f,.17f,.19f,1);
    camera.allowHDR = false; camera.allowMSAA = false;
    var shown = new[] { moveSprite, releaseSprite };
    for (int i = 0; i < 2; i++) {
        var child = new UnityEngine.GameObject("Pose" + i); child.layer = 31; child.transform.SetParent(preview.transform);
        child.transform.position = new UnityEngine.Vector3(i == 0 ? -.65f : .65f, 0, 0);
        child.AddComponent<UnityEngine.SpriteRenderer>().sprite = shown[i];
    }
    var rt = new UnityEngine.RenderTexture(800,480,24); rt.filterMode = UnityEngine.FilterMode.Point; camera.targetTexture = rt;
    camera.Render(); var previous = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
    var image = new UnityEngine.Texture2D(800,480,UnityEngine.TextureFormat.RGBA32,false);
    image.ReadPixels(new UnityEngine.Rect(0,0,800,480),0,0); image.Apply();
    System.IO.File.WriteAllBytes("/tmp/ConquestUncrownedOctopusPreview.png", UnityEngine.ImageConversion.EncodeToPNG(image));
    UnityEngine.RenderTexture.active = previous; camera.targetTexture = null; rt.Release();
    UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(cameraObject); UnityEngine.Object.Destroy(preview);

    var report = "PASS: normal octopus uses uncrowned Move; Attack triggers; no projectile during anticipation; one actual water projectile at O-mouth cell 09 after 0.375s; returns to Run; lethal interruption cancels pending shot.\n";
    System.IO.File.WriteAllText("Tools/Art/UncrownedOctopusVerification.txt", report);
    return report;
}
finally
{
    attack.StopAllCoroutines(); combat.StopAllCoroutines();
    foreach (var pooled in new System.Collections.Generic.List<PooledObject>(active))
        if (pooled != null && !before.Contains(pooled)) pooled.DestroyObj();
    UnityEngine.Time.timeScale = 1f;
}
