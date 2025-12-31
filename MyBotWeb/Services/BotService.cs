using BWAPI.NET;

namespace MyBotWeb.Services;

public class BotService : DefaultBWListener
{
  private BWClient? _bwClient;
  public Game? Game { get; private set; }
  public MyBot Bot { get; private set; } = new MyBot();
  public bool IsInGame { get; private set; }
  public string GameStatus { get; private set; } = "Not Connected";
  public int? GameSpeedToSet { get; set; } = null;
  public event Action? GameStartedOrEnded;
  public event Action? GameEnded;

  public void StartBot()
  {
    _bwClient = new BWClient(this);
    GameStatus = "Connected - Waiting for Game";
    _bwClient.StartGame();
  }

  public void ResetBot()
  {
    Console.WriteLine("ResetBot called - cleaning up BWClient");

    (_bwClient as IDisposable)?.Dispose();
    _bwClient = null;

    Game = null;
    Bot = new MyBot();
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
      Bot.OnStart(Game);
    }
  }

  public override void OnEnd(bool isWinner)
  {
    Console.WriteLine("Game ended. Winner: " + isWinner);
    GameStatus = isWinner ? "Game Over - Victory!" : "Game Over - Defeat";
    IsInGame = false;

    Bot.OnEnd(isWinner);
    ResetBot();
  }

  public override void OnFrame()
  {
    if (Game == null)
      return;
    if (GameSpeedToSet != null)
    {
      Game.SetLocalSpeed(GameSpeedToSet.Value);
      GameSpeedToSet = null;
    }
    Bot.OnFrame();
  }

  public override void OnUnitComplete(Unit unit)
  {
    // Delegate to MyBot
    Bot.OnUnitComplete(unit);
  }

  public override void OnUnitDestroy(Unit unit)
  {
    // Delegate to MyBot
    Bot.OnUnitDestroy(unit);
  }

  public override void OnUnitMorph(Unit unit)
  {
    // Delegate to MyBot
    Bot.OnUnitMorph(unit);
  }

  public override void OnSendText(string text)
  {
    // Delegate to MyBot
    Bot.OnSendText(text);
  }

  public override void OnReceiveText(Player player, string text)
  {
    // Delegate to MyBot
    Bot.OnReceiveText(player, text);
  }

  public override void OnPlayerLeft(Player player)
  {
    // Delegate to MyBot
    Bot.OnPlayerLeft(player);
  }

  public override void OnNukeDetect(Position target)
  {
    // Delegate to MyBot
    Bot.OnNukeDetect(target);
  }

  public override void OnUnitEvade(Unit unit)
  {
    // Delegate to MyBot
    Bot.OnUnitEvade(unit);
  }

  public override void OnUnitShow(Unit unit)
  {
    // Delegate to MyBot
    Bot.OnUnitShow(unit);
  }

  public override void OnUnitHide(Unit unit)
  {
    // Delegate to MyBot
    Bot.OnUnitHide(unit);
  }

  public override void OnUnitCreate(Unit unit)
  {
    // Delegate to MyBot
    Bot.OnUnitCreate(unit);
  }

  public override void OnUnitRenegade(Unit unit)
  {
    // Delegate to MyBot
    Bot.OnUnitRenegade(unit);
  }

  public override void OnSaveGame(string gameName)
  {
    // Delegate to MyBot
    Bot.OnSaveGame(gameName);
  }

  public override void OnUnitDiscover(Unit unit)
  {
    // Delegate to MyBot
    Bot.OnUnitDiscover(unit);
  }
}
