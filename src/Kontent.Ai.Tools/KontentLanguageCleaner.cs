using Kontent.Ai.Management;
using Kontent.Ai.Management.Configuration;
using Kontent.Ai.Management.Models.LanguageVariants;
using Kontent.Ai.Management.Models.Shared;

namespace Kontent.Ai.Tools
{
    public class KontentLanguageCleaner
    {
        private readonly ManagementClient _client;

        public KontentLanguageCleaner(string environmentId, string apiKey)
        {
            _client = new ManagementClient(new ManagementOptions
            {
                EnvironmentId = environmentId,
                ApiKey = apiKey,
            });
        }

        public async Task RemoveAllContentOfLanguageAsync(string languageCodename)
        {
            // Step 1: Get the first page of content items
            var contentItems = await _client.ListContentItemsAsync();

            while (contentItems is not null && contentItems.Any())
            {
                // Step 2: Create a list to hold all delete tasks for the current page
                var deleteTasks = new List<Task>();
                foreach (var item in contentItems)
                {
                    // Step 3: Create a task for deleting the language variant and add it to the list
                    var deleteTask = Task.Run(async () =>
                    {
                        try
                        {
                            await _client.DeleteLanguageVariantAsync(new LanguageVariantIdentifier(Reference.ById(item.Id), Reference.ByCodename(languageCodename)));
                            Console.WriteLine($"Deleted: '{languageCodename}' - '{item.Codename}'");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Failed: '{languageCodename}' - '{item.Codename}': {ex.Message}");
                            Console.ResetColor();
                        }
                    });

                    deleteTasks.Add(deleteTask);
                }

                // Step 4: Await all deletion tasks in the current page to complete before moving to the next page
                await Task.WhenAll(deleteTasks);

                try
                {                    
                    contentItems = await contentItems.GetNextPage();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Go to the next page");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                    contentItems = null;
                }
            }
        }
    }
}
