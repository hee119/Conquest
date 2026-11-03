using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

public sealed class OctopusVfxPlaycheck : MonoBehaviour
{
    public GameObject bossPrefab;
    private readonly Dictionary<string, object> report = new Dictionary<string, object>();
    private readonly List<string> errors = new List<string>();
    private Camera view;
    private PoolManager pool;
    private OctopusKing boss;
    private string directory;
    private readonly FieldInfo attackField = typeof(OctopusKing).GetField("attacking", BindingFlags.Instance | BindingFlags.NonPublic);
    private bool Attacking => boss != null && (bool)attackField.GetValue(boss);
    private void OnEnable() => Application.logMessageReceived += Log;
    private void OnDisable() => Application.logMessageReceived -= Log;
    private void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors.Add(message + "\n" + trace);
    }
    private IEnumerator Start()
    {
        directory = Path.GetFullPath("Tools/PixelFx/records/boss-runtime");
        Directory.CreateDirectory(directory);
        view = new GameObject("Verification Camera").AddComponent<Camera>();
        view.transform.position = new Vector3(0, 0, -20);view.orthographic = true;view.orthographicSize = 9;
        view.backgroundColor = new Color(.07f,.1f,.15f);view.clearFlags = CameraClearFlags.SolidColor;
        var light = new GameObject("Verification Light").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;light.intensity = 1;
        var player = new GameObject("Verification Player");player.tag = "Player";player.transform.position = new Vector3(4,-1,0);
        var rb = player.AddComponent<Rigidbody2D>();rb.gravityScale = 0;rb.bodyType = RigidbodyType2D.Kinematic;
        var movement = player.AddComponent<PlayerMove>();movement.enabled = false;movement.moveDir = new Vector2(.4f,.2f);movement.speed = 3;
        pool = new GameObject("Verification Pool").AddComponent<PoolManager>();
        var staging = new GameObject("Inactive Setup");staging.SetActive(false);
        var instance = Instantiate(bossPrefab, staging.transform);
        boss = instance.GetComponent<OctopusKing>();boss.runPatternsAutomatically = false;
        instance.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;instance.GetComponent<EnemyMove>().enabled = false;
        instance.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        instance.SetActive(false);instance.transform.SetParent(null);instance.transform.position = Vector3.zero;instance.SetActive(true);
        yield return null;
        report["health"] = instance.GetComponent<EnemyCombat>().hp;
        var mouth = boss.waterSpawnPoint.transform;
        boss.SpinWaterStreamAttack();
        float elapsed=0,maxGap=0;int samples=0;var sprites=new HashSet<string>();
        while(Attacking && elapsed<6){
            foreach(var beam in FindObjectsByType<HydroPump>(FindObjectsSortMode.None)){
                maxGap=Mathf.Max(maxGap,Vector3.Distance(beam.transform.position,mouth.position));samples++;
                sprites.Add(instance.GetComponent<SpriteRenderer>().sprite.name);
            }
            if(elapsed>.8f && elapsed<.9f)Capture("hydro.png");
            elapsed+=Time.deltaTime;yield return new WaitForEndOfFrame();
        }
        report["hydro"] = new { completed=!Attacking,samples,maxMouthGap=maxGap,spinFrames=sprites.Count,activeAfter=FindObjectsByType<HydroPump>(FindObjectsSortMode.None).Length };
        yield return new WaitForSeconds(.15f);
        boss.PredictiveWaterCannonAttack();elapsed=0;
        var shots=new HashSet<int>();float shotGap=0,minPredictionDot=1;
        while(Attacking && elapsed<7){
            foreach(var projectile in FindObjectsByType<OctopusWaterCannonProjectile>(FindObjectsSortMode.None)){
                if(shots.Add(projectile.GetInstanceID())){
                    shotGap=Mathf.Max(shotGap,Vector3.Distance(projectile.transform.position,mouth.position));
                    Vector2 expected=(Vector2)player.transform.position+movement.moveDir*movement.speed*.2f-(Vector2)projectile.transform.position;
                    minPredictionDot=Mathf.Min(minPredictionDot,Vector2.Dot(expected.normalized,projectile.GetComponent<Rigidbody2D>().linearVelocity.normalized));
                }
            }
            if(elapsed>.9f && elapsed<1f)Capture("cannon.png");
            elapsed+=Time.deltaTime;yield return new WaitForEndOfFrame();
        }
        report["cannon"]=new{completed=!Attacking,count=shots.Count,expected=boss.bulletSpawnCount,maxMouthGap=shotGap,minPredictionDot};
        var splash=pool.Spawn(1003,new Vector2(3,-1));yield return new WaitForSeconds(1.2f);report["smallSplashReturned"]=!splash.activeSelf;
        boss.groundStrikeCount=2;boss.groundAttackRadius=4.2f;boss.GroundCrackAttack();elapsed=0;
        var groundSpawns=new HashSet<string>();float maxRadius=0;
        while((Attacking || elapsed<4.6f) && elapsed<7){
            foreach(var ground in FindObjectsByType<GroundCrack>(FindObjectsSortMode.None)){
                groundSpawns.Add(ground.GetInstanceID()+":"+ground.GetComponent<PooledObject>().SpawnVersion);
                var area=ground.transform.Find("Damage Area");maxRadius=Mathf.Max(maxRadius,area.localScale.x);
            }
            if(elapsed>.9f&&elapsed<1f)Capture("ground.png");
            elapsed+=Time.deltaTime;yield return new WaitForEndOfFrame();
        }
        report["ground"]=new{completed=!Attacking,strikes=groundSpawns.Count,maxRadius,activeAfter=FindObjectsByType<GroundCrack>(FindObjectsSortMode.None).Length};
        boss.waterBombCount=3;boss.WaterBombRainAttack();elapsed=0;
        var bombs=new HashSet<string>();var impacts=new HashSet<string>();int maxAirborne=0;float bombGap=0;
        while((Attacking || elapsed<5) && elapsed<8){
            int active=0;
            foreach(var info in FindObjectsByType<BulletInfo>(FindObjectsSortMode.None))if(info.id==602){
                active++;var p=info.GetComponent<PooledObject>();
                if(bombs.Add(info.GetInstanceID()+":"+p.SpawnVersion))bombGap=Mathf.Max(bombGap,Vector3.Distance(info.transform.position,mouth.position));
            }
            maxAirborne=Mathf.Max(maxAirborne,active);
            foreach(var info in FindObjectsByType<BossPatternInfo>(FindObjectsSortMode.None))if(info.id==1004)
                impacts.Add(info.GetInstanceID()+":"+info.GetComponent<PooledObject>().SpawnVersion);
            if(elapsed>.9f&&elapsed<1f)Capture("bomb.png");
            elapsed+=Time.deltaTime;yield return new WaitForEndOfFrame();
        }
        report["bomb"]=new{completed=!Attacking,count=bombs.Count,impacts=impacts.Count,maxAirborne,maxMouthGap=bombGap};
        boss.SpinWaterStreamAttack();yield return new WaitForSeconds(.7f);instance.SetActive(false);yield return null;
        report["disabledBossStopsBeam"]=FindObjectsByType<HydroPump>(FindObjectsSortMode.None).Length==0;
        pool.ResetActiveObjects();yield return null;
        report["errors"]=errors;
        report["completed"]=true;
        File.WriteAllText("Tools/PixelFx/OctopusRuntimeVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        Debug.Log("OCTOPUS_VFX_CHECK_COMPLETE");
    }
    private void Capture(string name)
    {
        if(File.Exists(Path.Combine(directory,name)))return;
        var rt=RenderTexture.GetTemporary(1280,720,24);var old=RenderTexture.active;view.targetTexture=rt;
        view.Render();RenderTexture.active=rt;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(directory,name),texture.EncodeToPNG());
        Destroy(texture);view.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);
    }
}
