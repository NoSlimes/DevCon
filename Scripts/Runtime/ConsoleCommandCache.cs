using System;
using UnityEngine;

namespace NoSlimes.Util.DevCon
{
    internal class ConsoleCommandCache : ScriptableObject
    {
        public CommandEntry[] Commands;

        [Serializable]
        public class CommandEntry
        {
            public string CommandName;
            public string Description;
            public CommandFlags Flags;
            public string DeclaringType;
            public string MethodName;
            public string[] ParameterTypes;
        }

    }
}