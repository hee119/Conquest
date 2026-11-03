if (UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling)
    throw new System.Exception("Editor must be stopped and compilation complete.");
var report = "Tools/ResourceCleanup/2026-11-01";
var lines = System.IO.File.ReadAllLines(report + "/candidates.tsv");
var paths = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Skip(lines, 1), s => s.Split('\t')[0]));
var protectedPaths = new System.Collections.Generic.HashSet<string>(System.IO.File.ReadAllLines(report + "/protected-dependencies.txt"));
var roots = System.IO.File.ReadAllLines(report + "/roots.txt");
var currentDependencies = new System.Collections.Generic.HashSet<string>(UnityEditor.AssetDatabase.GetDependencies(roots, true));
foreach (var path in paths)
{
    var lower = path.ToLowerInvariant();
    if (protectedPaths.Contains(path) || currentDependencies.Contains(path) || lower.Contains("tree") || lower.Contains("wood") || lower.Contains("나무") || path.Contains("/Resources/"))
        throw new System.Exception("Protected asset found in deletion list: " + path);
}
var failed = new System.Collections.Generic.List<string>();
bool success = UnityEditor.AssetDatabase.DeleteAssets(paths, failed);
System.IO.File.WriteAllLines(report + "/delete-failed.txt", failed);
System.IO.File.WriteAllLines(report + "/deleted.txt", System.Linq.Enumerable.Where(paths, p => !System.IO.File.Exists(p)));
return $"success={success}; deleted={paths.Length - failed.Count}; failed={failed.Count}";
