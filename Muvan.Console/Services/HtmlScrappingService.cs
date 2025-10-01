using HtmlAgilityPack;

namespace Muvan.Console.Services;

public class HtmlScrappingService : IHtmlScrappingService, IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed = false;

    public HtmlScrappingService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    public string ScrapIngredients(string url)
    {
        try
        {
            // Fetch HTML content from the URL
            var response = _httpClient.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            
            var html = response.Content.ReadAsStringAsync().Result;
            
            // Parse HTML using HtmlAgilityPack
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            // Find the ingredients list with the specific class
            var ingredientsList = doc.DocumentNode
                .SelectSingleNode("//ul[@class='mm-recipes-structured-ingredients__list']");
            
            if (ingredientsList == null)
            {
                throw new InvalidOperationException("Could not find ingredients list with class 'mm-recipes-structured-ingredients__list' on the page.");
            }
            
            // Return the inner HTML of the ingredients list
            return ingredientsList.InnerHtml;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to fetch content from URL: {url}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error processing URL: {url}", ex);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
        }
        _disposed = true;
    }
}
