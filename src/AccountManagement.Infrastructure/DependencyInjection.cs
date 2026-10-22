using System.Net.Sockets;
using AccountManagement.Application.Common.Interfaces;
using AccountManagement.Infrastructure.Persistence;
using AccountManagement.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=RupaliAccountDb;Username=postgres;Password=postgres;";
        }

        string providerPreference = configuration["DatabaseProvider"] ?? "Auto";
        bool isPostgresAvailable = false;

        if (providerPreference.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            isPostgresAvailable = true;
        }
        else if (providerPreference.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
        {
            isPostgresAvailable = false;
        }
        else // "Auto" detection
        {
            try
            {
                using var tcpClient = new TcpClient();
                var connectResult = tcpClient.BeginConnect("127.0.0.1", 5432, null, null);
                bool connected = connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(400));
                if (connected && tcpClient.Connected)
                {
                    tcpClient.EndConnect(connectResult);
                    isPostgresAvailable = true;
                }
            }
            catch
            {
                isPostgresAvailable = false;
            }
        }

        if (isPostgresAvailable)
        {
            services.AddDbContext<AccountDbContext>(options =>
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly(typeof(AccountDbContext).Assembly.FullName)));
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolder = Path.Combine(appData, "RupaliAccount");
            Directory.CreateDirectory(dbFolder);
            string sqlitePath = Path.Combine(dbFolder, "RupaliAccount.db");

            services.AddDbContext<AccountDbContext>(options =>
                options.UseSqlite($"Data Source={sqlitePath}"));
        }

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AccountDbContext>());
        services.AddScoped<DatabaseSeeder>();

        // Domain & Application Services
        services.AddScoped<ILedgerService, LedgerService>();
        services.AddScoped<IRateService, RateService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IBackupService, PostgreSqlBackupService>();
        services.AddScoped<IMemoService, MemoService>();

        return services;
    }
}
