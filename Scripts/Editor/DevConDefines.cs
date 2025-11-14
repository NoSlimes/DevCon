using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace NoSlimes.Util.DevCon.Editor
{
    [InitializeOnLoad]
    internal static class DevConDefines
    {
        private static readonly string DEVCON_BUILTIN = "DEVCON_BUILTIN";
        private static readonly string DEVCON_ENABLECHEATS = "DEVCON_ENABLECHEATS";

        internal const string AutoRebuildCacheKey = "DevCon_AutoRebuildCache";
        internal const string IncludeBuiltInCommandsKey = "DevCon_IncludeBuiltInCommands";
        internal const string IncludeCheatCommandKey = "DevCon_IncludeCheatCommand";
        internal const string DetailedLoggingKey = "DevCon_DetailedLogging";

        static DevConDefines()
        {
            static void ApplyDefines()
            {
                bool enableBuiltin = EditorPrefs.GetBool("DevCon_IncludeBuiltInCommands", true);
                bool enableCheats = EditorPrefs.GetBool("DevCon_IncludeCheatCommand", true);

                if (enableBuiltin)
                    EnableBuiltinCommands();
                else
                    DisableBuiltinCommands();

                if (enableCheats)
                    EnableBuiltinCheatCommand();
                else
                    DisableBuiltinCheatCommand();

                EditorApplication.delayCall -= ApplyDefines;
            }

            EditorApplication.delayCall += ApplyDefines;
        }

        public static void EnableBuiltinCommands() => DefineSymbol(DEVCON_BUILTIN);
        public static void DisableBuiltinCommands() => UndefineSymbol(DEVCON_BUILTIN);

        public static void EnableBuiltinCheatCommand() => DefineSymbol(DEVCON_ENABLECHEATS);
        public static void DisableBuiltinCheatCommand() => UndefineSymbol(DEVCON_ENABLECHEATS);

        private static void DefineSymbol(string symbol)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                ForEachBuildTarget(buildTarget =>
                {
                    try
                    {
                        NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(buildTarget);
                        string symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                        HashSet<string> parts = new(symbols.Split(';').Select(s => s.Trim()));

                        if (parts.Add(symbol))
                        {
                            PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", parts));
                        }

                        if (EditorPrefs.GetBool(DetailedLoggingKey, false))
                            Debug.Log($"Added symbol {symbol} to {buildTarget}");
                    }
                    catch
                    {
                        // Skip invalid/unsupported targets
                    }
                });
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void UndefineSymbol(string symbol)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                ForEachBuildTarget(buildTarget =>
                {
                    try
                    {
                        NamedBuildTarget namedTarget = NamedBuildTarget.FromBuildTargetGroup(buildTarget);
                        string symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                        HashSet<string> parts = new(symbols.Split(';').Select(s => s.Trim()));

                        if (parts.Remove(symbol))
                        {
                            PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", parts));
                        }

                        if (EditorPrefs.GetBool(DetailedLoggingKey, false))
                            Debug.Log($"Removed symbol {symbol} from {buildTarget}");
                    }
                    catch
                    {
                        // Skip invalid/unsupported targets
                    }
                });
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void ForEachBuildTarget(Action<BuildTargetGroup> action)
        {
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown) continue;
                action(group);
            }
        }
    }
}
