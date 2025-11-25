using UnityEditor;

namespace NoSlimes.Util.DevCon.Editor
{
    [CustomEditor(typeof(DeveloperConsoleUI))]
    internal class DeveloperConsoleEditor : UnityEditor.Editor
    {
        #region Serialized Properties
        // Inputs
        private SerializedProperty inputSystemProp;
        private SerializedProperty toggleConsoleActionProp;
        private SerializedProperty autoCompleteActionProp;
        private SerializedProperty toggleConsoleKeyProp;
        private SerializedProperty autoCompleteKeyProp;
        private SerializedProperty commandSeparatorProp;

        // UI References
        private SerializedProperty consolePanelProp;
        private SerializedProperty inputFieldProp;
        private SerializedProperty scrollRectProp;
        private SerializedProperty consoleLogProp;
        private SerializedProperty maxLogLinesProp;

        // Behavior
        private SerializedProperty dontDestroyOnLoadProp;
        private SerializedProperty catchUnityLogsProp;
        private SerializedProperty controlCursorLockModeProp;

        // Customization
        private SerializedProperty bgColorProp;
        private SerializedProperty accentColorProp;
        private SerializedProperty textColorProp;
        private SerializedProperty warningColorProp;
        private SerializedProperty errorColorProp;
        private SerializedProperty assertColorProp;
        private SerializedProperty consoleFontProp;
        private SerializedProperty inputFontSizeProp;
        private SerializedProperty logFontSizeProp;
        #endregion

        // Editor State
        private bool showCustomization = false;

        private void OnEnable()
        {
            // Input
#if ENABLE_INPUT_SYSTEM
            inputSystemProp = serializedObject.FindProperty("inputSystem");
            toggleConsoleActionProp = serializedObject.FindProperty("toggleConsoleAction");
            autoCompleteActionProp = serializedObject.FindProperty("autoCompleteAction");
#endif
            toggleConsoleKeyProp = serializedObject.FindProperty("toggleConsoleKey");
            autoCompleteKeyProp = serializedObject.FindProperty("autoCompleteKey");
            commandSeparatorProp = serializedObject.FindProperty("commandSeparator");

            // UI
            consolePanelProp = serializedObject.FindProperty("consolePanel");
            inputFieldProp = serializedObject.FindProperty("inputField");
            scrollRectProp = serializedObject.FindProperty("scrollRect");
            consoleLogProp = serializedObject.FindProperty("consoleLog");
            maxLogLinesProp = serializedObject.FindProperty("maxLogLines");

            // Behavior
            dontDestroyOnLoadProp = serializedObject.FindProperty("dontDestroyOnLoad");
            catchUnityLogsProp = serializedObject.FindProperty("catchUnityLogs");
            controlCursorLockModeProp = serializedObject.FindProperty("controlCursorLockMode");

            // Customization
            bgColorProp = serializedObject.FindProperty("backgroundColor");
            accentColorProp = serializedObject.FindProperty("accentColor");
            textColorProp = serializedObject.FindProperty("textColor");
            warningColorProp = serializedObject.FindProperty("warningColor");
            errorColorProp = serializedObject.FindProperty("errorColor");
            assertColorProp = serializedObject.FindProperty("assertColor");
            consoleFontProp = serializedObject.FindProperty("consoleFont");
            inputFontSizeProp = serializedObject.FindProperty("inputFontSize");
            logFontSizeProp = serializedObject.FindProperty("logFontSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // --- Section: Input ---
            DrawSection("Input Settings", () =>
            {
#if ENABLE_INPUT_SYSTEM
                EditorGUILayout.PropertyField(inputSystemProp);

                DeveloperConsoleUI.InputSystemType selectedInputSystem = (DeveloperConsoleUI.InputSystemType)inputSystemProp.enumValueIndex;

                EditorGUILayout.Space(2);
                if (selectedInputSystem == DeveloperConsoleUI.InputSystemType.New)
                {
                    EditorGUILayout.PropertyField(toggleConsoleActionProp);
                    EditorGUILayout.PropertyField(autoCompleteActionProp);
                }
                else
                {
                    EditorGUILayout.PropertyField(toggleConsoleKeyProp);
                    EditorGUILayout.PropertyField(autoCompleteKeyProp);
                }
#else
                EditorGUILayout.PropertyField(toggleConsoleKeyProp);
                EditorGUILayout.PropertyField(autoCompleteKeyProp);
                EditorGUILayout.HelpBox("New Input System package not enabled in Project Settings.", MessageType.Warning);
#endif
                EditorGUILayout.Space(5);
                EditorGUILayout.PropertyField(commandSeparatorProp);
            });

            // --- Section: References ---
            DrawSection("UI References", () =>
            {
                EditorGUILayout.PropertyField(consolePanelProp);
                EditorGUILayout.PropertyField(inputFieldProp);
                EditorGUILayout.PropertyField(scrollRectProp);
                EditorGUILayout.PropertyField(consoleLogProp);
            });

            // --- Section: Configuration ---
            DrawSection("Configuration", () =>
            {
                EditorGUILayout.PropertyField(maxLogLinesProp);
                EditorGUILayout.PropertyField(dontDestroyOnLoadProp);
                EditorGUILayout.PropertyField(catchUnityLogsProp);
                EditorGUILayout.PropertyField(controlCursorLockModeProp);
            });

            // --- Section: Visuals (Foldout) ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            showCustomization = EditorGUILayout.Foldout(showCustomization, "Visual Customization", true);
            EditorGUI.indentLevel--;

            if (showCustomization)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(bgColorProp);
                EditorGUILayout.PropertyField(accentColorProp);
                EditorGUILayout.PropertyField(textColorProp);
                EditorGUILayout.PropertyField(warningColorProp);
                EditorGUILayout.PropertyField(errorColorProp);
                EditorGUILayout.PropertyField(assertColorProp);

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Typography", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(consoleFontProp);
                EditorGUILayout.PropertyField(inputFontSizeProp);
                EditorGUILayout.PropertyField(logFontSizeProp);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(5);
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws a contained inspector section with a bold header.
        /// </summary>
        private void DrawSection(string title, System.Action drawContent)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.indentLevel++;
            drawContent.Invoke();
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(3);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }
}