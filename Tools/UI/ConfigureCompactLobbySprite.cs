var source="Assets/Resources/UI/Option.png";
var path="Assets/Resources/UI/LobbyMenuOption.png";
if(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path)==null)
    UnityEditor.AssetDatabase.CopyAsset(source,path);
var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType=UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode=UnityEditor.SpriteImportMode.Multiple;
var textureSettings=new UnityEditor.TextureImporterSettings();
importer.ReadTextureSettings(textureSettings);
textureSettings.spriteMeshType=UnityEngine.SpriteMeshType.FullRect;
importer.SetTextureSettings(textureSettings);
importer.SaveAndReimport();
var factory=new UnityEditor.U2D.Sprites.SpriteDataProviderFactories();factory.Init();
var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
var edit=provider.GetDataProvider<UnityEditor.U2D.Sprites.ISpriteFrameEditCapability>();
if(edit==null)throw new System.Exception("Sprite rectangle editing is unsupported");
var capability=edit.GetEditCapability();
if(!capability.HasCapability(UnityEditor.U2D.Sprites.EEditCapability.EditSpriteRect))
    throw new System.Exception("Sprite rectangle editing is not permitted");
var rects=provider.GetSpriteRects();
if(rects.Length!=1)throw new System.Exception("Expected one sprite frame");
// The frame's visible border is y=176..582 in top-origin texture pixels.
// Keep a small margin for antialiasing, rather than hundreds of empty pixels.
rects[0].rect=new UnityEngine.Rect(50,186,1920,426);
provider.SetSpriteRects(rects);provider.Apply();importer.SaveAndReimport();
var sprite=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.Sprite>().First();
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Lobby.unity");
var layout=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UnityEngine.UI.VerticalLayoutGroup>(true)).First(l=>l.name=="SeletOptions");
layout.spacing=18f;
foreach(UnityEngine.Transform child in layout.transform){
    var image=child.GetComponent<UnityEngine.UI.Image>();
    image.overrideSprite=null;
    image.sprite=sprite;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(image);
    ((UnityEngine.RectTransform)child).SetSizeWithCurrentAnchors(UnityEngine.RectTransform.Axis.Vertical,130f);
}
UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((UnityEngine.RectTransform)layout.transform);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new{sprite=sprite.name,spriteRect=sprite.rect.ToString(),layout.spacing};
