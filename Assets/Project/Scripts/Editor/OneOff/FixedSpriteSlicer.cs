using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class FixedSpriteSlicer
{
    private const string BodyMenuPath = "Tools/DATN/One-off Setup/Sprite Slicer/Slice Selected Texture";

    private static readonly SliceDefinition[] BodySlices =
    {
        new("FrontHead", 0, 320, 480, 480, 0.5f, 0.2f),
        new("BackHead", 480, 320, 480, 480, 0.5f, 0.2f),
        new("LeftHead", 960, 320, 480, 480, 0.5f, 0.2f),

        new("FrontBody", 160, 160, 160, 160, 0.5f, 0.4f),
        new("BackBody", 640, 160, 160, 160, 0.5f, 0.4f),
        new("LeftBody", 1120, 160, 160, 160, 0.5f, 0.4f),

        new("FrontArmR", 80, 160, 80, 160, 0.5f, 0.7f),
        new("FrontArmL", 320, 160, 80, 160, 0.5f, 0.7f),
        new("BackArmL", 560, 160, 80, 160, 0.5f, 0.7f),
        new("BackArmR", 800, 160, 80, 160, 0.5f, 0.7f),
        new("LeftArmR", 1040, 160, 80, 160, 0.5f, 0.7f),
        new("LeftArmL", 1280, 160, 80, 160, 0.5f, 0.7f),

        new("FrontHandR", 80, 80, 80, 80, 0.5f, 0.5f),
        new("FrontHandL", 320, 80, 80, 80, 0.5f, 0.5f),
        new("BackHandL", 560, 80, 80, 80, 0.5f, 0.5f),
        new("BackHandR", 800, 80, 80, 80, 0.5f, 0.5f),
        new("LeftHandR", 1040, 80, 80, 80, 0.5f, 0.5f),
        new("LeftHandL", 1280, 80, 80, 80, 0.5f, 0.5f),

        new("FrontSleeveR", 80, 0, 80, 80, 0.5f, 0.5f),
        new("FrontSleeveL", 320, 0, 80, 80, 0.5f, 0.5f),
        new("BackSleeveL", 560, 0, 80, 80, 0.5f, 0.5f),
        new("BackSleeveR", 800, 0, 80, 80, 0.5f, 0.5f),
        new("LeftSleeveR", 1040, 0, 80, 80, 0.5f, 0.5f),
        new("LeftSleeveL", 1280, 0, 80, 80, 0.5f, 0.5f),

        new("FrontLegR", 160, 0, 80, 160, 0.5f, 0.6f),
        new("FrontLegL", 240, 0, 80, 160, 0.5f, 0.6f),
        new("BackLegL", 640, 0, 80, 160, 0.5f, 0.6f),
        new("BackLegR", 720, 0, 80, 160, 0.5f, 0.6f),
        new("LeftLegR", 1120, 0, 80, 160, 0.5f, 0.6f),
        new("LeftLegL", 1200, 0, 80, 160, 0.5f, 0.6f),
    };

    [MenuItem(BodyMenuPath)]
    private static void SliceSelectedBodyTexture()
    {
        var texturePath = AssetDatabase.GetAssetPath(Selection.activeObject);

        if (string.IsNullOrEmpty(texturePath))
        {
            EditorUtility.DisplayDialog("Body Sprite Slicer", "Select a texture asset in the Project window first.", "OK");
            return;
        }

        if (AssetImporter.GetAtPath(texturePath) is not TextureImporter importer)
        {
            EditorUtility.DisplayDialog("Body Sprite Slicer", "Selected asset is not a texture.", "OK");
            return;
        }

        SliceTexture(importer, BodySlices);
    }

    [MenuItem(BodyMenuPath, true)]
    private static bool ValidateSliceSelectedBodyTexture()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return AssetImporter.GetAtPath(path) is TextureImporter;
    }

    private static void SliceTexture(TextureImporter importer, IReadOnlyList<SliceDefinition> slices)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        var dataProvider = GetSpriteEditorDataProvider(importer);
        dataProvider.InitSpriteEditorDataProvider();

        var spriteRects = slices.Select(slice => new SpriteRect
        {
            name = slice.Name,
            rect = new Rect(slice.X, slice.Y, slice.Width, slice.Height),
            alignment = SpriteAlignment.Custom,
            pivot = new Vector2(slice.PivotX, slice.PivotY),
            border = Vector4.zero,
            spriteID = GUID.Generate(),
        }).ToArray();

        dataProvider.SetSpriteRects(spriteRects);
        SetNameFileIdPairs(dataProvider, spriteRects);
        dataProvider.Apply();
        importer.SaveAndReimport();

        Debug.Log($"[FixedSpriteSlicer] Sliced '{importer.assetPath}' as Body with {spriteRects.Length} sprites.");
    }

    private static ISpriteEditorDataProvider GetSpriteEditorDataProvider(TextureImporter importer)
    {
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        return factories.GetSpriteEditorDataProviderFromObject(importer);
    }

    private static void SetNameFileIdPairs(ISpriteEditorDataProvider dataProvider, IReadOnlyList<SpriteRect> spriteRects)
    {
        var nameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();

        if (nameFileIdDataProvider == null)
        {
            return;
        }

        var pairs = spriteRects
            .Select(spriteRect => new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID))
            .ToArray();

        nameFileIdDataProvider.SetNameFileIdPairs(pairs);
    }

    private readonly struct SliceDefinition
    {
        public readonly string Name;
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;
        public readonly float PivotX;
        public readonly float PivotY;

        public SliceDefinition(string name, int x, int y, int width, int height, float pivotX, float pivotY)
        {
            Name = name;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            PivotX = pivotX;
            PivotY = pivotY;
        }
    }
}
