using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Conquest.Editor.ImageImportPresets
{
    [InitializeOnLoad]
    internal static class TextureImportPresetQueue
    {
        private const string QueueKey = "Conquest.ImageImportPresets.Pending";
        private const string AutoPromptKey = "Conquest.ImageImportPresets.AutoPrompt";
        private const string InitializedKey = "Conquest.ImageImportPresets.Initialized.";
        private const string HandledPrefix = "Conquest.ImageImportPresets.Handled.";

        private static readonly List<string> pendingPaths = new List<string>();
        private static readonly HashSet<string> pendingSet = new HashSet<string>();
        private static bool initialized;
        private static bool promptOpen;

        static TextureImportPresetQueue()
        {
            if (!AutoPromptEnabled)
            {
                SessionState.SetString(QueueKey, string.Empty);
                return;
            }

            LoadPending();
            EditorApplication.delayCall += Initialize;
        }

        private static bool AutoPromptEnabled => EditorPrefs.GetBool(AutoPromptKey, false);

        private static string ProjectKey =>
            InitializedKey + Application.dataPath;

        private static void Initialize()
        {
            if (!AutoPromptEnabled)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Initialize;
                return;
            }

            if (!EditorPrefs.GetBool(ProjectKey, false))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Texture2D"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (AssetImporter.GetAtPath(path) is TextureImporter)
                        SetHandled(guid, true);
                }
                EditorPrefs.SetBool(ProjectKey, true);
            }

            initialized = true;
            ShowNextPrompt();
        }

        internal static void OnImported(string[] importedAssets)
        {
            if (!AutoPromptEnabled || !initialized || Application.isBatchMode)
                return;

            foreach (string path in importedAssets)
            {
                // Cutscene images are configured automatically during preprocessing.
                if (IsCutsceneImage(path))
                    continue;

                if (!IsImageFile(path) || !(AssetImporter.GetAtPath(path) is TextureImporter))
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid) || IsHandled(guid) || !pendingSet.Add(path))
                    continue;

                pendingPaths.Add(path);
            }

            SavePending();
            ShowNextPrompt();
        }

        internal static bool Apply(string path, TextureImportPreset preset, out string error)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                error = "이 이미지 파일을 찾을 수 없습니다.";
                return false;
            }

            SetHandled(guid, true);
            if (preset.ApplyTo(path, out error))
                return true;

            SetHandled(guid, false);
            return false;
        }

        internal static void MarkSkipped(string path)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
                SetHandled(guid, true);
        }

        internal static void Complete(string path)
        {
            pendingSet.Remove(path);
            pendingPaths.RemoveAll(candidate => candidate == path);
            promptOpen = false;
            SavePending();
            EditorApplication.delayCall += ShowNextPrompt;
        }

        internal static void PromptClosed()
        {
            promptOpen = false;
            EditorApplication.delayCall += ShowNextPrompt;
        }

        internal static void ShowNextPrompt()
        {
            if (!AutoPromptEnabled || !initialized || promptOpen || Application.isBatchMode ||
                EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            while (pendingPaths.Count > 0)
            {
                string path = pendingPaths[0];
                if (File.Exists(Path.GetFullPath(path)) && AssetImporter.GetAtPath(path) is TextureImporter)
                {
                    promptOpen = true;
                    TextureImportPresetWindow.ShowForImage(path);
                    return;
                }

                pendingPaths.RemoveAt(0);
                pendingSet.Remove(path);
            }

            SavePending();
        }

        private static bool IsImageFile(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".psd":
                case ".tif":
                case ".tiff":
                case ".bmp":
                case ".exr":
                case ".hdr":
                case ".gif":
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsCutsceneImage(string path) =>
            path.StartsWith("Assets/Cutscene/", StringComparison.OrdinalIgnoreCase) &&
            IsImageFile(path);

        private static bool IsHandled(string guid) =>
            EditorPrefs.GetBool(HandledPrefix + guid, false);

        private static void SetHandled(string guid, bool handled) =>
            EditorPrefs.SetBool(HandledPrefix + guid, handled);

        private static void LoadPending()
        {
            string json = SessionState.GetString(QueueKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                PendingData data = JsonUtility.FromJson<PendingData>(json);
                if (data?.paths == null)
                    return;

                foreach (string path in data.paths)
                {
                    if (!string.IsNullOrEmpty(path) && pendingSet.Add(path))
                        pendingPaths.Add(path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[TextureImportPreset] Pending import queue could not be restored: " + exception.Message);
                pendingPaths.Clear();
                pendingSet.Clear();
            }
        }

        private static void SavePending()
        {
            SessionState.SetString(
                QueueKey,
                JsonUtility.ToJson(new PendingData { paths = pendingPaths }));
        }

        [MenuItem("Tools/Conquest/Resume Pending Image Prompt")]
        private static void ResumePendingPrompt()
        {
            ShowNextPrompt();
        }

        [Serializable]
        private sealed class PendingData
        {
            public List<string> paths = new List<string>();
        }
    }

    internal sealed class TextureImportPresetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!TextureImportPresetQueue.IsCutsceneImage(assetPath))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            var platform = importer.GetDefaultPlatformTextureSettings();
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platform);
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            TextureImportPresetQueue.OnImported(importedAssets);
        }
    }
}
