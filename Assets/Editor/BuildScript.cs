using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arcanepunk.Editor
{
    public static class BuildScript
    {
        public static void PerformAndroidBuild()
        {
            // Player settings
            PlayerSettings.productName = "Arcanepunk Life Sim";
            PlayerSettings.companyName = "Arcanepunk Studios";
            PlayerSettings.applicationIdentifier = "com.arcanepunk.lifesim";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard_2_1);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // Graphics
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan, UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.managedStrippingLevel = ManagedStrippingLevel.High;

            // Build path
            string buildPath = "build/Android/ArcanepunkLifeSim.apk";
            System.IO.Directory.CreateDirectory("build/Android");

            // Scenes to include
            var scenes = System.Array.FindAll(EditorBuildSettings.scenes, s => s.enabled)
                .Select(s => s.path).ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("No scenes in build settings!");
                return;
            }

            // Build
            var report = BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.Android, BuildOptions.None);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {buildPath} ({report.summary.totalSize} bytes)");
            }
            else
            {
                Debug.LogError($"Build failed: {report.summary.result}");
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error) Debug.LogError(msg.content);
                    }
                }
                throw new System.Exception("Build failed");
            }
        }

        [MenuItem("Arcanepunk/Build Android APK")]
        public static void BuildAndroidMenu()
        {
            PerformAndroidBuild();
        }
    }
}