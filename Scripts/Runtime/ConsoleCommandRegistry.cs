using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NoSlimes.Util.DevCon
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class ConsoleCommandRegistry
    {
        private static ConsoleCommandCache _cache;

        private static readonly Dictionary<string, List<MethodInfo>> _commands = new();
        public static IReadOnlyDictionary<string, List<MethodInfo>> Commands => _commands;

        public static event Action<double> OnCacheLoaded;

#if UNITY_EDITOR
        static ConsoleCommandRegistry()
        {
            AssemblyReloadEvents.afterAssemblyReload += () =>
            {
                DiscoverCommandsEditor();
            };
        }

        [MenuItem("Tools/DevCon/Manual Build Command Cache")]
        public static void DiscoverCommandsEditor()
        {
            _commands.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var methods = new List<MethodInfo>();

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var t in types)
                {
                    foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        if (m.IsDefined(typeof(ConsoleCommandAttribute), false))
                        {
                            methods.Add(m);
                        }
                    }
                }
            }

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                string commandName = attribute.Command.ToLower();

                if (!_commands.ContainsKey(commandName))
                    _commands[commandName] = new List<MethodInfo>();

                _commands[commandName].Add(method);
            }

            _cache = Resources.Load<ConsoleCommandCache>("DevCon/ConsoleCommandCache");
            if (_cache == null)
                throw new InvalidOperationException("ConsoleCommandCache asset not found at 'Resources/DevCon/ConsoleCommandCache'");

            _cache.Commands = methods.Select(m => new ConsoleCommandCache.CommandEntry
            {
                CommandName = m.GetCustomAttribute<ConsoleCommandAttribute>()?.Command ?? m.Name,
                Description = m.GetCustomAttribute<ConsoleCommandAttribute>()?.Description ?? "",
                DeclaringType = m.DeclaringType.AssemblyQualifiedName,
                MethodName = m.Name,
                ParameterTypes = m.GetParameters().Select(p => p.ParameterType.AssemblyQualifiedName).ToArray()
            }).ToArray();

            EditorUtility.SetDirty(_cache);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DevConsole] Built command cache with {_cache.Commands.Length} entries.");
        }
#endif

        public static void LoadCache()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _cache = Resources.Load<ConsoleCommandCache>("DevCon/ConsoleCommandCache");
            if (_cache == null)
                throw new InvalidOperationException("ConsoleCommandCache asset not found at 'Resources/DevCon/ConsoleCommandCache'");

            _commands.Clear();

            foreach (var entry in _cache.Commands)
            {
                Type type = Type.GetType(entry.DeclaringType);

                if (_cache.ExcludeBuiltInCommands && type.FullName == typeof(BuiltInCommands).FullName)
                    continue;

                MethodInfo method = type.GetMethod(entry.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                if (!_commands.ContainsKey(entry.CommandName.ToLower()))
                    _commands[entry.CommandName.ToLower()] = new List<MethodInfo>();

                _commands[entry.CommandName.ToLower()].Add(method);
            }

            stopwatch.Stop();
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            OnCacheLoaded?.Invoke(elapsedMs);
        }
    }
}