using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NoSlimes.Util.DevCon
{
    public class ConsoleCommandRegistry
    {
        private readonly Dictionary<string, MethodInfo> _commands = new();

        public IReadOnlyDictionary<string, MethodInfo> Commands => _commands;

        public void DiscoverCommands()
        {
            _commands.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var methods = assemblies
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                .Where(m => m.GetCustomAttributes(typeof(ConsoleCommandAttribute), false).Length > 0);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                string commandName = attribute.Command.ToLower();

                if (!_commands.ContainsKey(commandName))
                {
                    _commands.Add(commandName, method);
                }
                else
                {
                    var existingMethod = _commands[commandName];
                    UnityEngine.Debug.LogWarning(
                        $"[DeveloperConsole] Duplicate command '{commandName}' found in '{method.DeclaringType.Name}', keeping '{existingMethod.DeclaringType.Name}'."
                    );
                }
            }
        }
    }
}
