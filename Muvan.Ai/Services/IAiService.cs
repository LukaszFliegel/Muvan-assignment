
namespace Muvan.Ai.Services;

public interface IAiService
{
    Task<List<string>> ScrapeAllergensFromHtml(string html);
}