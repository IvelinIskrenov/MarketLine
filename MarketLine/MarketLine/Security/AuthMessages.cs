namespace MarketLine.Security
{
    public static class AuthMessages
    {
        public const string InvalidLogin = "Невалиден имейл или парола.";
        public const string ForgotPassword = "Ако имейлът е регистриран, можете да зададете нова парола.";
        public const string PasswordChanged = "Паролата е сменена. Влезте с новата парола.";
        public const string RegisterFailed = "Регистрацията не беше успешна. Ако вече имате профил, влезте.";
        public const string ResetFailed = "Невалиден имейл или връзката е изтекла.";
        public const string LoginRateLimited = "Твърде много опити за вход. Опитайте отново след малко.";
    }
}
