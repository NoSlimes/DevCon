using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace NoSlimes.Util.DevCon
{
    public partial class ConsoleCommandInvoker
    {
        public static class Settings
        {
            public static Color TextColor = Color.white;
            public static Color WarningColor = Color.yellow;
            public static Color ErrorColor = Color.red;
            public static Color SecondaryErrorColor = new(1f, 0.6f, 0.6f);
        }

        private static string Colorize(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
        }
    }

    public static partial class ConsoleCommandInvoker
    {

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitOnPlay()
        {
            _ = typeof(ConsoleCommandInvoker); // forces the static constructor to run
        }

        static ConsoleCommandInvoker()
        {

            RegisterArgConverter<Vector3>(static arg =>
            {
                string[] parts = arg.Trim('(', ')').Split(',');
                if (parts.Length != 3)
                    throw new ArgumentException($"Could not convert '{arg}' to {typeof(Vector3).Name}");

                return new Vector3(
                    float.Parse(parts[0]),
                    float.Parse(parts[1]),
                    float.Parse(parts[2])
                );
            });

            RegisterArgConverter<Vector3Int>(static arg =>
            {
                string[] parts = arg.Trim('(', ')').Split(',');
                if (parts.Length != 3)
                    throw new ArgumentException($"Could not convert '{arg}' to {typeof(Vector3Int).Name}");

                return new Vector3Int(
                    int.Parse(parts[0]),
                    int.Parse(parts[1]),
                    int.Parse(parts[2])
                );
            });

            RegisterArgConverter<Vector2>(static arg =>
            {
                string[] parts = arg.Trim('(', ')').Split(',');
                if (parts.Length != 2)
                    throw new ArgumentException($"Could not convert '{arg}' to {typeof(Vector2).Name}");
                return new Vector2(
                    float.Parse(parts[0]),
                    float.Parse(parts[1])
                );
            });

            RegisterArgConverter<Vector2Int>(static arg =>
            {
                string[] parts = arg.Trim('(', ')').Split(',');
                if (parts.Length != 2)
                    throw new ArgumentException($"Could not convert '{arg}' to {typeof(Vector2Int).Name}");
                return new Vector2Int(
                    int.Parse(parts[0]),
                    int.Parse(parts[1])
                );
            });

            RegisterArgConverter<Color>(static arg =>
            {
                string[] parts = arg.Trim('(', ')').Split(',');
                if (parts.Length != 4)
                    throw new ArgumentException($"Could not convert '{arg}' to {typeof(Color).Name}");
                return new Color(
                    float.Parse(parts[0]),
                    float.Parse(parts[1]),
                    float.Parse(parts[2]),
                    float.Parse(parts[3])
                );
            });

            RegisterArgConverter<Quaternion>(static arg =>
            {
                string[] parts = arg.Trim('(', ')').Split(',');
                if (parts.Length != 4)
                    throw new ArgumentException($"Could not convert '{arg}' to {typeof(Quaternion).Name}");
                return new Quaternion(
                    float.Parse(parts[0]),
                    float.Parse(parts[1]),
                    float.Parse(parts[2]),
                    float.Parse(parts[3])
                );
            });
        }
    }

    public static partial class ConsoleCommandInvoker
    {
        /// <summary>
        /// Where command responses and console feedback are routed.
        /// </summary>
        internal static Action<string, bool> LogHandler { get; set; } = (msg, success) => { };

        /// <summary>
        /// Indicates whether cheat commands are currently allowed to execute.
        /// Commands marked with the <see cref="CommandFlags.Cheat"/> flag
        /// will only run if this property is <c>true</c>.
        /// </summary>
        public static bool CheatsEnabled { get; set; } = false;

        // Regex that matches quoted strings OR non-space sequences
        private static readonly Regex ArgTokenizer = new(
            @"[\""].+?[\""]|[^ ]+",
            RegexOptions.Compiled
        );

        private static readonly Dictionary<Type, Func<string, object>> ArgConverters = new();
        private static readonly Dictionary<MethodInfo, MethodInfo> SuggestionMethodCache = new();
        private static readonly Dictionary<Type, UnityEngine.Object> UnityInstanceCache = new();

        public static void RegisterArgConverter<T>(Func<string, T> converter)
        {
            ArgConverters[typeof(T)] = arg => converter(arg);
        }

        private static string[] Tokenize(string input)
        {
            var matches = ArgTokenizer.Matches(input);
            string[] results = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                results[i] = matches[i].Value.Trim('"');
            }
            return results;
        }

        private static object ConvertArg(string arg, Type targetType)
        {
            if (targetType == typeof(string)) return arg;

            Type typeToUse = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (ArgConverters.TryGetValue(typeToUse, out Func<string, object> converter))
                return converter(arg);

            if (targetType == typeof(int) && int.TryParse(arg, out int i)) return i;
            if (targetType == typeof(double) && double.TryParse(arg, out double d)) return d;
            if (targetType == typeof(long) && long.TryParse(arg, out long l)) return l;
            if (targetType == typeof(short) && short.TryParse(arg, out short s)) return s;
            if (targetType == typeof(byte) && byte.TryParse(arg, out byte by)) return by;
            if (targetType == typeof(decimal) && decimal.TryParse(arg, out decimal dec)) return dec;
            if (targetType == typeof(uint) && uint.TryParse(arg, out uint ui)) return ui;
            if (targetType == typeof(ulong) && ulong.TryParse(arg, out ulong ul)) return ul;
            if (targetType == typeof(ushort) && ushort.TryParse(arg, out ushort us)) return us;
            if (targetType == typeof(sbyte) && sbyte.TryParse(arg, out sbyte sb)) return sb;

            if (targetType == typeof(float) && float.TryParse(arg, out float f)) return f;
            if (targetType == typeof(bool) && bool.TryParse(arg, out bool b)) return b;
            if (targetType.IsEnum && Enum.TryParse(targetType, arg, true, out object e)) return e;
            throw new ArgumentException($"Could not convert '{arg}' to {targetType.Name}");
        }

        internal static void Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            LogHandler(Colorize("> ", Settings.TextColor) + input, true);

            string[] parts = Tokenize(input);
            if (parts.Length == 0) return;

            string command = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            if (!ConsoleCommandRegistry.Commands.TryGetValue(command, out List<MethodInfo> methodList))
            {
                LogHandler(Colorize($"Unknown command: '{command}'. Type 'help' for a list of commands.", Settings.WarningColor), false);
                return;
            }

            MethodInfo matchedMethod = null;
            object[] finalArgs = null;
            int bestScore = int.MinValue;

            List<string> candidateErrors = new();

            foreach (MethodInfo method in methodList)
            {
                ParameterInfo[] parameters = method.GetParameters();
                int paramOffset = 0;

                bool hasResponse = parameters.Length > 0 &&
                    (parameters[0].ParameterType == typeof(Action<string>) ||
                     parameters[0].ParameterType == typeof(Action<string, bool>));

                if (hasResponse) paramOffset = 1;

                if (args.Length > parameters.Length - paramOffset)
                {
                    candidateErrors.Add($"[{GetMethodSignature(method)}] Too many arguments provided.");
                    continue;
                }

                object[] tempArgs = new object[parameters.Length];
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

                            if (parameters[i].ParameterType == tempArgs[i].GetType())
                                score += 2;
                            else
                                score += 1;
                        }
                        catch (Exception ex)
                        {
                            string msg = ex.InnerException?.Message ?? ex.Message;
                            string paramName = parameters[i].Name;
                            string typeName = parameters[i].ParameterType.Name;

                            candidateErrors.Add($"[{GetMethodSignature(method)}] Error parsing arg '{paramName}' ({typeName}): {msg}");

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
                        candidateErrors.Add($"[{GetMethodSignature(method)}] Missing required argument '{parameters[i].Name}'.");
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
                LogHandler(Colorize($"Could not execute '{command}'. Potential reasons:.", Settings.ErrorColor), false);
                foreach (string error in candidateErrors)
                {
                    LogHandler(Colorize($"- {error}", Settings.SecondaryErrorColor), false);
                }
                return;
            }

            object target = ResolveTarget(matchedMethod);
            if (target != null || matchedMethod.IsStatic)
            {
                try
                {
                    ConsoleCommandAttribute attr = matchedMethod.GetCustomAttribute<ConsoleCommandAttribute>();

                    bool BlockLocal(string reason)
                    {
                        LogHandler(Colorize($"Cannot run '{attr.Command}': {reason}.", Settings.WarningColor), false);
                        return true;
                    }

                    if (attr.Flags.HasFlag(CommandFlags.Cheat) && !CheatsEnabled &&
                        BlockLocal("cheats are disabled")) return;

                    if (attr.Flags.HasFlag(CommandFlags.DebugOnly) && !Debug.isDebugBuild &&
                        BlockLocal("debug-only commands are not allowed in this build")) return;

                    if (attr.Flags.HasFlag(CommandFlags.EditorOnly) && !Application.isEditor &&
                        BlockLocal("editor-only commands are not allowed in builds")) return;

                    matchedMethod.Invoke(target, finalArgs);
                }
                catch (Exception e)
                {
                    string exceptionString = $"{e.InnerException?.Message ?? e.Message}";
#if DEBUG
                    exceptionString += Colorize($"\n {e.StackTrace}", Settings.SecondaryErrorColor);
#endif
                    LogHandler(Colorize($"Error: An exception occurred while executing command '{command}'{exceptionString}", Settings.ErrorColor), false);
                }
            }
            else
            {
                LogHandler(Colorize($"Error: Could not find instance of '{matchedMethod.DeclaringType.Name}' for command '{command}'.", Settings.ErrorColor), false);
            }
        }

        private static string GetMethodSignature(MethodInfo method)
        {
            string[] pars = method.GetParameters()
                .Where(p => !(p.ParameterType == typeof(Action<string>) || p.ParameterType == typeof(Action<string, bool>)))
                .Select(p => $"{p.ParameterType.Name} {p.Name}")
                .ToArray();

            if (pars.Length == 0) return "void";

            return string.Join(", ", pars);
        }

        private static object ResolveTarget(MethodInfo method)
        {
            if (method.IsStatic) return null;
            Type targetType = method.DeclaringType;

            if (UnityInstanceCache.TryGetValue(targetType, out UnityEngine.Object cachedInstance))
            {
                if (cachedInstance != null)
                    return cachedInstance;

                UnityInstanceCache.Remove(targetType);
            }

            object targetInstance = null;

            if (targetType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                targetInstance = UnityEngine.Object.FindFirstObjectByType(targetType);

                if (targetInstance != null)
                {
                    UnityInstanceCache[targetType] = (UnityEngine.Object)targetInstance;
                }
            }
            else
            {
                LogHandler(Colorize("Error: Non-static command methods must belong to a UnityEngine.Object subclass", Settings.ErrorColor), false);
            }

            return targetInstance;
        }

        public static string GetHelp(string commandName = "")
        {
            StringBuilder helpBuilder = new();

            if (string.IsNullOrEmpty(commandName))
            {
                helpBuilder.AppendLine("Available Commands:");
                foreach (KeyValuePair<string, List<MethodInfo>> kv in ConsoleCommandRegistry.Commands.OrderBy(c => c.Key))
                {
                    var methods = kv.Value.Distinct().ToList();
                    helpBuilder.AppendLine($"- {kv.Key} ({methods.Count} overload{(methods.Count > 1 ? "s" : "")}):");

                    foreach (MethodInfo method in methods)
                    {
                        ConsoleCommandAttribute attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();

                        // Skip hidden commands in general help listing
                        if (attribute.Flags.HasFlag(CommandFlags.Hidden))
                            continue;

                        ParameterInfo[] parameters = method.GetParameters();

                        string argsInfo = string.Join(" ", parameters
                            .Where((p, index) => !(index == 0 &&
                                (p.ParameterType == typeof(Action<string>) ||
                                 p.ParameterType == typeof(Action<string, bool>))))
                            .Select(p =>
                                p.HasDefaultValue
                                    ? $"<{p.Name} ({p.ParameterType.Name})={(p.DefaultValue is string s && s == string.Empty ? "\"\"" : p.DefaultValue)}>"
                                    : $"<{p.Name} ({p.ParameterType.Name})>"));

                        helpBuilder.AppendLine($"    {attribute.Command} {argsInfo} - {attribute.Description}");
                    }
                }
            }
            else
            {
                string cmdName = commandName.ToLower();
                if (ConsoleCommandRegistry.Commands.TryGetValue(cmdName, out List<MethodInfo> methodList))
                {
                    var methods = methodList.Distinct().ToList();

                    helpBuilder.AppendLine($"Command: {cmdName} ({methods.Count} overload{(methods.Count > 1 ? "s" : "")})");

                    foreach (MethodInfo method in methods)
                    {
                        ConsoleCommandAttribute attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                        ParameterInfo[] parameters = method.GetParameters();

                        string argsInfo = string.Join(" ", parameters
                            .Where((p, index) => !(index == 0 &&
                                (p.ParameterType == typeof(Action<string>) ||
                                 p.ParameterType == typeof(Action<string, bool>))))
                            .Select(p =>
                                p.HasDefaultValue
                                    ? $"<{p.Name} ({p.ParameterType.Name})={(p.DefaultValue is string s && s == string.Empty ? "\"\"" : p.DefaultValue)}>"
                                    : $"<{p.Name} ({p.ParameterType.Name})>"));

                        helpBuilder.AppendLine($"  Description: {attribute.Description}");
                        if (parameters.Length > 0) helpBuilder.AppendLine($"  Arguments: {argsInfo}");
                        helpBuilder.AppendLine();
                    }
                }
                else
                {
                    helpBuilder.AppendLine(Colorize($"Unknown command: '{cmdName}'", Settings.WarningColor));
                }
            }

            return helpBuilder.ToString();
        }

        public static IEnumerable<string> GetAutoCompleteSuggestions(MethodInfo method, int argIndex, string prefix)
        {
            var attr = method.GetCustomAttribute<ConsoleCommandAttribute>();
            var parameters = method.GetParameters();

            bool hasCallback = parameters.Length > 0 &&
                (parameters[0].ParameterType == typeof(Action<string>) ||
                 parameters[0].ParameterType == typeof(Action<string, bool>));

            if (hasCallback) argIndex++;
            if (argIndex >= parameters.Length) return Array.Empty<string>();

            var paramType = parameters[argIndex].ParameterType;

            if (!string.IsNullOrEmpty(attr?.AutoCompleteProvider))
            {
                if (!SuggestionMethodCache.TryGetValue(method, out MethodInfo providerMethod))
                {
                    providerMethod = method.DeclaringType.GetMethod(
                        attr.AutoCompleteProvider,
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    if (providerMethod != null &&
                        typeof(IEnumerable<string>).IsAssignableFrom(providerMethod.ReturnType))
                    {
                        SuggestionMethodCache[method] = providerMethod;
                    }
                    else
                    {
                        SuggestionMethodCache[method] = null;
                        LogHandler(Colorize($"Could not find static IEnumerable<string> {attr.AutoCompleteProvider}() in {method.DeclaringType.Name}", Settings.WarningColor), false);
                        Debug.LogWarning($"[DevCon] Could not find static IEnumerable<string> {attr.AutoCompleteProvider}() in {method.DeclaringType.Name}");
                    }
                }

                if (providerMethod != null)
                {
                    var providerParams = providerMethod.GetParameters();
                    var relativeArgIndex = argIndex - (hasCallback ? 1 : 0);

                    switch (providerParams.Length)
                    {
                        case 2 when providerParams[0].ParameterType == typeof(string) && providerParams[1].ParameterType == typeof(int):
                            return (IEnumerable<string>)providerMethod.Invoke(null, new object[] { prefix, relativeArgIndex });
                        case 1 when providerParams[0].ParameterType == typeof(string):
                            return (IEnumerable<string>)providerMethod.Invoke(null, new object[] { prefix });
                        case 1 when providerParams[0].ParameterType == typeof(int):
                            {
                                var suggestions = (IEnumerable<string>)providerMethod.Invoke(null, new object[] { relativeArgIndex });
                                return (suggestions ?? Array.Empty<string>()) 
                                    .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                            }
                        case 0:
                            {
                                var suggestions = (IEnumerable<string>)providerMethod.Invoke(null, null);
                                return (suggestions ?? Array.Empty<string>()) 
                                    .Where(s => s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                            }
                        default:
#if DEBUG
                            LogHandler(Colorize($"AutoComplete method '{providerMethod.Name}' has invalid parameters. Expected () or (string).", Settings.WarningColor), false);
#endif

                            Debug.LogWarning($"[DevCon] AutoComplete method '{providerMethod.Name}' has invalid parameters. Expected () or (string).");
                            break;
                    }
                }
            }

            if (paramType == typeof(bool))
                return new[] { "true", "false" }
                .Where(v => v.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)); // Reverse to suggest 'true' first

            if (paramType.IsEnum)
            {
                return Enum.GetNames(paramType)
                    .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(name => name);
            }

            return Array.Empty<string>();
        }
    }
}
