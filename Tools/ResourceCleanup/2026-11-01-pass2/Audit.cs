var report = "Tools/ResourceCleanup/2026-11-01-pass2";
var all = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(UnityEditor.AssetDatabase.GetAllAssetPaths(), p => p.StartsWith("Assets/") && !UnityEditor.AssetDatabase.IsValidFolder(p)));
var changed = System.IO.File.ReadAllLines(report + "/protected-worktree.txt");
var codeRoots = System.IO.File.ReadAllLines(report + "/code-path-roots.txt");
var extensions = new System.Collections.Generic.HashSet<string>(new[] { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tif", ".tiff", ".exr", ".hdr", ".mat", ".prefab", ".anim", ".controller", ".asset", ".fbx", ".obj", ".wav", ".mp3", ".ogg", ".ttf", ".otf", ".spriteatlas", ".spriteatlasv2", ".unity" });
var candidates = new System.Collections.Generic.HashSet<string>();
var treeRoots = new System.Collections.Generic.List<string>();
foreach (var p in all)
{
    var lower = p.ToLowerInvariant();
    bool tree = lower.Contains("tree") || lower.Contains("wood") || lower.Contains("나무") || p.StartsWith("Assets/Resources/Tiles/");
    if (tree) treeRoots.Add(p);
    bool area = !p.StartsWith("Assets/Scenes/");
    bool tools = p.StartsWith("Assets/GameAssets/SPUM/") || p.StartsWith("Assets/GameAssets/BansheeGz/") || p.StartsWith("Assets/GameAssets/Sprite Shaders Ultimate/") || p.StartsWith("Assets/Plugins/") || p.StartsWith("Assets/Editor/") || p.StartsWith("Assets/Settings/") || p.StartsWith("Assets/ProjectSettings/") || p.StartsWith("Assets/TextMesh Pro/") || p.StartsWith("Assets/URPDefaultResources/") || p.StartsWith("Assets/StreamingAssets/") || p.StartsWith("Assets/_Recovery/") || p.IndexOf('/', 7) < 0;
    bool worktree = System.Linq.Enumerable.Any(changed, c => p == c || p.StartsWith(c.TrimEnd('/') + "/"));
    if (area && !tools && !tree && !worktree && !p.Contains("/Resources/") && extensions.Contains(System.IO.Path.GetExtension(p).ToLowerInvariant())) candidates.Add(p);
}
var roots = new System.Collections.Generic.HashSet<string>(System.Linq.Enumerable.Where(all, p => !candidates.Contains(p)));
foreach (var path in codeRoots)
{
    if (UnityEditor.AssetDatabase.IsValidFolder(path))
        foreach (var child in all) { if (child.StartsWith(path + "/")) roots.Add(child); }
    else roots.Add(path);
}
var loadedRoots = new System.Collections.Generic.List<UnityEngine.Object>();
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    foreach (var root in UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).GetRootGameObjects()) loadedRoots.Add(root);
foreach (var obj in UnityEditor.EditorUtility.CollectDependencies(loadedRoots.ToArray()))
{
    var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
    if (!string.IsNullOrEmpty(path)) roots.Add(path);
}
var protectedPaths = new System.Collections.Generic.HashSet<string>(UnityEditor.AssetDatabase.GetDependencies(System.Linq.Enumerable.ToArray(roots), true));
var removable = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Where(candidates, p => !protectedPaths.Contains(p)), p => p));
System.IO.File.WriteAllLines(report + "/roots.txt", System.Linq.Enumerable.OrderBy(roots, p => p));
System.IO.File.WriteAllLines(report + "/protected-dependencies.txt", System.Linq.Enumerable.OrderBy(protectedPaths, p => p));
System.IO.File.WriteAllLines(report + "/tree-protected.txt", System.Linq.Enumerable.OrderBy(treeRoots, p => p));
var lines = new System.Collections.Generic.List<string> { "path\tguid\tbytes\ttype" };
long bytes = 0;
foreach (var p in removable)
{
    long size = new System.IO.FileInfo(p).Length;
    bytes += size;
    lines.Add(p + "\t" + UnityEditor.AssetDatabase.AssetPathToGUID(p) + "\t" + size + "\t" + UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(p)?.FullName);
}
System.IO.File.WriteAllLines(report + "/candidates.tsv", lines);
return $"assets={all.Length}; provisional={candidates.Count}; removable={removable.Length}; bytes={bytes}; protected tree paths={treeRoots.Count}";
