namespace Muvan.Console.Services
{
    public interface IHtmlScrappingService : IDisposable
    {
        string ScrapIngredients(string url);
    }
}