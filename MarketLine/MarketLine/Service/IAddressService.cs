using MarketLine.Models;

namespace MarketLine.Services
{
    public interface IAddressService
    {
        Task<List<Address>> GetByUserAsync(string userId);
        Task<Address?> GetDefaultAsync(string userId);
        Task AddAsync(string userId, AddressForm form);
        Task SetDefaultAsync(string userId, int addressId);
        Task DeleteAsync(string userId, int addressId);
    }
}
