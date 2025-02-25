using ElectronNET.API;

var builder = WebApplication.CreateBuilder(args);

// Add Electron support
builder.WebHost.UseElectron(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize Electron if running in Electron mode
if (HybridSupport.IsElectronActive)
{
    Task.Run(async () =>
    {
        var window = await Electron.WindowManager.CreateWindowAsync(new ElectronNET.API.Entities.BrowserWindowOptions
        {
            Width = 1200,
            Height = 800,
            Show = false
        });

        window.OnReadyToShow += () => window.Show();
        window.OnClosed += () => Electron.App.Quit();
    });
}

app.Run();