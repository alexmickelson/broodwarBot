using System.Diagnostics;

namespace MyBotWeb.Services;

public class CssWatcherService : IHostedService
{
  private Process? _process;
  private readonly ILogger<CssWatcherService> _logger;
  private readonly IHostEnvironment _environment;

  public CssWatcherService(ILogger<CssWatcherService> logger, IHostEnvironment environment)
  {
    _logger = logger;
    _environment = environment;
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    if (!_environment.IsDevelopment())
    {
      _logger.LogInformation("Not in development mode, skipping CSS watcher");
      return Task.CompletedTask;
    }

    _process = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = "pnpm",
        Arguments = "run watch",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = _environment.ContentRootPath,
      },
    };

    _process.OutputDataReceived += (sender, args) =>
    {
      if (!string.IsNullOrEmpty(args.Data))
      {
        _logger.LogInformation("CSS Watcher: {Output}", args.Data);
      }
    };

    _process.ErrorDataReceived += (sender, args) =>
    {
      if (!string.IsNullOrEmpty(args.Data))
      {
        // Tailwind outputs build info to stderr, only log actual errors
        if (
          args.Data.Contains("error", StringComparison.OrdinalIgnoreCase)
          || args.Data.Contains("fail", StringComparison.OrdinalIgnoreCase)
        )
        {
          _logger.LogWarning("CSS Watcher Error: {Error}", args.Data);
        }
        else
        {
          _logger.LogInformation("CSS Watcher: {Output}", args.Data);
        }
      }
    };

    _process.Start();
    _process.BeginOutputReadLine();
    _process.BeginErrorReadLine();
    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    if (_process != null && !_process.HasExited)
    {
      _logger.LogInformation("Stopping CSS watcher...");
      try
      {
        _process.Kill(true);
        _process.WaitForExit(5000);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error stopping CSS watcher");
      }
      finally
      {
        _process.Dispose();
      }
    }

    return Task.CompletedTask;
  }
}
