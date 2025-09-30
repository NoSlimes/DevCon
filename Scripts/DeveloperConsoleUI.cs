using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

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
        [SerializeField] private bool controlCursorLockMode = false;

        private ConsoleCommandRegistry registry;
        private ConsoleInvoker invoker;

        private readonly List<string> logHistory = new();
        private readonly List<string> commandHistory = new();
        private int commandHistoryIndex = -1;
        private CursorLockMode originalCursorLockMode;

        public static ConsoleCommandRegistry Registry => _instance != null ? _instance.registry : null;
        public static ConsoleInvoker Invoker => _instance != null ? _instance.invoker : null;

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

            registry = new ConsoleCommandRegistry();
            registry.DiscoverCommands();

            invoker = new ConsoleInvoker(registry);
            invoker.LogHandler = LogToConsole;
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLogMessage;
            inputField.onSubmit.AddListener((cmd) =>
            {
                if (string.IsNullOrWhiteSpace(cmd)) return;

                invoker.Execute(cmd);
                commandHistory.Add(cmd);
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
            string currentText = inputField.text;
            if (string.IsNullOrWhiteSpace(currentText))
                return;

            string[] parts = currentText.Split(' ');
            string commandPrefix = parts[0].ToLower();

            if (commandPrefix == "help")
            {
                string argPrefix = parts.Length > 1 ? parts[1].ToLower() : "";

                var matches = new List<string>();
                foreach (var kv in registry.Commands.Keys)
                {
                    if (string.IsNullOrEmpty(argPrefix) || kv.StartsWith(argPrefix, StringComparison.OrdinalIgnoreCase))
                        matches.Add(kv);
                }

                if (matches.Count == 0)
                    return;

                if (matches.Count == 1)
                {
                    parts = new string[] { "help", matches[0] };
                    inputField.text = string.Join(" ", parts);
                    StartCoroutine(MoveCaretToEndCoroutine());
                }
                else
                {
                    LogToConsole("Help matches: " + string.Join(", ", matches));
                }

                return;
            }

            var commandMatches = new List<string>();
            foreach (var kv in registry.Commands.Keys)
            {
                if (kv.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase))
                    commandMatches.Add(kv);
            }

            if (commandMatches.Count == 0)
                return;

            if (commandMatches.Count == 1)
            {
                parts[0] = commandMatches[0];
                inputField.text = string.Join(" ", parts);
                StartCoroutine(MoveCaretToEndCoroutine());
            }
            else
            {
                LogToConsole("Matches: " + string.Join(", ", commandMatches));
            }
        }


#if ENABLE_INPUT_SYSTEM
        private void OnToggleConsoleAction(InputAction.CallbackContext context) => ToggleConsole();
#endif

        #region Built-in basic commands
        [ConsoleCommand("help", "Shows a list of commands or details for one command.")]
        public static void HelpCommand(string commandName = "")
        {
            string output = _instance.invoker.GetHelp(commandName);
            UnityEngine.Debug.Log(output);
        }

        [ConsoleCommand("clear", "Clears the console log.")]
        public static void ClearCommand() => ClearLog();
        #endregion
    }
}
