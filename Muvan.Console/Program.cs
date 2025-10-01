// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Muvan.Console.Services;
using Muvan.Ai.Services;
using Muvan.Ai.Models;

try
{
    // Check if there is exactly one input parameter
    if (args.Length != 1)
    {
        Console.WriteLine("Usage: Muvan.Console.exe <URL>");
        Console.WriteLine("Please provide exactly one URL parameter.");
        return;
    }

    string inputUrl = args[0];

    // Check if the input parameter is a valid URL
    if (!Uri.TryCreate(inputUrl, UriKind.Absolute, out Uri? validatedUri) || 
        (validatedUri.Scheme != Uri.UriSchemeHttp && validatedUri.Scheme != Uri.UriSchemeHttps))
    {
        Console.WriteLine($"Invalid URL: {inputUrl}");
        Console.WriteLine("Please provide a valid HTTP or HTTPS URL.");
        return;
    }

    Console.WriteLine($"Recipe: {inputUrl}");

    // Create configuration
    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddUserSecrets<Program>()
        .Build();

    // Configure Azure OpenAI
    var azureOpenAIConfig = new AzureOpenAIConfig();
    configuration.GetSection("AzureOpenAI").Bind(azureOpenAIConfig);

    // Scrape the URL using IHtmlScrappingService
    using var scrapingService = new HtmlScrappingService();
    string scrapedHtml = scrapingService.ScrapIngredients(inputUrl);
    
    // Use AI service to extract allergens from HTML
    var aiService = new AiService(azureOpenAIConfig);
    var allergens = await aiService.ScrapeAllergensFromHtml(scrapedHtml);
    
    // Print allergens response
    Console.WriteLine("Allergens found:");
    foreach (var allergen in allergens)
    {
        Console.WriteLine($"- {allergen}");
    }

    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}");
}



