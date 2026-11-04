#if UNITY_EDITOR
using System.IO;
using Drowned.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Drowned.Editor
{
    /// <summary>
    /// Cria (ou recria) <c>Assets/Scenes/Demo.unity</c>: uma cena vazia com um único objeto <c>DemoBootstrap</c>,
    /// que monta todo o resto por código ao dar Play (SCRUM-188). Menu <b>Drowned → Criar cena Demo</b> ou, em batch:
    ///
    /// <code>
    /// Unity.exe -batchmode -nographics -projectPath . -executeMethod Drowned.Editor.DemoSceneCreator.Create -quit
    /// </code>
    ///
    /// A cena é propositalmente mínima: tudo que ela contém pode ser regenerado por este comando, então editar
    /// a cena à mão não é necessário nem recomendado.
    /// </summary>
    public static class DemoSceneCreator
    {
        public const string ScenePath = "Assets/Scenes/Demo.unity";

        [MenuItem("Drowned/Criar cena Demo", priority = 3)]
        public static void Create()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Demo.unity] saia do Play antes de criar a cena.");
                return;
            }
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("DemoBootstrap");
            go.AddComponent<DemoBootstrap>();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[Demo.unity] não consegui salvar em {ScenePath}");
                return;
            }
            AssetDatabase.Refresh();
            Debug.Log($"[Demo.unity] salva em {ScenePath}. Abra e dê Play.");
        }
    }
}
#endif
