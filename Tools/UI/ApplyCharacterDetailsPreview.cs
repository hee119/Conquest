if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");
if(!scene.IsValid())scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
LobbyController lobby=null;foreach(var root in scene.GetRootGameObjects()){lobby=root.GetComponentInChildren<LobbyController>(true);if(lobby!=null)break;}
if(lobby==null)throw new System.Exception("Missing LobbyController");
var lobbyData=new UnityEditor.SerializedObject(lobby);var panel=(UnityEngine.GameObject)lobbyData.FindProperty("characterPanel").objectReferenceValue;
var details=panel.transform.Find("Recessed Panel/RightDetails");if(details==null)throw new System.Exception("Missing details");
if(panel.GetComponent<CharacterDetailsPreview>()!=null)throw new System.Exception("Already installed");
var view=panel.AddComponent<CharacterDetailsPreview>();
var sourceText=details.Find("CharacterDescription").GetComponent<TMPro.TextMeshProUGUI>();var sourceButton=details.Find("EnhanceButton").GetComponent<UnityEngine.UI.Button>();
var translations=new System.Collections.Generic.Dictionary<string,string>();
foreach(var line in System.IO.File.ReadAllLines("Assets/Resources/TextData/Localization.csv")){var row=line.Split(',');if(row.Length>1)translations[row[0]]=row[1];}
System.Func<string,UnityEngine.Transform,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.RectTransform> rect=(name,parent,position,size)=>{var go=new UnityEngine.GameObject(name,typeof(UnityEngine.RectTransform));go.transform.SetParent(parent,false);var r=(UnityEngine.RectTransform)go.transform;r.anchorMin=r.anchorMax=new UnityEngine.Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;};
System.Func<string,string,UnityEngine.Transform,UnityEngine.Vector2,UnityEngine.Vector2,float,TMPro.TextMeshProUGUI> text=(name,key,parent,position,size,fontSize)=>{var r=rect(name,parent,position,size);var t=r.gameObject.AddComponent<TMPro.TextMeshProUGUI>();t.font=sourceText.font;t.fontSharedMaterial=sourceText.fontSharedMaterial;t.fontSize=fontSize;t.enableAutoSizing=false;t.color=UnityEngine.Color.white;t.alignment=TMPro.TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;t.text=translations.ContainsKey(key)?translations[key]:key;if(!string.IsNullOrEmpty(key)){var l=t.gameObject.AddComponent<LocalizedText>();l.key=key;}return t;};
System.Func<string,string,UnityEngine.Transform,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.UI.Button> button=(name,key,parent,position,size)=>{var r=rect(name,parent,position,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sourceButton.GetComponent<UnityEngine.UI.Image>().sprite;image.type=sourceButton.GetComponent<UnityEngine.UI.Image>().type;image.color=sourceButton.GetComponent<UnityEngine.UI.Image>().color;var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;b.colors=sourceButton.colors;var label=text("Label",key,r,UnityEngine.Vector2.zero,size-new UnityEngine.Vector2(12,6),25);label.alignment=TMPro.TextAlignmentOptions.Center;return b;};
var statsButton=button("View Stats","character_view_stats",details,new UnityEngine.Vector2(230,175),new UnityEngine.Vector2(205,80));
var playButton=button("Preview Basic Attack","character_preview_attack",details,new UnityEngine.Vector2(230,80),new UnityEngine.Vector2(205,80));
var overlay=rect("Character Stats",panel.transform,UnityEngine.Vector2.zero,UnityEngine.Vector2.zero);overlay.anchorMin=UnityEngine.Vector2.zero;overlay.anchorMax=UnityEngine.Vector2.one;overlay.offsetMin=overlay.offsetMax=UnityEngine.Vector2.zero;var dim=overlay.gameObject.AddComponent<UnityEngine.UI.Image>();dim.color=new UnityEngine.Color(0,0,0,.86f);
var card=rect("Stats Card",overlay,new UnityEngine.Vector2(0,-35),new UnityEngine.Vector2(1400,720));var backdrop=card.gameObject.AddComponent<UnityEngine.UI.Image>();backdrop.color=new UnityEngine.Color(.025f,.035f,.05f,.99f);var outline=card.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=UnityEngine.Color.white;outline.effectDistance=new UnityEngine.Vector2(1.5f,-1.5f);
var title=text("Stats Title","",card,new UnityEngine.Vector2(-30,300),new UnityEngine.Vector2(1180,65),38);title.text="검사  +0";
var close=button("Close Stats","",card,new UnityEngine.Vector2(630,300),new UnityEngine.Vector2(65,65));close.GetComponentInChildren<TMPro.TMP_Text>().text="×";
var statNames=new[]{"hp","regen","speed","attack","defense","crit_chance","crit_damage","pierce","range","attack_speed"};
var values=new TMPro.TMP_Text[statNames.Length];
for(int i=0;i<statNames.Length;i++){
 float y=220-i*47;string key="character_stat_"+statNames[i];
 text("Name "+statNames[i],key,card,new UnityEngine.Vector2(-495,y),new UnityEngine.Vector2(260,44),25);
 var value=text("Value "+statNames[i],"",card,new UnityEngine.Vector2(-295,y),new UnityEngine.Vector2(140,44),26);value.alignment=TMPro.TextAlignmentOptions.MidlineRight;values[i]=value;
 text("Description "+statNames[i],key+"_desc",card,new UnityEngine.Vector2(215,y),new UnityEngine.Vector2(790,44),22);
 var line=rect("Divider "+i,card,new UnityEngine.Vector2(0,y-23),new UnityEngine.Vector2(1260,1));line.gameObject.AddComponent<UnityEngine.UI.Image>().color=new UnityEngine.Color(1,1,1,.12f);line.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
}
var note=text("Stats Note","character_stats_note",card,new UnityEngine.Vector2(0,-290),new UnityEngine.Vector2(1260,60),23);note.alignment=TMPro.TextAlignmentOptions.Center;
var data=new UnityEditor.SerializedObject(view);data.FindProperty("statsPanel").objectReferenceValue=overlay.gameObject;data.FindProperty("statsTitle").objectReferenceValue=title;data.FindProperty("statsButton").objectReferenceValue=statsButton;data.FindProperty("playButton").objectReferenceValue=playButton;data.FindProperty("closeButton").objectReferenceValue=close;
var statValues=data.FindProperty("statValues");statValues.arraySize=values.Length;for(int i=0;i<values.Length;i++)statValues.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
var previews=data.FindProperty("previews");previews.arraySize=2;
for(int i=0;i<2;i++){
 var p=previews.GetArrayElementAtIndex(i);var target=(UnityEngine.GameObject)lobbyData.FindProperty(i==0?"swordmanIdlePreview":"mageIdlePreview").objectReferenceValue;
 var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(i==0?"Assets/Animation/Attack.anim":"Assets/Animation/MageAttack.anim");
 var bindings=UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip);var binding=System.Array.Find(bindings,b=>b.type==typeof(UnityEngine.SpriteRenderer)&&b.propertyName=="m_Sprite");var keys=UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip,binding);
 if(keys==null||keys.Length==0)throw new System.Exception("Missing attack frames");
 p.FindPropertyRelative("characterId").intValue=i+1;p.FindPropertyRelative("image").objectReferenceValue=target.GetComponent<UnityEngine.UI.Image>();p.FindPropertyRelative("idleAnimator").objectReferenceValue=target.GetComponent<UnityEngine.Animator>();p.FindPropertyRelative("duration").floatValue=UnityEngine.Mathf.Max(clip.length+1f/clip.frameRate,UnityEditor.AnimationUtility.GetAnimationClipSettings(clip).stopTime);
 var frames=p.FindPropertyRelative("frames");var times=p.FindPropertyRelative("times");frames.arraySize=times.arraySize=keys.Length;
 for(int n=0;n<keys.Length;n++){if(keys[n].value==null)throw new System.Exception("Null sprite");frames.GetArrayElementAtIndex(n).objectReferenceValue=keys[n].value;times.GetArrayElementAtIndex(n).floatValue=keys[n].time;}
}
data.ApplyModifiedPropertiesWithoutUndo();lobbyData.FindProperty("detailsPreview").objectReferenceValue=view;lobbyData.ApplyModifiedPropertiesWithoutUndo();
view.SetCharacter(1);overlay.gameObject.SetActive(false);
UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/TextData/Localization.csv");UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Saved localized stats modal with 10 current enhancement stats/descriptions and real sword/mage attack frame previews. Existing progression controls preserved.";
