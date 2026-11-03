var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyController>(UnityEngine.FindObjectsInactive.Include);var data=new UnityEditor.SerializedObject(lobby);var panel=(UnityEngine.GameObject)data.FindProperty("characterPanel").objectReferenceValue;var menu=(UnityEngine.GameObject)data.FindProperty("mainMenu").objectReferenceValue;var view=panel.GetComponent<CharacterDetailsPreview>();panel.SetActive(true);menu.SetActive(false);lobby.PreviewSwordman();
var canvas=lobby.GetComponentInParent<UnityEngine.Canvas>();var camera=UnityEngine.Camera.main;var mode=canvas.renderMode;var oldCam=canvas.worldCamera;float plane=canvas.planeDistance;var oldTarget=camera.targetTexture;var oldActive=UnityEngine.RenderTexture.active;var rt=UnityEngine.RenderTexture.GetTemporary(1600,900,24);var image=new UnityEngine.Texture2D(1600,900,UnityEngine.TextureFormat.RGBA32,false);
try{
 canvas.renderMode=UnityEngine.RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.targetTexture=rt;
 void Capture(string path){UnityEngine.Canvas.ForceUpdateCanvases();foreach(var text in canvas.GetComponentsInChildren<TMPro.TMP_Text>(true))text.ForceMeshUpdate();camera.Render();UnityEngine.RenderTexture.active=rt;image.ReadPixels(new UnityEngine.Rect(0,0,1600,900),0,0);image.Apply();System.IO.File.WriteAllBytes(path,image.EncodeToPNG());}
 view.HideStats();Capture("Tools/UI/CharacterDetailsPreview.png");view.ShowStats();Capture("Tools/UI/CharacterStatsPreview.png");view.HideStats();return "Captured character selection and stat descriptions";
}finally{
 canvas.renderMode=mode;canvas.worldCamera=oldCam;canvas.planeDistance=plane;camera.targetTexture=oldTarget;UnityEngine.RenderTexture.active=oldActive;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
 UnityEngine.PlayerPrefs.SetInt("SelectedCharacter",UnityEditor.SessionState.GetInt("CharacterPreviewPreviousSelected",1));UnityEngine.PlayerPrefs.Save();
}
