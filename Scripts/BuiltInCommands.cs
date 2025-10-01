using System;
using System.Linq;
using UnityEngine;

namespace NoSlimes.Util.DevCon
{
    public static class BuiltInCommands
    {
        #region Application

        [ConsoleCommand("quit", "Quits the application.")]
        public static void QuitCommand(Action<string> response)
        {
            response("Quitting application...");
            Application.Quit();
        }

        [ConsoleCommand("crash", "Crashes the application (for testing purposes).")]
        public static void CrashCommand(Action<string> response)
        {
            response("Crashing application...");
            UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.Abort);
        }

        [ConsoleCommand("version", "Prints the application version.")]
        public static void VersionCommand(Action<string> response) =>
            response($"Application version: {Application.version}");

        [ConsoleCommand("platform", "Prints the current runtime platform.")]
        public static void PlatformCommand(Action<string> response) =>
            response($"Running on: {Application.platform}");

        [ConsoleCommand("dataPath", "Prints the data path of the application.")]
        public static void DataPathCommand(Action<string> response) =>
            response($"Data path: {Application.dataPath}");

        [ConsoleCommand("persistentDataPath", "Prints the persistent data path.")]
        public static void PersistentDataPathCommand(Action<string> response) =>
            response($"Persistent data path: {Application.persistentDataPath}");

        [ConsoleCommand("setTargetFPS", "Sets Application.targetFrameRate.")]
        public static void SetTargetFPSCommand(Action<string> response, int fps)
        {
            Application.targetFrameRate = fps;
            response($"Target frame rate set to {fps}");
        }

        [ConsoleCommand("uptime", "Prints the time since startup.")]
        public static void UptimeCommand(Action<string> response) =>
            response($"Uptime: {Time.realtimeSinceStartup:F2} seconds");

        #endregion

        #region Scene Management

