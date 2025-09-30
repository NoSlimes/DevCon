using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine.UIElements;

using Cursor = UnityEngine.Cursor;
using UnityEngine.UI;
using System.Collections;
using System;

namespace NoSlimes.Util.DeveloperConsole
{
    public class DeveloperConsole : MonoBehaviour
    {
        public enum InputSystemType { New, Old }

        [SerializeField] private InputSystemType inputSystem = InputSystemType.Old;

#if ENABLE_INPUT_SYSTEM
        [Tooltip("The InputActionReference for toggling the console (New Input System).")]
        [SerializeField] private InputActionReference toggleConsoleAction;

        private InputAction historyUpAction;
        private InputAction historyDownAction;

#endif

        [Tooltip("The KeyCode for toggling the console (Old Input System).")]
        [SerializeField] private KeyCode toggleConsoleKey = KeyCode.BackQuote;

        [SerializeField] private GameObject consolePanel;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TMP_Text consoleLog;
        [SerializeField] private int maxLogLines = 100;

        [Tooltip("Keeps the console active when scene changes.")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool controlCursorLockMode = false;

        private Dictionary<string, MethodInfo> commands;
        private readonly List<string> logHistory = new();

        private readonly List<string> commandHistory = new();
        private int commandHistoryIndex = -1;

        private CursorLockMode originalCursorLockMode;

        public static event Action<bool> OnConsoleToggled;

        private void Awake()
        {
#if !ENABLE_INPUT_SYSTEM
            if (inputSystem == InputSystemType.New)
            {
                Debug.LogWarning("Developer Console: New Input System is not enabled in Player Settings. Switching to Old Input System.");
                inputSystem = InputSystemType.Old;
            }
#else
            if (inputSystem == InputSystemType.New)
            {
                historyUpAction = new InputAction("HistoryUp", binding: "<Keyboard>/upArrow");
                historyDownAction = new InputAction("HistoryDown", binding: "<Keyboard>/downArrow");
            }
#endif

            GetComponentInChildren<Canvas>().sortingOrder = 1000; // Ensure console is on top of other UI elements

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            DiscoverCommands();
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLogMessage;
            inputField.onSubmit.AddListener(ProcessCommand);

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
                    historyUpAction.performed += OnHistoryUpAction;
                    historyUpAction.Enable();
                }
                if (historyDownAction != null)
                {
                    historyDownAction.performed += OnHistoryDownAction;
                    historyDownAction.Enable();
                }
            }
#endif
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLogMessage;
            inputField.onSubmit.RemoveListener(ProcessCommand);

#if ENABLE_INPUT_SYSTEM
            if (inputSystem == InputSystemType.New)
            {
                if (toggleConsoleAction != null)
                {
                    toggleConsoleAction.action.performed -= OnToggleConsoleAction;
                }
                if (historyUpAction != null)
                {
                    historyUpAction.performed -= OnHistoryUpAction;
                    historyUpAction.Disable();
                }
                if (historyDownAction != null)
                {
                    historyDownAction.performed -= OnHistoryDownAction;
                    historyDownAction.Disable();
                }
            }
#endif
        }

