using System;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// Keeps every scene-building tool aligned with the playable scene route.
    /// </summary>
    public static class GameBuildSceneCatalog
    {
        public const string TutorialScenePath =
            "Assets/_Project/Scenes/TutorialRework/Tutorial.unity";

        private static readonly string[] OrderedScenePaths =
        {
            "Assets/_Project/Scenes/MainScene.unity",
            TutorialScenePath,
            "Assets/_Project/Scenes/Stage1.unity",
            "Assets/_Project/Scenes/Stage2.unity",
            "Assets/_Project/Scenes/StageBattingCage.unity",
            "Assets/_Project/Scenes/Stage5.unity",
            "Assets/_Project/Scenes/EndingScene.unity"
        };

        public static void Apply()
        {
            EditorBuildSettingsScene[] scenes =
                new EditorBuildSettingsScene[OrderedScenePaths.Length];
            for (int i = 0; i < OrderedScenePaths.Length; i++)
            {
                string scenePath = OrderedScenePaths[i];
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    throw new InvalidOperationException(
                        $"Required playable scene is missing: {scenePath}");
                }

                scenes[i] = new EditorBuildSettingsScene(scenePath, true);
            }

            EditorBuildSettings.scenes = scenes;
        }

        public static void Validate()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length != OrderedScenePaths.Length)
            {
                throw new InvalidOperationException(
                    "Build Settings must contain only the active scene route.");
            }

            for (int i = 0; i < OrderedScenePaths.Length; i++)
            {
                if (!scenes[i].enabled || scenes[i].path != OrderedScenePaths[i])
                {
                    throw new InvalidOperationException(
                        $"Build index {i} is '{scenes[i].path}'; " +
                        $"expected '{OrderedScenePaths[i]}'.");
                }
            }
        }
    }
}
