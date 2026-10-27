namespace MarketLine.Security
{
    public static class InputLimits
    {
        public const int Name = 20;
        public const int LongName = 50;
        public const int Email = 256;
        public const int Phone = 20;
        public const int Address = 200;
        public const int City = 80;
        public const int PostalCode = 12;
        public const int ProductName = 100;
        public const int ProductDescription = 2000;
        public const int Brand = 40;
        public const int Features = 200;
        public const int Url = 500;
        public const int Search = 80;
        public const int Category = 80;
        public const int Promo = 30;
        public const int Password = 100;
        public const int OrderNumber = 40;
        public const int QuantityMax = 99;
        public const int StockMax = 10000;
        public const int PriceMax = 100000;
        public const int PageTitle = 150;
        public const int PageBody = 20000;
        public const string PhonePattern = @"^$|^\+?[0-9\s\-()]{6,20}$";
        public const string PostalPattern = @"^[A-Za-z0-9\s\-]{3,12}$";
    }
}
