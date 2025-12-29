using System.Diagnostics;

namespace MyBotWeb.Services;

public class StarCraftService
{
    private Process? _chaosLauncherProcess;
    private readonly string _starcraftBasePath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..", "Starcraft"
    );

    public List<string> GetMapOptions()
    {
        var mapsDirectory = Path.Combine(_starcraftBasePath, "Maps");

        if (!Directory.Exists(mapsDirectory))
        {
            Console.WriteLine($"Warning: Maps directory not found at {mapsDirectory}");
            return new List<string>();
        }

        var mapFiles = Directory.GetFiles(mapsDirectory, "*.sc?", SearchOption.TopDirectoryOnly)
            .Select(path => $"maps/{Path.GetFileName(path)}")
            .OrderBy(name => name)
            .ToList();

        return mapFiles;
    }

    public async Task StartStarCraftAsync(GamePreferences? gamePreferences = null)
    {
        gamePreferences ??= new GamePreferences();

        // Configure bwapi.ini for auto-start
        ConfigureBwapiIni(gamePreferences);

        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(_starcraftBasePath, "BWAPI", "Chaoslauncher", "Chaoslauncher.exe"),
            WorkingDirectory = Path.Combine(_starcraftBasePath, "BWAPI", "Chaoslauncher"),
            UseShellExecute = false,
            Verb = "", // Ensure no elevation
            CreateNoWindow = false,
        };

        _chaosLauncherProcess = Process.Start(startInfo);

        await ClickStartButton();
    }

    private async Task ClickStartButton()
    {
        await Task.Delay(100);

        IntPtr startButtonHandle = IntPtr.Zero;
        IntPtr chaosWindow = WindowUtils.FindWindow(null, "Chaoslauncher");

        if (chaosWindow != IntPtr.Zero)
        {
            Console.WriteLine("Found Chaoslauncher window");

            // Enumerate all child windows to find the Start button
            WindowUtils.EnumChildWindows(chaosWindow, (hwnd, lParam) =>
            {
                var className = new System.Text.StringBuilder(256);
                WindowUtils.GetClassName(hwnd, className, className.Capacity);

                var windowText = new System.Text.StringBuilder(256);
                WindowUtils.GetWindowText(hwnd, windowText, windowText.Capacity);

                string text = windowText.ToString();
                string cls = className.ToString();

                Console.WriteLine($"Found child window: Class='{cls}', Text='{text}'");

                // Look for button with "Start" text (case-insensitive)
                if (text.Equals("Start", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("Start", StringComparison.OrdinalIgnoreCase))
                {
                    startButtonHandle = hwnd;
                    Console.WriteLine($"Found Start button! Handle: {hwnd}");
                    return false; // Stop enumeration
                }

                return true; // Continue enumeration
            }, IntPtr.Zero);

            if (startButtonHandle != IntPtr.Zero)
            {
                Console.WriteLine("Clicking Start button...");
                WindowUtils.ClickButton(startButtonHandle);
            }
            else
            {
                Console.WriteLine("Warning: Could not find Start button in Chaoslauncher");
            }
        }
        else
        {
            Console.WriteLine("Warning: Could not find Chaoslauncher window");
        }
    }

    private void ConfigureBwapiIni(GamePreferences gamePreferences)
    {
        var bwapiIniPath = Path.Combine(_starcraftBasePath, "bwapi-data", "bwapi.ini");
        Console.WriteLine($"Configuring BWAPI settings at: {bwapiIniPath}");

        if (!File.Exists(bwapiIniPath))
        {
            Console.WriteLine("Warning: bwapi.ini not found");
            return;
        }

        var lines = File.ReadAllLines(bwapiIniPath);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Set auto_menu to SINGLE_PLAYER
            if (line.StartsWith("auto_menu", StringComparison.OrdinalIgnoreCase) &&
                !line.StartsWith("auto_menu_", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = "auto_menu = SINGLE_PLAYER";
                Console.WriteLine("Set auto_menu = SINGLE_PLAYER");
            }
            // Set race to player's selected race
            else if (line.StartsWith("race", StringComparison.OrdinalIgnoreCase) &&
                     !line.StartsWith("race_", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"race = {gamePreferences.PlayerRace}";
                Console.WriteLine($"Set race = {gamePreferences.PlayerRace}");
            }
            // Set map
            else if (line.StartsWith("map", StringComparison.OrdinalIgnoreCase) &&
                     !line.StartsWith("mapiteration", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"map = {gamePreferences.Map}";
                Console.WriteLine($"Set map = {gamePreferences.Map}");
            }
            // Set enemy_count (total computer players)
            else if (line.StartsWith("enemy_count", StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"enemy_count = {gamePreferences.ComputerPlayerSlots}";
                Console.WriteLine($"Set enemy_count = {gamePreferences.ComputerPlayerSlots}");
            }
            // Set enemy races
            else if (line.StartsWith("enemy_race_", StringComparison.OrdinalIgnoreCase))
            {
                var raceNumStr = line.Substring(11).Split('=')[0].Trim();
                if (int.TryParse(raceNumStr, out int raceNum) && raceNum > 0 && raceNum <= gamePreferences.ComputerRaces.Length)
                {
                    var race = gamePreferences.ComputerRaces[raceNum - 1];
                    lines[i] = $"enemy_race_{raceNum} = {race}";
                    Console.WriteLine($"Set enemy_race_{raceNum} = {race}");
                }
            }
            // Set first enemy_race (without number) to the first computer's race
            else if (line.StartsWith("enemy_race", StringComparison.OrdinalIgnoreCase) &&
                     !line.StartsWith("enemy_race_", StringComparison.OrdinalIgnoreCase))
            {
                var defaultRace = gamePreferences.ComputerRaces.Length > 0 ? gamePreferences.ComputerRaces[0] : "Protoss";
                lines[i] = $"enemy_race = {defaultRace}";
                Console.WriteLine($"Set enemy_race = {defaultRace}");
            }
        }

        File.WriteAllLines(bwapiIniPath, lines);
        Console.WriteLine("BWAPI configuration updated successfully");
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
                RedirectStandardError = true
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
