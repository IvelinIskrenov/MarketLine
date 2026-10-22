using MarketLine.Models;

namespace MarketLine.Services
{
    public interface ICheckoutService
    {
        void SaveDraft(CheckoutForm form);
        CheckoutForm? GetDraft();
        PlacedOrder PlaceOrder(CheckoutForm form, CartViewModel cart);
        PlacedOrder? GetOrder(string orderNumber);
    }
}
