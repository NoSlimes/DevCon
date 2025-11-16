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
        private static readonly string DEVCON_DEFINE = "DEVCON";
        private static readonly string DEVCON_BUILTIN = "DEVCON_BUILTIN";
        private static readonly string DEVCON_ENABLECHEATS = "DEVCON_ENABLECHEATS";

        internal static string KeyPrefix => $"{PlayerSettings.companyName}_{PlayerSettings.productName}_{PlayerSettings.productGUID}";

        internal static string AutoRebuildCacheKey => $"{KeyPrefix}_DevCon_AutoRebuildCache";
        internal static string IncludeBuiltInCommandsKey => $"{KeyPrefix}_DevCon_IncludeBuiltInCommands";
        internal static string IncludeCheatCommandKey => $"{KeyPrefix}_DevCon_IncludeCheatCommand";
        internal static string DetailedLoggingKey => $"{KeyPrefix}_DevCon_DetailedLogging";

        static DevConDefines()
        {
            static void ApplyDefines()
            {
                bool enableBuiltin = EditorPrefs.GetBool(IncludeBuiltInCommandsKey, true);
                bool enableCheats = EditorPrefs.GetBool(IncludeCheatCommandKey, true);

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

            DefineSymbol(DEVCON_DEFINE);
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
