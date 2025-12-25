using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MyBotWeb.Services;

public class StarCraftService : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hwndParent, EnumChildProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    private delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);

    private const uint BM_CLICK = 0x00F5;
    private const uint WM_CLOSE = 0x0010;

    private Process? _chaosLauncherProcess;
    private readonly string _starcraftBasePath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..", "Starcraft"
    );

    private bool _disposed = false;

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

    public async Task StartStarCraftAsync(GamePreferences? gamePreferences = null, bool useMultiInstance = false)
    {
        gamePreferences ??= new GamePreferences();

        // Configure bwapi.ini for auto-start
        ConfigureBwapiIni(gamePreferences);

        var chaosLauncherExe = useMultiInstance
            ? "Chaoslauncher - MultiInstance.exe"
            : "Chaoslauncher.exe";

        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(_starcraftBasePath, "BWAPI", "Chaoslauncher", chaosLauncherExe),
            WorkingDirectory = Path.Combine(_starcraftBasePath, "BWAPI", "Chaoslauncher"),
            UseShellExecute = false,
        };

        _chaosLauncherProcess = Process.Start(startInfo);

        // Wait for Chaoslauncher window to appear and click the Start button
        await ClickStartButton();
    }

    private async Task ClickStartButton()
    {
        await Task.Delay(2000);

        IntPtr startButtonHandle = IntPtr.Zero;
        IntPtr chaosWindow = FindWindow(null, "Chaoslauncher");

        if (chaosWindow != IntPtr.Zero)
        {
            Console.WriteLine("Found Chaoslauncher window");

            // Enumerate all child windows to find the Start button
            EnumChildWindows(chaosWindow, (hwnd, lParam) =>
            {
                var className = new System.Text.StringBuilder(256);
                GetClassName(hwnd, className, className.Capacity);

                var windowText = new System.Text.StringBuilder(256);
                GetWindowText(hwnd, windowText, windowText.Capacity);

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
                SendMessage(startButtonHandle, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
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
        Console.WriteLine("Looking for StarCraft window...");
        
        // Try common StarCraft window names
        string[] windowTitles = { "Brood War", "StarCraft", "Broodwar" };
        
        foreach (var title in windowTitles)
        {
            IntPtr starcraftWindow = FindWindow(null, title);
            if (starcraftWindow != IntPtr.Zero)
            {
                Console.WriteLine($"Found {title} window, sending close message...");
                SendMessage(starcraftWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                return;
            }
        }
        
        Console.WriteLine("StarCraft window not found");
    }

    public void StopChaosLauncher()
    {
        Console.WriteLine("Stopping chaoslauncherprocess");
        try
        {
            if (_chaosLauncherProcess != null && !_chaosLauncherProcess.HasExited)
            {

                _chaosLauncherProcess.Kill();
                _chaosLauncherProcess.WaitForExit();
                Console.WriteLine("Chaoslauncher process stopped.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping chaoslauncher process: {ex.Message}");
        }
        Console.WriteLine("Shutdown complete");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Console.WriteLine("Disposing StarCraftService...");
            CloseStarCraftWindow();
            StopChaosLauncher();
            _chaosLauncherProcess?.Dispose();
            _disposed = true;
        }
    }
}
