using BWAPI.NET;
using MyBot;

namespace MyBotWeb.Services;

public class BotService : DefaultBWListener
{
    private BWClient? _bwClient;
    public Game? Game { get; private set; }
    private bool _isProcessingFrame = false;
    private readonly MyBot.MyBot _myBot;
    public bool IsConnected { get; private set; }
    public bool IsInGame { get; private set; }
    public string GameStatus { get; private set; } = "Not Connected";

    public event Action? GameStarted;

    public BotService()
    {
        _myBot = new MyBot.MyBot();
    }

    public void StartBot()
    {
        _bwClient = new BWClient(this);
        IsConnected = true;
        GameStatus = "Connected - Waiting for Game";
        _bwClient.StartGame();
    }

    public override void OnStart()
    {
        Game = _bwClient?.Game;
        IsInGame = true;
        GameStatus = "In Game - Playing";

        // Trigger the GameStarted event
        GameStarted?.Invoke();

        // Delegate to MyBot
        if (Game != null)
        {
            _myBot.OnStart(Game);
        }
    }

    public override void OnEnd(bool isWinner)
    {
        GameStatus = isWinner ? "Game Over - Victory!" : "Game Over - Defeat";
        IsInGame = false;

        // Delegate to MyBot
        _myBot.OnEnd(isWinner);
    }

    public override void OnFrame()
    {
        if (Game == null) return;

        // Skip this frame if the previous one is still processing
        if (_isProcessingFrame) return;

        _isProcessingFrame = true;
        try
        {
            // Delegate to MyBot
            _myBot.OnFrame();
        }
        finally
        {
            _isProcessingFrame = false;
        }
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
