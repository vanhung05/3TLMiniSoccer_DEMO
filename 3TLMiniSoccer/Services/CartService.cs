using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using Microsoft.EntityFrameworkCore;

namespace _3TLMiniSoccer.Services
{
    public class CartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CartItem>> LayGioHangNguoiDungAsync(int userId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userId)
                .OrderBy(ci => ci.AddedAt)
                .ToListAsync();
        }

        public async Task<CartItem?> LayGioHangTheoIdAsync(int cartItemId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
        }

        public async Task<CartItem> ThemVaoGioHangAsync(int userId, int productId, int quantity = 1)
        {
            // Check if item already exists in cart
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);

            if (existingItem != null)
            {
                // Update existing item quantity
                existingItem.Quantity += quantity;
                // No need to update AddedAt for quantity changes
                
                await _context.SaveChangesAsync();
                return existingItem;
            }
            else
            {
                // Create new cart item
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity,
                    AddedAt = DateTime.Now
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync();
                return cartItem;
            }
        }

        public async Task<bool> XoaKhoiGioHangAsync(int cartItemId)
        {
            try
            {
                var cartItem = await _context.CartItems.FindAsync(cartItemId);
                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message?.Contains("database triggers") == true || 
                                                                        ex.InnerException?.Message?.Contains("OUTPUT clause") == true)
            {
                // Handle trigger conflict by using raw SQL
                var deleteSql = "DELETE FROM CartItems WHERE CartItemId = @CartItemId";
                
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(deleteSql,
                    new Microsoft.Data.SqlClient.SqlParameter("@CartItemId", cartItemId));
                
                return rowsAffected > 0;
            }
        }

        public async Task<bool> XoaGioHangNguoiDungAsync(int userId)
        {
            try
            {
                var cartItems = await _context.CartItems.Where(ci => ci.UserId == userId).ToListAsync();
                if (cartItems.Any())
                {
                    _context.CartItems.RemoveRange(cartItems);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message?.Contains("database triggers") == true || 
                                                                        ex.InnerException?.Message?.Contains("OUTPUT clause") == true)
            {
                // Handle trigger conflict by using raw SQL
                var deleteSql = "DELETE FROM CartItems WHERE UserId = @UserId";
                
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(deleteSql,
                    new Microsoft.Data.SqlClient.SqlParameter("@UserId", userId));
                
                return rowsAffected > 0;
            }
        }

        public async Task<decimal> LayTongGioHangAsync(int userId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userId)
                .SumAsync(ci => ci.Quantity * ci.Product.Price);
        }

        public async Task<bool> CapNhatGioHangAsync(int cartItemId, int quantity)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                // No need to update AddedAt for quantity changes
                
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<int> LaySoLuongGioHangAsync(int userId)
        {
            return await _context.CartItems
                .Where(ci => ci.UserId == userId)
                .SumAsync(ci => ci.Quantity);
        }
    }
}
