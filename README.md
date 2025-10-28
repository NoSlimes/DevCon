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

### 5. Auto-completion

* DevCon supports tab-based auto-completion for commands and their arguments.
* Partial command names are matched case-insensitively.

---
