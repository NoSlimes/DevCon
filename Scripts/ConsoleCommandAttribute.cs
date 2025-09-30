using System;

namespace NoSlimes.Util.DevCon
{

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        public string Command { get; }
        public string Description { get; }


        public ConsoleCommandAttribute(string command, string description = "")
        {
            Command = command;
            Description = description;
        }
    }
}
