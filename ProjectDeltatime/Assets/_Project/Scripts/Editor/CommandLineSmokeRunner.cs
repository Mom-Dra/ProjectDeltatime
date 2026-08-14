using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// Owns the editor callback lifetime shared by command-line PlayMode smoke
    /// tests. Test-specific phases and assertions remain in each test class.
    /// </summary>
    internal sealed class CommandLineSmokeRunner
    {
        private readonly string runningKey;
        private readonly EditorApplication.CallbackFunction update;
        private readonly Action<PlayModeStateChange> playModeChanged;
        private readonly Application.LogCallback logMessageReceived;
        private bool attached;

        public CommandLineSmokeRunner(
            string runningKey,
            EditorApplication.CallbackFunction update,
            Action<PlayModeStateChange> playModeChanged,
            Application.LogCallback logMessageReceived = null)
        {
            this.runningKey = runningKey;
            this.update = update ?? throw new ArgumentNullException(nameof(update));
            this.playModeChanged = playModeChanged ??
                throw new ArgumentNullException(nameof(playModeChanged));
            this.logMessageReceived = logMessageReceived;
        }

        public bool IsRunning => SessionState.GetBool(runningKey, false);

        public void OpenSceneAndEnterPlayMode(string scenePath)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Attach();
            EditorApplication.EnterPlaymode();
        }

        public void ResumeSceneAndEnterPlayMode(string scenePath)
        {
            if (!IsRunning || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            OpenSceneAndEnterPlayMode(scenePath);
        }

        public void Attach()
        {
            if (attached)
            {
                return;
            }

            attached = true;
            EditorApplication.update -= update;
            EditorApplication.update += update;
            EditorApplication.playModeStateChanged -= playModeChanged;
            EditorApplication.playModeStateChanged += playModeChanged;
            if (logMessageReceived != null)
            {
                Application.logMessageReceived -= logMessageReceived;
                Application.logMessageReceived += logMessageReceived;
            }
        }

        public void Detach()
        {
            if (!attached)
            {
                return;
            }

            attached = false;
            EditorApplication.update -= update;
            EditorApplication.playModeStateChanged -= playModeChanged;
            if (logMessageReceived != null)
            {
                Application.logMessageReceived -= logMessageReceived;
            }
        }
    }
}
