var report = "Tools/ResourceCleanup/2026-11-01-pass2";
var all = UnityEditor.AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&!UnityEditor.AssetDatabase.IsValidFolder(p)).ToArray();
var prefix = "Assets/GameAssets/SPUM/";
var outside = all.Where(p=>!p.StartsWith(prefix)).ToArray();
var keep = new System.Collections.Generic.HashSet<string>(UnityEditor.AssetDatabase.GetDependencies(outside,true));
var loaded = new System.Collections.Generic.List<UnityEngine.Object>();
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) loaded.AddRange(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).GetRootGameObjects());
foreach(var obj in UnityEditor.EditorUtility.CollectDependencies(loaded.ToArray()))
{
    var path=UnityEditor.AssetDatabase.GetAssetPath(obj);
    if(!string.IsNullOrEmpty(path)) keep.Add(path);
}
var spum = all.Where(p=>p.StartsWith(prefix)).ToArray();
var removable = spum.Where(p=>!keep.Contains(p)).ToArray();
System.IO.File.WriteAllLines(report+"/spum-preserved.txt",spum.Where(p=>!removable.Contains(p)));
var lines=System.IO.File.ReadAllLines(report+"/candidates.tsv").ToList();
foreach(var p in removable) lines.Add(p+"\t"+UnityEditor.AssetDatabase.AssetPathToGUID(p)+"\t"+new System.IO.FileInfo(p).Length+"\t"+UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(p)?.FullName);
System.IO.File.WriteAllLines(report+"/candidates.tsv",lines);
var roots=System.IO.File.ReadAllLines(report+"/roots.txt").Where(p=>!removable.Contains(p)).ToArray();
System.IO.File.WriteAllLines(report+"/roots.txt",roots);
System.IO.File.WriteAllLines(report+"/protected-dependencies.txt",UnityEditor.AssetDatabase.GetDependencies(roots,true));
return new {spumFiles=spum.Length,remove=removable.Length,preserve=spum.Where(p=>!removable.Contains(p)).ToArray()};
