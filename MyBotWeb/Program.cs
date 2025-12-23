using MyBotWeb.Components;
using MyBotWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add bot service as singleton
builder.Services.AddSingleton<BotService>();

var app = builder.Build();

// Start the bot in the background
var botService = app.Services.GetRequiredService<BotService>();
_ = Task.Run(() => botService.StartBot());



app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
