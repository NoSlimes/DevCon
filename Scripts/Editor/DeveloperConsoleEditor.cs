using UnityEditor;
using UnityEngine;

namespace NoSlimes.Util.DevCon
{
    [CustomEditor(typeof(DeveloperConsoleUI))]
    public class DeveloperConsoleEditor : UnityEditor.Editor
    {
        private bool lastExcludeBuiltinValue;

        private SerializedProperty inputSystemProp;
        private SerializedProperty toggleConsoleActionProp;
        private SerializedProperty autoCompleteActionProp;
        private SerializedProperty toggleConsoleKeyProp;
        private SerializedProperty autoCompleteKeyProp;
        private SerializedProperty consolePanelProp;
        private SerializedProperty inputFieldProp;
        private SerializedProperty scrollRectProp;
        private SerializedProperty consoleLogProp;
        private SerializedProperty maxLogLinesProp;
        private SerializedProperty dontDestroyOnLoadProp;
        private SerializedProperty catchUnityLogsProp;
        private SerializedProperty controlCursorLockModeProp;
        private SerializedProperty commandSeparatorProp;

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            inputSystemProp = serializedObject.FindProperty("inputSystem");

            toggleConsoleActionProp = serializedObject.FindProperty("toggleConsoleAction");
            autoCompleteActionProp = serializedObject.FindProperty("autoCompleteAction");

#endif

            toggleConsoleKeyProp = serializedObject.FindProperty("toggleConsoleKey");
            autoCompleteKeyProp = serializedObject.FindProperty("autoCompleteKey");
            consolePanelProp = serializedObject.FindProperty("consolePanel");
            inputFieldProp = serializedObject.FindProperty("inputField");
            scrollRectProp = serializedObject.FindProperty("scrollRect");
            consoleLogProp = serializedObject.FindProperty("consoleLog");
            maxLogLinesProp = serializedObject.FindProperty("maxLogLines");
            dontDestroyOnLoadProp = serializedObject.FindProperty("dontDestroyOnLoad");
            catchUnityLogsProp = serializedObject.FindProperty("catchUnityLogs");
            controlCursorLockModeProp = serializedObject.FindProperty("controlCursorLockMode");
            commandSeparatorProp = serializedObject.FindProperty("commandSeparator");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Input Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(inputSystemProp);

            DeveloperConsoleUI.InputSystemType selectedInputSystem = (DeveloperConsoleUI.InputSystemType)inputSystemProp.enumValueIndex;

#if ENABLE_INPUT_SYSTEM
            switch (selectedInputSystem)
            {
                case DeveloperConsoleUI.InputSystemType.New:
                    EditorGUILayout.PropertyField(toggleConsoleActionProp);
                    EditorGUILayout.PropertyField(autoCompleteActionProp);
                    break;

                case DeveloperConsoleUI.InputSystemType.Old:
                    EditorGUILayout.PropertyField(toggleConsoleKeyProp);
                    EditorGUILayout.PropertyField(autoCompleteKeyProp);
                    break;
            }
#else
            EditorGUILayout.PropertyField(toggleConsoleKeyProp);
            EditorGUILayout.PropertyField(autoCompleteKeyProp);
            EditorGUILayout.HelpBox("The new Input System package is not enabled. Please enable it in Project Settings to use new input features.", MessageType.Info);
#endif

            EditorGUILayout.PropertyField(commandSeparatorProp);

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
            EditorGUILayout.PropertyField(catchUnityLogsProp);
            EditorGUILayout.PropertyField(controlCursorLockModeProp);

            EditorGUILayout.Space(10);

            var so = Resources.Load<ConsoleCommandCache>("DevCon/ConsoleCommandCache");
            if (so != null)
            {
                bool newValue = EditorGUILayout.Toggle("Exclude Built-In Commands from Cache", so.ExcludeBuiltInCommands);

                if (newValue != so.ExcludeBuiltInCommands)
                {
                    so.ExcludeBuiltInCommands = newValue;   
                    lastExcludeBuiltinValue = newValue;   

                    EditorUtility.SetDirty(so);          
                    AssetDatabase.SaveAssets();           
                }
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}
