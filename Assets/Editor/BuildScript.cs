using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Athanor.EditorTools
{
    /// Build por línea de comandos:
    ///   Unity.exe -batchmode -projectPath . -buildTarget Android
    ///             -executeMethod Athanor.EditorTools.BuildScript.BuildApk -logFile build.log
    public static class BuildScript
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string PackageId = "com.vesperoni.athanor";

        public static void BuildApk()
        {
            try
            {
                Configure();
                EnsureScene();

                Directory.CreateDirectory("Builds");
                string apkPath = $"Builds/athanor-v{Athanor.GameVersion.Version}.apk";

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = apkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                };

                var report = BuildPipeline.BuildPlayer(options);
                bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
                Debug.Log($"[BuildScript] Resultado: {report.summary.result}, " +
                          $"tamaño: {report.summary.totalSize / (1024 * 1024)} MB, salida: {apkPath}");
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError("[BuildScript] Excepción: " + e);
                EditorApplication.Exit(1);
            }
        }

        /// Solo configura y guarda la escena (para la primera importación / verificación).
        public static void Prepare()
        {
            try
            {
                Configure();
                EnsureScene();
                Debug.Log("[BuildScript] Prepare OK");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[BuildScript] Excepción: " + e);
                EditorApplication.Exit(1);
            }
        }

        static void Configure()
        {
            PlayerSettings.productName = "Athanor";
            PlayerSettings.companyName = "Vesperoni";
            PlayerSettings.bundleVersion = Athanor.GameVersion.Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.Android.bundleVersionCode = Athanor.GameVersion.VersionCode;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)23;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            EditorUserBuildSettings.buildAppBundle = false; // APK para sideload/releases

            AssetDatabase.SaveAssets();
        }

        static void EnsureScene()
        {
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
