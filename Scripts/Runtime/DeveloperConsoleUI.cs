using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        private readonly List<string> logHistory = new();
        private readonly List<string> commandHistory = new();
        private int commandHistoryIndex = -1;
        private CursorLockMode originalCursorLockMode;

        private string lastTypedPrefix = "";
        private List<string> currentMatches = new List<string>();
        private int autoCompleteIndex = -1;

        public static event Action<bool> OnConsoleToggled;

        private void Awake()
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

            ConsoleCommandInvoker.LogHandler = LogToConsole;
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
                FocusInputField();
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
                LogType.Log => "white",
                LogType.Warning => "yellow",
                LogType.Error => "red",
                LogType.Exception => "red",
                _ => "white",
            };

            LogToConsole($"<color={color}>{logString}</color>");
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
            string color = success ? "white" : "red";
            LogToConsole($"<color={color}>{message}</color>");
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
            if (string.IsNullOrWhiteSpace(fullInput)) return;

            string commandPrefix = ""; 
            string activeCommand;      

            int lastSeparatorIndex = fullInput.LastIndexOf(commandSeparator);

            if (lastSeparatorIndex != -1)
            {
                commandPrefix = fullInput.Substring(0, lastSeparatorIndex + 1);
                activeCommand = fullInput.Substring(lastSeparatorIndex + 1);
            }
            else
            {
                commandPrefix = "";
                activeCommand = fullInput;
            }

            string trimmedActiveCommand = activeCommand.TrimStart();
            if (string.IsNullOrEmpty(trimmedActiveCommand)) return;

            string[] parts = trimmedActiveCommand.Split(' ');
            bool isHelpArg = parts.Length > 1 && parts[0].ToLower() == "help";
            string typedPrefix = isHelpArg ? (parts.Length > 1 ? parts[1] : "") : parts[0];

            if (typedPrefix != lastTypedPrefix)
            {
                lastTypedPrefix = typedPrefix;
                autoCompleteIndex = -1;

                currentMatches = ConsoleCommandRegistry.Commands.Keys
                    .Where(k => k.StartsWith(typedPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (currentMatches.Count > 1)
                {
                    LogToConsole((isHelpArg ? "Help matches: " : "Matches: ") + string.Join(", ", currentMatches));
                }
                else if (currentMatches.Count == 1)
                {
                    if (isHelpArg) parts[1] = currentMatches[0];
                    else parts[0] = currentMatches[0];

                    string completedSegment = string.Join(" ", parts);

                    string leadingWhitespace = activeCommand.Substring(0, activeCommand.Length - trimmedActiveCommand.Length);
                    inputField.text = commandPrefix + leadingWhitespace + completedSegment;

                    lastTypedPrefix = isHelpArg ? parts[1] : parts[0];
                    StartCoroutine(MoveCaretToEndCoroutine());
                    return;
                }
                else
                {
                    currentMatches.Clear();
                }

                return; 
            }

            if (currentMatches.Count == 0) return;

            autoCompleteIndex = (autoCompleteIndex + 1) % currentMatches.Count;

            if (isHelpArg) parts[1] = currentMatches[autoCompleteIndex];
            else parts[0] = currentMatches[autoCompleteIndex];

            string cycledSegment = string.Join(" ", parts);

            string leadingWhitespaceCycle = activeCommand.Substring(0, activeCommand.Length - trimmedActiveCommand.Length);
            inputField.text = commandPrefix + leadingWhitespaceCycle + cycledSegment;

            lastTypedPrefix = isHelpArg ? parts[1] : parts[0];
            StartCoroutine(MoveCaretToEndCoroutine());
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
    }
}
