#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using Drowned.UI;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Drowned.Editor
{
    /// <summary>
    /// Build Windows do Drowned (SCRUM-194). As cenas são passadas aqui, em ordem (menu primeiro), então não é
    /// preciso mexer nas Build Settings. Menu <b>Drowned → Build Windows</b> ou em batch:
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod Drowned.Editor.BuildScript.BuildWindows -logFile build.log
    /// </code>
    /// Saída: <c>Builds/Windows/Drowned.exe</c> e <c>Builds/Drowned-Windows.zip</c> (pasta ignorada pelo Git).
    /// </summary>
    public static class BuildScript
    {
        public const string OutputDir = "Builds/Windows";
        public const string ExeName = "Drowned.exe";
        public const string ZipPath = "Builds/Drowned-Windows.zip";

        public static readonly string[] Scenes =
        {
            "Assets/Scenes/" + SceneFlow.MainMenuScene + ".unity",
            "Assets/Scenes/" + SceneFlow.GameScene + ".unity",
        };

        [MenuItem("Drowned/Build Windows", priority = 20)]
        public static void BuildWindows()
        {
            foreach (var scene in Scenes)
            {
                if (File.Exists(scene)) continue;
                Fail($"cena {scene} não existe; rode Drowned → Criar cenas (MainMenu + Game).");
                return;
            }

            if (Directory.Exists(OutputDir)) Directory.Delete(OutputDir, true);
            Directory.CreateDirectory(OutputDir);

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = Path.Combine(OutputDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Fail($"build terminou com {summary.result}: {summary.totalErrors} erro(s). Veja o log acima.");
                return;
            }

            if (File.Exists(ZipPath)) File.Delete(ZipPath);
            ZipFile.CreateFromDirectory(OutputDir, ZipPath, System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);
            Debug.Log($"[Build] OK: {Path.GetFullPath(Path.Combine(OutputDir, ExeName))} " +
                      $"({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:0} s). Zip: {Path.GetFullPath(ZipPath)}");
        }

        private static void Fail(string message)
        {
            Debug.LogError("[Build] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
#endif