        private void Update()
        {
            if (inputSystem != InputSystemType.Old)
                return;

            if (Input.GetKeyDown(toggleConsoleKey))
            {
                ToggleConsole();
            }

            if (consolePanel.activeSelf && inputField.isFocused)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    NavigateCommandHistory(1);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    NavigateCommandHistory(-1);
                }
            }
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

        private void DiscoverCommands()
        {
            commands = new Dictionary<string, MethodInfo>();
            var methods = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                .Where(m => m.GetCustomAttributes(typeof(ConsoleCommandAttribute), false).Length > 0);

            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                var parameters = method.GetParameters();

                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string[]))
                {
                    Debug.LogWarning(
                        $"[DeveloperConsole] Command '{attribute.Command}' in class '{method.DeclaringType.Name}' has an invalid signature. " +
                        $"Methods must have exactly one parameter of type 'string[]'.");
                    continue;
                }

                string commandName = attribute.Command.ToLower();
                if (!commands.ContainsKey(commandName))
                {
                    commands.Add(commandName, method);
                }
                else
                {
                    var existingMethod = commands[commandName];
                    Debug.LogWarning($"[DeveloperConsole] Duplicate command found: '{commandName}'. " +
                                     $"Defined in both '{existingMethod.DeclaringType.Name}' and '{method.DeclaringType.Name}'. " +
                                     $"The one in '{existingMethod.DeclaringType.Name}' will be used.");
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        private void OnToggleConsoleAction(InputAction.CallbackContext context)
        {
            ToggleConsole();
        }

        private void OnHistoryUpAction(InputAction.CallbackContext context)
        {
            if (consolePanel.activeSelf && inputField.isFocused)
                NavigateCommandHistory(1);
        }

        private void OnHistoryDownAction(InputAction.CallbackContext context)
        {
            if (consolePanel.activeSelf && inputField.isFocused)
                NavigateCommandHistory(-1);
        }
#endif

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
                if (controlCursorLockMode)
                    Cursor.lockState = originalCursorLockMode;

                inputField.DeactivateInputField();
            }

            OnConsoleToggled?.Invoke(isActive);
        }

        private void FocusInputField()
        {
            inputField.Select();
            inputField.ActivateInputField();
        }

        private void ProcessCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;

            LogToConsole("> " + input);

            commandHistory.Insert(0, input);
            commandHistoryIndex = -1;

            string[] parts = input.Split(' ');
            string command = parts[0].ToLower();
            string[] args = parts.Length > 1 ? parts.Skip(1).ToArray() : new string[0];

            if (commands.ContainsKey(command))
            {
                MethodInfo methodInfo = commands[command];
                var attribute = methodInfo.GetCustomAttribute<ConsoleCommandAttribute>();

                if (args.Length < attribute.MinArgs)
                {
                    string argsInfo = attribute.ArgNames.Length > 0
                        ? string.Join(" ", attribute.ArgNames.Select(a => $"<{a}>"))
                        : "";
                    LogToConsole($"<color=red>Error: Command '{command}' requires at least {attribute.MinArgs} argument(s). Usage: {attribute.Command} {argsInfo}</color>");
                }
                else if (attribute.MaxArgs != -1 && args.Length > attribute.MaxArgs)
                {
                    string argsInfo = attribute.ArgNames.Length > 0
                        ? string.Join(" ", attribute.ArgNames.Select(a => $"<{a}>"))
                        : "";
                    LogToConsole($"<color=red>Error: Command '{command}' accepts at most {attribute.MaxArgs} argument(s). Usage: {attribute.Command} {argsInfo}</color>");
                }
                else
                {
                    object target = methodInfo.IsStatic ? null : FindFirstObjectByType(methodInfo.DeclaringType);
                    if (target != null || methodInfo.IsStatic)
                    {
                        methodInfo.Invoke(target, new object[] { args });
                    }
                    else
                    {
                        LogToConsole($"<color=red>Error: Could not find an active instance of the script containing the command '{command}'. Consider making the command method static.</color>");
                    }
                }
            }
            else
            {
                LogToConsole($"<color=yellow>Unknown command: '{command}'. Type 'help' for a list of commands.</color>");
            }

            inputField.text = "";
            FocusInputField();
        }

        private void LogToConsole(string message)
        {
            logHistory.Add(message);

            if (logHistory.Count > maxLogLines)
            {
                logHistory.RemoveRange(0, logHistory.Count - maxLogLines);
            }

            StringBuilder sb = new StringBuilder();
            foreach (var line in logHistory)
            {
                sb.AppendLine(line);
            }
            consoleLog.text = sb.ToString();

            StartCoroutine(ScrollToBottomCoroutine());
        }

        private IEnumerator ScrollToBottomCoroutine()
        {
            yield return new WaitForEndOfFrame();

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0;
        }

        private void NavigateCommandHistory(int direction)
        {
            if (commandHistory.Count == 0) return;

            commandHistoryIndex += direction;

            if (commandHistoryIndex < 0)
            {
                commandHistoryIndex = -1;
                inputField.text = "";
            }
            else if (commandHistoryIndex >= commandHistory.Count)
            {
                commandHistoryIndex = commandHistory.Count - 1;
            }

            if (commandHistoryIndex >= 0)
            {
                inputField.text = commandHistory[commandHistoryIndex];
                StartCoroutine(MoveCaretToEndCoroutine());
            }
        }

        private IEnumerator MoveCaretToEndCoroutine()
        {
            yield return new WaitForEndOfFrame();
            inputField.MoveTextEnd(false);
        }

        // --- BUILT-IN COMMANDS ---

        [ConsoleCommand("help", "Displays a list of all available commands or details for a specific command.", 0, 1, "commandName")]
        public void ShowHelp(string[] args)
        {
            StringBuilder helpBuilder = new StringBuilder();

            if (args.Length == 0)
            {
                helpBuilder.AppendLine("Available Commands:");

                foreach (var command in commands.OrderBy(c => c.Key))
                {
                    var attribute = command.Value.GetCustomAttribute<ConsoleCommandAttribute>();
                    string argsInfo = attribute.ArgNames.Length > 0
                        ? string.Join(" ", attribute.ArgNames.Select(a => $"<{a}>"))
                        : "";

                    helpBuilder.AppendLine($"{attribute.Command} {argsInfo} - {attribute.Description}");
                }
            }
            else
            {
                string cmdName = args[0].ToLower();

                if (commands.TryGetValue(cmdName, out var method))
                {
                    var attribute = method.GetCustomAttribute<ConsoleCommandAttribute>();
                    string argsInfo = attribute.ArgNames.Length > 0
                        ? string.Join(" ", attribute.ArgNames.Select(a => $"<{a}>"))
                        : "";

                    helpBuilder.AppendLine($"Command: {attribute.Command}");
                    helpBuilder.AppendLine($"Description: {attribute.Description}");

                    if (attribute.ArgNames.Length > 0)
                        helpBuilder.AppendLine($"Arguments: {argsInfo}");

                    helpBuilder.AppendLine($"Min Args: {attribute.MinArgs}, Max Args: {(attribute.MaxArgs < 0 ? "unlimited" : attribute.MaxArgs)}");
                }
                else
                {
                    helpBuilder.AppendLine($"<color=yellow>Unknown command: '{cmdName}'</color>");
                }
            }

            LogToConsole(helpBuilder.ToString());
        }


        [ConsoleCommand("clear", "Clears the console log.")]
        public void ClearConsole(string[] args)
        {
            logHistory.Clear();
            consoleLog.text = "";
        }
    }
}