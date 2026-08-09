using Deltatime.Combat;
using Deltatime.Visuals;
using UnityEditor;
using UnityEngine;

namespace Deltatime.EditorTools
{
    /// <summary>
    /// Lets artists tune the held, world, and muzzle transforms of the
    /// current weapon models while the calibration scene is running.
    /// </summary>
    public sealed class WeaponModelCalibrationWindow : EditorWindow
    {
        private static readonly string[] DefinitionPaths =
        {
            "Assets/_Project/Pistol.asset",
            "Assets/_Project/AutomaticRifle.asset",
            "Assets/_Project/Shotgun.asset",
            "Assets/_Project/MeleeWeapon.asset"
        };

        private WeaponDefinition[] definitions;
        private string[] definitionNames;
        private WeaponDefinition selectedDefinition;
        private SerializedObject serializedDefinition;
        private int selectedIndex;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Prototype/Animation/Calibrate Weapon Models")]
        public static void Open()
        {
            GetWindow<WeaponModelCalibrationWindow>(
                "Weapon Model Calibration");
        }

        private void OnEnable()
        {
            LoadDefinitions();
            SelectDefinitionFromSelection();
        }

        private void OnSelectionChange()
        {
            SelectDefinitionFromSelection();
            Repaint();
        }

        private void OnGUI()
        {
            LoadDefinitions();
            EditorGUILayout.LabelField(
                "Weapon Model Calibration",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "WeaponCalibration을 Play한 뒤 무기를 선택하고 값을 조절하세요. " +
                "변경은 WeaponDefinition 에셋에 즉시 저장되며, Scene Gizmos의 " +
                "청록색 점/선은 실제 발사 총구입니다. 총구 회전은 모델 정렬용입니다.",
                MessageType.Info);

            DrawWeaponSelection();
            if (selectedDefinition == null)
            {
                return;
            }

            DrawPreviewControls();
            DrawTransformFields();
        }

        private void DrawWeaponSelection()
        {
            if (definitions == null || definitions.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "WeaponDefinition 에셋을 찾지 못했습니다.",
                    MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();
            int requestedIndex = EditorGUILayout.Popup(
                "Weapon",
                selectedIndex,
                definitionNames);
            WeaponDefinition requestedDefinition =
                (WeaponDefinition)EditorGUILayout.ObjectField(
                    "Definition",
                    selectedDefinition,
                    typeof(WeaponDefinition),
                    false);
            if (EditorGUI.EndChangeCheck())
            {
                if (requestedDefinition != selectedDefinition)
                {
                    SetSelectedDefinition(requestedDefinition);
                }
                else if (requestedIndex != selectedIndex)
                {
                    SetSelectedDefinition(definitions[requestedIndex]);
                }
            }
        }

        private void DrawPreviewControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = Application.isPlaying;
                if (GUILayout.Button("플레이어에 장착"))
                {
                    EquipOnPlayer();
                }

                if (GUILayout.Button("라이브 미리보기 갱신"))
                {
                    RefreshLivePreviews();
                }

                GUI.enabled = true;
                if (GUILayout.Button("에셋 저장"))
                {
                    AssetDatabase.SaveAssets();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "적이 없는 WeaponCalibration Play Mode에서 안전하게 확인할 수 있습니다.",
                    MessageType.None);
            }
        }

        private void DrawTransformFields()
        {
            if (serializedDefinition == null)
            {
                return;
            }

            serializedDefinition.Update();
            EditorGUI.BeginChangeCheck();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("오른손 모델", EditorStyles.boldLabel);
            DrawVectorProperty("위치", "heldModelLocalPosition");
            DrawVectorProperty("회전", "heldModelLocalEulerAngles");
            DrawVectorProperty("크기", "heldModelLocalScale");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("실제 발사 총구", EditorStyles.boldLabel);
            DrawVectorProperty("모델 내부 위치", "heldMuzzleLocalPosition");
            DrawVectorProperty("모델 내부 회전", "heldMuzzleLocalEulerAngles");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("바닥·투척·공중 드롭 모델", EditorStyles.boldLabel);
            DrawVectorProperty("위치", "worldModelLocalPosition");
            DrawVectorProperty("회전", "worldModelLocalEulerAngles");
            DrawVectorProperty("크기", "worldModelLocalScale");

            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                serializedDefinition.ApplyModifiedProperties();
                selectedDefinition.MarkModelVisualsCalibrated();
                EditorUtility.SetDirty(selectedDefinition);
                AssetDatabase.SaveAssets();
                RefreshLivePreviews();
                SceneView.RepaintAll();
            }
        }

        private void DrawVectorProperty(string label, string propertyName)
        {
            SerializedProperty property =
                serializedDefinition.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
        }

        private void EquipOnPlayer()
        {
            GameObject player = GameObject.Find("Player");
            WeaponController weapon = player == null
                ? null
                : player.GetComponent<WeaponController>();
            if (weapon == null)
            {
                ShowNotification(new GUIContent("Player를 찾지 못했습니다."));
                return;
            }

            weapon.Equip(
                selectedDefinition,
                selectedDefinition.AmmunitionCapacity);
            WeaponVisualPresenter presenter =
                weapon.GetComponent<WeaponVisualPresenter>();
            presenter?.RefreshVisual();
            Selection.activeGameObject = player;
            SceneView.RepaintAll();
        }

        private void RefreshLivePreviews()
        {
            WeaponVisualPresenter[] presenters =
                FindObjectsByType<WeaponVisualPresenter>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            for (int i = 0; i < presenters.Length; i++)
            {
                WeaponController weapon =
                    presenters[i].GetComponent<WeaponController>();
                if (weapon != null && weapon.Definition == selectedDefinition)
                {
                    presenters[i].RefreshVisual();
                }
            }

            SceneView.RepaintAll();
        }

        private void LoadDefinitions()
        {
            if (definitions != null && definitions.Length == DefinitionPaths.Length)
            {
                return;
            }

            definitions = new WeaponDefinition[DefinitionPaths.Length];
            definitionNames = new string[DefinitionPaths.Length];
            for (int i = 0; i < DefinitionPaths.Length; i++)
            {
                definitions[i] = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    DefinitionPaths[i]);
                definitionNames[i] = definitions[i] == null
                    ? DefinitionPaths[i]
                    : definitions[i].DisplayName;
            }

            if (selectedDefinition == null)
            {
                SetSelectedDefinition(definitions[0]);
            }
        }

        private void SelectDefinitionFromSelection()
        {
            WeaponDefinition selected = Selection.activeObject as WeaponDefinition;
            if (selected != null)
            {
                SetSelectedDefinition(selected);
            }
        }

        private void SetSelectedDefinition(WeaponDefinition definition)
        {
            selectedDefinition = definition;
            serializedDefinition = definition == null
                ? null
                : new SerializedObject(definition);
            if (definitions == null)
            {
                return;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] == definition)
                {
                    selectedIndex = i;
                    return;
                }
            }
        }
    }
}
