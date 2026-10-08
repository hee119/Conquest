using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Conquest.Editor.ImageImportPresets
{
    public sealed class TextureImportPresetWindow : EditorWindow
    {
        private const string PresetFolder = "Assets/Editor/TextureImportPresets";
        private string imagePath;
        private TextureImportPreset[] presets;
        private DropdownField presetDropdown;
        private Label statusLabel;

        [MenuItem("Tools/Conquest/Image Import Presets")]
        private static void OpenManager()
        {
            TextureImportPresetWindow window = GetWindow<TextureImportPresetWindow>();
            window.titleContent = new GUIContent("Image Import Presets");
            window.imagePath = null;
            window.minSize = new Vector2(430, 370);
            window.Show();
        }

        [MenuItem("Tools/Conquest/Create Starter Image Presets")]
        private static void CreateStarterPresets()
        {
            CreateStarterPresetAssets();
        }

        private static void CreateStarterPresetAssets()
        {
            EnsureFolder();
            CreateStarter("Pixel Sprite Single", TextureImporterType.Sprite, SpriteImportMode.Single,
                FilterMode.Point, TextureImporterCompression.Uncompressed);
            CreateStarter("Sprite Sheet 64 Grid", TextureImporterType.Sprite, SpriteImportMode.Multiple,
                FilterMode.Point, TextureImporterCompression.Uncompressed, SpriteSlicePresetMode.GridByCellSize);
            CreateStarter("UI Sprite Single", TextureImporterType.Sprite, SpriteImportMode.Single,
                FilterMode.Bilinear, TextureImporterCompression.Compressed);
            CreateStarter("Texture Default", TextureImporterType.Default, SpriteImportMode.Single,
                FilterMode.Bilinear, TextureImporterCompression.Compressed);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void ShowForImage(string path)
        {
            var window = CreateInstance<TextureImportPresetWindow>();
            window.imagePath = path;
            window.titleContent = new GUIContent("Choose Image Preset");
            window.minSize = new Vector2(440, 430);
            window.maxSize = new Vector2(760, 700);
            window.ShowModalUtility();
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(imagePath))
                TextureImportPresetQueue.PromptClosed();
        }

        public void CreateGUI()
        {
            RefreshPresets();
            VisualElement root = rootVisualElement;
            root.Clear();

            var title = new Label(imagePath == null ? "Texture Import Presets" : "Choose a preset for this image");
            title.AddToClassList("unity-heading");
            root.Add(title);

            if (!string.IsNullOrEmpty(imagePath))
            {
                root.Add(new Label(imagePath));
            }

            root.Add(new Label(
                "새 이미지마다 프리셋을 고릅니다. Assets/Editor/TextureImportPresets에 프리셋 에셋을 추가하면 목록에 자동으로 나타납니다."));

            presetDropdown = new DropdownField("Import preset");
            root.Add(presetDropdown);

            var options = new VisualElement();
            options.Add(new Button(RefreshPresets) { text = "Refresh list" });
            options.Add(new Button(CreateCustomPreset) { text = "Create preset" });
            options.Add(new Button(OpenPresetFolder) { text = "Open preset folder" });
            root.Add(options);

            root.Add(new HelpBox(
                "프리셋 에셋의 Inspector에서 Texture Type, Single/Multiple, 그리드 셀 크기/개수, 압축, 필터, PPU 등을 편집할 수 있습니다.",
                HelpBoxMessageType.Info));

            statusLabel = new Label();
            root.Add(statusLabel);

            if (imagePath == null)
            {
                root.Add(new Button(() =>
                {
                    CreateStarterPresetAssets();
                    RefreshPresets();
                    statusLabel.text = "기본 프리셋 4개를 준비했습니다.";
                }) { text = "Create starter presets" });
                return;
            }

            var actions = new VisualElement();
            actions.Add(new Button(ApplySelectedPreset) { text = "Apply preset" });
            actions.Add(new Button(SkipImage) { text = "Skip this image" });
            root.Add(actions);
            RefreshDropdown();
        }

        private void RefreshPresets()
        {
            presets = AssetDatabase.FindAssets("t:TextureImportPreset", new[] { PresetFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<TextureImportPreset>)
                .Where(preset => preset != null)
                .OrderBy(preset => preset.PresetName)
                .ToArray();
            RefreshDropdown();
        }

        private void RefreshDropdown()
        {
            if (presetDropdown == null)
                return;

            var choices = presets == null
                ? new System.Collections.Generic.List<string>()
                : presets.Select(PresetLabel).ToList();
            presetDropdown.choices = choices;

            if (choices.Count > 0)
            {
                if (!choices.Contains(presetDropdown.value))
                    presetDropdown.value = choices[0];
                presetDropdown.SetEnabled(true);
            }
            else
            {
                presetDropdown.SetEnabled(false);
                if (statusLabel != null)
                    statusLabel.text = "프리셋이 없습니다. Create preset을 눌러 추가하세요.";
            }
        }

        private static string PresetLabel(TextureImportPreset preset)
        {
            string path = AssetDatabase.GetAssetPath(preset);
            return preset.PresetName + " (" + Path.GetFileNameWithoutExtension(path) + ")";
        }

        private TextureImportPreset SelectedPreset()
        {
            if (presets == null || presetDropdown == null)
                return null;
            return presets.FirstOrDefault(preset => PresetLabel(preset) == presetDropdown.value);
        }

        private void ApplySelectedPreset()
        {
            TextureImportPreset preset = SelectedPreset();
            if (preset == null)
            {
                statusLabel.text = "먼저 적용할 프리셋을 선택하세요.";
                return;
            }

            if (!TextureImportPresetQueue.Apply(imagePath, preset, out string error))
            {
                statusLabel.text = error;
                return;
            }

            TextureImportPresetQueue.Complete(imagePath);
            Close();
        }

        private void SkipImage()
        {
            TextureImportPresetQueue.MarkSkipped(imagePath);
            TextureImportPresetQueue.Complete(imagePath);
            Close();
        }

        private void CreateCustomPreset()
        {
            EnsureFolder();
            string path = EditorUtility.SaveFilePanelInProject(
                "Create texture import preset",
                "New Image Preset",
                "asset",
                "Choose a name for the preset.",
                PresetFolder);
            if (string.IsNullOrEmpty(path))
                return;

            var preset = CreateInstance<TextureImportPreset>();
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshPresets();
            Selection.activeObject = preset;
            EditorGUIUtility.PingObject(preset);
            statusLabel.text = "프리셋을 만들었습니다. Inspector에서 설정하고 목록을 새로고침하세요.";
        }

        private static void OpenPresetFolder()
        {
            EnsureFolder();
            EditorUtility.RevealInFinder(Path.GetFullPath(PresetFolder));
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");
            if (!AssetDatabase.IsValidFolder(PresetFolder))
                AssetDatabase.CreateFolder("Assets/Editor", "TextureImportPresets");
        }

        private static void CreateStarter(
            string displayName,
            TextureImporterType textureType,
            SpriteImportMode spriteMode,
            FilterMode filter,
            TextureImporterCompression compression,
            SpriteSlicePresetMode sliceMode = SpriteSlicePresetMode.None)
        {
            string path = PresetFolder + "/" + displayName + ".asset";
            if (AssetDatabase.LoadAssetAtPath<TextureImportPreset>(path) != null)
                return;

            var preset = CreateInstance<TextureImportPreset>();
            preset.ConfigureStarter(displayName, textureType, spriteMode, filter, compression, sliceMode);
            AssetDatabase.CreateAsset(preset, path);
        }
    }
}
