using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GordonMann.EditorTools
{
    public static class GordonWindowsBuild
    {
        internal const string PlaytestFolder = "Builds/Gordon Mann - The Vault Job";
        const BuildOptions CompressionFlags = BuildOptions.CompressWithLz4 | BuildOptions.CompressWithLz4HC;

        [MenuItem("Tools/Scene Builder/Build Gordon Mann Windows Playtest")]
        public static void ChooseDestination()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string parent = EditorUtility.OpenFolderPanel("Choose a parent folder for the playtest", "", "");
            if (string.IsNullOrEmpty(parent)) return;
            string folder = Path.Combine(parent, "Gordon Mann - The Vault Job");
            Build(folder,BuildOptions.None);
            EditorUtility.RevealInFinder(Path.Combine(folder,"Gordon Mann.exe"));
        }

        public static void BuildPlaytest() => Build(PlaytestFolder,BuildOptions.None);

        internal static void Build(string folder,BuildOptions options)
        {
            string output = Path.GetFullPath(folder);
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                throw new IOException($"The output folder already contains files. Choose a new folder: {output}");
            string[] scenes = VaultJobBlockout.VaultSceneList(VaultJobBlockout.BlockoutScenes)
                .Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || Path.GetFileNameWithoutExtension(scenes[0]) != "MainMenu")
                throw new InvalidOperationException("The Vault Job build must begin with its title screen.");
            Directory.CreateDirectory(output);
            if ((options & CompressionFlags) == 0) options |= BuildOptions.CompressWithLz4HC;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(output,"Gordon Mann.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = options
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Windows build {report.summary.result}, {report.summary.totalErrors} errors. Output: {output}");
            FreeAssetIntegration.PackageCredits(output);
            Debug.Log($"Gordon Mann Windows build succeeded. {scenes.Length} scenes, {report.summary.totalSize} bytes. Output: {output}");
        }
    }
}

