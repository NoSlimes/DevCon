using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NoSlimes.Util.DevCon
{
    public class ConsoleCommandCache : ScriptableObject
    {
        public bool ExcludeBuiltInCommands = false;
        public CommandEntry[] Commands;

        private void OnEnable()
        {
#if UNITY_EDITOR
            ExcludeBuiltInCommands = EditorPrefs.GetBool("DevCon_ExcludeBuiltInCommands", false);
#endif
        }

        [Serializable]
        public class CommandEntry
        {
            public string CommandName;
            public string Description;
            public string DeclaringType;
            public string MethodName;
            public string[] ParameterTypes;
        }

    }
}