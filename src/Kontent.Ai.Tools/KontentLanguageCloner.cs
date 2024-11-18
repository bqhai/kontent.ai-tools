using Kontent.Ai.Management;
using Kontent.Ai.Management.Configuration;
using Kontent.Ai.Management.Models.LanguageVariants;
using Kontent.Ai.Management.Models.Shared;

namespace Kontent.Ai.Tools
{
    public class KontentLanguageCloner
    {
        private readonly ManagementClient _client;

        public KontentLanguageCloner(string environmentId, string apiKey)
        {
            _client = new ManagementClient(new ManagementOptions
            {
                EnvironmentId = environmentId,
                ApiKey = apiKey,
            });
        }

        public async Task CloneContentToNewLanguageAsync(string sourceLanguage, string targetLanguage)
        {
            int maxConcurrentRequests = 10;  // Matches the 10 requests per second limit
            int delayBetweenRequestsMs = 150; // Helps avoid the 400 requests per minute limit
            using var semaphore = new SemaphoreSlim(maxConcurrentRequests);

            var contentItems = await _client.ListContentItemsAsync();

            while (contentItems is not null && contentItems.Any())
            {
                var cloneTasks = new List<Task>();
                foreach (var item in contentItems )
                {
                    var cloneTask = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            var sourceVariant = await _client.GetLanguageVariantAsync(new LanguageVariantIdentifier(Reference.ById(item.Id), Reference.ByCodename(sourceLanguage)));
                            var clonedVariant = new LanguageVariantUpsertModel
                            {
                                Elements = sourceVariant.Elements,  // Clone elements directly
                            };

                            // Upsert the cloned variant to the target language
                            await _client.UpsertLanguageVariantAsync(new LanguageVariantIdentifier(Reference.ById(item.Id), Reference.ByCodename(targetLanguage)), clonedVariant);

                            // Publish the cloned variant to make it live
                            await _client.PublishLanguageVariantAsync(new LanguageVariantIdentifier(Reference.ById(item.Id), Reference.ByCodename(targetLanguage)));

                            Console.WriteLine($"Cloned '{item.Codename}' to language '{targetLanguage}'");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Failed '{item.Codename}' to language '{targetLanguage}': {ex.Message}");
                            Console.ResetColor();
                        }
                        finally
                        {
                            semaphore.Release();
                        }

                        await Task.Delay(delayBetweenRequestsMs);
                    });

                    cloneTasks.Add(cloneTask);
                }

                await Task.WhenAll(cloneTasks);

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
