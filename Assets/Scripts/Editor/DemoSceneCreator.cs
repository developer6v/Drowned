#if UNITY_EDITOR
using System.IO;
using Drowned.Core;
using Drowned.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Drowned.Editor
{
    /// <summary>
    /// Cria (ou recria) as duas cenas do jogo em <c>Assets/Scenes</c>, cada uma com um único objeto que monta
    /// o resto por código ao dar Play:
    /// <list type="bullet">
    /// <item><c>MainMenu.unity</c>: objeto <c>MainMenu</c> com <see cref="MainMenuView"/> (SCRUM-178).</item>
    /// <item><c>Game.unity</c>: objeto <c>DemoBootstrap</c> com <see cref="DemoBootstrap"/> (SCRUM-188).</item>
    /// </list>
    /// Menu <b>Drowned → Criar cenas (MainMenu + Game)</b> ou, em batch:
    /// <code>
    /// Unity.exe -batchmode -nographics -projectPath . -executeMethod Drowned.Editor.DemoSceneCreator.Create -quit
    /// </code>
    /// Os nomes vêm de <see cref="SceneFlow"/>. Para a build, alguém adiciona as duas nas Build Settings
    /// (MainMenu primeiro); no editor o <see cref="SceneFlow"/> carrega pelo caminho mesmo sem isso.
    /// </summary>
    public static class DemoSceneCreator
    {
        public static string GameScenePath => "Assets/Scenes/" + SceneFlow.GameScene + ".unity";
        public static string MainMenuScenePath => "Assets/Scenes/" + SceneFlow.MainMenuScene + ".unity";

        [MenuItem("Drowned/Criar cenas (MainMenu + Game)", priority = 3)]
        public static void Create()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Cenas] saia do Play antes de criar as cenas.");
                return;
            }
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
            CreateScene(MainMenuScenePath, "MainMenu", go => go.AddComponent<MainMenuView>());
            CreateScene(GameScenePath, "DemoBootstrap", go => go.AddComponent<DemoBootstrap>());
            AssetDatabase.Refresh();
        }

        private static void CreateScene(string path, string objectName, System.Action<GameObject> setup)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject(objectName);
            setup(go);
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                Debug.LogError("[Cenas] não consegui salvar " + path);
                return;
            }
            Debug.Log("[Cenas] salva: " + path);
        }
    }
}
#endif
