#if UNITY_EDITOR
using System.Collections.Generic;
using Deltatime.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Deltatime.Tests.EditMode
{
    public sealed class EnemyMovementBalanceTests
    {
        private const float RangedMoveSpeed = 4.25f;
        private const float RangedRotationSpeed = 275f;
        private const float MeleeMoveSpeed = 6f;
        private const float MeleeRotationSpeed = 325f;

        private static readonly SceneExpectation[] ActiveScenes =
        {
            new SceneExpectation(
                "Assets/_Project/Scenes/TutorialRework/Tutorial.unity",
                3,
                2),
            new SceneExpectation(
                "Assets/_Project/Scenes/Stage1.unity",
                2,
                1),
            new SceneExpectation(
                "Assets/_Project/Scenes/Stage2.unity",
                2,
                1),
            new SceneExpectation(
                "Assets/_Project/Scenes/StageBattingCage.unity",
                0,
                6),
            new SceneExpectation(
                "Assets/_Project/Scenes/Stage5.unity",
                3,
                2)
        };

        [Test]
        public void ActiveScenes_UseRaisedMovementBalanceForEveryEnemy()
        {
            for (int i = 0; i < ActiveScenes.Length; i++)
            {
                ValidateScene(ActiveScenes[i]);
            }
        }

        private static void ValidateScene(SceneExpectation expectation)
        {
            Scene scene = SceneManager.GetSceneByPath(expectation.Path);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
            {
                scene = EditorSceneManager.OpenScene(
                    expectation.Path,
                    OpenSceneMode.Additive);
            }

            try
            {
                ValidateSceneContents(scene, expectation);
            }
            finally
            {
                if (openedForValidation)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateSceneContents(
            Scene scene,
            SceneExpectation expectation)
        {
            List<EnemyMotor> motors = new List<EnemyMotor>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                motors.AddRange(
                    roots[i].GetComponentsInChildren<EnemyMotor>(true));
            }

            int rangedCount = 0;
            int meleeCount = 0;
            for (int i = 0; i < motors.Count; i++)
            {
                EnemyMotor motor = motors[i];
                bool isRanged = motor.GetComponent<EnemyShooter>() != null;
                bool isMelee = motor.GetComponent<EnemyChaser>() != null;
                Assert.That(
                    isRanged ^ isMelee,
                    Is.True,
                    $"{expectation.Path}: {motor.name} has an unclassified " +
                    "or conflicting enemy role.");

                SerializedObject settings = new SerializedObject(motor);
                settings.Update();
                SerializedProperty rotationSpeed =
                    settings.FindProperty("rotationSpeed");
                Assert.That(
                    rotationSpeed,
                    Is.Not.Null,
                    $"{expectation.Path}: {motor.name} has no rotationSpeed.");

                if (isRanged)
                {
                    rangedCount++;
                    AssertBalance(
                        expectation.Path,
                        motor,
                        rotationSpeed.floatValue,
                        RangedMoveSpeed,
                        RangedRotationSpeed);
                }
                else
                {
                    meleeCount++;
                    AssertBalance(
                        expectation.Path,
                        motor,
                        rotationSpeed.floatValue,
                        MeleeMoveSpeed,
                        MeleeRotationSpeed);
                }
            }

            Assert.That(
                rangedCount,
                Is.EqualTo(expectation.RangedCount),
                expectation.Path + ": ranged enemy count changed.");
            Assert.That(
                meleeCount,
                Is.EqualTo(expectation.MeleeCount),
                expectation.Path + ": melee enemy count changed.");
            Assert.That(
                motors.Count,
                Is.EqualTo(expectation.RangedCount + expectation.MeleeCount),
                expectation.Path + ": EnemyMotor count changed.");
        }

        private static void AssertBalance(
            string scenePath,
            EnemyMotor motor,
            float actualRotationSpeed,
            float expectedMoveSpeed,
            float expectedRotationSpeed)
        {
            Assert.That(
                motor.MoveSpeed,
                Is.EqualTo(expectedMoveSpeed).Within(0.0001f),
                $"{scenePath}: {motor.name} move speed changed.");
            Assert.That(
                actualRotationSpeed,
                Is.EqualTo(expectedRotationSpeed).Within(0.0001f),
                $"{scenePath}: {motor.name} rotation speed changed.");
        }

        private readonly struct SceneExpectation
        {
            public SceneExpectation(
                string path,
                int rangedCount,
                int meleeCount)
            {
                Path = path;
                RangedCount = rangedCount;
                MeleeCount = meleeCount;
            }

            public string Path { get; }
            public int RangedCount { get; }
            public int MeleeCount { get; }
        }
    }
}
#endif
