using CryptoFileExchange.Tests;

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
            if (args.Length > 0 && (args[0] == "--test" || args[0] == "-t"))
            {
                Console.WriteLine("Running in Console Test Mode...\n");
                TestRunner.RunAllTests();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}