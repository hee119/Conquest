if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var report=new System.Collections.Generic.List<string>();
foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/Prefabs/Enemy","Assets/Resources/Prefabs/Boss"})){
 var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
 if(asset.GetComponent<EnemyKitingMove>()==null&&asset.GetComponent<OctopusKing>()==null&&asset.GetComponent<SlimeKing>()==null)continue;
 var prefab=UnityEditor.PrefabUtility.LoadPrefabContents(path);
 try{var facing=prefab.GetComponent<EnemyFacing>();if(facing==null)facing=prefab.AddComponent<EnemyFacing>();var so=new UnityEditor.SerializedObject(facing);so.FindProperty("body").objectReferenceValue=prefab.GetComponentInChildren<UnityEngine.SpriteRenderer>(true);so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab,path);report.Add(path);}
 finally{UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);}
}
UnityEditor.AssetDatabase.SaveAssets();System.IO.File.WriteAllLines("Tools/Enemy/FacingPrefabs.txt",report);return string.Join("\n",report);
