using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BaitSwitch.EditorTools
{
    /// <summary>
    /// Tools > Bait & Switch > Build Linux Server / Build Windows Client.
    /// Server: Linux Dedicated Server (Mono), headless, auto-starts Mirror on UDP 7777 (Builds/Server).
    /// Client: Windows 64-bit (Builds/Windows/BaitAndSwitch.exe). Both use Assets/Scenes/Sea.unity.
    /// </summary>
    public static class BuildTools
    {
        const string Scene = "Assets/Scenes/Sea.unity";

        [MenuItem("Tools/Bait & Switch/Build Linux Server")]
        public static void BuildServer() => Run(BuildTarget.StandaloneLinux64, (int)StandaloneBuildSubtarget.Server,
                                                "Builds/Server/BaitSwitchServer.x86_64", BuildOptions.None);

        [MenuItem("Tools/Bait & Switch/Build Windows Client")]
        public static void BuildClient() => Run(BuildTarget.StandaloneWindows64, (int)StandaloneBuildSubtarget.Player,
                                                "Builds/Windows/BaitAndSwitch.exe", BuildOptions.None);

        /// <summary>
        /// The browser client (Builds/WebGL). The Linux server image carries it in app/web and serves it itself, so an
        /// Edgegap deployment's https://fqdn:port/ address is a playable link. Afterwards the editor goes back to the
        /// Windows player target.
        /// </summary>
        [MenuItem("Tools/Bait & Switch/Build WebGL (browser)")]
        public static void BuildWebGL()
        {
            // gzip (~4x smaller download); WebHost serves *.gz with Content-Encoding: gzip so the browser unpacks it
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.template = "PROJECT:BaitSwitch";   // full-window canvas + our loading screen
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.defaultWebScreenWidth = 1600;
            PlayerSettings.defaultWebScreenHeight = 900;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
            Run(BuildTarget.WebGL, 0, "Builds/WebGL", BuildOptions.None);
            RestorePlayer();
        }

        static void Run(BuildTarget target, int subtarget, string path, BuildOptions options)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PlayerSettings.companyName = "Overpowered Jam";
            PlayerSettings.productName = "Bait & Switch";
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = path,
                target = target,
                subtarget = subtarget,
                options = options,
            });
            var s = report.summary;
            Debug.Log($"[Build] {target} {s.result} in {s.totalTime.TotalSeconds:F0}s, {s.totalSize / (1024 * 1024)} MB, errors {s.totalErrors} → {path}");
            // a server build leaves UNITY_SERVER defined in the editor (the front end then disables itself): go back
            if (subtarget == (int)StandaloneBuildSubtarget.Server) RestorePlayer();
        }

        [MenuItem("Tools/Bait & Switch/Restore Windows Player Target")]
        public static void RestorePlayer()
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        }
    }
}
