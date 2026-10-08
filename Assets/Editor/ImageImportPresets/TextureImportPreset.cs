using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Conquest.Editor.ImageImportPresets
{
    public enum SpriteSlicePresetMode
    {
        None,
        GridByCellSize,
        GridByCellCount
    }

    [CreateAssetMenu(
        fileName = "Texture Import Preset",
        menuName = "Conquest/Texture Import Preset")]
    public sealed class TextureImportPreset : ScriptableObject
    {
        [SerializeField] private string presetName = "New Image Preset";
        [SerializeField] private TextureImporterType textureType = TextureImporterType.Sprite;
        [SerializeField] private SpriteImportMode spriteImportMode = SpriteImportMode.Single;
        [SerializeField, Min(32)] private int maxTextureSize = 2048;
        [SerializeField] private bool sRGBTexture = true;
        [SerializeField] private bool alphaIsTransparency = true;
        [SerializeField] private bool mipmapEnabled;
        [SerializeField] private bool isReadable;
        [SerializeField] private FilterMode filterMode = FilterMode.Point;
        [SerializeField] private TextureWrapMode wrapMode = TextureWrapMode.Clamp;
        [SerializeField] private TextureImporterCompression compression = TextureImporterCompression.Uncompressed;
        [SerializeField, Range(0, 100)] private int compressionQuality = 50;
        [SerializeField, Min(0.01f)] private float pixelsPerUnit = 100f;
        [SerializeField] private SpriteMeshType spriteMeshType = SpriteMeshType.FullRect;
        [SerializeField] private SpriteAlignment spriteAlignment = SpriteAlignment.Center;
        [SerializeField] private Vector2 spritePivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private SpriteSlicePresetMode sliceMode;
        [SerializeField] private Vector2Int cellSize = new Vector2Int(64, 64);
        [SerializeField] private Vector2Int cellCount = new Vector2Int(4, 4);
        [SerializeField] private Vector2Int sliceOffset;
        [SerializeField] private Vector2Int slicePadding;

        public string PresetName => string.IsNullOrWhiteSpace(presetName) ? name : presetName;

        internal void ConfigureStarter(
            string displayName,
            TextureImporterType importerType,
            SpriteImportMode importMode,
            FilterMode filter,
            TextureImporterCompression textureCompression,
            SpriteSlicePresetMode gridMode = SpriteSlicePresetMode.None)
        {
            presetName = displayName;
            textureType = importerType;
            spriteImportMode = importMode;
            filterMode = filter;
            compression = textureCompression;
            sliceMode = gridMode;
        }

        internal bool ApplyTo(string assetPath, out string error)
        {
            error = null;
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
            {
                error = "이 파일은 TextureImporter로 열 수 없습니다.";
                return false;
            }

            if (sliceMode != SpriteSlicePresetMode.None &&
                (textureType != TextureImporterType.Sprite || spriteImportMode != SpriteImportMode.Multiple))
            {
                error = "그리드 슬라이스 프리셋은 Texture Type이 Sprite이고 Sprite Mode가 Multiple이어야 합니다.";
                return false;
            }

            importer.textureType = textureType;
            importer.maxTextureSize = Mathf.Max(32, maxTextureSize);
            importer.sRGBTexture = sRGBTexture;
            importer.mipmapEnabled = mipmapEnabled;
            importer.isReadable = isReadable;
            importer.filterMode = filterMode;
            importer.wrapMode = wrapMode;
            importer.textureCompression = compression;
            importer.compressionQuality = Mathf.Clamp(compressionQuality, 0, 100);

            if (textureType == TextureImporterType.Sprite)
            {
                importer.spriteImportMode = spriteImportMode;
                importer.spritePixelsPerUnit = Mathf.Max(0.01f, pixelsPerUnit);
                importer.spritePivot = spritePivot;
                importer.alphaIsTransparency = alphaIsTransparency;

                var importerSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(importerSettings);
                importerSettings.spriteMeshType = spriteMeshType;
                importerSettings.spriteAlignment = (int)spriteAlignment;
                importer.SetTextureSettings(importerSettings);
            }

            importer.SaveAndReimport();

            if (sliceMode == SpriteSlicePresetMode.None)
                return true;

            return ApplyGridSlices(assetPath, out error);
        }

        private bool ApplyGridSlices(string assetPath, out string error)
        {
            error = null;
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer))
            {
                error = "다시 임포트한 이미지의 TextureImporter를 찾을 수 없습니다.";
                return false;
            }

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
            {
                error = "이 이미지에서 Sprite Editor 데이터 공급자를 사용할 수 없습니다. 슬라이스를 적용하지 않았습니다.";
                return false;
            }

            dataProvider.InitSpriteEditorDataProvider();
            ISpriteFrameEditCapability editCapability =
                dataProvider.GetDataProvider<ISpriteFrameEditCapability>();
            if (editCapability == null ||
                !editCapability.GetEditCapability().HasCapability(EEditCapability.CreateAndDeleteSprite))
            {
                error = "이 임포터는 스프라이트 생성/삭제를 지원하지 않습니다. 슬라이스를 적용하지 않았습니다.";
                return false;
            }

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            List<SpriteRect> spriteRects = BuildGrid(assetPath, width, height, out error);
            if (spriteRects == null)
                return false;

            dataProvider.SetSpriteRects(spriteRects.ToArray());

            ISpriteNameFileIdDataProvider nameFileIdProvider =
                dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameFileIdProvider != null)
            {
                var pairs = new List<SpriteNameFileIdPair>(spriteRects.Count);
                foreach (SpriteRect spriteRect in spriteRects)
                    pairs.Add(new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID));
                nameFileIdProvider.SetNameFileIdPairs(pairs);
            }

            dataProvider.Apply();
            importer.SaveAndReimport();
            return true;
        }

        private List<SpriteRect> BuildGrid(string assetPath, int width, int height, out string error)
        {
            error = null;
            int offsetX = Mathf.Max(0, sliceOffset.x);
            int offsetY = Mathf.Max(0, sliceOffset.y);
            int paddingX = Mathf.Max(0, slicePadding.x);
            int paddingY = Mathf.Max(0, slicePadding.y);
            int columns;
            int rows;
            int sliceWidth;
            int sliceHeight;

            if (sliceMode == SpriteSlicePresetMode.GridByCellCount)
            {
                columns = Mathf.Max(1, cellCount.x);
                rows = Mathf.Max(1, cellCount.y);
                sliceWidth = (width - offsetX * 2 - paddingX * (columns - 1)) / columns;
                sliceHeight = (height - offsetY * 2 - paddingY * (rows - 1)) / rows;
            }
            else
            {
                sliceWidth = Mathf.Max(1, cellSize.x);
                sliceHeight = Mathf.Max(1, cellSize.y);
                columns = Mathf.Max(0, (width - offsetX * 2 + paddingX) / (sliceWidth + paddingX));
                rows = Mathf.Max(0, (height - offsetY * 2 + paddingY) / (sliceHeight + paddingY));
            }

            if (sliceWidth <= 0 || sliceHeight <= 0 || columns <= 0 || rows <= 0)
            {
                error = "현재 이미지 크기와 프리셋의 셀 크기/개수/여백으로는 슬라이스를 만들 수 없습니다.";
                return null;
            }

            long total = (long)columns * rows;
            if (total > 4096)
            {
                error = $"슬라이스 수가 너무 많습니다 ({total}). 셀 크기를 키우거나 셀 개수를 줄여 주세요.";
                return null;
            }

            var result = new List<SpriteRect>((int)total);
            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    var spriteRect = new SpriteRect
                    {
                        name = $"{baseName}_{index:000}",
                        spriteID = GUID.Generate(),
                        rect = new Rect(
                            offsetX + column * (sliceWidth + paddingX),
                            offsetY + row * (sliceHeight + paddingY),
                            sliceWidth,
                            sliceHeight),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        border = Vector4.zero
                    };
                    result.Add(spriteRect);
                    index++;
                }
            }

            return result;
        }
    }
}
