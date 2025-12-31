using System.Diagnostics;

namespace MyBotWeb.Services;

public class StarCraftService
{
    private readonly BotService _botService;

    public StarCraftService(BotService botService)
    {
        _botService = botService;
        _botService.GameEnded += StopAndReset;
    }

    private Process? _chaosLauncherProcess;
    private static readonly string _starcraftBasePath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..",
        "Starcraft"
    );

    public List<string> GetMapOptions()
    {
        var mapsDirectory = Path.Combine(_starcraftBasePath, "Maps");

        if (!Directory.Exists(mapsDirectory))
        {
            Console.WriteLine($"Warning: Maps directory not found at {mapsDirectory}");
            return new List<string>();
        }

        var mapFiles = Directory
            .GetFiles(mapsDirectory, "*.sc?", SearchOption.TopDirectoryOnly)
            .Select(path => $"maps/{Path.GetFileName(path)}")
            .OrderBy(name => name)
            .ToList();

        return mapFiles;
    }

    public async Task StartStarCraftAsync(GamePreferences gamePreferences)
    {
        BwapiConfigService.ConfigureBwapiIni(gamePreferences);

        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(
                _starcraftBasePath,
                "BWAPI",
                "Chaoslauncher",
                "Chaoslauncher - MultiInstance.exe"
            ),
            WorkingDirectory = Path.Combine(_starcraftBasePath, "BWAPI", "Chaoslauncher"),
            UseShellExecute = false,
            Verb = "",
            CreateNoWindow = false,
        };

        _chaosLauncherProcess = Process.Start(startInfo);

        await Task.Delay(400);
        ClickStartButton();
    }

    private void ClickStartButton()
    {
        IntPtr startButtonHandle = IntPtr.Zero;
        IntPtr chaosWindow = WindowUtils.FindWindow(null, "Chaoslauncher");

        if (chaosWindow == IntPtr.Zero)
        {
            Console.WriteLine("Warning: Could not find Chaoslauncher window");
            return;
        }
        Console.WriteLine("Found Chaoslauncher window");

        WindowUtils.EnumChildWindows(
            chaosWindow,
            (hwnd, lParam) =>
            {
                var className = new System.Text.StringBuilder(256);
                WindowUtils.GetClassName(hwnd, className, className.Capacity);
                string cls = className.ToString();

                var windowText = new System.Text.StringBuilder(256);
                WindowUtils.GetWindowText(hwnd, windowText, windowText.Capacity);
                string text = windowText.ToString();

                if (text.Contains("Start", StringComparison.OrdinalIgnoreCase))
                {
                    startButtonHandle = hwnd;
                    Console.WriteLine($"Found Start button! Handle: {hwnd}");
                    return false; // Stop enumeration
                }
                return true; // Continue enumeration
            },
            IntPtr.Zero
        );

        if (startButtonHandle == IntPtr.Zero)
        {
            Console.WriteLine("Warning: Could not find Start button in Chaoslauncher");
            return;
        }
        Console.WriteLine("Clicking Start button...");
        WindowUtils.ClickButton(startButtonHandle);
    }

    private void CloseStarCraftWindow()
    {
        Console.WriteLine("Looking for StarCraft process...");

        try
        {
            // Use taskkill command which can force-terminate processes
            var processInfo = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = "/F /IM StarCraft.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var process = Process.Start(processInfo);
            if (process != null)
            {
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                Console.WriteLine($"taskkill output: {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"taskkill error: {error}");
                }

                process.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error using taskkill: {ex.Message}");
        }
    }

    public void StopChaosLauncher()
    {
        Console.WriteLine("Stopping chaoslauncherprocess");
        if (_chaosLauncherProcess != null && !_chaosLauncherProcess.HasExited)
        {
            _chaosLauncherProcess.Kill();
            _chaosLauncherProcess.WaitForExit();
            _chaosLauncherProcess?.Dispose();
            _chaosLauncherProcess = null;
            Console.WriteLine("Chaoslauncher process stopped.");
        }
    }

    public void StopAndReset()
    {
        Console.WriteLine("Disposing StarCraftService...");
        CloseStarCraftWindow();
        StopChaosLauncher();
    }
}
