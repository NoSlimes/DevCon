## Setup

Use the menu to create and add the Developer Console prefab to your scene:  
- `Assets → Create → DevCon → Developer Console`  
- `Tools → DevCon → Create Developer Console Prefab`  

This will create a prefab variant in the folder currently open in the Project window.

## Command Parameters

When defining commands, methods can have the following parameters:

### 1. Response callback (optional)

```csharp
Action<string> response        // Receives console messages
Action<string, bool> response  // Receives message + success/failure
````

* `string` = message to log
* `bool` = whether the command succeeded

**Example:**

```csharp
[ConsoleCommand("setFOV", "Sets main camera FOV.")]
public static void SetFOVCommand(Action<string, bool> response, float fov)
{
    Camera.main.fieldOfView = fov;
    response($"FOV set to {fov}", true);
}
```

---

### 2. Arguments

DevCon supports a wide range of argument types, with automatic type conversion from strings:

| Type | Description | Example Input |
|------|-------------|---------------|
| `string` | Any text. Use quotes if it contains spaces. | `"Hello world"` |
| `int` | Integer numbers. | `42` |
| `float` | Decimal numbers. | `3.14` |
| `bool` | True or false. | `true` / `false` |
| `enum` | Any enum type. Matches enum names (case-insensitive). | `MoveMode.Walk` |
| `Vector2` | 2D vector `(x,y)` format. | `(1.0,2.5)` |
| `Vector3` | 3D vector `(x,y,z)` format. | `(0,1,0)` |
| `Color` | RGBA color `(r,g,b,a)` format. | `(1,0,0,1)` for opaque red |
| `Quaternion` | Rotation `(x,y,z,w)` format. | `(0,0,0,1)` |

#### Notes

* **Default values:** Arguments can have defaults if omitted:

```csharp
[ConsoleCommand("screenshot", "Takes a screenshot.")]
public static void ScreenshotCommand(Action<string> response, string filename = "screenshot.png")
{
    ScreenCapture.CaptureScreenshot(filename);
    response($"Saved screenshot as {filename}", true);
}
````

* **Multiple arguments:** Separate with spaces. Strings containing spaces must be quoted:

```
spawnEnemy "Big Slime" (0,1,0) true
```

* **Type conversion:** DevCon automatically converts argument strings to the required type. Errors are logged if conversion fails.

* **Nullable types:** Nullable parameters are supported, e.g., `int?`, `float?`.

* **Custom argument types:** You can register your own converters for custom types. For example, the built-in `Vector3` converter:

```csharp
ConsoleCommandInvoker.RegisterArgConverter<Vector3>(arg =>
{
    var parts = arg.Trim('(', ')').Split(',');
    if (parts.Length != 3)
        throw new ArgumentException($"Could not convert '{arg}' to Vector3");

    return new Vector3(
        float.Parse(parts[0]),
        float.Parse(parts[1]),
        float.Parse(parts[2])
    );
});
```

Once registered, DevCon will automatically convert arguments of that type when invoking commands.

---

### 3. Command Execution

Commands are invoked by typing in the console:

```
<command> [arg1] [arg2] ...
```

* The first parameter can optionally be a response callback (`Action<string>` or `Action<string, bool>`).
* Remaining parameters are parsed and converted automatically.
* Quoted strings are supported: `"Hello World"`.
* Errors such as missing or invalid arguments are automatically logged to the console.

---

### 4. Chained Commands

Multiple commands can be executed in a single line using the `|`(configurable) separator:

```
command1 arg1 | command2 arg2 arg3
```

Each command runs sequentially, and errors in one command do not prevent subsequent commands from executing.

---

### 5. Command Flags

Commands can include **flags** to control when and how they are available. Flags are optional and can be combined using the bitwise OR operator (`|`).

| Flag | Description |
|------|-------------|
| `None` | No special behavior. Command is always available. |
| `DebugOnly` | Command is only available in debug builds. Cannot be combined with `EditorOnly`. |
| `EditorOnly` | Command is only available in the Unity editor. Cannot be combined with `DebugOnly`. |
| `Cheat` | Marks the command as a cheat. Only runs if `CheatsEnabled` is `true`. |
| `Mod` | Command is added by a mod or external plugin. |
| `Hidden` | Command is hidden from help listings, but can still be invoked. |

**Cheat Commands Note:**

* DevCon includes a built-in `enablecheats` command to toggle cheat commands globally.  
* Alternatively, you can disable the built-in command and provide your own mechanism for enabling cheats by setting `ConsoleCommandInvoker.CheatsEnabled` manually.  
* Commands with the `Cheat` flag will only run if `CheatsEnabled` is `true`.

**Example:**

```csharp
[ConsoleCommand("godMode", "Enables invincibility.", CommandFlags.Cheat | CommandFlags.DebugOnly)]
public static void GodModeCommand(Action<string> response)
{
    Player.Instance.Invincible = true;
    response("God mode enabled!", true);
}
```
---

Here is the updated section 6, expanded to include the new functionality for registering auto-complete methods.

---

### 6. Auto-completion & Suggestions

DevCon supports tab-based auto-completion for both command names and argument values.

*   **Command Names:** unmatched text is automatically completed against registered commands (case-insensitive).
*   **Built-in Types:** Arguments of type `bool` (true/false) and `enum` are automatically auto-completed.

#### Custom Argument Suggestions

You can provide dynamic suggestions for your string arguments (e.g., Item IDs, Enemy Names) by referencing a static method in the `[ConsoleCommand]` attribute.

**How to register:**
1.  Create a `static` method in the same class that returns `IEnumerable<string>` (or `string[]`).
2.  Pass the method's name to the `autoCompleteMethod` parameter in the attribute.

**Example 1: Simple List (System handles filtering)**
Useful for small lists. The system retrieves all options and filters them based on what the user typed.

```csharp
[ConsoleCommand("spawn", "Spawns an entity.", autoCompleteMethod: nameof(GetEntityNames))]
public static void SpawnCommand(string entityName)
{
    // Spawn logic...
}

// Can be private, must be static
private static IEnumerable<string> GetEntityNames()
{
    return new[] { "Slime", "Goblin", "Dragon", "Skeleton" };
}
```

**Example 2: Advanced Filtering (You handle filtering)**
Useful for large datasets (like item databases). If your method accepts a `string` parameter, DevCon will pass the current input prefix to you, allowing you to optimize the search.

```csharp
[ConsoleCommand("give", "Gives an item.", autoCompleteMethod: nameof(SearchItems))]
public static void GiveCommand(string itemId) { ... }

// The 'prefix' contains what the user has typed so far (e.g., "Swor")
private static IEnumerable<string> SearchItems(string prefix)
{
    return ItemDatabase.AllItems
        .Where(item => item.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        .Select(item => item.Name);
}
```

*Note: If a suggestion contains spaces (e.g., `"Big Slime"`), it will automatically be wrapped in quotes when selected.*

---
