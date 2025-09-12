using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QRCodeRegenerator.Forms;
using QRCodeRegenerator.Services.Database;
using QRCodeRegenerator.Services.Processing;
using QRCodeRegenerator.Services.QRCode;
using QRCodeRegenerator.Services.SAP;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QRCodeRegenerator
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        public static IServiceProvider ServiceProvider { get; private set; }

        [STAThread]
        static void Main()
        {
            // Allocate console window for logs
            AllocConsole();
            Console.WriteLine("Rainbow QR Application Started");
            Console.WriteLine("Configuration logs will appear here...");
            Console.WriteLine("=====================================");

            ApplicationConfiguration.Initialize();

            var host = CreateHostBuilder().Build();
            ServiceProvider = host.Services;

            using (var scope = host.Services.CreateScope())
            {
                var mainForm = scope.ServiceProvider.GetRequiredService<MainForm>();
                Application.Run(mainForm);
            }
        }

        static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Database Services
                    services.AddSingleton<IConfigService, ConfigService>();
                    services.AddSingleton<ITransactionService, TransactionService>();
                    services.AddSingleton<ICompletedDocumentsService, CompletedDocumentsService>();

                    // Core Services
                    services.AddSingleton<IQRCodeGenerationService, QRCodeGenerationService>();
                    services.AddSingleton<ISAPIntegrationService, SAPIntegrationService>();
                    services.AddSingleton<IProcessingService, ProcessingService>();

                    // HTTP Client
                    services.AddHttpClient();

                    // Forms
                    services.AddTransient<MainForm>();
                    services.AddTransient<ConfigurationForm>();
                    services.AddTransient<RecordSelectionForm>();

                    // Logging
                    services.AddLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                    });
                });
    }
}