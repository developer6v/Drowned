#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Drowned.Editor
{
    /// <summary>
    /// Importa os ícones da HUD (SCRUM-184) como Sprite sem ninguém mexer no Inspector: qualquer PNG dentro de
    /// uma pasta <c>Resources/Icons/</c> vira Sprite (2D and UI), Single, PPU 128, sem mipmap e sem compressão.
    /// Fica dentro de <c>#if UNITY_EDITOR</c> no assembly Drowned.Runtime, como o <see cref="SpriteSheetPostprocessor"/>.
    /// </summary>
    public sealed class IconPostprocessor : AssetPostprocessor
    {
        private const string Marker = "/Resources/Icons/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains(Marker) || !assetPath.EndsWith(".png")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 256;
        }
    }
}
#endif
