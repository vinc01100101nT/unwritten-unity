using UnityEditor;
using UnityEngine;

/// <summary>
/// Auto-applies crisp pixel-art import settings to every texture under
/// <c>Assets/Art/</c> so you never hand-fix import settings again. Runs on every
/// import / reimport.
///
/// NOTE: art that was imported BEFORE this script existed won't pick up the
/// settings until you reimport it — right-click <c>Assets/Art</c> → Reimport.
///
/// It does NOT slice multi-sprite sheets. For a character/tileset sheet, set
/// Sprite Mode = Multiple in the Inspector and grid-slice it in the Sprite
/// Editor (see unity/README.md). The Multiple mode + your slices are preserved
/// across reimports; only PPU / filtering / compression are enforced.
/// </summary>
public class PixelArtImportPostprocessor : AssetPostprocessor
{
    // Ninja Adventure (and the rest of our packs) are authored at 16 px / tile.
    const int PixelsPerUnit = 16;

    void OnPreprocessTexture()
    {
        // Only touch the art folder; leave UI/editor/imported-package textures alone.
        if (!assetPath.Replace('\\', '/').Contains("/Art/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;                       // crisp, no blur
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;           // never rescale pixel sheets
        importer.wrapMode = TextureWrapMode.Clamp;

        // Keep Multiple (sliced) sheets sliced; default a fresh texture to Single.
        if (importer.spriteImportMode != SpriteImportMode.Multiple)
            importer.spriteImportMode = SpriteImportMode.Single;
    }
}
