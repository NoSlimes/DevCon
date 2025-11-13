using System;

namespace NoSlimes.Util.DevCon
{
    [Flags]
    public enum CommandFlags
    {
        None = 0,
        DebugOnly = 1 << 0, // Command is only available in debug builds
        EditorOnly = 1 << 1, // Command is only available in the editor
        Cheat = 1 << 2, // Command is considered a cheat command
        Mod = 1 << 3, // Command is added by a mod
        Hidden = 1 << 4 // Command will not show up in help listings
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        public string Command { get; }
        public string Description { get; }
        public CommandFlags Flags { get; set; } = CommandFlags.None;

        public ConsoleCommandAttribute(string command, string description = "", CommandFlags flags = CommandFlags.None)
        {
            if ((flags & CommandFlags.DebugOnly) != 0 && (flags & CommandFlags.EditorOnly) != 0)
            {
                throw new ArgumentException("DebugOnly and EditorOnly cannot be set at the same time.");
            }

            Command = command;
            Description = description;
            Flags = flags;
        }
    }
}
