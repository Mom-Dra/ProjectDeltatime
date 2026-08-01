using System.Collections.Generic;
using Deltatime.Enemies;
using Deltatime.InputSystem;
using Deltatime.Player;
using Deltatime.Replay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.Level
{
    public sealed class StageController : MonoBehaviour
    {
        public enum StageState
        {
            Active,
            Cleared,
            Replaying,
            PlayerDead
        }

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private StageReplayController replay;

        private readonly HashSet<EnemyHealth> livingEnemies = new HashSet<EnemyHealth>();

        public StageState CurrentState { get; private set; } = StageState.Active;
        public int RemainingEnemyCount => livingEnemies.Count;
        public float RealPlayTime { get; private set; }
        public bool CombatAllowed => CurrentState == StageState.Active;

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died += HandlePlayerDied;
            }
        }

        private void Start()
        {
            ValidateConfiguration();
        }

        private void Update()
        {
            if (CurrentState == StageState.Active)
            {
                RealPlayTime += UnityEngine.Time.unscaledDeltaTime;
            }

            if (input != null && input.RestartPressed)
            {
                RestartStage();
            }

            if (CurrentState == StageState.Replaying &&
                input != null &&
                input.ReplayVisionTogglePressed &&
                replay != null)
            {
                replay.SetOmniscientView(
                    !replay.IsOmniscientViewEnabled);
            }
        }

        public void RegisterEnemy(EnemyHealth enemy)
        {
            if (enemy != null && enemy.IsAlive)
            {
                livingEnemies.Add(enemy);
            }
        }

        public void UnregisterEnemy(EnemyHealth enemy)
        {
            if (enemy != null)
            {
                livingEnemies.Remove(enemy);
            }
        }

        public void NotifyEnemyDied(EnemyHealth enemy)
        {
            livingEnemies.Remove(enemy);
            if (CurrentState == StageState.Active && livingEnemies.Count == 0)
            {
                CurrentState = StageState.Cleared;
                if (playerCombat != null)
                {
                    playerCombat.SetCombatEnabled(false);
                }

                if (replay != null && replay.RequestReplay())
                {
                    CurrentState = StageState.Replaying;
                }
            }
        }

        public void Configure(
            PlayerInputReader inputReader,
            PlayerHealth health,
            PlayerCombat combat,
            StageReplayController replayController)
        {
            input = inputReader;
            playerHealth = health;
            playerCombat = combat;
            replay = replayController;
        }

        private void HandlePlayerDied()
        {
            if (CurrentState != StageState.Active)
            {
                return;
            }

            CurrentState = StageState.PlayerDead;
            if (playerCombat != null)
            {
                playerCombat.SetCombatEnabled(false);
            }
        }

        private void RestartStage()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
            else
            {
                SceneManager.LoadScene(activeScene.name);
            }
        }

        private void ValidateConfiguration()
        {
            if (input == null ||
                playerHealth == null ||
                playerCombat == null ||
                replay == null)
            {
                Debug.LogError($"{nameof(StageController)} is missing required references.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
            }
        }
    }
}
