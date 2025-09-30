using System.Linq;
using UnityEngine;

namespace NoSlimes.Util.DevCon
{
    public static class BuiltInCommands
    {
        #region Application

        [ConsoleCommand("quit", "Quits the application.")]
        public static void QuitCommand()
        {
            Debug.Log("Quitting application...");
            Application.Quit();
        }

        [ConsoleCommand("crash", "Crashes the application (for testing purposes).")]
        public static void CrashCommand()
        {
            Debug.Log("Crashing application...");
            UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.Abort);
        }

        [ConsoleCommand("version", "Prints the application version.")]
        public static void VersionCommand() =>
            Debug.Log($"Application version: {Application.version}");

        [ConsoleCommand("platform", "Prints the current runtime platform.")]
        public static void PlatformCommand() =>
            Debug.Log($"Running on: {Application.platform}");

        [ConsoleCommand("dataPath", "Prints the data path of the application.")]
        public static void DataPathCommand() =>
            Debug.Log($"Data path: {Application.dataPath}");

        [ConsoleCommand("persistentDataPath", "Prints the persistent data path.")]
        public static void PersistentDataPathCommand() =>
            Debug.Log($"Persistent data path: {Application.persistentDataPath}");

        [ConsoleCommand("setTargetFPS", "Sets Application.targetFrameRate.")]
        public static void SetTargetFPSCommand(int fps)
        {
            Application.targetFrameRate = fps;
            Debug.Log($"Target frame rate set to {fps}");
        }

        [ConsoleCommand("uptime", "Prints the time since startup.")]
        public static void UptimeCommand() =>
            Debug.Log($"Uptime: {Time.realtimeSinceStartup:F2} seconds");

        #endregion

        #region Scene Management

        [ConsoleCommand("reloadScene", "Reloads the current scene.")]
        public static void ReloadSceneCommand()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Debug.Log($"Reloading scene: {scene.name}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        [ConsoleCommand("loadScene", "Loads a scene by name.")]
        public static void LoadSceneCommand(string sceneName)
        {
            Debug.Log($"Loading scene: {sceneName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        #endregion

        #region Time & Physics

        [ConsoleCommand("gravityScale", "Sets the global gravity scale.")]
        public static void GravityScaleCommand(float scale)
        {
            Physics.gravity = new Vector3(0, -9.81f * scale, 0);
            Debug.Log($"Global gravity scale set to {scale}. New gravity: {Physics.gravity}");
        }

        [ConsoleCommand("timeScale", "Sets the global time scale.")]
        public static void TimeScaleCommand(float scale)
        {
            Time.timeScale = scale;
            Debug.Log($"Global time scale set to {scale}.");
        }

        [ConsoleCommand("fixedDeltaTime", "Sets Time.fixedDeltaTime.")]
        public static void FixedDeltaTimeCommand(float seconds)
        {
            Time.fixedDeltaTime = seconds;
            Debug.Log($"FixedDeltaTime set to {seconds}");
        }

        #endregion

        #region Graphics & Quality

        [ConsoleCommand("vsync", "Sets VSync count (0 = off, 1 = every vsync, 2 = every 2nd vsync).")]
        public static void VSyncCommand(int count)
        {
            QualitySettings.vSyncCount = count;
            Debug.Log($"VSync set to {count}");
        }

        [ConsoleCommand("setQuality", "Sets the graphics quality level by index or name.")]
        public static void SetQualityCommand(string quality)
        {
            if (int.TryParse(quality, out var index))
                QualitySettings.SetQualityLevel(index, true);
            else
                QualitySettings.SetQualityLevel(QualitySettings.names.ToList().IndexOf(quality), true);

            Debug.Log($"Graphics quality set to {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        }

        [ConsoleCommand("listQuality", "Lists available graphics quality levels.")]
        public static void ListQualityCommand() =>
            Debug.Log("Available quality levels: " + string.Join(", ", QualitySettings.names));

        [ConsoleCommand("fullscreen", "Toggles fullscreen mode.")]
        public static void FullscreenCommand(bool enabled)
        {
            Screen.fullScreen = enabled;
            Debug.Log($"Fullscreen set to {enabled}");
        }

        [ConsoleCommand("resolutions", "Lists supported screen resolutions.")]
        public static void ResolutionsCommand()
        {
            var resolutions = Screen.resolutions.Select(r => $"{r.width}x{r.height}@{r.refreshRate}Hz");
            Debug.Log("Supported resolutions:\n" + string.Join("\n", resolutions));
        }

        [ConsoleCommand("setResolution", "Sets the screen resolution (width, height, fullscreen).")]
        public static void SetResolutionCommand(int width, int height, bool fullscreen = true)
        {
            Screen.SetResolution(width, height, fullscreen);
            Debug.Log($"Resolution set to {width}x{height}, fullscreen={fullscreen}");
        }

        #endregion

        #region Camera & Debug

        [ConsoleCommand("setFOV", "Sets the main camera's field of view.")]
        public static void SetFOVCommand(float fov)
        {
            if (Camera.main != null)
            {
                Camera.main.fieldOfView = fov;
                Debug.Log($"Main camera FOV set to {fov}");
            }
            else Debug.LogWarning("No main camera found.");
        }

        [ConsoleCommand("toggleWireframe", "Toggles wireframe rendering.")]
        public static void ToggleWireframeCommand(bool enabled)
        {
            GL.wireframe = enabled;
            Debug.Log($"Wireframe mode: {enabled}");
        }

        [ConsoleCommand("screenshot", "Takes a screenshot and saves it.")]
        public static void ScreenshotCommand(string filename = "screenshot.png")
        {
            ScreenCapture.CaptureScreenshot(filename);
            Debug.Log($"Screenshot saved: {filename}");
        }

        #endregion

        #region System Information

        [ConsoleCommand("systemInfo", "Prints system information (GPU, CPU, RAM).")]
        public static void SystemInfoCommand()
        {
            Debug.Log($"Device: {SystemInfo.deviceName} ({SystemInfo.deviceModel})");
            Debug.Log($"OS: {SystemInfo.operatingSystem}");
            Debug.Log($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            Debug.Log($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB VRAM)");
            Debug.Log($"RAM: {SystemInfo.systemMemorySize} MB");
        }

        #endregion

        #region PlayerPrefs

        [ConsoleCommand("setPref", "Sets a PlayerPref (string).")]
        public static void SetPrefCommand(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            Debug.Log($"PlayerPref set: {key} = {value}");
        }

        [ConsoleCommand("getPref", "Gets a PlayerPref (string).")]
        public static void GetPrefCommand(string key)
        {
            string value = PlayerPrefs.GetString(key, "(not found)");
            Debug.Log($"PlayerPref: {key} = {value}");
        }

        [ConsoleCommand("delPref", "Deletes a PlayerPref key.")]
        public static void DeletePrefCommand(string key)
        {
            PlayerPrefs.DeleteKey(key);
            Debug.Log($"Deleted PlayerPref: {key}");
        }

        [ConsoleCommand("clearPrefs", "Clears all PlayerPrefs.")]
        public static void ClearPrefsCommand()
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("All PlayerPrefs cleared.");
        }

        #endregion
    }
}
