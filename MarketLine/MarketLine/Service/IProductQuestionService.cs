using MarketLine.Models;

namespace MarketLine.Services
{
    public interface IProductQuestionService
    {
        Task<string?> AskAsync(int itemId, string? authorName, string? question, string? userId);
        Task<string?> AnswerAsync(int id, string? answer, string? adminUserId);
        Task<List<ProductQuestion>> GetByItemAsync(int itemId);
        Task<List<ProductQuestion>> GetRecentAsync();
        Task DeleteAsync(int id);
    }
}
