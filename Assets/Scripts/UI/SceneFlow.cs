using UnityEngine;
using UnityEngine.SceneManagement;

namespace Drowned.UI
{
    /// <summary>
    /// Navegação entre cenas (menu principal, jogo). Os nomes são os das cenas em <c>Assets/Scenes</c>;
    /// se a cena não estiver nas Build Settings, avisa e recarrega a atual para não travar.
    /// </summary>
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        public static void ReloadCurrent()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public static void LoadGame()
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(GameScene)) SceneManager.LoadScene(GameScene);
            else
            {
                Debug.LogWarning($"Cena '{GameScene}' não está nas Build Settings; recarregando a cena atual.");
                ReloadCurrent();
            }
        }

        public static void LoadMainMenu()
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(MainMenuScene)) SceneManager.LoadScene(MainMenuScene);
            else
            {
                Debug.LogWarning($"Cena '{MainMenuScene}' não está nas Build Settings; recarregando a cena atual.");
                ReloadCurrent();
            }
        }

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
