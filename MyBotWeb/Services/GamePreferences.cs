namespace MyBotWeb.Services;

public record GamePreferences(
    string Map = "maps/(2)Boxer.scm",
    string PlayerRace = "Protoss",
    int HumanPlayerSlots = 0,
    int ComputerPlayerSlots = 1,
    string[] ComputerRaces = null!,
    string[] PlayerSlots = null!
)
{
    public GamePreferences() : this("maps/(2)Boxer.scm", "Protoss", 0, 1, new[] { "Protoss" }, new[] { "Server", "Computer - Random", "None", "None", "None", "None", "None", "None" })
    {
    }
}

public enum Race
{
    Terran,
    Protoss,
    Zerg,
    Random
}
