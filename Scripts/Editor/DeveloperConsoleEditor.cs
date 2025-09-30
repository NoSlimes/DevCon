using UnityEditor;
using UnityEngine;

namespace NoSlimes.Util.DeveloperConsole
{
    [CustomEditor(typeof(DeveloperConsole))]
    public class DeveloperConsoleEditor : UnityEditor.Editor
    {
        private SerializedProperty inputSystemProp;
        private SerializedProperty toggleConsoleActionProp;
        private SerializedProperty toggleConsoleKeyProp;
        private SerializedProperty consolePanelProp;
        private SerializedProperty inputFieldProp;
        private SerializedProperty scrollRectProp;
        private SerializedProperty consoleLogProp;
        private SerializedProperty maxLogLinesProp;
        private SerializedProperty dontDestroyOnLoadProp;
        private SerializedProperty controlCursorLockModeProp;

        private void OnEnable()
        {
            inputSystemProp = serializedObject.FindProperty("inputSystem");

#if ENABLE_INPUT_SYSTEM
            toggleConsoleActionProp = serializedObject.FindProperty("toggleConsoleAction");
#endif

            toggleConsoleKeyProp = serializedObject.FindProperty("toggleConsoleKey");
            consolePanelProp = serializedObject.FindProperty("consolePanel");
            inputFieldProp = serializedObject.FindProperty("inputField");
            scrollRectProp = serializedObject.FindProperty("scrollRect");
            consoleLogProp = serializedObject.FindProperty("consoleLog");
            maxLogLinesProp = serializedObject.FindProperty("maxLogLines");
            dontDestroyOnLoadProp = serializedObject.FindProperty("dontDestroyOnLoad");
            controlCursorLockModeProp = serializedObject.FindProperty("controlCursorLockMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Input Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(inputSystemProp);

            DeveloperConsole.InputSystemType selectedInputSystem = (DeveloperConsole.InputSystemType)inputSystemProp.enumValueIndex;

#if ENABLE_INPUT_SYSTEM
            switch (selectedInputSystem)
            {
                case DeveloperConsole.InputSystemType.New:
                    EditorGUILayout.PropertyField(toggleConsoleActionProp);
                    break;

                case DeveloperConsole.InputSystemType.Old:
                    EditorGUILayout.PropertyField(toggleConsoleKeyProp);
                    break;
            }
#else
            EditorGUILayout.PropertyField(toggleConsoleKeyProp);
#endif

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("UI & Logging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(consolePanelProp);
            EditorGUILayout.PropertyField(inputFieldProp);
            EditorGUILayout.PropertyField(scrollRectProp);
            EditorGUILayout.PropertyField(consoleLogProp);
            EditorGUILayout.PropertyField(maxLogLinesProp);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Console Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(dontDestroyOnLoadProp);
            EditorGUILayout.PropertyField(controlCursorLockModeProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
