using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// Produces a conservative, read-only asset dependency report. Candidates
    /// are never deleted automatically and all saved project scenes, Resources,
    /// input assets and literal asset paths used by editor builders are roots.
    /// </summary>
    public static class ProjectAssetDependencyAudit
    {
        private const string ProjectAssetRoot = "Assets/_Project";
        private const string ReportPath = "Logs/Validation/AssetDependencyAudit.txt";

        private static readonly Regex AssetPathLiteral = new Regex(
            "\\\"(Assets/[^\\\"]+)\\\"",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> CandidateExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".anim", ".asset", ".controller", ".fbx", ".mat", ".mp3",
                ".ogg", ".overridecontroller", ".png", ".prefab", ".psd",
                ".shader", ".shadergraph", ".tga", ".wav"
            };

        [MenuItem("Tools/Project Quality/Generate Asset Dependency Audit")]
        public static void GenerateFromMenu()
        {
            string fullPath = GenerateReport();
            Debug.Log($"Asset dependency audit written to {fullPath}.");
        }

        public static void RunFromCommandLine()
        {
            try
            {
                GenerateReport();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static string GenerateReport()
        {
            SortedSet<string> roots = CollectDependencyRoots();
            string[] dependencies = AssetDatabase.GetDependencies(
                roots.ToArray(),
                true);
            HashSet<string> referenced = new HashSet<string>(
                dependencies,
                StringComparer.OrdinalIgnoreCase);

            List<string> candidates = CollectCandidates(referenced, roots);
            List<string> missingLiteralPaths = CollectMissingBuilderLiteralPaths();
            List<string> orphanMetaFiles = CollectOrphanMetaFiles();

            string fullPath = Path.GetFullPath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(
                fullPath,
                BuildReport(
                    roots,
                    referenced,
                    candidates,
                    missingLiteralPaths,
                    orphanMetaFiles),
                new UTF8Encoding(false));
            return fullPath;
        }

        private static SortedSet<string> CollectDependencyRoots()
        {
            SortedSet<string> roots = new SortedSet<string>(
                StringComparer.OrdinalIgnoreCase);

            AddAssetsUnder("Assets/_Project/Scenes", roots);
            AddAssetsUnder("Assets/_Project/Resources", roots);
            AddAssetsUnder("Assets/_Project/Input", roots);

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            for (int index = 0; index < buildScenes.Length; index++)
            {
                AddExistingAsset(buildScenes[index].path, roots);
            }

            UnityEngine.Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            for (int index = 0; index < preloadedAssets.Length; index++)
            {
                AddExistingAsset(
                    AssetDatabase.GetAssetPath(preloadedAssets[index]),
                    roots);
            }

            foreach (string literalPath in CollectBuilderLiteralPaths())
            {
                AddExistingAsset(literalPath, roots);
            }

            return roots;
        }

        private static void AddAssetsUnder(
            string folder,
            ISet<string> destination)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });
            for (int index = 0; index < guids.Length; index++)
            {
                AddExistingAsset(AssetDatabase.GUIDToAssetPath(guids[index]), destination);
            }
        }

        private static void AddExistingAsset(
            string assetPath,
            ISet<string> destination)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                AssetDatabase.IsValidFolder(assetPath) ||
                AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            {
                return;
            }

            destination.Add(assetPath.Replace('\\', '/'));
        }

        private static IEnumerable<string> CollectBuilderLiteralPaths()
        {
            string editorFolder = Path.Combine(
                Application.dataPath,
                "_Project/Scripts/Editor");
            if (!Directory.Exists(editorFolder))
            {
                yield break;
            }

            foreach (string file in Directory.EnumerateFiles(
                         editorFolder,
                         "*.cs",
                         SearchOption.TopDirectoryOnly))
            {
                string source = File.ReadAllText(file);
                MatchCollection matches = AssetPathLiteral.Matches(source);
                for (int index = 0; index < matches.Count; index++)
                {
                    yield return matches[index].Groups[1].Value.Replace('\\', '/');
                }
            }
        }

        private static List<string> CollectMissingBuilderLiteralPaths()
        {
            return CollectBuilderLiteralPaths()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(path =>
                    !AssetDatabase.IsValidFolder(path) &&
                    AssetDatabase.LoadMainAssetAtPath(path) == null)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> CollectCandidates(
            ISet<string> referenced,
            ISet<string> roots)
        {
            List<string> candidates = new List<string>();
            string[] guids = AssetDatabase.FindAssets(
                string.Empty,
                new[] { ProjectAssetRoot });
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                if (AssetDatabase.IsValidFolder(path) ||
                    roots.Contains(path) ||
                    referenced.Contains(path) ||
                    !CandidateExtensions.Contains(Path.GetExtension(path)))
                {
                    continue;
                }

                candidates.Add(path);
            }

            candidates.Sort(StringComparer.OrdinalIgnoreCase);
            return candidates;
        }

        private static List<string> CollectOrphanMetaFiles()
        {
            string projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, ".."));
            List<string> orphanMetaFiles = new List<string>();
            foreach (string metaPath in Directory.EnumerateFiles(
                         Application.dataPath,
                         "*.meta",
                         SearchOption.AllDirectories))
            {
                string assetPath = metaPath.Substring(0, metaPath.Length - 5);
                if (File.Exists(assetPath) || Directory.Exists(assetPath))
                {
                    continue;
                }

                orphanMetaFiles.Add(
                    Path.GetRelativePath(projectRoot, metaPath).Replace('\\', '/'));
            }

            orphanMetaFiles.Sort(StringComparer.OrdinalIgnoreCase);
            return orphanMetaFiles;
        }

        private static string BuildReport(
            IReadOnlyCollection<string> roots,
            IReadOnlyCollection<string> referenced,
            IReadOnlyCollection<string> candidates,
            IReadOnlyCollection<string> missingLiteralPaths,
            IReadOnlyCollection<string> orphanMetaFiles)
        {
            StringBuilder builder = new StringBuilder(4096);
            builder.AppendLine("ProjectDeltatime Asset Dependency Audit");
            builder.AppendLine($"Generated (UTC): {DateTime.UtcNow:O}");
            builder.AppendLine("Deletion policy: report only; no asset is removed by this tool.");
            builder.AppendLine($"Dependency roots: {roots.Count}");
            builder.AppendLine($"Resolved dependencies: {referenced.Count}");
            AppendSection(builder, "Unreferenced candidates", candidates);
            AppendSection(builder, "Missing builder literal paths", missingLiteralPaths);
            AppendSection(builder, "Orphan .meta files", orphanMetaFiles);
            return builder.ToString();
        }

        private static void AppendSection(
            StringBuilder builder,
            string title,
            IReadOnlyCollection<string> values)
        {
            builder.AppendLine();
            builder.AppendLine($"[{title}] ({values.Count})");
            foreach (string value in values)
            {
                builder.AppendLine(value);
            }
        }
    }
}
