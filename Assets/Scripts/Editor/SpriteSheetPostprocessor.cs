#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Drowned.Editor
{
    /// <summary>
    /// Configura automaticamente o import dos spritesheets gerados por
    /// <c>tools/blender/render_batch.py</c> (SCRUM-154).
    ///
    /// Quando um PNG chamado <c>&lt;Nome&gt;_sheet.png</c> tem um <c>&lt;Nome&gt;_sheet.json</c>
    /// ao lado, este postprocessor lê o layout (tamanho do frame, direções, frames, pivô, PPU)
    /// e deixa a textura como Sprite (2D and UI), Multiple, sem compressão, fatiada em grade.
    /// Os sprites ficam nomeados <c>&lt;Nome&gt;_&lt;Dir&gt;_&lt;frame&gt;</c>, por exemplo
    /// <c>Housing_L1_S_000</c>. Ninguém precisa abrir o Sprite Editor.
    ///
    /// Fica dentro de <c>#if UNITY_EDITOR</c> no assembly Drowned.Runtime para não precisar
    /// de um asmdef de editor separado (CLAUDE.md).
    /// </summary>
    public sealed class SpriteSheetPostprocessor : AssetPostprocessor
    {
        private const string SheetSuffix = "_sheet.png";

        [Serializable]
        private sealed class SheetMeta
        {
            public string name;
            public int[] frameSize;
            public int directions;
            public string[] directionOrder;
            public int frames;
            public float[] pivot;
            public int pixelsPerUnit;
        }

        private void OnPreprocessTexture()
        {
            if (!TryReadMeta(assetPath, out var meta)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = meta.pixelsPerUnit > 0 ? meta.pixelsPerUnit : 128;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = NextPowerOfTwo(Math.Max(meta.frameSize[0] * Math.Max(meta.frames, 1),
                                                              meta.frameSize[1] * Math.Max(meta.directions, 1)));

            // Fatiamento em grade: linhas = direções (de cima para baixo), colunas = frames.
            // Unity mede os rects a partir do canto inferior esquerdo.
            int w = meta.frameSize[0];
            int h = meta.frameSize[1];
            int rows = Math.Max(meta.directions, 1);
            int cols = Math.Max(meta.frames, 1);
            int sheetHeight = rows * h;
            var pivot = meta.pivot != null && meta.pivot.Length == 2
                ? new Vector2(meta.pivot[0], meta.pivot[1])
                : new Vector2(0.5f, 0.5f);

            var rects = new List<SpriteMetaData>(rows * cols);
            for (int r = 0; r < rows; r++)
            {
                string dir = meta.directionOrder != null && r < meta.directionOrder.Length
                    ? meta.directionOrder[r]
                    : r.ToString();
                for (int c = 0; c < cols; c++)
                {
                    rects.Add(new SpriteMetaData
                    {
                        name = $"{meta.name}_{dir}_{c:000}",
                        rect = new Rect(c * w, sheetHeight - (r + 1) * h, w, h),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = pivot,
                    });
                }
            }
#pragma warning disable CS0618 // spritesheet ainda é a forma mais simples sem depender do pacote 2D Sprite Editor
            importer.spritesheet = rects.ToArray();
#pragma warning restore CS0618
        }

        private static bool TryReadMeta(string pngPath, out SheetMeta meta)
        {
            meta = null;
            if (!pngPath.EndsWith(SheetSuffix, StringComparison.OrdinalIgnoreCase)) return false;
            string jsonPath = pngPath.Substring(0, pngPath.Length - ".png".Length) + ".json";
            if (!File.Exists(jsonPath)) return false;
            try
            {
                meta = JsonUtility.FromJson<SheetMeta>(File.ReadAllText(jsonPath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SpriteSheetPostprocessor: não consegui ler {jsonPath}: {e.Message}");
                return false;
            }
            if (meta == null || meta.frameSize == null || meta.frameSize.Length != 2) return false;
            if (string.IsNullOrEmpty(meta.name))
                meta.name = Path.GetFileName(pngPath).Replace(SheetSuffix, "");
            return true;
        }

        private static int NextPowerOfTwo(int v)
        {
            int p = 32;
            while (p < v && p < 16384) p *= 2;
            return p;
        }
    }
}
#endif
