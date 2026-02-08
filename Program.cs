using CryptoFileExchange.Tests;
using Serilog;
using System;
using System.IO;

namespace CryptoFileExchange
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Konfiguriši Serilog
            ConfigureSerilog();

            try
            {
                if (args.Length > 0 && (args[0] == "--test" || args[0] == "-t"))
                {
                    Console.WriteLine("Running in Console Test Mode...\n");
                    TestRunner.RunAllTests();
                    return;
                }

                Log.Information("Application starting...");

                // To customize application configuration such as set high DPI settings or default font,
                // see https://aka.ms/applicationconfiguration.
                ApplicationConfiguration.Initialize();
                Application.Run(new Form1());

                Log.Information("Application shutting down...");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureSerilog()
        {
            // Kreiraj logs direktorijum ako ne postoji
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // Konfiguriši Serilog sa File i Console sink-ovima
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 30) // ?uva logove 30 dana
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Serilog configured successfully. Log directory: {LogDirectory}", logDirectory);
        }
    }
}