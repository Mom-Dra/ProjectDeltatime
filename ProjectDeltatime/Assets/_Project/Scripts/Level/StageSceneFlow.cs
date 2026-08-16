using UnityEngine.SceneManagement;

namespace Deltatime.Level
{
    /// <summary>
    /// Defines the playable stage route independently of the Build Settings index.
    /// </summary>
    public static class StageSceneFlow
    {
        public const string MainSceneName = "MainScene";
        public const string EndingSceneName = "EndingScene";

        private static readonly string[] PlayableStageNames =
        {
            "Stage1",
            "Stage2",
            "Stage5"
        };

        private static readonly string[] DisplayStageNames =
        {
            "Stage1",
            "Stage2",
            "Stage5",
            "Stage6"
        };

        public static bool TryGetNextDestination(
            string currentSceneName,
            out string destinationSceneName)
        {
            for (int i = 0; i < PlayableStageNames.Length; i++)
            {
                if (PlayableStageNames[i] != currentSceneName)
                {
                    continue;
                }

                destinationSceneName = i + 1 < PlayableStageNames.Length
                    ? PlayableStageNames[i + 1]
                    : EndingSceneName;
                return true;
            }

            destinationSceneName = string.Empty;
            return false;
        }

        public static bool IsPlayableStage(Scene scene)
        {
            for (int i = 0; i < PlayableStageNames.Length; i++)
            {
                if (PlayableStageNames[i] == scene.name)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetDisplayStageNumber(
            string sceneName,
            out int stageNumber)
        {
            for (int i = 0; i < DisplayStageNames.Length; i++)
            {
                if (DisplayStageNames[i] != sceneName)
                {
                    continue;
                }

                stageNumber = i + 1;
                return true;
            }

            stageNumber = 0;
            return false;
        }
    }
}
