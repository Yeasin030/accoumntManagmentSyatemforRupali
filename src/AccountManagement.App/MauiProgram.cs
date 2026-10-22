using System.Reflection;
using AccountManagement.App.ViewModels;
using AccountManagement.App.Views;
using AccountManagement.Application;
using AccountManagement.Infrastructure;
using AccountManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AccountManagement.App;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 1. Build Configuration from appsettings.json
        string appDir = AppDomain.CurrentDomain.BaseDirectory;
        string configPath = Path.Combine(appDir, "appsettings.json");

        var configBuilder = new ConfigurationBuilder();
        if (File.Exists(configPath))
        {
            configBuilder.AddJsonFile(configPath, optional: true, reloadOnChange: true);
        }

        var config = configBuilder.Build();
        builder.Services.AddSingleton<IConfiguration>(config);

        // 2. Register Clean Architecture layers
        builder.Services.AddInfrastructureServices(config);
        builder.Services.AddApplicationServices();

        // 3. Register ViewModels
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<LedgerViewModel>();
        builder.Services.AddTransient<NewTransactionViewModel>();
        builder.Services.AddTransient<RateManagerViewModel>();
        builder.Services.AddTransient<CustomerManagerViewModel>();
        builder.Services.AddTransient<PaymentExpenseViewModel>();
        builder.Services.AddTransient<BackupRestoreViewModel>();

        // 4. Register Views
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<LedgerPage>();
        builder.Services.AddTransient<NewTransactionPage>();
        builder.Services.AddTransient<RateManagerPage>();
        builder.Services.AddTransient<CustomerManagerPage>();
        builder.Services.AddTransient<PaymentExpensePage>();
        builder.Services.AddTransient<BackupRestorePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // 5. Initialize & Seed Database asynchronously
        Task.Run(async () =>
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
                logger.LogInformation("Ensuring PostgreSQL database tables are created...");
                await dbContext.Database.EnsureCreatedAsync();

                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();
                logger.LogInformation("Database initialization & seed complete.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database connection or seeding failed. Please verify PostgreSQL service and connection string in appsettings.json.");
            }
        });

        return app;
    }
}
