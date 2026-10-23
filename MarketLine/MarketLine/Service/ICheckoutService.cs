using MarketLine.Models;

namespace MarketLine.Services
{
    public interface ICheckoutService
    {
        void SaveDraft(CheckoutForm form);
        CheckoutForm? GetDraft();
        void ClearDraft();
    }
}
