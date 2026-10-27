using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Arcanepunk.Editor
{
    public static class SetupScene
    {
        [MenuItem("Arcanepunk/Create Main Scene")]
        public static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create GameManager
            var gm = new GameObject("GameManager");
            gm.AddComponent<ArcanepunkGameManager>();

            // Create EventSystem for UI
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Save scene
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");

            // Add to build settings
            var buildScenes = new[] { "Assets/Scenes/Main.unity" };
            EditorBuildSettings.scenes = buildScenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();

            Debug.Log("Main scene created and added to build settings");
        }
    }
}