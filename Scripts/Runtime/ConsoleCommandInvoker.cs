using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NoSlimes.Util.DevCon
{
    public static class ConsoleCommandInvoker
    {
        /// <summary>
        /// Where command responses and console feedback are routed.
        /// </summary>
        public static Action<string, bool> LogHandler { get; set; } = (msg, success) => { };

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
            if (targetType == typeof(double) && double.TryParse(arg, out var d)) return d;
            if (targetType == typeof(long) && long.TryParse(arg, out var l)) return l;
            if (targetType == typeof(short) && short.TryParse(arg, out var s)) return s;
            if (targetType == typeof(byte) && byte.TryParse(arg, out var by)) return by;
            if (targetType == typeof(decimal) && decimal.TryParse(arg, out var dec)) return dec;
            if (targetType == typeof(uint) && uint.TryParse(arg, out var ui)) return ui;
            if (targetType == typeof(ulong) && ulong.TryParse(arg, out var ul)) return ul;
            if (targetType == typeof(ushort) && ushort.TryParse(arg, out var us)) return us;
            if (targetType == typeof(sbyte) && sbyte.TryParse(arg, out var sb)) return sb;

            if (targetType == typeof(float) && float.TryParse(arg, out var f)) return f;
            if (targetType == typeof(bool) && bool.TryParse(arg, out var b)) return b;
            if (targetType.IsEnum && Enum.TryParse(targetType, arg, true, out var e)) return e;
            throw new ArgumentException($"Could not convert '{arg}' to {targetType.Name}");
        }

        public static void Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            LogHandler("> " + input, true);

            string[] parts = Tokenize(input);
            if (parts.Length == 0) return;

            string command = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            if (!ConsoleCommandRegistry.Commands.TryGetValue(command, out var methodList))
            {
                LogHandler($"<color=yellow>Unknown command: '{command}'. Type 'help' for a list of commands.</color>", false);
                return;
            }

            MethodInfo matchedMethod = null;
            object[] finalArgs = null;
            int bestScore = int.MinValue;

            foreach (var method in methodList)
            {
                var parameters = method.GetParameters();
                int paramOffset = 0;

                bool hasResponse = parameters.Length > 0 &&
                    (parameters[0].ParameterType == typeof(Action<string>) ||
                     parameters[0].ParameterType == typeof(Action<string, bool>));

                if (hasResponse) paramOffset = 1;

                if (args.Length > parameters.Length - paramOffset)
                    continue; // too many arguments for this overload

                var tempArgs = new object[parameters.Length];
                if (hasResponse)
                {
                    tempArgs[0] = parameters[0].ParameterType == typeof(Action<string, bool>)
                        ? LogHandler
                        : new Action<string>(msg => LogHandler(msg, true));
                }

                bool success = true;
                int score = 0;

                for (int i = paramOffset; i < parameters.Length; i++)
                {
                    int argIndex = i - paramOffset;
                    if (argIndex < args.Length)
                    {
                        try
                        {
                            tempArgs[i] = ConvertArg(args[argIndex], parameters[i].ParameterType);

                            // Exact type match scores higher
                            if (parameters[i].ParameterType == tempArgs[i].GetType())
                                score += 2;
                            else
                                score += 1;
                        }
                        catch
                        {
                            success = false;
                            break;
                        }
                    }
                    else if (parameters[i].HasDefaultValue)
                    {
                        tempArgs[i] = parameters[i].DefaultValue;
                        score += 1;
                    }
                    else
                    {
                        success = false;
                        break;
                    }
                }

                if (success && score > bestScore)
                {
                    bestScore = score;
                    matchedMethod = method;
                    finalArgs = tempArgs;
                }
            }

            if (matchedMethod == null)
            {
                LogHandler($"<color=red>Error: No overload for '{command}' matches the provided arguments.</color>", false);
                return;
            }

            object target = matchedMethod.IsStatic ? null : UnityEngine.Object.FindFirstObjectByType(matchedMethod.DeclaringType);
            if (target != null || matchedMethod.IsStatic)
            {
                try
                {
                    matchedMethod.Invoke(target, finalArgs);
                }
                catch (Exception e)
                {
                    LogHandler($"<color=red>Error while executing '{command}': {e.InnerException?.Message ?? e.Message}</color>", false);
                }
            }
            else
            {
                LogHandler($"<color=red>Error: Could not find instance of '{matchedMethod.DeclaringType.Name}' for command '{command}'.</color>", false);
            }
        }
        public static string GetHelp(string commandName = "")
        {
            StringBuilder helpBuilder = new();

            if (string.IsNullOrEmpty(commandName))
            {
                helpBuilder.AppendLine("Available Commands:");
                foreach (var kv in ConsoleCommandRegistry.Commands.OrderBy(c => c.Key))
                {
                    var methods = kv.Value.Distinct().ToList();
                    helpBuilder.AppendLine($"- {kv.Key} ({methods.Count} overload{(methods.Count > 1 ? "s" : "")}):");

                    foreach (var method in methods)
                    {
                        var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                        var parameters = method.GetParameters();

                        string argsInfo = string.Join(" ", parameters
                            .Where((p, index) => !(index == 0 &&
                                (p.ParameterType == typeof(Action<string>) ||
                                 p.ParameterType == typeof(Action<string, bool>))))
                            .Select(p =>
                                p.HasDefaultValue
                                    ? $"<{p.Name}={p.DefaultValue}>"
                                    : $"<{p.Name}>"));

                        helpBuilder.AppendLine($"    {attribute.Command} {argsInfo} - {attribute.Description}");
                    }
                }
            }
            else
            {
                string cmdName = commandName.ToLower();
                if (ConsoleCommandRegistry.Commands.TryGetValue(cmdName, out var methodList))
                {
                    var methods = methodList.Distinct().ToList();

                    helpBuilder.AppendLine($"Command: {cmdName} ({methods.Count} overload{(methods.Count > 1 ? "s" : "")})");

                    foreach (var method in methods)
                    {
                        var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                        var parameters = method.GetParameters();

                        string argsInfo = string.Join(" ", parameters
                            .Where((p, index) => !(index == 0 &&
                                (p.ParameterType == typeof(Action<string>) ||
                                 p.ParameterType == typeof(Action<string, bool>))))
                            .Select(p =>
                                p.HasDefaultValue
                                    ? $"<{p.Name}={p.DefaultValue}>"
                                    : $"<{p.Name}>"));

                        helpBuilder.AppendLine($"  Description: {attribute.Description}");
                        if (parameters.Length > 0) helpBuilder.AppendLine($"  Arguments: {argsInfo}");
                        helpBuilder.AppendLine(); // extra line between overloads
                    }
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
