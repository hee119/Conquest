if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Edit mode required.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Stage1.unity");
bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Stage1.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
var rows=new System.Collections.Generic.List<string>();
foreach(var view in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RunRewardView>(true))){
 var heading=view.transform.Find("Heading");if(heading!=null)UnityEditor.Undo.DestroyObjectImmediate(heading.gameObject);
 var so=new UnityEditor.SerializedObject(view);var cards=so.FindProperty("rewardCards");var groups=so.FindProperty("rewardCardGroups");if(cards==null)throw new System.Exception("New reward script not loaded");cards.arraySize=groups.arraySize=2;
 int i=0;foreach(var name in new[]{"GoldCard","FragmentCard"}){
  var card=view.transform.Find(name).GetComponent<UnityEngine.RectTransform>();var group=card.GetComponent<UnityEngine.CanvasGroup>();if(group==null)group=UnityEditor.Undo.AddComponent<UnityEngine.CanvasGroup>(card.gameObject);
  group.alpha=1f;group.interactable=false;group.blocksRaycasts=false;cards.GetArrayElementAtIndex(i).objectReferenceValue=card;groups.GetArrayElementAtIndex(i).objectReferenceValue=group;i++;
 }
 so.ApplyModifiedProperties();rows.Add(view.transform.parent.parent.name+": heading removed; gold then fragments, unscaled reveal");
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Save failed");
if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
System.IO.File.WriteAllLines("Tools/UI/RewardSequenceChanges.txt",rows);return string.Join("\n",rows);
