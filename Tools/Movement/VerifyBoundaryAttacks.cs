var report = "Tools/Movement/BoundaryAttacksVerification.txt";
System.IO.File.WriteAllText(report, "Actual edge attack verification\n");
var previousStart = UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene;
UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/Scenes/Stage1.unity");
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
PlayerMove player = null;
UnityEngine.Rigidbody2D body = null;
SkillSpawner attacks = null;
var directions = new[] { UnityEngine.Vector2.up, UnityEngine.Vector2.down, UnityEngine.Vector2.left, UnityEngine.Vector2.right,
    new UnityEngine.Vector2(1,1).normalized, new UnityEngine.Vector2(-1,1).normalized,
    new UnityEngine.Vector2(1,-1).normalized, new UnityEngine.Vector2(-1,-1).normalized };
int phase = -1, lastFrame = -1, attackFrames = 0;
float phaseStart = 0, maxSpeed = 0, maxDrift = 0;
UnityEngine.Vector2 anchor = default;
double started = UnityEditor.EditorApplication.timeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction tick = null;
System.Action finish = () => {
    UnityEditor.EditorApplication.update -= tick;
    UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene = previousStart;
    UnityEngine.Time.timeScale = 1;
    UnityEditor.EditorApplication.isPlaying = false;
};
tick = () => {
try {
    if (!UnityEditor.EditorApplication.isPlaying) return;
    UnityEngine.Application.runInBackground = true;
    if (UnityEditor.EditorApplication.timeSinceStartup - started > 100) throw new System.Exception("Timeout");
    if (lastFrame == UnityEngine.Time.frameCount) return;
    lastFrame = UnityEngine.Time.frameCount;
    if (player == null) {
        player = UnityEngine.Object.FindFirstObjectByType<PlayerMove>();
        if (player == null || StageManager.Instance == null || PoolManager.Instance == null) { player = null; return; }
        StageManager.Instance.StopRun();
        body = player.GetComponent<UnityEngine.Rigidbody2D>();
        attacks = player.GetComponent<SkillSpawner>();
        var input = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input != null) input.enabled = false;
        foreach (var events in UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(UnityEngine.FindObjectsSortMode.None)) events.enabled = false;
        player.GetComponent<PlayerCombat>().hp = 100000;
        phaseStart = UnityEngine.Time.time + 1f;
        return;
    }
    if (UnityEngine.Time.time < phaseStart) return;
    if (phase < 0 || UnityEngine.Time.time - phaseStart >= 2f) {
        if (phase >= 0) {
            if (attackFrames == 0 || maxSpeed > player.speed + .05f || maxDrift > .15f)
                throw new System.Exception($"edge {phase}: attack frames={attackFrames}, speed={maxSpeed}, drift={maxDrift}");
            System.IO.File.AppendAllText(report, $"PASS edge {phase} {directions[phase]}: attack frames={attackFrames}, max speed={maxSpeed:F4}, inward drift={maxDrift:F4}\n");
        }
        phase++;
        if (phase == directions.Length) { System.IO.File.AppendAllText(report,"COMPLETE\n"); finish(); return; }
        var tilemap = StageManager.Instance.backgroundTilemap;
        var min = tilemap.transform.TransformPoint(tilemap.localBounds.min);
        var max = tilemap.transform.TransformPoint(tilemap.localBounds.max);
        var dir = directions[phase];
        body.position = new UnityEngine.Vector2(dir.x < 0 ? min.x : dir.x > 0 ? max.x : (min.x+max.x)/2,
            dir.y < 0 ? min.y : dir.y > 0 ? max.y : (min.y+max.y)/2);
        body.linearVelocity = UnityEngine.Vector2.zero;
        typeof(PlayerMove).GetMethod("KeepInsideTilemapBounds",flags).Invoke(player,null);
        anchor = body.position;
        player.moveDir = dir;
        player.StopMove(1);
        phaseStart = UnityEngine.Time.time;
        maxSpeed = maxDrift = 0;
        attackFrames = 0;
    }
    attacks.SkillUse(attacks.skillId1);
    if (player.GetComponent<AnimationController>().IsBasicAttacking) attackFrames++;
    maxSpeed = UnityEngine.Mathf.Max(maxSpeed,body.linearVelocity.magnitude);
    maxDrift = UnityEngine.Mathf.Max(maxDrift,UnityEngine.Vector2.Distance(body.position,anchor));
} catch (System.Exception e) { System.IO.File.AppendAllText(report,"FAIL: "+e+"\n"); finish(); }
};
UnityEditor.EditorApplication.update += tick;
UnityEditor.EditorApplication.isPlaying = true;
return "Running attacks against four edges and four corners";
