if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");if(!scene.IsValid())scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
LobbyController lobby=null;foreach(var root in scene.GetRootGameObjects()){lobby=root.GetComponentInChildren<LobbyController>(true);if(lobby!=null)break;}
var lobbyData=new UnityEditor.SerializedObject(lobby);var panel=(UnityEngine.GameObject)lobbyData.FindProperty("characterPanel").objectReferenceValue;var details=panel.transform.Find("Recessed Panel/RightDetails");var enhancement=details.Find("CharacterEnhancement");
if(details.Find("Summary Stats")!=null)throw new System.Exception("Already installed");
var template=details.Find("CharacterDescription").GetComponent<TMPro.TextMeshProUGUI>();
System.Func<string,UnityEngine.Transform,UnityEngine.Vector2,UnityEngine.Vector2,UnityEngine.RectTransform> rect=(name,parent,pos,size)=>{var go=new UnityEngine.GameObject(name,typeof(UnityEngine.RectTransform));go.transform.SetParent(parent,false);var r=(UnityEngine.RectTransform)go.transform;r.anchorMin=r.anchorMax=new UnityEngine.Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;};
System.Func<string,UnityEngine.Transform,UnityEngine.Vector2,UnityEngine.Vector2,float,TMPro.TextMeshProUGUI> text=(name,parent,pos,size,fontSize)=>{var r=rect(name,parent,pos,size);var t=r.gameObject.AddComponent<TMPro.TextMeshProUGUI>();t.font=template.font;t.fontSharedMaterial=template.fontSharedMaterial;t.fontSize=fontSize;t.enableAutoSizing=false;t.color=UnityEngine.Color.white;t.alignment=TMPro.TextAlignmentOptions.MidlineLeft;t.raycastTarget=false;return t;};
var container=rect("Summary Stats",details,new UnityEngine.Vector2(0,-131),new UnityEngine.Vector2(640,122));var summary=container.gameObject.AddComponent<CharacterSummaryStats>();
var keys=new[]{"character_stat_hp","character_stat_attack","character_stat_cooldown"};var labels=new[]{"HP","공격력","공격 쿨타임"};var icons=new[]{"Vitality","PowerBoost","QuickCast"};var colors=new[]{new UnityEngine.Color(.16f,.5f,.2f,1),new UnityEngine.Color(.55f,.16f,.16f,1),new UnityEngine.Color(.13f,.32f,.6f,1)};
var values=new TMPro.TMP_Text[3];
for(int i=0;i<3;i++){
 float y=41-i*41;var row=rect("Stat Row "+i,container,new UnityEngine.Vector2(0,y),new UnityEngine.Vector2(640,38));var bg=row.gameObject.AddComponent<UnityEngine.UI.Image>();bg.color=new UnityEngine.Color(.03f,.05f,.08f,.95f);bg.raycastTarget=false;
 var tile=rect("Icon Tile",row,new UnityEngine.Vector2(-292,0),new UnityEngine.Vector2(34,34));tile.gameObject.AddComponent<UnityEngine.UI.Image>().color=colors[i];tile.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
 var iconRect=rect("Icon",tile,UnityEngine.Vector2.zero,new UnityEngine.Vector2(28,28));var image=iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<ImageInfo>("Assets/Resources/StatUpgradeIcon/"+icons[i]+".asset").image;image.preserveAspect=true;image.raycastTarget=false;image.material=null;
 var label=text("Label",row,new UnityEngine.Vector2(-135,0),new UnityEngine.Vector2(260,38),24);label.text=labels[i];label.gameObject.AddComponent<LocalizedText>().key=keys[i];
 var value=text("Value",row,new UnityEngine.Vector2(172,0),new UnityEngine.Vector2(270,38),26);value.alignment=TMPro.TextAlignmentOptions.MidlineRight;values[i]=value;
}
var so=new UnityEditor.SerializedObject(summary);so.FindProperty("hpValue").objectReferenceValue=values[0];so.FindProperty("attackValue").objectReferenceValue=values[1];so.FindProperty("cooldownValue").objectReferenceValue=values[2];so.ApplyModifiedPropertiesWithoutUndo();lobbyData.FindProperty("summaryStats").objectReferenceValue=summary;lobbyData.ApplyModifiedPropertiesWithoutUndo();
// Keep enhancement cost/actions, replacing the old verbose bonus/next text with the rows.
enhancement.Find("Bonus").gameObject.SetActive(false);enhancement.Find("Next").gameObject.SetActive(false);
var level=(UnityEngine.RectTransform)enhancement.Find("Level");level.anchoredPosition=new UnityEngine.Vector2(-230,277);level.sizeDelta=new UnityEngine.Vector2(220,40);level.GetComponent<TMPro.TMP_Text>().fontSize=24;
var cost=(UnityEngine.RectTransform)enhancement.Find("Cost");cost.anchoredPosition=new UnityEngine.Vector2(0,-72);cost.sizeDelta=new UnityEngine.Vector2(650,28);cost.GetComponent<TMPro.TMP_Text>().fontSize=22;
foreach(var name in new[]{"SelectButton","EnhanceButton"}){var r=(UnityEngine.RectTransform)details.Find(name);r.anchoredPosition=new UnityEngine.Vector2(r.anchoredPosition.x,-260);r.sizeDelta=new UnityEngine.Vector2(250,78);}
summary.SetCharacter(1);UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/TextData/Localization.csv");UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "Saved inline HP/attack/basic cooldown rows with green next-enhancement deltas; basic attack preview and enhancement actions retained.";
