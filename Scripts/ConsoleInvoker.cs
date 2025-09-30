using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NoSlimes.Util.DevCon
{
    public class ConsoleInvoker
    {
        private readonly ConsoleCommandRegistry _registry;

        public Action<string> LogHandler { get; set; } = msg => { };

        public ConsoleInvoker(ConsoleCommandRegistry registry)
        {
            _registry = registry;
        }

        // Regex that matches quoted strings OR non-space sequences
        private static readonly Regex ArgTokenizer = new Regex(
            @"[\""].+?[\""]|[^ ]+",
            RegexOptions.Compiled
        );

        private static string[] Tokenize(string input)
        {
            return ArgTokenizer.Matches(input)
                .Select(m => m.Value.Trim('"'))
                .ToArray();
        }

        private static object ConvertArg(string arg, Type targetType)
        {
            if (targetType == typeof(string)) return arg;
            if (targetType == typeof(int) && int.TryParse(arg, out var i)) return i;
            if (targetType == typeof(float) && float.TryParse(arg, out var f)) return f;
            if (targetType == typeof(bool) && bool.TryParse(arg, out var b)) return b;
            if (targetType.IsEnum && Enum.TryParse(targetType, arg, true, out var e)) return e;
            throw new ArgumentException($"Could not convert '{arg}' to {targetType.Name}");
        }

        public void Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            LogHandler("> " + input);

            string[] parts = Tokenize(input);
            if (parts.Length == 0) return;

            string command = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            if (_registry.Commands.TryGetValue(command, out var methodInfo))
            {
                var parameters = methodInfo.GetParameters();
                object[] finalArgs = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i < args.Length)
                    {
                        try { finalArgs[i] = ConvertArg(args[i], parameters[i].ParameterType); }
                        catch (Exception e) { LogHandler($"<color=red>Error: {e.Message}</color>"); return; }
                    }
                    else
                    {
                        if (parameters[i].HasDefaultValue) finalArgs[i] = parameters[i].DefaultValue;
                        else { LogHandler($"<color=red>Error: Missing required argument '{parameters[i].Name}'.</color>"); return; }
                    }
                }

                object target = methodInfo.IsStatic ? null : UnityEngine.Object.FindFirstObjectByType(methodInfo.DeclaringType);
                if (target != null || methodInfo.IsStatic)
                    methodInfo.Invoke(target, finalArgs);
                else
                    LogHandler($"<color=red>Error: Could not find instance of '{methodInfo.DeclaringType.Name}' for command '{command}'.</color>");
            }
            else
            {
                LogHandler($"<color=yellow>Unknown command: '{command}'. Type 'help' for a list of commands.</color>");
            }
        }

        public string GetHelp(string commandName = "")
        {
            StringBuilder helpBuilder = new();

            if (string.IsNullOrEmpty(commandName))
            {
                helpBuilder.AppendLine("Available Commands:");
                foreach (var kv in _registry.Commands.OrderBy(c => c.Key))
                {
                    var attribute = kv.Value.GetCustomAttribute<ConsoleCommandAttribute>();
                    var parameters = kv.Value.GetParameters();

                    string argsInfo = string.Join(" ", parameters.Select(p =>
                        p.HasDefaultValue ? $"<{p.Name}={p.DefaultValue}>" : $"<{p.Name}>"));

                    helpBuilder.AppendLine($"{attribute.Command} {argsInfo} - {attribute.Description}");
                }
            }
            else
            {
                string cmdName = commandName.ToLower();
                if (_registry.Commands.TryGetValue(cmdName, out var method))
                {
                    var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                    var parameters = method.GetParameters();

                    string argsInfo = string.Join(" ", parameters.Select(p =>
                        p.HasDefaultValue ? $"<{p.Name}={p.DefaultValue}>" : $"<{p.Name}>"));

                    helpBuilder.AppendLine($"Command: {attribute.Command}");
                    helpBuilder.AppendLine($"Description: {attribute.Description}");
                    if (parameters.Length > 0) helpBuilder.AppendLine($"Arguments: {argsInfo}");
                }
                else
                {
                    helpBuilder.AppendLine($"<color=yellow>Unknown command: '{cmdName}'</color>");
                }
            }

            return helpBuilder.ToString();
        }
    }
}