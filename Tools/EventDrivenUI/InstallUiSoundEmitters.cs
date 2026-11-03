if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
int prefabs=0,emitters=0;
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets"})){
 string path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
 var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
 if(asset==null)continue;
 bool needs=false;
 foreach(var selectable in asset.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
  if((selectable is UnityEngine.UI.Button||selectable is UnityEngine.UI.Toggle)&&selectable.GetComponent<UiSoundEmitter>()==null){needs=true;break;}
 if(!needs)continue;
 var root=UnityEditor.PrefabUtility.LoadPrefabContents(path);int count=0;
 try{
  foreach(var selectable in root.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
   if((selectable is UnityEngine.UI.Button||selectable is UnityEngine.UI.Toggle)&&selectable.GetComponent<UiSoundEmitter>()==null){selectable.gameObject.AddComponent<UiSoundEmitter>();count++;}
  if(count>0){UnityEditor.PrefabUtility.SaveAsPrefabAsset(root,path);prefabs++;emitters+=count;}
 }finally{UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
}
UnityEditor.AssetDatabase.SaveAssets();
return "UI sound emitters installed: "+emitters+" controls in "+prefabs+" prefabs. Scenes register once on sceneLoaded; runtime factories register new controls explicitly.";
