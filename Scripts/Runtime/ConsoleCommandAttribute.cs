using System;

namespace NoSlimes.Util.DevCon
{
    [Flags]
    public enum CommandFlags
    {
        None = 0,
        DebugOnly = 1 << 0,
        EditorOnly = 1 << 1,
        Cheat = 1 << 2,
        Mod = 1 << 3,
        Hidden = 1 << 4
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        public string Command { get; }
        public string Description { get; }

        public CommandFlags Flags { get; set; } = CommandFlags.None;
        public string AutoCompleteProvider { get; set; } = "";

        public ConsoleCommandAttribute(string command)
        {
            Command = command;
            Description = "";
        }

        public ConsoleCommandAttribute(string command, string description)
        {
            Command = command;
            Description = description;
        }


        [Obsolete("Use [ConsoleCommand(cmd, desc, Flags = ...)] instead.")]
        public ConsoleCommandAttribute(string command, string description, CommandFlags flags)
        {
            Command = command;
            Description = description;
            Flags = flags;
        }

        [Obsolete("Use [ConsoleCommand(cmd, desc, Flags = ..., AutoCompleteProvider = ...)] instead.")]
        public ConsoleCommandAttribute(string command, string description, CommandFlags flags, string autoCompleteMethod)
        {
            Command = command;
            Description = description;
            Flags = flags;
            AutoCompleteProvider = autoCompleteMethod;
        }
    }
}