using MyBotWeb.Components;
using MyBotWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add bot service as singleton
builder.Services.AddSingleton<BotService>();
builder.Services.AddSingleton<StarCraftService>();
builder.Services.AddSingleton<UserPreferencesService>();

builder.Services.AddHostedService<CssWatcherService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();