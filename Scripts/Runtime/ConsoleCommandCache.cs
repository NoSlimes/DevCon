using System;
using UnityEngine;

namespace NoSlimes.Util.DevCon
{
    public class ConsoleCommandCache : ScriptableObject
    {
        public bool ExcludeBuiltInCommands = false;
        public CommandEntry[] Commands;

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