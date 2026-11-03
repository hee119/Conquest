var path = "Assets/Resources/UI/Title/PlanetaryPioneerLogo.png";
UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
importer.textureType = UnityEditor.TextureImporterType.Sprite;
importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
importer.mipmapEnabled = false;
importer.filterMode = UnityEngine.FilterMode.Point;
importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
importer.maxTextureSize = 2048;
importer.SaveAndReimport();
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Loading.unity");
if (!scene.isLoaded)
    scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Loading.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
var title = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.RectTransform>(true)).First(r => r.name == "Game Title");
foreach (var component in title.GetComponents<UnityEngine.Component>())
    if (component is TMPro.TMP_Text || component is LocalizedText || component is UnityEngine.UI.ContentSizeFitter || component is CharacterTitleAlignment)
        UnityEngine.Object.DestroyImmediate(component);
var image = title.GetComponent<UnityEngine.UI.Image>();
if (image == null) image = title.gameObject.AddComponent<UnityEngine.UI.Image>();
image.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
image.color = UnityEngine.Color.white;
image.preserveAspect = true;
image.raycastTarget = false;
title.anchorMin = title.anchorMax = new UnityEngine.Vector2(.5f, .65f);
title.pivot = new UnityEngine.Vector2(.5f, .5f);
title.anchoredPosition = UnityEngine.Vector2.zero;
title.sizeDelta = new UnityEngine.Vector2(1100f, 1100f * 1024f / 1536f);
title.localScale = UnityEngine.Vector3.one;
title.gameObject.SetActive(true);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return new { image = image.sprite.name, size = title.sizeDelta.ToString(), scene = scene.path };
