using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoSlimes.Util.DevCon.Editor
{
    /// <summary>
    /// DevCon settings window using UI Toolkit
    /// </summary>
    public class DevConEditorWindow : EditorWindow
    {


        private ConsoleCommandCache commandCache;

        private bool isAutoRebuildEnabled;
        private bool includeBuiltInCommands;
        private bool includeCheatCommand;
        private bool isDetailedLoggingEnabled;

        [MenuItem("Tools/DevCon/DevCon Window")]
        public static void ShowWindow()
        {
            DevConEditorWindow window = GetWindow<DevConEditorWindow>("DevCon");
            window.minSize = new Vector2(350, 400);
            window.maxSize = new Vector2(350, 600);
        }

        private void OnEnable()
        {
            commandCache = Resources.Load<ConsoleCommandCache>("DevCon/ConsoleCommandCache");

            isAutoRebuildEnabled = EditorPrefs.GetBool(DevConDefines.AutoRebuildCacheKey, true);
            includeBuiltInCommands = EditorPrefs.GetBool(DevConDefines.IncludeBuiltInCommandsKey, true);
            includeCheatCommand = EditorPrefs.GetBool(DevConDefines.IncludeCheatCommandKey, true);
            isDetailedLoggingEnabled = EditorPrefs.GetBool(DevConDefines.DetailedLoggingKey, false);

            rootVisualElement.Clear();
        }

        private void OnDisable()
        {
            SaveCache();
            commandCache = null;
        }

        private void CreateGUI()
        {
            if (commandCache == null)
                return;

            // Root container
            var root = new VisualElement
            {
                style =
                {
                    paddingLeft = 12,
                    paddingRight = 12,
                    paddingTop = 12,
                    paddingBottom = 12,
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.Stretch,
                    justifyContent = Justify.FlexStart
                }
            };

            root.Add(new Label("DevCon Settings")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 16,
                    marginBottom = 10
                }
            });

            // Runtime & Editor section
            VisualElement runtimeSection = CreateSection("Runtime and Editor");

            var includeBuiltInToggle = new ToggleButton("Include Built-In Commands in Cache", includeBuiltInCommands);
            includeBuiltInToggle.OnValueChanged += SetIncludeBuiltInCommands;
            runtimeSection.Add(includeBuiltInToggle);

            var includeCheatToggle = new ToggleButton("Include Built-In Cheat Command in Cache", includeCheatCommand);
            includeCheatToggle.OnValueChanged += SetIncludeCheatCommand;
            runtimeSection.Add(includeCheatToggle);

            root.Add(runtimeSection);

            // Editor section
            VisualElement editorSection = CreateSection("Editor");

            var autoRebuildToggle = new ToggleButton("Auto Rebuild Cache", isAutoRebuildEnabled);
            autoRebuildToggle.OnValueChanged += val =>
            {
                isAutoRebuildEnabled = val;
                EditorPrefs.SetBool(DevConDefines.AutoRebuildCacheKey, val);
            };
            editorSection.Add(autoRebuildToggle);

            var detailedLoggingToggle = new ToggleButton("Detailed Logging", isDetailedLoggingEnabled);
            detailedLoggingToggle.OnValueChanged += val =>
            {
                isDetailedLoggingEnabled = val;
                EditorPrefs.SetBool(DevConDefines.DetailedLoggingKey, val);
            };
            editorSection.Add(detailedLoggingToggle);

            var rebuildButton = new Button(() => ConsoleCommandRegistry.DiscoverCommandsEditor())
            {
                text = "Manual Rebuild Command Cache",
                style =
                {
                    marginTop = 10,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = new Color(0.2f, 0.6f, 0.9f),
                    color = Color.white,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4
                }
            };
            editorSection.Add(rebuildButton);

            root.Add(editorSection);

            root.Add(new Label("All changes are saved automatically.")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.7f, 0.7f, 0.7f),
                    marginTop = 8
                }
            });

            rootVisualElement.Add(root);
        }

        private void SetIncludeBuiltInCommands(bool value)
        {
            includeBuiltInCommands = value;

            if (includeBuiltInCommands)
                DevConDefines.EnableBuiltinCommands();
            else
                DevConDefines.DisableBuiltinCommands();

            EditorPrefs.SetBool(DevConDefines.IncludeBuiltInCommandsKey, value);
            SaveCache();
        }

        private void SetIncludeCheatCommand(bool value)
        {
            includeCheatCommand = value;

            if (includeCheatCommand)
                DevConDefines.EnableBuiltinCheatCommand();
            else
                DevConDefines.DisableBuiltinCheatCommand();

            EditorPrefs.SetBool(DevConDefines.IncludeCheatCommandKey, value);
            SaveCache();
        }

        private void SaveCache()
        {
            if (commandCache == null) return;
            EditorUtility.SetDirty(commandCache);
            AssetDatabase.SaveAssets();
        }

        private VisualElement CreateSection(string titleText)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.Stretch,
                    marginTop = 12
                }
            };

            var titleLabel = new Label(titleText)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14,
                    marginBottom = 6
                }
            };

            var separator = new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new Color(0.5f, 0.5f, 0.5f),
                    marginBottom = 6
                }
            };

            container.Add(titleLabel);
            container.Add(separator);
            return container;
        }
    }

    public class ToggleButton : VisualElement
    {
        public bool Value { get; private set; }

        private readonly VisualElement labelContainer;
        private readonly VisualElement indicator;
        private readonly Label label;

        public event System.Action<bool> OnValueChanged;

        private readonly Color activeColor = new(0.5f, 0.9f, 0.5f); // gentle green
        private readonly Color inactiveColor = new(0.9f, 0.5f, 0.5f); // gentle red
        private readonly Color backgroundColor = new(0.3f, 0.3f, 0.3f); // gray

        public ToggleButton(string text, bool initialValue = false)
        {
            Value = initialValue;

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Stretch;
            style.justifyContent = Justify.FlexStart;
            style.height = 30;
            style.backgroundColor = backgroundColor;
            style.borderBottomLeftRadius = 6;
            style.borderTopLeftRadius = 6;
            style.borderBottomRightRadius = 6;
            style.borderTopRightRadius = 6;
            style.marginBottom = 6;
            style.overflow = Overflow.Hidden;

            labelContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    paddingLeft = 6,
                    paddingRight = 6,
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart,
                    alignItems = Align.Center
                }
            };
            Add(labelContainer);

            label = new Label(text)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    flexShrink = 0
                }
            };
            labelContainer.Add(label);

            indicator = new VisualElement
            {
                style =
                {
                    width = Length.Percent(10),
                    backgroundColor = Value ? activeColor : inactiveColor,
                    alignSelf = Align.Stretch,
                    borderBottomRightRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 0,
                    borderTopLeftRadius = 0
                }
            };
            Add(indicator);

            RegisterCallback<MouseDownEvent>(evt => Toggle());
        }

        public void Toggle()
        {
            Value = !Value;
            indicator.style.backgroundColor = Value ? activeColor : inactiveColor;
            OnValueChanged?.Invoke(Value);
        }

        public void SetValue(bool value)
        {
            Value = value;
            indicator.style.backgroundColor = Value ? activeColor : inactiveColor;
        }
    }
}
