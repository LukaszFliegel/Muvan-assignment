using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Muvan.Ai.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muvan.Ai.Services;

public class AiService : IAiService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ChatHistory _chatHistory;

    public AiService(AzureOpenAIConfig config)
    {
        var builder = Kernel.CreateBuilder();

        builder.AddAzureOpenAIChatCompletion(
            deploymentName: config.DeploymentName,
            endpoint: config.Endpoint,
            apiKey: config.ApiKey);

        _kernel = builder.Build();
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        _chatHistory = new ChatHistory();

        //_chatHistory.AddSystemMessage("You are a helpful assistant.");
    }

    public async Task<List<string>> ScrapeAllergensFromHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return new List<string>();

        var prompt = $@"
You are an expert food allergen detector. Your task is to analyze the provided HTML content from a recipe webpage and identify all allergenic ingredients.

Please focus on the following common allergens according to FDA regulations:
- Milk (dairy products)
- Cheese (as a separate product)
- Eggs
- Fish
- Shellfish (crustaceans)
- Tree nuts (almonds, walnuts, pecans, etc.)
- Peanuts
- Wheat (gluten-containing grains)
- Soybeans (soy)
- Sesame

Instructions:
1. Extract all ingredients from the HTML content
2. Identify which ingredients contain or are derived from the allergens listed above
3. Return ONLY the allergenic ingredients as a simple list
4. Remove duplicates
5. Use the most common/recognizable name for each allergen
6. If an ingredient contains multiple allergens, list it once but mention the primary allergen

HTML Content:
{html}

Please respond with only a JSON array of allergenic ingredients found, for example:
[""milk"", ""eggs"", ""wheat flour"", ""peanuts""]

If no allergens are found, return an empty array: []";

        try
        {
            _chatHistory.AddSystemMessage(prompt);
            
            var response = await _chatCompletionService.GetChatMessageContentAsync(_chatHistory);
            var responseText = response.Content?.Trim() ?? "";

            // Parse the JSON response
            var allergens = ParseAllergenResponse(responseText);
            return allergens;
        }
        catch (Exception ex)
        {
            // Log the exception (you might want to inject a logger)
            Console.WriteLine($"Error calling AI service: {ex.Message}");
            return new List<string>();
        }
    }

    private List<string> ParseAllergenResponse(string response)
    {
        try
        {
            // Clean the response - remove any markdown formatting
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            // Simple JSON array parsing for string array
            if (cleanedResponse.StartsWith("[") && cleanedResponse.EndsWith("]"))
            {
                var content = cleanedResponse.Substring(1, cleanedResponse.Length - 2);
                if (string.IsNullOrWhiteSpace(content))
                    return new List<string>();

                var items = content.Split(',')
                    .Select(item => item.Trim().Trim('"').Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return items;
            }

            return new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
