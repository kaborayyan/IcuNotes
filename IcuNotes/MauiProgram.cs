using IcuNotes.Data;
using IcuNotes.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace IcuNotes
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Build the full path of the SQLite database file.
            // AppDataDirectory is a safe cross-platform place for app data.
            var databasePath = Path.Combine(FileSystem.Current.AppDataDirectory, "IcuNotes.db");

            // Register a factory that can create AppDbContext instances when needed.
            // We use SQLite as the database provider.
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
            {
                // System.Diagnostics.Debug.WriteLine($"Database path: {databasePath}");
                options.UseSqlite($"Data Source={databasePath}");
            });

            // Register your own app services here.
            builder.Services.AddSingleton<PatientService>();

            // Register MudBlazor services.
            // This is needed for MudBlazor features such as dialogs, snackbars,
            // popovers, and other interactive Material Design components.
            builder.Services.AddMudServices();

            // Register the MAUI Blazor WebView.
            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Apply EF Core migrations when the app starts.
            // This keeps the SQLite database schema aligned with your migrations.
            using (var scope = app.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var dbContext = dbFactory.CreateDbContext();

                dbContext.Database.Migrate();
            }

            return app;
        }
    }
}
