#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Drowned.Editor
{
    /// <summary>
    /// Importa os quadros da introdução (SCRUM-198) como Sprite sem mexer no Inspector: qualquer PNG dentro de
    /// uma pasta <c>Resources/Intro/</c> vira Sprite (2D and UI), Single, sem mipmap, até 2048 px.
    /// Mesmo padrão do <see cref="IconPostprocessor"/>.
    /// </summary>
    public sealed class IntroPostprocessor : AssetPostprocessor
    {
        private const string Marker = "/Resources/Intro/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains(Marker) || !assetPath.EndsWith(".png")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.maxTextureSize = 2048;
        }
    }
}
#endif
