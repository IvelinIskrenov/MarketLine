using MarketLine.Data;
using MarketLine.Models;
using MarketLine.Security;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class ProductQuestionService : IProductQuestionService
    {
        private readonly MarketLineDbContext _context;

        public ProductQuestionService(MarketLineDbContext context)
        {
            _context = context;
        }

        public async Task<string?> AskAsync(int itemId, string? authorName, string? question, string? userId)
        {
            if (itemId <= 0)
            {
                return "Продуктът не е намерен.";
            }

            var item = await _context.Items.FirstOrDefaultAsync(current => current.id == itemId);
            if (item == null || !item.IsLive)
            {
                return "Продуктът не е намерен.";
            }

            question = Clean(question, InputLimits.Question);
            if (question == null)
            {
                return "Въведете въпрос.";
            }

            authorName = Clean(authorName, InputLimits.LongName) ?? "Клиент";

            _context.ProductQuestions.Add(new ProductQuestion
            {
                ItemId = itemId,
                UserId = userId,
                AuthorName = authorName,
                Question = question,
                AskedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string?> AnswerAsync(int id, string? answer, string? adminUserId)
        {
            var current = await _context.ProductQuestions.FirstOrDefaultAsync(question => question.Id == id);
            if (current == null)
            {
                return "Въпросът не е намерен.";
            }

            answer = Clean(answer, InputLimits.Answer);
            if (answer == null)
            {
                return "Въведете отговор.";
            }

            current.Answer = answer;
            current.AnsweredAt = DateTime.Now;
            current.AnsweredByUserId = adminUserId;
            await _context.SaveChangesAsync();
            return null;
        }

        public Task<List<ProductQuestion>> GetByItemAsync(int itemId)
        {
            return _context.ProductQuestions
                .Where(question => question.ItemId == itemId)
                .OrderBy(question => question.AskedAt)
                .ToListAsync();
        }

        public Task<List<ProductQuestion>> GetRecentAsync()
        {
            return _context.ProductQuestions
                .Include(question => question.Item)
                .OrderBy(question => question.AnsweredAt.HasValue)
                .ThenByDescending(question => question.AskedAt)
                .Take(200)
                .ToListAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var current = await _context.ProductQuestions.FindAsync(id);
            if (current == null)
            {
                return;
            }

            _context.ProductQuestions.Remove(current);
            await _context.SaveChangesAsync();
        }

        private static string? Clean(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            return value.Length > maxLength ? value[..maxLength] : value;
        }
    }
}
