using System.Net;
using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class ProductQuestionTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public ProductQuestionTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Guest_CanAskQuestionOnProductPage()
        {
            var item = await FindItemAsync("Омега-3 капсули");
            var client = _factory.CreateAppClient();
            var details = await client.GetAsync("/Item/Details/" + item.id);
            details.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync());
            Assert.Contains("Въпроси и отговори", html);
            Assert.Contains("Задайте въпрос", html);

            var token = FormRequest.ReadToken(html);
            var ask = await client.PostAsync("/ProductQuestion/Ask", FormRequest.Create(
                token,
                ("itemId", item.id.ToString()),
                ("authorName", "Иван Петров"),
                ("question", "Подходящ ли е при бременност?")));
            Assert.Equal(HttpStatusCode.Redirect, ask.StatusCode);

            var updated = await client.GetAsync("/Item/Details/" + item.id);
            updated.EnsureSuccessStatusCode();
            var updatedHtml = WebUtility.HtmlDecode(await updated.Content.ReadAsStringAsync());
            Assert.Contains("Подходящ ли е при бременност?", updatedHtml);
            Assert.Contains("Очаква отговор", updatedHtml);
            Assert.Contains("Иван Петров", updatedHtml);
        }

        [Fact]
        public async Task Ask_RejectsEmptyQuestion()
        {
            var item = await FindItemAsync("Витамин C 1000 mg");
            var client = _factory.CreateAppClient();
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Item/Details/" + item.id);
            await client.PostAsync("/ProductQuestion/Ask", FormRequest.Create(
                token,
                ("itemId", item.id.ToString()),
                ("authorName", "Гост"),
                ("question", "   ")));

            var details = await client.GetAsync("/Item/Details/" + item.id);
            var html = WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync());
            Assert.Contains("Въведете въпрос", html);
            Assert.DoesNotContain("Гост", html);
        }

        [Fact]
        public async Task Admin_CanAnswerQuestionAndItAppearsOnProductPage()
        {
            var item = await FindItemAsync("Душ гел с алое");
            var customer = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(customer, "/Item/Details/" + item.id);
            await customer.PostAsync("/ProductQuestion/Ask", FormRequest.Create(
                token,
                ("itemId", item.id.ToString()),
                ("question", "Има ли аромат на мента?")));

            var admin = await LoginAsync(MarketLineFactory.AdminEmail, MarketLineFactory.AdminPassword);
            var list = await admin.GetAsync("/Admin/Questions");
            list.EnsureSuccessStatusCode();
            var listHtml = WebUtility.HtmlDecode(await list.Content.ReadAsStringAsync());
            Assert.Contains("Има ли аромат на мента?", listHtml);
            Assert.Contains("Чака отговор", listHtml);

            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            var question = await db.ProductQuestions.FirstAsync(current => current.Question.Contains("мента"));
            var answerToken = FormRequest.ReadToken(listHtml);
            var answer = await admin.PostAsync("/Admin/AnswerQuestion", FormRequest.Create(
                answerToken,
                ("id", question.Id.ToString()),
                ("answer", "Не, ароматът е на алое.")));
            Assert.Equal(HttpStatusCode.Redirect, answer.StatusCode);

            var product = await customer.GetAsync("/Item/Details/" + item.id);
            product.EnsureSuccessStatusCode();
            var productHtml = WebUtility.HtmlDecode(await product.Content.ReadAsStringAsync());
            Assert.Contains("Има ли аромат на мента?", productHtml);
            Assert.Contains("Не, ароматът е на алое.", productHtml);
            Assert.DoesNotContain("Очаква отговор от администратор.", productHtml);
        }

        private async Task<HttpClient> LoginAsync(string email, string password)
        {
            var client = _factory.CreateAppClient();
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Login");
            var response = await client.PostAsync("/Account/Login", FormRequest.Create(
                token,
                ("Email", email),
                ("Password", password),
                ("RememberMe", "false")));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            return client;
        }

        private async Task<Item> FindItemAsync(string name)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            return await db.Items.FirstAsync(current => current.name == name);
        }
    }
}
