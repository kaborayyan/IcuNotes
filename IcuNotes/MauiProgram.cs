using IcuNotes.Data;
using IcuNotes.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

            // You can register your own services here later.
            // Example:
            // builder.Services.AddScoped<PatientService>();
            builder.Services.AddSingleton<PatientService>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Create the database file and tables if they do not exist yet.
            // This is useful while starting the project.
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
