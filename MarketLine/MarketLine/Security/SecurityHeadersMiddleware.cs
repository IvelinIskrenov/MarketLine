namespace MarketLine.Security
{
    public static class SecurityHeadersMiddleware
    {
        public static IApplicationBuilder UseMarketLineSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    var headers = context.Response.Headers;
                    headers["X-Content-Type-Options"] = "nosniff";
                    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    headers["X-Frame-Options"] = "SAMEORIGIN";
                    return Task.CompletedTask;
                });

                await next();
            });
        }
    }
}
