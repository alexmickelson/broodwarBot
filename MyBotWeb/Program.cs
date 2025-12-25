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

var app = builder.Build();

var starCraftService = app.Services.GetRequiredService<StarCraftService>();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();