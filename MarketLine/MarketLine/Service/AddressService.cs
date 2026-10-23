using MarketLine.Data;
using MarketLine.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketLine.Services
{
    public class AddressService : IAddressService
    {
        private readonly MarketLineDbContext _context;

        public AddressService(MarketLineDbContext context)
        {
            _context = context;
        }

        public Task<List<Address>> GetByUserAsync(string userId)
        {
            return _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.Id)
                .ToListAsync();
        }

        public Task<Address?> GetDefaultAsync(string userId)
        {
            return _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.Id)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(string userId, AddressForm form)
        {
            var hasAddresses = await _context.Addresses.AnyAsync(a => a.UserId == userId);
            var makeDefault = form.IsDefault || !hasAddresses;

            if (makeDefault)
            {
                await ClearDefaultAsync(userId);
            }

            _context.Addresses.Add(new Address
            {
                UserId = userId,
                Label = string.IsNullOrWhiteSpace(form.Label) ? null : form.Label.Trim(),
                AddressLine = form.AddressLine.Trim(),
                City = form.City.Trim(),
                PostalCode = form.PostalCode.Trim(),
                IsDefault = makeDefault
            });

            await _context.SaveChangesAsync();
        }

        public async Task SetDefaultAsync(string userId, int addressId)
        {
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
            if (address == null) return;

            await ClearDefaultAsync(userId);
            address.IsDefault = true;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string userId, int addressId)
        {
            var address = await _context.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);
            if (address == null) return;

            var wasDefault = address.IsDefault;
            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            if (wasDefault)
            {
                var next = await _context.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);
                if (next != null)
                {
                    next.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task ClearDefaultAsync(string userId)
        {
            var current = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();

            foreach (var address in current)
            {
                address.IsDefault = false;
            }
        }
    }
}
