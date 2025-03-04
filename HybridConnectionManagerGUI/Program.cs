using ElectronNET.API;
using HybridConnectionManager.Library;

var builder = WebApplication.CreateBuilder(args);

// Add Electron support
builder.WebHost.UseElectron(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Main}/{action=Index}/{id?}");

// Initialize Electron if running in Electron mode
if (HybridSupport.IsElectronActive)
{
    Task.Run(async () =>
    {
        var window = await Electron.WindowManager.CreateWindowAsync(new ElectronNET.API.Entities.BrowserWindowOptions
        {
            Width = 1200,
            Height = 800,
            Show = false,
            WebPreferences = new ElectronNET.API.Entities.WebPreferences
            {
                NodeIntegration = true,
                ContextIsolation = false,
                EnableRemoteModule = true,
                DevTools = false,
                WebSecurity = false, // Note: Only disable this if you trust your content
            },
            Icon = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/assets", Util.GetIconFile()),
            AutoHideMenuBar = true,
        });

        window.OnReadyToShow += () => window.Show();
        window.OnClosed += () => Electron.App.Quit();
    });
}

app.Run();