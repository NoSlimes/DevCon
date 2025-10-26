using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

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
                            Assert.IsTrue(m.IsStatic || t.IsSubclassOf(typeof(UnityEngine.Object)), 
                                $"Non-static command '{m.Name}' is in class '{t.Name}' which does not inherit from UnityEngine.Object. Command methods in standard C# classes must be static. \n Aborting command discovery.");

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
            {
                string folderPath = "Assets/Resources/DevCon";
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                if (!AssetDatabase.IsValidFolder(folderPath))
                    AssetDatabase.CreateFolder("Assets/Resources", "DevCon");

                _cache = ScriptableObject.CreateInstance<ConsoleCommandCache>();
                AssetDatabase.CreateAsset(_cache, folderPath + "/ConsoleCommandCache.asset");
                AssetDatabase.SaveAssets();
                _cache = AssetDatabase.LoadAssetAtPath<ConsoleCommandCache>(folderPath + "/ConsoleCommandCache.asset");
            }

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
                if (type == null)
                {
                    Debug.LogWarning($"Type '{entry.DeclaringType}' not found.");
                    continue;
                }

                if (_cache.ExcludeBuiltInCommands && type == typeof(BuiltInCommands))
                    continue;

                // Get all methods with the given name
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                  .Where(m => m.Name == entry.MethodName)
                                  .ToArray();

                if (methods.Length == 0)
                {
                    Debug.LogWarning($"Method '{entry.MethodName}' not found on type '{type.FullName}'.");
                    continue;
                }

                string key = entry.CommandName.ToLower();
                if (!_commands.ContainsKey(key))
                    _commands[key] = new List<MethodInfo>();

                // Add methods only if not already added
                foreach (var method in methods)
                {
                    if (!_commands[key].Contains(method))
                        _commands[key].Add(method);
                }
            }

            stopwatch.Stop();
            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            OnCacheLoaded?.Invoke(elapsedMs);
        }
    }
}