using System;

namespace NoSlimes.Util.DeveloperConsole
{

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        public string Command { get; }
        public string Description { get; }
        public int MinArgs { get; }
        public int MaxArgs { get; }
        public string[] ArgNames { get; } = new string[0];


        public ConsoleCommandAttribute(string command, string description = "", int minArgs = 0, int maxArgs = -1, params string[] argNames)
        {
            Command = command;
            Description = description;
            MinArgs = minArgs;
            MaxArgs = maxArgs;
            if (argNames != null && argNames.Length > 0)
            {
                ArgNames = argNames;
            }
        }
    }
}
