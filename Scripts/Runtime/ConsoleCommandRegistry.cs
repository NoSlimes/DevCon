using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static NoSlimes.Util.DevCon.ConsoleCommandCache;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NoSlimes.Util.DevCon
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class ConsoleCommandRegistry
    {
        private static ConsoleCommandCache _cache;
        private static readonly HashSet<Assembly> runtimeAssemblies = new();
        private static readonly Dictionary<string, List<MethodInfo>> _commands = new();

        public static IReadOnlyDictionary<string, List<MethodInfo>> Commands => _commands;
        public static event Action<double> OnCacheLoaded;

#if UNITY_EDITOR
        internal static string KeyPrefix => $"{PlayerSettings.companyName}_{PlayerSettings.productName}_{PlayerSettings.productGUID}";
        private static string AutoRebuildCacheKey => $"{KeyPrefix}_DevCon_AutoRebuildCache";
        private static string DetailedLoggingKey => $"{KeyPrefix}_DevCon_DetailedLogging";

        static ConsoleCommandRegistry()
        {
            AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReload;
        }

        private static void AfterAssemblyReload()
        {
            static void callback()
            {
                DiscoverCommandsEditor();
                EditorApplication.delayCall -= callback;
            }

            if (EditorPrefs.GetBool(AutoRebuildCacheKey, true))
                EditorApplication.delayCall += callback;
        }

        [MenuItem("Tools/DevCon/Manual Build Command Cache")]
        public static void DiscoverCommandsEditor()
        {
            DiscoverCommands(AppDomain.CurrentDomain.GetAssemblies());
        }
#endif

        /// <summary>
        /// Discovers console commands in the specified assemblies.
        /// Can be called at runtime or in the editor.
        /// </summary>
        /// <param name="assemblies">Assemblies to search. If null, searches all loaded assemblies.</param>
        internal static void DiscoverCommands(IEnumerable<Assembly> assemblies = null, bool overwrite = true)
        {
            if (overwrite)
                _commands.Clear();

            assemblies ??= AppDomain.CurrentDomain.GetAssemblies();

            List<(MethodInfo Method, ConsoleCommandAttribute Attr)> validCommands = new();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var t in types)
                {
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        try
                        {
                            var attr = m.GetCustomAttribute<ConsoleCommandAttribute>();

                            if (attr != null)
                            {
                                if (!m.IsStatic && !t.IsSubclassOf(typeof(UnityEngine.Object)))
                                {
                                    Debug.LogError($"[DevCon] Non-static command '{attr.Command}' in '{t.Name}.{m.Name}' is in a standard C# class. Must be static. Skipping.");
                                    continue;
                                }

                                validCommands.Add((m, attr));
                            }
                        }
                        catch (MissingMethodException ex)
                        {
                            Debug.LogWarning($"[DevCon] Skipped command in '{t.Name}.{m.Name}'. Missing Method (likely attribute version mismatch): {ex.Message}");
                        }
                        catch (TypeLoadException ex)
                        {
                            Debug.LogWarning($"[DevCon] Skipped command in '{t.Name}.{m.Name}'. Type Load Error: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[DevCon] Error processing method '{m.Name}' in type '{t.Name}': {ex}");
                        }
                    }
                }
            }

            foreach (var (method, attribute) in validCommands)
            {
                string commandName = attribute.Command.ToLower();

                if (!_commands.ContainsKey(commandName))
                    _commands[commandName] = new List<MethodInfo>();

                _commands[commandName].Add(method);
            }

#if UNITY_EDITOR
            UpdateCacheEditor(validCommands.Select(x => x.Method).ToList());
#endif
        }

