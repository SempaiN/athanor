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
                AssetDatabase.Refresh();
                AssetBaker.ConfigureImporters();
                Configure();
                EnsureScene();

                // Ruta absoluta anclada al proyecto (el CWD de Unity en batchmode no es fiable)
                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string buildsDir = Path.Combine(projectRoot, "Builds");
                Directory.CreateDirectory(buildsDir);
                string apkPath = Path.Combine(buildsDir, $"athanor-v{Athanor.GameVersion.Version}.apk");

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = apkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                };

                var report = BuildPipeline.BuildPlayer(options);
                bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded
                          && File.Exists(apkPath);
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

        /// Build AAB firmado para Google Play. Requiere release.keystore en la raíz
        /// (tools/crear-keystore.bat) y las variables de entorno ATHANOR_KEYSTORE_PASS
        /// y ATHANOR_KEY_PASS. Desactiva el actualizador de GitHub (símbolo PLAY_STORE).
        public static void BuildAab()
        {
            try
            {
                AssetDatabase.Refresh();
                AssetBaker.ConfigureImporters();
                Configure();
                EnsureScene();

                string projectRoot = Directory.GetParent(Application.dataPath).FullName;
                string keystore = Path.Combine(projectRoot, "release.keystore");
                string ksPass = Environment.GetEnvironmentVariable("ATHANOR_KEYSTORE_PASS");
                string keyPass = Environment.GetEnvironmentVariable("ATHANOR_KEY_PASS");
                if (!File.Exists(keystore) || string.IsNullOrEmpty(ksPass))
                {
                    Debug.LogError("[BuildScript] Falta release.keystore o ATHANOR_KEYSTORE_PASS. " +
                                   "Ver docs/PLAY_STORE.md §2-3.");
                    EditorApplication.Exit(1);
                    return;
                }

                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keystorePass = ksPass;
                PlayerSettings.Android.keyaliasName = "athanor";
                PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(keyPass) ? ksPass : keyPass;

                // Sin actualizador externo en la build de store
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, "PLAY_STORE");
                EditorUserBuildSettings.buildAppBundle = true;

                string aabPath = Path.Combine(projectRoot, "Builds",
                    $"athanor-v{Athanor.GameVersion.Version}.aab");
                Directory.CreateDirectory(Path.GetDirectoryName(aabPath));

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = aabPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                });
                bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded
                          && File.Exists(aabPath);
                Debug.Log($"[BuildScript] AAB: {report.summary.result}, salida: {aabPath}");
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

            EnsureAppIcon();
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.078f, 0.071f, 0.122f); // #14121F

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)23;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;

            EditorUserBuildSettings.buildAppBundle = false; // APK para sideload/releases
            // Sin símbolos residuales: PLAY_STORE solo lo agrega BuildAab después
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, string.Empty);

            AssetDatabase.SaveAssets();
        }

        const string IconPath = "Assets/Icon/app_icon.png";

        /// Genera el ícono de la app (matraz sobre fondo oscuro con aro ámbar) si no existe,
        /// y lo asigna como ícono por defecto. Reemplazable poniendo otro PNG en la misma ruta.
        static void EnsureAppIcon()
        {
            if (!File.Exists(IconPath))
            {
                Directory.CreateDirectory("Assets/Icon");
                File.WriteAllBytes(IconPath, DrawIconPng());
                AssetDatabase.ImportAsset(IconPath);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (tex != null)
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);
        }

        static byte[] DrawIconPng()
        {
            const int size = 512;
            var bg = new Color(0.133f, 0.118f, 0.20f);      // #221E33
            var amber = new Color(0.949f, 0.647f, 0.255f);  // #F2A541
            var violet = new Color(0.608f, 0.447f, 0.812f); // #9B72CF

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];

            const float cx = 256f, cy = 256f;
            const float corner = 90f;                        // esquinas redondeadas del fondo
            const float ringR = 208f, ringW = 9f;            // aro exterior
            const float flaskScale = 0.72f, flaskCy = 236f;  // matraz centrado abajo

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Fondo con esquinas redondeadas
                    float ex = Mathf.Max(Mathf.Abs(x - cx) - (256 - corner), 0);
                    float ey = Mathf.Max(Mathf.Abs(y - cy) - (256 - corner), 0);
                    float cornerDist = Mathf.Sqrt(ex * ex + ey * ey);
                    float bgA = Mathf.Clamp01(corner - cornerDist + 1);
                    var c = new Color(bg.r, bg.g, bg.b, bgA);

                    // Aro ámbar
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float ringA = Mathf.Clamp01(ringW - Mathf.Abs(d - ringR) + 1) * 0.85f;
                    if (ringA > 0) c = Color.Lerp(c, amber, ringA * bgA);

                    // Matraz (reutiliza la silueta del juego, escalada)
                    float srcY = (y - flaskCy) / flaskScale + 256f;
                    if (srcY >= 0 && srcY < 512)
                    {
                        float hw = Athanor.UI.UiTheme.FlaskHalfWidth((int)srcY) * flaskScale;
                        float dx = Mathf.Abs(x - cx);
                        float fa = Mathf.Clamp01(hw - dx + 1);
                        if (fa > 0)
                        {
                            var flaskColor = srcY < 240 ? violet : amber;
                            c = Color.Lerp(c, flaskColor, fa * bgA);
                        }
                    }

                    px[y * size + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            return png;
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
