var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/Animation/OctopusKingWaterShot.anim");
var events=UnityEditor.AnimationUtility.GetAnimationEvents(clip).Where(e=>e.functionName!="FireWaterCannon").ToList();
events.Add(new UnityEngine.AnimationEvent{time=9f/40f,functionName="FireWaterCannon"});
UnityEditor.AnimationUtility.SetAnimationEvents(clip,events.ToArray());
UnityEditor.EditorUtility.SetDirty(clip);
var path="Assets/Resources/Prefabs/Boss/OctopusKing.prefab";
var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);
try
{
    var boss=root.GetComponent<OctopusKing>();
    boss.bulletSpawnCount=5;
    boss.fanBulletCount=10;boss.fanBurstCount=5;boss.fanSpreadDegrees=120;
    boss.radialBulletCount=10;boss.radialBurstCount=5;
    boss.waterShotAnimation=clip;
    root.GetComponent<UnityEngine.Animator>().cullingMode=UnityEngine.AnimatorCullingMode.AlwaysAnimate;
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);
}
finally {UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
UnityEditor.AssetDatabase.SaveAssets();
return new{clip=clip.name,clip.length,events=events.Select(e=>new{e.time,e.functionName}).ToArray()};
