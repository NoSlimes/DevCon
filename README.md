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

### 2. Arguments

Supported argument types:

* `string` — text
* `int` — integer numbers
* `float` — decimal numbers
* `bool` — true/false
* `enum` — any enum type

Arguments can also have default values:

```csharp
[ConsoleCommand("screenshot", "Takes a screenshot.")]
public static void ScreenshotCommand(Action<string> response, string filename = "screenshot.png")
{
    ScreenCapture.CaptureScreenshot(filename);
    response($"Saved screenshot as {filename}", true);
}
```

### 3. Command Execution

Commands are invoked by typing in the console:

```
<command> [arg1] [arg2] ...
```

* Quoted strings are supported: `"Hello World"`
* First parameter can optionally be a callback (`Action<string>` or `Action<string, bool>`).
* Remaining parameters are automatically converted from strings.
* Errors are logged automatically if arguments are missing or invalid.


## Notes

* Commands can be **modified** and **extended** freely.
* The response callback allows commands to report success or failure.