#if UNITY_EDITOR
        private static void UpdateCacheEditor(List<MethodInfo> methods)
        {
            _cache = Resources.Load<ConsoleCommandCache>("DevCon/ConsoleCommandCache");
            if (_cache == null)
            {
                string folderPath = "Assets/Resources/DevCon";
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                if (!AssetDatabase.IsValidFolder(folderPath))
                    AssetDatabase.CreateFolder("Assets/Resources", "DevCon");

                _cache = ScriptableObject.CreateInstance<ConsoleCommandCache>();
                AssetDatabase.CreateAsset(_cache, folderPath + "/ConsoleCommandCache.asset");
            }

            List<CommandEntry> previousCommands = null;
            bool detailedLogging = EditorPrefs.GetBool(DetailedLoggingKey, false) && _cache.Commands != null;
            if (detailedLogging)
            {
                previousCommands = new List<CommandEntry>(_cache.Commands);
            }

            _cache.Commands = methods.Select(static m =>
            {
                var attr = m.GetCustomAttribute<ConsoleCommandAttribute>();
                return new CommandEntry
                {
                    CommandName = attr?.Command ?? m.Name,
                    Description = attr?.Description ?? "",
                    Flags = attr?.Flags ?? CommandFlags.None,
                    DeclaringType = m.DeclaringType?.AssemblyQualifiedName,
                    MethodName = m.Name,
                    ParameterTypes = m.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToArray()
                };
            }).ToArray();

            EditorUtility.SetDirty(_cache);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DevConsole] Built command cache with {_cache.Commands.Length} entries.");

            if (detailedLogging)
            {
                LogCacheChanges(previousCommands, _cache.Commands);
            }
        }

        private static void LogCacheChanges(List<CommandEntry> previousCommands, CommandEntry[] currentCommands)
        {
            if (previousCommands == null) return;

            foreach (var entry in currentCommands)
            {
                var prevEntry = previousCommands.FirstOrDefault(e => e.CommandName == entry.CommandName);
                if (prevEntry == null)
                {
                    Debug.Log($"[DevConsole] New command added: {entry.CommandName}");
                }
                else
                {
                    if (prevEntry.DeclaringType != entry.DeclaringType || prevEntry.MethodName != entry.MethodName || prevEntry.Flags != entry.Flags)
                    {
                        Debug.Log($"[DevConsole] Command modified: {entry.CommandName} (was {prevEntry.DeclaringType}.{prevEntry.MethodName}, now {entry.DeclaringType}.{entry.MethodName})");
                    }
                }
            }
            foreach (var prevEntry in previousCommands)
            {
                if (!currentCommands.Any(e => e.CommandName == prevEntry.CommandName))
                {
                    Debug.Log($"[DevConsole] Command removed: {prevEntry.CommandName}");
                }
            }
        }
#endif

        public static void DiscoverCommandsInAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            if (!runtimeAssemblies.Contains(assembly))
            {
                runtimeAssemblies.Add(assembly);
            }

            DiscoverCommands(new[] { assembly }, false);
        }

        public static void LoadCache()
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _cache = Resources.Load<ConsoleCommandCache>("DevCon/ConsoleCommandCache");
            if (_cache == null)
                throw new InvalidOperationException("ConsoleCommandCache asset not found at 'Resources/DevCon/ConsoleCommandCache'");

            _commands.Clear();

            foreach (var entry in _cache.Commands)
            {
                Type type = Type.GetType(entry.DeclaringType);
                if (type == null)
                {
                    Debug.LogWarning($"[DevConsole] Type '{entry.DeclaringType}' not found.");
                    continue;
                }

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                  .Where(m => m.Name == entry.MethodName)
                                  .ToArray();

                if (methods.Length == 0)
                {
                    Debug.LogWarning($"[DevConsole] Method '{entry.MethodName}' not found on type '{type.FullName}'.");
                    continue;
                }

                string key = entry.CommandName.ToLower();
                if (!_commands.ContainsKey(key))
                    _commands[key] = new List<MethodInfo>();

                foreach (var method in methods)
                {
                    if (!_commands[key].Contains(method))
                    {
                        if (!FilterCommand(method))
                            continue;

                        _commands[key].Add(method);
                    }
                }
            }

            if (runtimeAssemblies.Count > 0)
            {
                Debug.Log($"[DevConsole] Discovering commands in {runtimeAssemblies.Count} runtime assemblies.");
                DiscoverCommands(runtimeAssemblies, false);
            }

            stopwatch.Stop();
            OnCacheLoaded?.Invoke(stopwatch.Elapsed.TotalMilliseconds);
        }

        private static bool FilterCommand(MethodInfo method)
        {
            var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
            if (attribute == null)
                return false;

            var flags = attribute.Flags;

            if (flags.HasFlag(CommandFlags.DebugOnly) && !Debug.isDebugBuild)
                return false;
            if (flags.HasFlag(CommandFlags.EditorOnly) && !Application.isEditor)
                return false;

            return true;
        }
    }
}