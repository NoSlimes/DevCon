using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NoSlimes.Util.DevCon
{
    public class DeveloperConsoleUI : MonoBehaviour
    {
        private static DeveloperConsoleUI _instance;

        public enum InputSystemType { New, Old }

        [SerializeField] private InputSystemType inputSystem = InputSystemType.Old;
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private InputActionReference toggleConsoleAction;
        [SerializeField] private InputActionReference autoCompleteAction;
        private InputAction historyUpAction;
        private InputAction historyDownAction;
#endif

        [SerializeField] private KeyCode toggleConsoleKey = KeyCode.BackQuote;
        [SerializeField] private KeyCode autoCompleteKey = KeyCode.Tab;
        [SerializeField] private GameObject consolePanel;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TMP_Text consoleLog;
        [SerializeField] private int maxLogLines = 100;
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool catchUnityLogs = true;
        [SerializeField] private bool controlCursorLockMode = false;
        [SerializeField] private char commandSeparator = '|';

        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.95f);
        [SerializeField] private Color accentColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color errorColor = Color.red;
        [SerializeField] private Color exceptionColor = Color.red;
        [SerializeField] private Color assertColor = new(1, 0.6f, 0.6f);

        [SerializeField] private TMP_FontAsset consoleFont;
        [SerializeField] private int inputFontSize = 14;
        [SerializeField] private int logFontSize = 20;

        private readonly List<string> logHistory = new();
        private readonly List<string> commandHistory = new();
        private int commandHistoryIndex = -1;
        private CursorLockMode originalCursorLockMode;

        // Autocomplete state
        private string lastTypedPrefix = "";
        private List<string> currentMatches = new List<string>();
        private int autoCompleteIndex = -1;
        private string cachedBaseCommand = "";
        private bool ignoreNextValueChange = false;

        public static event Action<bool> OnConsoleToggled;

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

#if !ENABLE_INPUT_SYSTEM
            if (inputSystem == InputSystemType.New)
            {
                Debug.LogWarning("Developer Console: New Input System is not enabled. Switching to Old Input System.");
                inputSystem = InputSystemType.Old;
            }
#else
            if (inputSystem == InputSystemType.New)
            {
                historyUpAction = new InputAction("HistoryUp", binding: "<Keyboard>/upArrow");
                historyDownAction = new InputAction("HistoryDown", binding: "<Keyboard>/downArrow");
            }
#endif

            GetComponentInChildren<Canvas>().sortingOrder = 1000;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            ConsoleCommandRegistry.OnCacheLoaded += HandleCacheLoaded;
            ConsoleCommandRegistry.LoadCache();

            ConsoleCommandInvoker.LogHandler += LogToConsole;

            ApplyStyles();
        }

        protected virtual void ApplyStyles()
        {
            if (consolePanel.TryGetComponent(out Image consolePanelImage))
            {
                consolePanelImage.color = backgroundColor;
            }

            foreach (var img in consolePanel.GetComponentsInChildren<Image>())
            {
                if (img.gameObject != consolePanel && img.gameObject != inputField.gameObject)
                {
                    img.color = accentColor;
                }
            }

            if (consoleFont != null)
            {
                inputField.textComponent.font = consoleFont;
                consoleLog.font = consoleFont;
            }

            inputField.textComponent.fontSize = inputFontSize;
            consoleLog.fontSize = logFontSize;

            ConsoleCommandInvoker.Settings.TextColor = textColor;
            ConsoleCommandInvoker.Settings.WarningColor = warningColor;
            ConsoleCommandInvoker.Settings.ErrorColor = errorColor;
            ConsoleCommandInvoker.Settings.SecondaryErrorColor = Color.Lerp(errorColor, Color.white, 0.4f);
        }

        private void HandleCacheLoaded(double ms)
        {
            int totalMethods = ConsoleCommandRegistry.Commands.Sum(kv => kv.Value.Count);
            LogToConsole($"[DevConsole] Loaded {totalMethods} commands in {ms:F3} ms.");
            ConsoleCommandRegistry.OnCacheLoaded -= HandleCacheLoaded;
        }

        private void OnEnable()
        {
            if (catchUnityLogs)
                Application.logMessageReceived += HandleLogMessage;

            inputField.onSubmit.AddListener((cmd) =>
            {
                if (string.IsNullOrWhiteSpace(cmd)) return;

                string[] commands = cmd.Split(commandSeparator, StringSplitOptions.RemoveEmptyEntries);

                foreach (var singleCommand in commands)
                {
                    string trimmedCommand = singleCommand.Trim();
                    if (string.IsNullOrEmpty(trimmedCommand)) continue;

                    ConsoleCommandInvoker.Execute(trimmedCommand);
                }

                if (commandHistory.Count == 0 || commandHistory[^1] != cmd)
                {
                    commandHistory.Add(cmd);
                }

                inputField.text = "";
                commandHistoryIndex = -1;
                lastTypedPrefix = "";
                currentMatches.Clear();
                FocusInputField();
            });

            inputField.onValueChanged.AddListener((val) =>
            {
                if (ignoreNextValueChange)
                {
                    ignoreNextValueChange = false;
                    return;
                }
                autoCompleteIndex = -1;
            });

            consolePanel.SetActive(false);

#if ENABLE_INPUT_SYSTEM
            if (inputSystem == InputSystemType.New)
            {
                if (toggleConsoleAction != null)
                {
                    toggleConsoleAction.action.performed += OnToggleConsoleAction;
                    toggleConsoleAction.action.Enable();
                }
                if (historyUpAction != null)
                {
                    historyUpAction.performed += _ => NavigateCommandHistory(-1);
                    historyUpAction.Enable();
                }
                if (historyDownAction != null)
                {
                    historyDownAction.performed += _ => NavigateCommandHistory(1);
                    historyDownAction.Enable();
                }
                if (autoCompleteAction != null)
                {
                    autoCompleteAction.action.performed += _ => AutoComplete();
                    autoCompleteAction.action.Enable();
                }
            }
#endif
        }

        private void OnDisable()
        {
            if (catchUnityLogs)
                Application.logMessageReceived -= HandleLogMessage;

            inputField.onSubmit.RemoveAllListeners();
            inputField.onValueChanged.RemoveAllListeners();

#if ENABLE_INPUT_SYSTEM
            if (inputSystem == InputSystemType.New)
            {
                if (toggleConsoleAction != null)
                    toggleConsoleAction.action.performed -= OnToggleConsoleAction;
                historyUpAction?.Disable();
                historyDownAction?.Disable();
                autoCompleteAction.action?.Disable();
            }
#endif
        }

        private void OnDestroy()
        {
            ConsoleCommandInvoker.LogHandler -= LogToConsole;
        }

        private void Update()
        {
            if (inputSystem == InputSystemType.Old)
            {
                if (Input.GetKeyDown(toggleConsoleKey)) ToggleConsole();

                if (consolePanel.activeSelf && inputField.isFocused)
                {
                    if (Input.GetKeyDown(KeyCode.UpArrow)) NavigateCommandHistory(-1);
                    else if (Input.GetKeyDown(KeyCode.DownArrow)) NavigateCommandHistory(1);
                    else if (Input.GetKeyDown(autoCompleteKey)) AutoComplete();
                }
            }
        }

        public void ShowConsole(bool show)
        {
            if (consolePanel.activeSelf != show)
                ToggleConsole();
        }

        public static void ClearLog()
        {
            if (_instance == null) return;

            _instance.logHistory.Clear();
            _instance.consoleLog.text = "";
        }

        private void HandleLogMessage(string logString, string stackTrace, LogType type)
        {
            string color = type switch
            {
                LogType.Log => FormatColorLocal(textColor),
                LogType.Warning => FormatColorLocal(warningColor),
                LogType.Error => FormatColorLocal(errorColor),
                LogType.Exception => FormatColorLocal(exceptionColor),
                LogType.Assert => FormatColorLocal(assertColor),
                _ => "white",
            };

            LogToConsole($"<color={color}>{logString}</color>");
            return;

            static string FormatColorLocal(Color color)
            {
                return ColorUtility.ToHtmlStringRGBA(color);
            }
        }

        private void ToggleConsole()
        {
            bool isActive = !consolePanel.activeSelf;
            consolePanel.SetActive(isActive);

            if (isActive)
            {
                if (controlCursorLockMode)
                {
                    originalCursorLockMode = Cursor.lockState;
                    Cursor.lockState = CursorLockMode.None;
                }
                FocusInputField();
            }
            else
            {
                if (controlCursorLockMode) Cursor.lockState = originalCursorLockMode;
                inputField.DeactivateInputField();
                inputField.text = "";
                commandHistoryIndex = -1;
            }

            OnConsoleToggled?.Invoke(isActive);
        }

        private void FocusInputField()
        {
            inputField.Select();
            inputField.ActivateInputField();
        }

        private void LogToConsole(string message)
        {
            logHistory.Add(message);
            if (logHistory.Count > maxLogLines)
                logHistory.RemoveRange(0, logHistory.Count - maxLogLines);

            consoleLog.text = string.Join("\n", logHistory);
            StartCoroutine(ScrollToBottomCoroutine());
        }

        private void LogToConsole(string message, bool success)
        {
            string color = success ? ColorUtility.ToHtmlStringRGBA(textColor) : ColorUtility.ToHtmlStringRGBA(errorColor);
            LogToConsole($"<color=#{color}>{message}</color>");
        }

        private IEnumerator ScrollToBottomCoroutine()
        {
            yield return new WaitForEndOfFrame();
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0;
        }

        private void NavigateCommandHistory(int direction)
        {
            if (commandHistory.Count == 0) return;

            if (commandHistoryIndex == -1)
                commandHistoryIndex = commandHistory.Count;

            commandHistoryIndex += direction;

            if (commandHistoryIndex < 0)
            {
                commandHistoryIndex = 0;
            }
            else if (commandHistoryIndex >= commandHistory.Count)
            {
                commandHistoryIndex = commandHistory.Count;
                inputField.text = "";
                return;
            }

            inputField.text = commandHistory[commandHistoryIndex];
            StartCoroutine(MoveCaretToEndCoroutine());
        }

        private IEnumerator MoveCaretToEndCoroutine()
        {
            yield return new WaitForEndOfFrame();
            inputField.MoveTextEnd(false);
        }

        private void AutoComplete()
        {
            string fullInput = inputField.text;

            // Allow empty string to proceed (to show all commands)
            if (fullInput == null) fullInput = "";

            string globalPrefix = "";
            string activeCommand = fullInput;
            int lastSeparatorIndex = fullInput.LastIndexOf(commandSeparator);

            if (lastSeparatorIndex != -1)
            {
                globalPrefix = fullInput.Substring(0, lastSeparatorIndex + 1);
                activeCommand = fullInput.Substring(lastSeparatorIndex + 1);
            }

            string whitespaceBeforeCmd = "";
            if (activeCommand.Length > 0 && char.IsWhiteSpace(activeCommand[0]))
            {
                int trimStart = 0;
                while (trimStart < activeCommand.Length && char.IsWhiteSpace(activeCommand[trimStart]))
                    trimStart++;

                whitespaceBeforeCmd = activeCommand.Substring(0, trimStart);
                activeCommand = activeCommand.TrimStart();
            }

            // --- Regex Tokenizing that handles Empty String ---
            string[] parts;
            if (string.IsNullOrEmpty(activeCommand))
            {
                parts = new string[] { "" }; // Simulate one empty argument
            }
            else
            {
                var tokenMatches = Regex.Matches(activeCommand, @"[\""].+?[\""]|[^ ]+");
                var partsList = tokenMatches.Cast<Match>().Select(m => m.Value).ToList();
                if (activeCommand.EndsWith(" ")) partsList.Add("");
                parts = partsList.ToArray();
            }

            int currentPartIndex = parts.Length - 1;
            string typedPrefix = parts[currentPartIndex];
            string cleanPrefix = typedPrefix.Replace("\"", "");

            bool isHelpArg = (parts.Length > 1 && parts[0].Equals("help", StringComparison.OrdinalIgnoreCase));

            if (autoCompleteIndex == -1 || (typedPrefix != lastTypedPrefix))
            {
                bool isContinuingCycle = (currentMatches.Count > 0 && autoCompleteIndex != -1 && typedPrefix == currentMatches[autoCompleteIndex]);

                if (!isContinuingCycle)
                {
                    autoCompleteIndex = -1;
                    currentMatches.Clear();

                    if (currentPartIndex == 0 || isHelpArg)
                    {
                        currentMatches = ConsoleCommandRegistry.Commands.Keys
                            .Where(k => k.StartsWith(cleanPrefix, StringComparison.OrdinalIgnoreCase))
                            .OrderBy(k => k)
                            .ToList();
                    }
                    else
                    {
                        string commandName = parts[0].ToLower();
                        if (ConsoleCommandRegistry.Commands.TryGetValue(commandName, out List<MethodInfo> methods))
                        {
                            int methodArgIndex = currentPartIndex - 1;
                            HashSet<string> distinctSuggestions = new HashSet<string>();

                            foreach (var method in methods)
                            {
                                var suggestions = ConsoleCommandInvoker.GetAutoCompleteSuggestions(method, methodArgIndex, cleanPrefix);
                                foreach (var s in suggestions) distinctSuggestions.Add(s);
                            }
                            currentMatches = distinctSuggestions.OrderBy(s => s).ToList();
                        }
                    }

                    // Build base string
                    if (parts.Length > 1)
                    {
                        string preSegments = string.Join(" ", parts, 0, parts.Length - 1);
                        cachedBaseCommand = globalPrefix + whitespaceBeforeCmd + preSegments + " ";
                    }
                    else
                    {
                        cachedBaseCommand = globalPrefix + whitespaceBeforeCmd;
                    }
                }
            }

            if (currentMatches.Count == 0) return;

            autoCompleteIndex = (autoCompleteIndex + 1) % currentMatches.Count;
            string selectedMatch = currentMatches[autoCompleteIndex];

            // Quote handling
            if (selectedMatch.Contains(" ") && !selectedMatch.StartsWith("\""))
            {
                selectedMatch = $"\"{selectedMatch}\"";
            }

            ignoreNextValueChange = true;
            inputField.text = cachedBaseCommand + selectedMatch;

            inputField.caretPosition = inputField.text.Length;

            lastTypedPrefix = selectedMatch;
        }

#if ENABLE_INPUT_SYSTEM
        private void OnToggleConsoleAction(InputAction.CallbackContext context) => ToggleConsole();
#endif

        #region Built-in basic commands
        [ConsoleCommand("help", "Shows a list of commands or details for one command.")]
        public static void HelpCommand(Action<string> response, string commandName = "")
        {
            string output = ConsoleCommandInvoker.GetHelp(commandName);
            response(output);
        }

        [ConsoleCommand("clear", "Clears the console log.")]
        public static void ClearCommand() => ClearLog();

        [ConsoleCommand("toggleUnityLogs", "Toggles display of unity debug logs")]
        public static void ToggleUnityLogsCommand(Action<string> response)
        {
            if (_instance == null) return;
            _instance.catchUnityLogs = !_instance.catchUnityLogs;

            if (_instance.catchUnityLogs)
                Application.logMessageReceived += _instance.HandleLogMessage;
            else
                Application.logMessageReceived -= _instance.HandleLogMessage;

            var color = _instance.catchUnityLogs ? "green" : "red";
            response($"Unity logs are now <color={color}>{(_instance.catchUnityLogs ? "enabled" : "disabled")}</color>");
        }
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyStyles();
        }
#endif
    }
}