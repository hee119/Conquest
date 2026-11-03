var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var results = new System.Collections.Generic.List<string>();
foreach(var path in new[]{"Assets/Scenes/CutSceen.unity","Assets/Scenes/Lobby.unity","Assets/Scenes/Stage1.unity"})
{
    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
    bool opened=!scene.isLoaded;
    if(opened) scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
    var gate=scene.GetRootGameObjects().Select(r=>r.GetComponent<PreparedScene>()).FirstOrDefault(c=>c!=null);
    if(gate==null)
    {
        var roots=scene.GetRootGameObjects();
        var go=new UnityEngine.GameObject("Prepared Scene");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
        gate=go.AddComponent<PreparedScene>();
        var data=new UnityEditor.SerializedObject(gate);
        var refs=data.FindProperty("roots"); var active=data.FindProperty("initiallyActive");
        refs.arraySize=active.arraySize=roots.Length;
        for(int i=0;i<roots.Length;i++)
        {
            refs.GetArrayElementAtIndex(i).objectReferenceValue=roots[i];
            active.GetArrayElementAtIndex(i).boolValue=roots[i].activeSelf;
            roots[i].SetActive(false);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    results.Add(scene.name+": "+scene.rootCount+" roots");
    if(opened) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
}
UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
UnityEditor.AssetDatabase.SaveAssets();
return results;
