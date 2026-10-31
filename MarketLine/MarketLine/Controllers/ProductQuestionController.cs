using MarketLine.Models;
using MarketLine.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MarketLine.Controllers
{
    public class ProductQuestionController : Controller
    {
        private readonly IProductQuestionService _questions;
        private readonly UserManager<User> _users;

        public ProductQuestionController(IProductQuestionService questions, UserManager<User> users)
        {
            _questions = questions;
            _users = users;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask(int itemId, string? authorName, string? question)
        {
            if (itemId <= 0)
            {
                return RedirectToAction("Index", "Item");
            }

            var user = await _users.GetUserAsync(User);
            if (string.IsNullOrWhiteSpace(authorName) && user != null)
            {
                authorName = $"{user.FirstName} {user.LastName}".Trim();
            }

            var error = await _questions.AskAsync(itemId, authorName, question, user?.Id);
            if (error != null)
            {
                TempData["CartError"] = error;
            }
            else
            {
                TempData["CartMessage"] = "Въпросът е изпратен. Администраторът ще отговори на страницата на продукта.";
            }

            return RedirectToAction("Details", "Item", new { id = itemId });
        }
    }
}
