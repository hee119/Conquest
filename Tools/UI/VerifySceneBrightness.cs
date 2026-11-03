if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit-only verification; do not stop the user's game.");
bool hadKey=UnityEngine.PlayerPrefs.HasKey("SceneBrightness");float oldValue=UnityEngine.PlayerPrefs.GetFloat("SceneBrightness",.5f);
var scenes=new System.Collections.Generic.List<UnityEngine.SceneManagement.Scene>();var profiles=new System.Collections.Generic.List<UnityEngine.Rendering.VolumeProfile>();var results=new System.Collections.Generic.List<string>();
var callback=typeof(SceneBrightnessSettings).GetMethod("OnSceneLoaded",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
try{
 for(int i=0;i<2;i++){
  var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();scenes.Add(scene);
  var root=new UnityEngine.GameObject("Brightness volume");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);var volume=root.AddComponent<UnityEngine.Rendering.Volume>();volume.isGlobal=true;var profile=UnityEngine.ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();profiles.Add(profile);volume.sharedProfile=profile;
  var settings=new UnityEngine.GameObject("Settings closed");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(settings,scene);settings.SetActive(false);
  UnityEngine.PlayerPrefs.SetFloat("SceneBrightness",.8f);callback.Invoke(null,new object[]{scene,UnityEngine.SceneManagement.LoadSceneMode.Additive});
  if(!volume.profile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var color)||!color.postExposure.overrideState||UnityEngine.Mathf.Abs(color.postExposure.value-.6f)>.0001f||settings.activeInHierarchy)throw new System.Exception("Saved brightness not applied with settings closed");
  results.Add("PASS: scene "+i+" receives saved exposure 0.6 with settings closed");
  UnityEngine.PlayerPrefs.SetFloat("SceneBrightness",.2f);callback.Invoke(null,new object[]{scene,UnityEngine.SceneManagement.LoadSceneMode.Additive});if(UnityEngine.Mathf.Abs(color.postExposure.value+.6f)>.0001f)throw new System.Exception("Updated preference ignored");results.Add("PASS: re-entering scene reads current saved preference");
  profiles.Add(volume.profile);
 }
 UnityEngine.PlayerPrefs.SetFloat("SceneBrightness",-3);if(SceneBrightnessSettings.SavedValue!=0)throw new System.Exception("Lower bound");UnityEngine.PlayerPrefs.SetFloat("SceneBrightness",3);if(SceneBrightnessSettings.SavedValue!=1)throw new System.Exception("Upper bound");results.Add("PASS: saved brightness clamps to slider range");
}finally{
 if(hadKey)UnityEngine.PlayerPrefs.SetFloat("SceneBrightness",oldValue);else UnityEngine.PlayerPrefs.DeleteKey("SceneBrightness");
 foreach(var scene in scenes)UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);foreach(var profile in profiles)if(profile!=null)UnityEngine.Object.DestroyImmediate(profile);
}
System.IO.File.WriteAllLines("Tools/UI/SceneBrightnessVerification.txt",results);return string.Join("\n",results);
