using UnityEngine;
using UnityEditor;

namespace NoSlimes.Util.DevCon.Editor
{
	internal class DevConWindowInitializer: ScriptableObject
	{
        private const string FirstTimeKey = "DevCon_FirstTime_Shown";

        static DevConWindowInitializer()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorPrefs.GetBool(FirstTimeKey, false))
                {
                    DevConEditorWindow.ShowWindow();
                    EditorPrefs.SetBool(FirstTimeKey, true);
                }
            };
        }
    }
}