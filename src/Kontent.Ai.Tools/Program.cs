using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Kontent.Ai.Tools
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Load settings
            var kontentSettings = LoadSettings();

            while (true)
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Run Cleaner");
                Console.WriteLine("2. Run Cloner");
                Console.WriteLine("3. Exit");
                Console.Write("Enter your choice: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await RunCleanerAsync(kontentSettings);
                        break;
                    case "2":
                        await RunClonerAsync(kontentSettings);
                        break;
                    case "3":
                        Console.WriteLine("Exiting the application.");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }

                Console.WriteLine();
            }
        }

        private static KontentSettings LoadSettings()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var kontentSettings = new KontentSettings();
            configuration.GetSection("KontentSettings").Bind(kontentSettings);

            return kontentSettings;
        }

        private static async Task RunCleanerAsync(KontentSettings settings)
        {
            Console.WriteLine("Starting the cleaning process...");

            var cleaner = new KontentLanguageCleaner(settings.EnvironmentId, settings.ApiKey);

            var deleteTimer = Stopwatch.StartNew();
            await cleaner.RemoveAllContentOfLanguageAsync(settings.DestinationLanguage);
            deleteTimer.Stop();

            Console.WriteLine($"Time taken to delete language variants: {deleteTimer.Elapsed}");
        }

        private static async Task RunClonerAsync(KontentSettings settings)
        {
            Console.WriteLine("Starting the cloning process...");

            var cloner = new KontentLanguageCloner(settings.EnvironmentId, settings.ApiKey);

            var cloneTimer = Stopwatch.StartNew();
            await cloner.CloneContentToNewLanguageAsync(settings.SourceLanguage, settings.DestinationLanguage);
            cloneTimer.Stop();

            Console.WriteLine($"Time taken to clone language variants: {cloneTimer.Elapsed}");
        }
    }   
}
