using BWAPI.NET;

namespace MyBot;

public class MyBot
{
    private Game? _game;

    public void OnStart(Game game)
    {
        _game = game;
    }

    public void OnEnd(bool isWinner)
    {
        // Bot logic for game end
    }

    public void OnFrame()
    {
        if (_game == null) return;

        _game.DrawTextScreen(100, 100, "Bot running via Blazor!");

        if (_game.GetFrameCount() % 10 == 0)
        {
            System.Console.WriteLine("Frame: " + _game.GetFrameCount());
        }
    }

    public void OnUnitComplete(Unit unit)
    {
        if (_game == null) return;

        if (unit.GetUnitType().IsWorker())
        {
            Unit? closestMineral = null;
            int closestDistance = int.MaxValue;

            foreach (Unit mineral in _game.GetMinerals())
            {
                int distance = unit.GetDistance(mineral);
                if (distance < closestDistance)
                {
                    closestMineral = mineral;
                    closestDistance = distance;
                }
            }

            if (closestMineral != null)
            {
                unit.Gather(closestMineral);
            }
        }
    }

    public void OnUnitDestroy(Unit unit)
    {
        // Bot logic for unit destruction
    }

    public void OnUnitMorph(Unit unit)
    {
        // Bot logic for unit morphing
    }

    public void OnSendText(string text)
    {
        // Bot logic for text messages
    }

    public void OnReceiveText(Player player, string text)
    {
        // Bot logic for received text
    }

    public void OnPlayerLeft(Player player)
    {
        // Bot logic for player leaving
    }

    public void OnNukeDetect(Position target)
    {
        // Bot logic for nuke detection
    }

    public void OnUnitEvade(Unit unit)
    {
        // Bot logic for unit evasion
    }

    public void OnUnitShow(Unit unit)
    {
        // Bot logic for unit visibility
    }

    public void OnUnitHide(Unit unit)
    {
        // Bot logic for unit hiding
    }

    public void OnUnitCreate(Unit unit)
    {
        // Bot logic for unit creation
    }

    public void OnUnitRenegade(Unit unit)
    {
        // Bot logic for unit renegade
    }

    public void OnSaveGame(string gameName)
    {
        // Bot logic for save game
    }

    public void OnUnitDiscover(Unit unit)
    {
        // Bot logic for unit discovery
    }
}