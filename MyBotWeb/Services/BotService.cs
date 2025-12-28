using BWAPI.NET;
using MyBot;

namespace MyBotWeb.Services;

public class BotService : DefaultBWListener
{
    private BWClient? _bwClient;
    public Game? Game { get; private set; }
    private bool _isProcessingFrame = false;
    private MyBot.MyBot _myBot = new MyBot.MyBot();
    public bool IsInGame { get; private set; }
    public string GameStatus { get; private set; } = "Not Connected";

    public event Action? GameStartedOrEnded;
    private bool shouldLeave = false;

    public void StartBot()
    {
        _myBot = new MyBot.MyBot();
        _bwClient = new BWClient(this);
        GameStatus = "Connected - Waiting for Game";
        _bwClient.StartGame();
    }

    public void ResetBot()
    {
        _bwClient = null;
        Game = null;
        IsInGame = false;
        GameStatus = "Not Connected";
        GameStartedOrEnded?.Invoke();
    }

    public override void OnStart()
    {
        Game = _bwClient?.Game;
        Game?.EnableFlag(Flag.UserInput);
        IsInGame = true;
        GameStatus = "In Game - Playing";
        GameStartedOrEnded?.Invoke();

        if (Game != null)
        {
            _myBot.OnStart(Game);
        }
    }

    public override void OnEnd(bool isWinner)
    {
        Console.WriteLine("Game ended. Winner: " + isWinner);
        GameStatus = isWinner ? "Game Over - Victory!" : "Game Over - Defeat";
        IsInGame = false;

        _myBot.OnEnd(isWinner);

        GameStartedOrEnded?.Invoke();
    }

    public override void OnFrame()
    {
        if (Game == null) return;

        _myBot.OnFrame();

    }

    public override void OnUnitComplete(Unit unit)
    {
        // Delegate to MyBot
        _myBot.OnUnitComplete(unit);
    }

    public override void OnUnitDestroy(Unit unit)
    {
        // Delegate to MyBot
        _myBot.OnUnitDestroy(unit);
    }

    public override void OnUnitMorph(Unit unit)
    {
        // Delegate to MyBot
        _myBot.OnUnitMorph(unit);
    }

    public override void OnSendText(string text)
    {
        // Delegate to MyBot
        _myBot.OnSendText(text);
    }

    public override void OnReceiveText(Player player, string text)
    {
        // Delegate to MyBot
        _myBot.OnReceiveText(player, text);
    }

    public override void OnPlayerLeft(Player player)
    {
        // Delegate to MyBot
        _myBot.OnPlayerLeft(player);
    }

    public override void OnNukeDetect(Position target)
    {
        // Delegate to MyBot
        _myBot.OnNukeDetect(target);
    }

    public override void OnUnitEvade(Unit unit)
    {
        // Delegate to MyBot
        _myBot.OnUnitEvade(unit);
    }

    public override void OnUnitShow(Unit unit)
    {
        // Delegate to MyBot
        _myBot.OnUnitShow(unit);
    }

    public override void OnUnitHide(Unit unit)
    {
        // Delegate to MyBot
        _myBot.OnUnitHide(unit);
    }

    public override void OnUnitCreate(Unit unit)
    {
        // Delegate to MyBot
        _myBot.OnUnitCreate(unit);
    }

    public override void OnUnitRenegade(Unit unit)
    {
        // Delegate to MyBot
        _myBot.OnUnitRenegade(unit);
    }

    public override void OnSaveGame(string gameName)
    {
        // Delegate to MyBot
        _myBot.OnSaveGame(gameName);
    }

    public override void OnUnitDiscover(Unit unit)
    {
        // Delegate to MyBot
        _myBot.OnUnitDiscover(unit);
    }
}
