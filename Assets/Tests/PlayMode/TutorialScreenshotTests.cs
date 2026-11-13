using System.Collections;
using System.IO;
using Drowned.Core;
using Drowned.UI;
using Drowned.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Drowned.Tests
{
    /// <summary>
    /// Prints do tutorial visual (SCRUM-197) e da introdução (SCRUM-198) para revisar sem abrir o editor.
    /// Mesmo esquema do <see cref="HudScreenshotTests"/>: <see cref="ExplicitAttribute"/>, sem -nographics:
    /// <code>
    /// Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Drowned.Tests.TutorialScreenshotTests -testResults shots.xml -logFile -
    /// </code>
    /// PNGs em <c>Logs/Screenshots/</c>. A marca de "tutorial feito" do PlayerPrefs é guardada e devolvida no fim.
    /// </summary>
    [Explicit("Gera prints para revisão visual; rodar com -testFilter")]
    public class TutorialScreenshotTests
    {
        private const int Width = 1920;
        private const int Height = 1080;

        private static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Screenshots"));

        private int _tutorialDone;
        private int _introSeen;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _tutorialDone = PlayerPrefs.GetInt(TutorialView.DoneKey, 0);
            _introSeen = PlayerPrefs.GetInt(IntroView.SeenKey, 0);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.transform.parent == null) Object.Destroy(go);
            if (_rt != null)
            {
                _rt.Release();
                _rt = null;
            }
            PlayerPrefs.SetInt(TutorialView.DoneKey, _tutorialDone);
            PlayerPrefs.SetInt(IntroView.SeenKey, _introSeen);
            PlayerPrefs.Save();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Prints_dos_passos_do_tutorial()
        {
            TutorialView.ResetDone();
            var scene = SceneManager.CreateScene("ShotTutorial");
            SceneManager.SetActiveScene(scene);
            yield return null;
            new GameObject("DemoBootstrap").AddComponent<DemoBootstrap>();
            for (int i = 0; i < 10; i++) yield return null;

            var hud = Object.FindAnyObjectByType<HudBuilder>();
            Assert.IsNotNull(hud, "sem HUD");
            var seq = hud.Tutorial.Sequence;
            Assert.IsNotNull(seq, "tutorial não começou");
            Assert.AreEqual(GameSpeed.Paused, GameManagerBehaviour.Instance.Game.Clock.Speed, "o tutorial começa com o jogo pausado");

            yield return Shot("tutorial_1_recursos.png");
            seq.Notify(TutorialTrigger.Acknowledge);
            yield return Shot("tutorial_2_camera.png");
            seq.Notify(TutorialTrigger.CameraMoved);
            yield return Shot("tutorial_3_moradia.png");

            // Escolhe a Moradia pelo mesmo caminho do botão
            var game = GameManagerBehaviour.Instance.Game;
            Drowned.Resources.StructureData housing = null;
            foreach (var d in game.Rules.Structures)
                if (d != null && d.Category == Drowned.Resources.StructureCategory.Housing) { housing = d; break; }
            Assert.IsNotNull(housing, "sem Moradia nas regras");
            BuildModeController.Instance.Select(housing);
            yield return null;
            Assert.AreEqual(TutorialTarget.BuildSpot, seq.Current.Target);
            yield return Shot("tutorial_4_mapa.png");

            BuildModeController.Instance.Cancel();
            seq.Notify(TutorialTrigger.StructureBuilt);
            yield return Shot("tutorial_5_velocidade.png");
            seq.Notify(TutorialTrigger.SpeedChanged); // o evento real só conta fora da pausa; aqui avança direto
            game.Clock.SetSpeed(GameSpeed.Normal);
            yield return Shot("tutorial_6_colapso.png");
            seq.Notify(TutorialTrigger.Acknowledge);
            yield return Shot("tutorial_7_monumento.png");
        }

        [UnityTest]
        public IEnumerator Prints_da_introducao()
        {
            var scene = SceneManager.CreateScene("ShotIntro");
            SceneManager.SetActiveScene(scene);
            yield return null;
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            var view = IntroView.Play(null);
            for (int i = 0; i < view.Sequence.Count; i++)
            {
                // Meio do quadro, com a legenda inteira
                view.Sequence.Update(view.Sequence.Current.Duration * 0.6f);
                yield return null;
                Capture($"intro_{i + 1}.png");
                view.Sequence.Next();
                yield return null;
            }
        }

        private IEnumerator Shot(string name)
        {
            // Converte os canvases antes e espera: o LateUpdate do tutorial posiciona seta e balão já no tamanho final
            PrepareCanvases();
            yield return null;
            yield return new WaitForSecondsRealtime(0.4f);
            Capture(name);
        }

        /// <summary>Igual ao de <see cref="HudScreenshotTests"/>: canvases em Screen Space Camera, render num RenderTexture, PNG.</summary>
        private static RenderTexture _rt;

        /// <summary>
        /// Deixa a câmera renderizando num RenderTexture 1920x1080 o tempo todo (não só na hora do print): assim o
        /// canvas tem o tamanho final já nos frames de espera e o tutorial calcula seta e escurecimento nele.
        /// </summary>
        private static void PrepareCanvases()
        {
            var cam = Camera.main;
            Assert.IsNotNull(cam, "sem Main Camera");
            if (_rt == null) _rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = _rt;
            cam.aspect = (float)Width / Height;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.ScreenSpaceCamera) continue;
                var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = Width / UiTheme.ReferenceResolution.x;
                }
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = cam.nearClipPlane + 1f;
                canvas.sortingOrder = 32000 + Mathf.Clamp(canvas.sortingOrder, 0, 700);
            }
        }

        private static void Capture(string fileName)
        {
            PrepareCanvases();
            var cam = Camera.main;

            var rt = _rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var active = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;

            Directory.CreateDirectory(OutDir);
            string path = Path.Combine(OutDir, fileName);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
            Debug.Log("[Screenshot] " + path);
        }
    }
}
