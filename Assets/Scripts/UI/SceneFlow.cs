using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drowned.UI
{
    /// <summary>
    /// Navegação entre cenas (menu principal, jogo). Os nomes são os das cenas em <c>Assets/Scenes</c>.
    /// Na build, as cenas precisam estar nas Build Settings. No editor, se não estiverem, carrega pelo caminho
    /// <c>Assets/Scenes/&lt;Nome&gt;.unity</c>, então dá para testar o fluxo menu → jogo sem mexer em ProjectSettings.
    /// </summary>
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        public static void ReloadCurrent()
        {
            Time.timeScale = 1f;
            var current = SceneManager.GetActiveScene();
            if (current.buildIndex >= 0)
            {
                SceneManager.LoadScene(current.buildIndex);
                return;
            }
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(current.path))
            {
                LoadByPathInEditor(current.path);
                return;
            }
#endif
            Debug.LogWarning("SceneFlow: não sei recarregar a cena atual (sem build index nem caminho).");
        }

        public static void LoadGame() => Load(GameScene);

        public static void LoadMainMenu() => Load(MainMenuScene);

        private static void Load(string sceneName)
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }
#if UNITY_EDITOR
            string path = "Assets/Scenes/" + sceneName + ".unity";
            if (System.IO.File.Exists(path))
            {
                Debug.Log($"SceneFlow: '{sceneName}' não está nas Build Settings; carregando {path} (só funciona no editor).");
                LoadByPathInEditor(path);
                return;
            }
#endif
            Debug.LogWarning($"Cena '{sceneName}' não está nas Build Settings; recarregando a cena atual.");
            ReloadCurrent();
        }

#if UNITY_EDITOR
        private static void LoadByPathInEditor(string path) =>
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#endif

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
