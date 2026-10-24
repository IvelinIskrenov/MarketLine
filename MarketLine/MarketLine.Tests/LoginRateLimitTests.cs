using System.Net;

namespace MarketLine.Tests
{
    public class LoginRateLimitTests
    {
        [Fact]
        public async Task Login_IsRateLimited_AfterRepeatedFailures()
        {
            await using var factory = new MarketLineFactory { LoginPermitLimit = 3 };
            var client = factory.CreateAppClient();

            HttpResponseMessage? last = null;
            for (var i = 0; i < 3; i++)
            {
                var token = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Login");
                last = await client.PostAsync("/Account/Login", FormRequest.Create(
                    token,
                    ("Email", "anyone@test.com"),
                    ("Password", "wrong-password"),
                    ("RememberMe", "false")));
                Assert.Equal(HttpStatusCode.OK, last.StatusCode);
            }

            var blockedToken = await FormRequest.GetAntiForgeryTokenAsync(client, "/Account/Login");
            last = await client.PostAsync("/Account/Login", FormRequest.Create(
                blockedToken,
                ("Email", "anyone@test.com"),
                ("Password", "wrong-password"),
                ("RememberMe", "false")));

            Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
            Assert.Contains("Твърде много опити", await last.Content.ReadAsStringAsync());
        }
    }
}
