var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Loading.unity");var roots=scene.GetRootGameObjects();
var canvas=System.Array.Find(roots,r=>r.name=="Loading Canvas").GetComponent<UnityEngine.Canvas>();var camera=System.Array.Find(roots,r=>r.name=="Loading Camera").GetComponent<UnityEngine.Camera>();var loader=System.Array.Find(roots,r=>r.name=="Startup Loading").GetComponent<StartupLoading>();
var bar=canvas.transform.Find("Loading Experience Bar").GetComponent<ExpBar>();var slider=bar.GetComponent<UnityEngine.UI.Slider>();var label=bar.transform.Find("Loading Progress").GetComponent<TMPro.TMP_Text>();var tip=canvas.transform.Find("Loading Tip").GetComponent<TMPro.TMP_Text>();
float value=slider.value;string oldLabel=label.text,oldTip=tip.text;var mode=canvas.renderMode;var oldCam=canvas.worldCamera;float plane=canvas.planeDistance;var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;var rt=UnityEngine.RenderTexture.GetTemporary(1280,720,24);var texture=new UnityEngine.Texture2D(1280,720,UnityEngine.TextureFormat.RGBA32,false);
try{
 var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
 var cycle=(System.Collections.IEnumerator)typeof(StartupLoading).GetMethod("RotateTips",flags).Invoke(loader,null);cycle.MoveNext();string first=tip.text;cycle.MoveNext();if(first==tip.text)throw new System.Exception("Tip did not change");
 typeof(StartupLoading).GetMethod("ShowProgress",flags).Invoke(loader,new object[]{.55f});if(label.text!="55/100")throw new System.Exception("Progress format mismatch");
 tip.text="증강의 레벨이 5 이고 합성 가능한 증강이 있다면 합성 버튼이 활성화 됩니다.";
 canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.targetTexture=rt;UnityEngine.Canvas.ForceUpdateCanvases();
 foreach(var text in canvas.GetComponentsInChildren<TMPro.TMP_Text>(true))text.ForceMeshUpdate();
 if(tip.isTextOverflowing)throw new System.Exception("Tip clipped");camera.Render();UnityEngine.RenderTexture.active=rt;texture.ReadPixels(new UnityEngine.Rect(0,0,1280,720),0,0);texture.Apply();System.IO.File.WriteAllBytes("Tools/UI/LoadingTipsPreview.png",texture.EncodeToPNG());
 return "PASS: tips alternate; 55/100 format; longest tip fits; black outline material assigned.";
}finally{
 slider.value=value;label.text=oldLabel;tip.text=oldTip;canvas.renderMode=mode;canvas.worldCamera=oldCam;canvas.planeDistance=plane;camera.targetTexture=oldTarget;UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