        [ConsoleCommand("reloadScene", "Reloads the current scene.")]
        public static void ReloadSceneCommand(Action<string> response)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            response($"Reloading scene: {scene.name}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        [ConsoleCommand("loadScene", "Loads a scene by name.")]
        public static void LoadSceneCommand(Action<string> response, string sceneName)
        {
            response($"Loading scene: {sceneName}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        [ConsoleCommand("listScenes", "Lists all scenes in the build settings.")]
        public static void ListScenesCommand(Action<string> response)
        {
            var scenes = Enumerable
                .Range(0, UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
                .Select(i => System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)));

            response("Scenes in build settings: " + string.Join(", ", scenes));
        }

        #endregion

        #region Time & Physics

        [ConsoleCommand("gravityScale", "Sets the global gravity scale.")]
        public static void GravityScaleCommand(Action<string> response, float scale)
        {
            Physics.gravity = new Vector3(0, -9.81f * scale, 0);
            response($"Global gravity scale set to {scale}. New gravity: {Physics.gravity}");
        }

        [ConsoleCommand("timeScale", "Sets the global time scale.")]
        public static void TimeScaleCommand(Action<string> response, float scale)
        {
            Time.timeScale = scale;
            response($"Global time scale set to {scale}.");
        }

        [ConsoleCommand("fixedDeltaTime", "Sets Time.fixedDeltaTime.")]
        public static void FixedDeltaTimeCommand(Action<string> response, float seconds)
        {
            Time.fixedDeltaTime = seconds;
            response($"FixedDeltaTime set to {seconds}");
        }

        #endregion

        #region Graphics & Quality

        [ConsoleCommand("vsync", "Sets VSync count (0 = off, 1 = every vsync, 2 = every 2nd vsync).")]
        public static void VSyncCommand(Action<string> response, int count)
        {
            QualitySettings.vSyncCount = count;
            response($"VSync set to {count}");
        }

        [ConsoleCommand("setQuality", "Sets the graphics quality level by index or name.")]
        public static void SetQualityCommand(Action<string> response, string quality)
        {
            if (int.TryParse(quality, out var index))
                QualitySettings.SetQualityLevel(index, true);
            else
                QualitySettings.SetQualityLevel(QualitySettings.names.ToList().IndexOf(quality), true);

            response($"Graphics quality set to {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        }

        [ConsoleCommand("listQuality", "Lists available graphics quality levels.")]
        public static void ListQualityCommand(Action<string> response) =>
            response("Available quality levels: " + string.Join(", ", QualitySettings.names));

        [ConsoleCommand("fullscreen", "Toggles fullscreen mode.")]
        public static void FullscreenCommand(Action<string> response, bool enabled)
        {
            Screen.fullScreen = enabled;
            response($"Fullscreen set to {enabled}");
        }

        [ConsoleCommand("resolutions", "Lists supported screen resolutions.")]
        public static void ResolutionsCommand(Action<string> response)
        {
            var resolutions = Screen.resolutions.Select(r => $"{r.width}x{r.height}@{r.refreshRateRatio}Hz");
            response("Supported resolutions:\n" + string.Join("\n", resolutions));
        }

        [ConsoleCommand("setResolution", "Sets the screen resolution (width, height, fullscreen).")]
        public static void SetResolutionCommand(Action<string> response, int width, int height, bool fullscreen)
        {
            Screen.SetResolution(width, height, fullscreen);
            response($"Resolution set to {width}x{height}, fullscreen={fullscreen}");
        }

        #endregion

        #region Camera & Debug

        [ConsoleCommand("setFOV", "Sets the main camera's field of view.")]
        public static void SetFOVCommand(Action<string> response, float fov)
        {
            if (Camera.main != null)
            {
                Camera.main.fieldOfView = fov;
                response($"Main camera FOV set to {fov}");
            }
            else response("<color=yellow>No main camera found.</color>");
        }

        [ConsoleCommand("toggleWireframe", "Toggles wireframe rendering.")]
        public static void ToggleWireframeCommand(Action<string> response, bool enabled)
        {
            GL.wireframe = enabled;
            response($"Wireframe mode: {enabled}");
        }

        [ConsoleCommand("screenshot", "Takes a screenshot and saves it.")]
        public static void ScreenshotCommand(Action<string> response, string filename = "screenshot.png")
        {
            ScreenCapture.CaptureScreenshot(filename);
            response($"Screenshot saved: {filename}");
        }

        #endregion

        #region System Information

        [ConsoleCommand("systemInfo", "Prints system information (GPU, CPU, RAM).")]
        public static void SystemInfoCommand(Action<string> response)
        {
            response($"Device: {SystemInfo.deviceName} ({SystemInfo.deviceModel})");
            response($"OS: {SystemInfo.operatingSystem}");
            response($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            response($"GPU: {SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsMemorySize} MB VRAM)");
            response($"RAM: {SystemInfo.systemMemorySize} MB");
        }

        #endregion

        #region PlayerPrefs

        [ConsoleCommand("setPref", "Sets a PlayerPref (string).")]
        public static void SetPrefCommand(Action<string> response, string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            response($"PlayerPref set: {key} = {value}");
        }

        [ConsoleCommand("getPref", "Gets a PlayerPref (string).")]
        public static void GetPrefCommand(Action<string> response, string key)
        {
            string value = PlayerPrefs.GetString(key, "(not found)");
            response($"PlayerPref: {key} = {value}");
        }

        [ConsoleCommand("delPref", "Deletes a PlayerPref key.")]
        public static void DeletePrefCommand(Action<string> response, string key)
        {
            PlayerPrefs.DeleteKey(key);
            response($"Deleted PlayerPref: {key}");
        }

        [ConsoleCommand("clearPrefs", "Clears all PlayerPrefs.")]
        public static void ClearPrefsCommand(Action<string> response)
        {
            PlayerPrefs.DeleteAll();
            response("All PlayerPrefs cleared.");
        }

        #endregion
    }
}
