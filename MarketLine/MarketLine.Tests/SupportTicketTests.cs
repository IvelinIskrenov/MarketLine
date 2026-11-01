using System.Net;
using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketLine.Tests
{
    public class SupportTicketTests : IClassFixture<MarketLineFactory>
    {
        private readonly MarketLineFactory _factory;

        public SupportTicketTests(MarketLineFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Guest_IsRedirectedFromTickets()
        {
            var client = _factory.CreateAppClient();
            var response = await client.GetAsync("/Profile/Tickets");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
        }

        [Fact]
        public async Task Customer_CanCreateTicketAndSeeItInProfile()
        {
            var client = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Profile/Tickets");
            var create = await client.PostAsync("/Profile/CreateTicket", FormRequest.Create(
                token,
                ("subject", "Проблем с доставка"),
                ("topic", TicketTopics.Delivery),
                ("message", "Колетът още не е пристигнал.")));
            Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);

            var list = await client.GetAsync("/Profile/Tickets");
            list.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await list.Content.ReadAsStringAsync());
            Assert.Contains("Проблем с доставка", html);
            Assert.Contains("Отворен", html);

            var ticket = await FindTicketAsync("Проблем с доставка");
            var details = await client.GetAsync("/Profile/Ticket/" + ticket.Id);
            details.EnsureSuccessStatusCode();
            var detailsHtml = WebUtility.HtmlDecode(await details.Content.ReadAsStringAsync());
            Assert.Contains("Колетът още не е пристигнал.", detailsHtml);
        }

        [Fact]
        public async Task Customer_CannotViewAnotherUsersTicket()
        {
            var owner = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(owner, "/Profile/Tickets");
            await owner.PostAsync("/Profile/CreateTicket", FormRequest.Create(
                token,
                ("subject", "Личен билет"),
                ("topic", TicketTopics.Other),
                ("message", "Тайно съобщение за поддръжка.")));

            var ticket = await FindTicketAsync("Личен билет");
            var other = await LoginAsync(MarketLineFactory.OtherEmail, MarketLineFactory.OtherPassword);
            var response = await other.GetAsync("/Profile/Ticket/" + ticket.Id);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var list = await other.GetAsync("/Profile/Tickets");
            var html = WebUtility.HtmlDecode(await list.Content.ReadAsStringAsync());
            Assert.DoesNotContain("Личен билет", html);
            Assert.DoesNotContain("Тайно съобщение за поддръжка.", html);
        }

        [Fact]
        public async Task Create_RejectsEmptySubjectAndUnknownOrder()
        {
            var client = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Profile/Tickets");
            await client.PostAsync("/Profile/CreateTicket", FormRequest.Create(
                token,
                ("subject", "   "),
                ("topic", TicketTopics.Other),
                ("message", "Текст без тема.")));

            var empty = await client.GetAsync("/Profile/Tickets");
            var emptyHtml = WebUtility.HtmlDecode(await empty.Content.ReadAsStringAsync());
            Assert.Contains("Въведете тема", emptyHtml);

            await client.PostAsync("/Profile/CreateTicket", FormRequest.Create(
                await FormRequest.GetAntiForgeryTokenAsync(client, "/Profile/Tickets"),
                ("subject", "Свързан с поръчка"),
                ("topic", TicketTopics.Order),
                ("message", "Имам въпрос."),
                ("orderNumber", "ML-MISSING-0000")));

            var missing = await client.GetAsync("/Profile/Tickets");
            var missingHtml = WebUtility.HtmlDecode(await missing.Content.ReadAsStringAsync());
            Assert.Contains("Поръчката не е намерена", missingHtml);
        }

        [Fact]
        public async Task Admin_CanReplyAndCustomerSeesTheAnswer()
        {
            var customer = await LoginAsync(MarketLineFactory.CustomerEmail, MarketLineFactory.CustomerPassword);
            var token = await FormRequest.GetAntiForgeryTokenAsync(customer, "/Profile/Tickets");
            await customer.PostAsync("/Profile/CreateTicket", FormRequest.Create(
                token,
                ("subject", "Въпрос за плащане"),
                ("topic", TicketTopics.Payment),
                ("message", "Мога ли да платя с карта?")));

            var ticket = await FindTicketAsync("Въпрос за плащане");
            var admin = await LoginAsync(MarketLineFactory.AdminEmail, MarketLineFactory.AdminPassword);
            var list = await admin.GetAsync("/Admin/Tickets");
            list.EnsureSuccessStatusCode();
            var listHtml = WebUtility.HtmlDecode(await list.Content.ReadAsStringAsync());
            Assert.Contains("Въпрос за плащане", listHtml);
            Assert.Contains("Отворени билети", WebUtility.HtmlDecode(await (await admin.GetAsync("/Admin")).Content.ReadAsStringAsync()));

            var details = await admin.GetAsync("/Admin/Ticket/" + ticket.Id);
            details.EnsureSuccessStatusCode();
            var reply = await admin.PostAsync("/Admin/ReplyTicket", FormRequest.Create(
                FormRequest.ReadToken(await details.Content.ReadAsStringAsync()),
                ("id", ticket.Id.ToString()),
                ("message", "Да, наложен платеж или карта при куриера.")));
            Assert.Equal(HttpStatusCode.Redirect, reply.StatusCode);

            var customerView = await customer.GetAsync("/Profile/Ticket/" + ticket.Id);
            customerView.EnsureSuccessStatusCode();
            var html = WebUtility.HtmlDecode(await customerView.Content.ReadAsStringAsync());
            Assert.Contains("Мога ли да платя с карта?", html);
            Assert.Contains("Да, наложен платеж или карта при куриера.", html);
            Assert.Contains("Изчаква отговор", html);
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

        private async Task<SupportTicket> FindTicketAsync(string subject)
        {
            using var scope = _factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MarketLineDbContext>();
            return await db.SupportTickets.FirstAsync(ticket => ticket.Subject == subject);
        }
    }
}
