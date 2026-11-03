if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Editor must be idle.");
if(typeof(RunRewardView).GetField("goldLabel",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)==null)throw new System.Exception("Refresh scripts first.");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Stage1.unity");
if(!scene.IsValid()||!scene.isLoaded)throw new System.Exception("Stage1 must be loaded.");
var report=new System.Collections.Generic.List<string>();
System.Action<UnityEngine.RectTransform,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.Vector2> place=(r,anchor,pos,size)=>{UnityEditor.Undo.RecordObject(r,"Arrange reward item");r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=pos;r.sizeDelta=size;};
System.Func<UnityEngine.Transform,string,TMPro.TMP_Text,TMPro.TextMeshProUGUI> label=(parent,name,style)=>{
 var found=parent.Find(name);var go=found!=null?found.gameObject:new UnityEngine.GameObject(name,typeof(UnityEngine.RectTransform),typeof(UnityEngine.CanvasRenderer),typeof(TMPro.TextMeshProUGUI));
 if(found==null){UnityEditor.Undo.RegisterCreatedObjectUndo(go,"Create reward label");go.transform.SetParent(parent,false);}
 var text=go.GetComponent<TMPro.TextMeshProUGUI>();text.font=style.font;text.fontSharedMaterial=style.fontSharedMaterial;text.color=UnityEngine.Color.white;text.raycastTarget=false;text.textWrappingMode=TMPro.TextWrappingModes.NoWrap;
 return text;
};
foreach(var view in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RunRewardView>(true))){
 var root=view.transform;var heading=root.Find("Heading").GetComponent<TMPro.TMP_Text>();var gold=root.Find("GoldCard");var fragment=root.Find("FragmentCard");
 foreach(var card in new[]{gold,fragment}){
 var frame=card.GetComponent<UnityEngine.UI.Image>();UnityEditor.Undo.RecordObject(frame,"Use square reward frame");frame.preserveAspect=false;frame.sprite=null;frame.type=UnityEngine.UI.Image.Type.Simple;frame.color=new UnityEngine.Color(.03f,.03f,.03f,1f);frame.raycastTarget=false;
 var border=card.GetComponent<UnityEngine.UI.Outline>();if(border==null)border=UnityEditor.Undo.AddComponent<UnityEngine.UI.Outline>(card.gameObject);border.effectColor=new UnityEngine.Color(1f,1f,1f,.7f);border.effectDistance=new UnityEngine.Vector2(1.5f,-1.5f);border.useGraphicAlpha=true;
 }
 var countGold=gold.Find("Gold").GetComponent<TMPro.TMP_Text>();var countFragment=fragment.Find("Fragments").GetComponent<TMPro.TMP_Text>();var portrait=fragment.Find("Portrait").GetComponent<UnityEngine.UI.Image>();
 place((UnityEngine.RectTransform)gold,new UnityEngine.Vector2(.5f,.5f),new UnityEngine.Vector2(-104f,0f),new UnityEngine.Vector2(180f,180f));
 place((UnityEngine.RectTransform)fragment,new UnityEngine.Vector2(.5f,.5f),new UnityEngine.Vector2(104f,0f),new UnityEngine.Vector2(180f,180f));
 foreach(var text in new[]{countGold,countFragment}){
  UnityEditor.Undo.RecordObject(text,"Place reward quantity");place(text.rectTransform,new UnityEngine.Vector2(1f,0f),new UnityEngine.Vector2(-12f,10f),new UnityEngine.Vector2(156f,32f));
  text.alignment=TMPro.TextAlignmentOptions.BottomRight;text.fontSize=30f;text.enableAutoSizing=true;text.fontSizeMin=16f;text.fontSizeMax=30f;text.color=UnityEngine.Color.white;text.raycastTarget=false;text.textWrappingMode=TMPro.TextWrappingModes.NoWrap;
 }
 countGold.text="×300";countFragment.text="×6";
 place(portrait.rectTransform,new UnityEngine.Vector2(.5f,.5f),new UnityEngine.Vector2(0f,4f),new UnityEngine.Vector2(84f,88f));portrait.preserveAspect=true;portrait.raycastTarget=false;
 var goldLabel=label(gold,"ItemName",heading);var fragmentLabel=label(fragment,"ItemName",heading);
 foreach(var text in new[]{goldLabel,fragmentLabel}){place(text.rectTransform,new UnityEngine.Vector2(.5f,.5f),new UnityEngine.Vector2(0f,66f),new UnityEngine.Vector2(156f,24f));text.alignment=TMPro.TextAlignmentOptions.Center;text.fontSize=20f;text.enableAutoSizing=true;text.fontSizeMin=12f;text.fontSizeMax=20f;}
 goldLabel.text="골드";fragmentLabel.text="캐릭터 파편";
 var circle=UnityEditor.AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>("UI/Skin/Knob.psd");if(circle==null)throw new System.Exception("Missing circle sprite");
 var iconTransform=gold.Find("GoldIcon");var icon=iconTransform!=null?iconTransform.gameObject:new UnityEngine.GameObject("GoldIcon",typeof(UnityEngine.RectTransform),typeof(UnityEngine.CanvasRenderer),typeof(UnityEngine.UI.Image));
 if(iconTransform==null){UnityEditor.Undo.RegisterCreatedObjectUndo(icon,"Create gold symbol");icon.transform.SetParent(gold,false);}
 place((UnityEngine.RectTransform)icon.transform,new UnityEngine.Vector2(.5f,.5f),new UnityEngine.Vector2(0f,4f),new UnityEngine.Vector2(88f,88f));var outer=icon.GetComponent<UnityEngine.UI.Image>();outer.sprite=circle;outer.color=UnityEngine.Color.white;outer.raycastTarget=false;
 var insetTransform=icon.transform.Find("Inset");var inset=insetTransform!=null?insetTransform.gameObject:new UnityEngine.GameObject("Inset",typeof(UnityEngine.RectTransform),typeof(UnityEngine.CanvasRenderer),typeof(UnityEngine.UI.Image));
 if(insetTransform==null){UnityEditor.Undo.RegisterCreatedObjectUndo(inset,"Create coin inset");inset.transform.SetParent(icon.transform,false);}
 place((UnityEngine.RectTransform)inset.transform,new UnityEngine.Vector2(.5f,.5f),UnityEngine.Vector2.zero,new UnityEngine.Vector2(78f,78f));var inner=inset.GetComponent<UnityEngine.UI.Image>();inner.sprite=circle;inner.color=new UnityEngine.Color(.03f,.03f,.03f,1f);inner.raycastTarget=false;
 var glyph=label(icon.transform,"Glyph",heading);place(glyph.rectTransform,new UnityEngine.Vector2(.5f,.5f),UnityEngine.Vector2.zero,new UnityEngine.Vector2(70f,70f));glyph.text="G";glyph.fontSize=48f;glyph.enableAutoSizing=false;glyph.alignment=TMPro.TextAlignmentOptions.Center;
 var so=new UnityEditor.SerializedObject(view);so.FindProperty("heading").objectReferenceValue=heading;so.FindProperty("goldText").objectReferenceValue=countGold;so.FindProperty("fragmentText").objectReferenceValue=countFragment;so.FindProperty("portrait").objectReferenceValue=portrait;so.FindProperty("goldLabel").objectReferenceValue=goldLabel;so.FindProperty("fragmentLabel").objectReferenceValue=fragmentLabel;so.ApplyModifiedProperties();
 report.Add(root.parent.parent.name+": 180x180 gold/shard cards; lower-right white quantities; references wired.");
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Save failed");
System.IO.File.WriteAllLines("Tools/UI/RewardBoxesChanges.txt",report);return string.Join("\n",report);
