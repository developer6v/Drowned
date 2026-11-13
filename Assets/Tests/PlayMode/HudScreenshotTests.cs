using System.Collections;
using System.IO;
using Drowned.Core;
using Drowned.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Drowned.Tests
{
    /// <summary>
    /// Tira prints da HUD e do menu principal em 1920×1080 para revisar o visual sem abrir o editor (SCRUM-192).
    /// <see cref="ExplicitAttribute"/>: não roda junto com os outros testes, só quando pedido pelo filtro:
    /// <code>
    /// Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Drowned.Tests.HudScreenshotTests -testResults shots.xml -logFile -
    /// </code>
    /// Precisa de placa de vídeo (não usar -nographics). Os PNGs vão para <c>Logs/Screenshots/</c> (ignorado pelo Git; a pasta Temp é apagada quando o Unity fecha).
    /// </summary>
    [Explicit("Gera prints para revisão visual; rodar com -testFilter")]
    public class HudScreenshotTests
    {
        private const int Width = 1920;
        private const int Height = 1080;

        private static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Screenshots"));

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.transform.parent == null) Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Print_da_HUD()
        {
            var scene = SceneManager.CreateScene("ShotHud");
            SceneManager.SetActiveScene(scene);
            yield return null;
            new GameObject("DemoBootstrap").AddComponent<DemoBootstrap>();
            for (int i = 0; i < 10; i++) yield return null;
            // Um dia simulado para a HUD ter taxas e números reais
            var gm = GameManagerBehaviour.Instance;
            Assert.IsNotNull(gm);
            gm.Game.Clock.Advance(gm.Game.Clock.SecondsPerDay);
            yield return new WaitForSecondsRealtime(0.5f); // fade de cor dos botões
            Capture("hud.png");
        }

        [UnityTest]
        public IEnumerator Print_dos_especialistas_na_cidade()
        {
            var scene = SceneManager.CreateScene("ShotSpecialists");
            SceneManager.SetActiveScene(scene);
            yield return null;
            new GameObject("DemoBootstrap").AddComponent<DemoBootstrap>();
            for (int i = 0; i < 10; i++) yield return null;
            var gm = GameManagerBehaviour.Instance;
            Assert.IsNotNull(gm);
            // Libera os 4 especialistas pelo mesmo caminho do save (SCRUM-152)
            var ids = new System.Collections.Generic.List<string>();
            foreach (var s in gm.Game.Specialists.All) ids.Add(s.Id);
            gm.Game.Specialists.Restore(ids);
            yield return new WaitForSecondsRealtime(1.5f); // espalham um pouco
            var walkers = GameObject.Find("Inhabitants");
            Assert.IsNotNull(walkers, "sem Inhabitants");
            Vector3 c = Vector3.zero;
            int n = 0;
            foreach (Transform t in walkers.transform) { c += t.position; n++; }
            Assert.Greater(n, 0, "nenhum mergulhador");
            var cam = Camera.main;
            cam.transform.position = new Vector3(c.x / n, c.y / n, cam.transform.position.z);
            cam.orthographicSize = 2.2f;
            Capture("especialistas.png");
        }

        [UnityTest]
        public IEnumerator Print_do_menu_principal()
        {
            var scene = SceneManager.CreateScene("ShotMenu");
            SceneManager.SetActiveScene(scene);
            yield return null;
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            new GameObject("MainMenu").AddComponent<MainMenuView>();
            yield return new WaitForSecondsRealtime(0.5f); // fade de cor dos botões
            Capture("menu.png");
        }

        /// <summary>Passa os canvases para Screen Space Camera, renderiza a câmera principal num RenderTexture e grava o PNG.</summary>
        private static void Capture(string fileName)
        {
            var cam = Camera.main;
            Assert.IsNotNull(cam, "sem Main Camera");
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas) continue;
                // Em batch a "tela" é virtual e pequena; fixa a escala para a UI sair 1:1 com a imagem.
                var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = Width / UiTheme.ReferenceResolution.x;
                }
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.pixelPerfect = true;
                canvas.worldCamera = cam;
                canvas.planeDistance = cam.nearClipPlane + 1f;
                canvas.sortingOrder = 32000; // no jogo a HUD é overlay e fica sempre por cima do mundo
            }

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previous = cam.targetTexture;
            cam.targetTexture = rt;
            cam.aspect = (float)Width / Height; // em batch a tela virtual não é 16:9
            foreach (var v in Object.FindObjectsByType<Drowned.World.ScreenVignette>(FindObjectsSortMode.None)) v.Follow();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            cam.targetTexture = previous;
            cam.ResetAspect();

            Directory.CreateDirectory(OutDir);
            string path = Path.Combine(OutDir, fileName);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
            rt.Release();
            Debug.Log("[Screenshot] " + path);
        }
    }
}
