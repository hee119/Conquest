if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Runtime check requires Play mode.");
UnityEngine.Time.timeScale = 0f;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var manager = GameManager.Instance;
int burn = manager.burnChance, mold = manager.moldChance;
manager.burnChance = 0; manager.moldChance = 0;
var obj = PoolManager.Instance.Spawn(151, UnityEngine.Vector2.zero);
obj.name = "SlimeKingAnimationVerification";
var king = obj.GetComponent<SlimeKing>();
king.StopAllCoroutines();
var agent = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
agent.enabled = false;
king.enemyMove.enabled = false;
king.splitSpawnCount = 0;
var animator = obj.GetComponent<UnityEngine.Animator>();
animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
animator.Rebind(); animator.Update(0f);
king.ResetHealth(100f);
king.TakeDamage(10f, UnityEngine.Vector2.zero);
animator.Update(.02f);
if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Move") || king.hp != 90f)
    throw new System.Exception("Nonlethal damage interrupted king movement.");
king.TakeDamage(100f, UnityEngine.Vector2.zero);
animator.Update(.1f);
if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Hit") || !obj.activeInHierarchy || king.isDead)
    throw new System.Exception("Lethal damage must play Hit before releasing the pooled boss.");
if (!(bool)typeof(EnemyCombat).GetField("isDying", flags).GetValue(king))
    throw new System.Exception("Lethal hit did not enter dying phase.");
foreach (var collider in obj.GetComponents<UnityEngine.Collider2D>())
    if (collider.enabled) throw new System.Exception("Dying boss still collides.");
if (king.enemyMove.enabled) throw new System.Exception("Dying boss still moves.");
if (!UnityEditor.AssetDatabase.GetAssetPath(obj.GetComponent<UnityEngine.SpriteRenderer>().sprite).EndsWith("SlimeKingHit.png"))
    throw new System.Exception("Hit is not using the crowned sheet.");

var regular = PoolManager.Instance.Spawn(106, new UnityEngine.Vector2(100,100));
var normalCombat = regular.GetComponent<EnemyCombat>();
normalCombat.StopAllCoroutines();
var normalAgent = regular.GetComponent<UnityEngine.AI.NavMeshAgent>();
if (normalAgent != null) normalAgent.enabled = false;
normalCombat.ResetHealth(100);
normalCombat.TakeDamage(1f, UnityEngine.Vector2.zero);
var normalAnimator = regular.GetComponent<UnityEngine.Animator>();
normalAnimator.Update(.02f);
if (!normalAnimator.GetCurrentAnimatorStateInfo(0).IsName("Hit"))
    throw new System.Exception("Regular slime nonlethal Hit regression.");
regular.GetComponent<PooledObject>().DestroyObj();
manager.burnChance = burn; manager.moldChance = mold;
var report = "PASS: king nonlethal hit stays in Move; lethal hit enters crowned Hit while still active; collisions and movement stop; regular slime still plays Hit on nonlethal damage.\n";
System.IO.File.WriteAllText("Tools/Art/SlimeKingAnimationVerification.txt", report);
return report;
