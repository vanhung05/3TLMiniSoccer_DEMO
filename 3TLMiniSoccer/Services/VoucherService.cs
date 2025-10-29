using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Services
{
    public class VoucherService
    {
        private readonly ApplicationDbContext _context;

        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DiscountCode>> LayTatCaVoucherAsync()
        {
            return await _context.DiscountCodes
                .Where(dc => dc.IsActive && dc.CodeType == "Voucher")
                .OrderBy(dc => dc.Name)
                .ToListAsync();
        }

        public async Task<DiscountCode?> LayVoucherTheoMaAsync(string code)
        {
            return await _context.DiscountCodes
                .FirstOrDefaultAsync(dc => dc.Code.ToUpper() == code.ToUpper() && dc.IsActive && dc.CodeType == "Voucher");
        }

        public async Task<bool> KiemTraVoucherHopLeAsync(string code)
        {
            var voucher = await LayVoucherTheoMaAsync(code);
            if (voucher == null) return false;

            var now = DateTime.Now;
            return voucher.ValidFrom <= now && voucher.ValidTo >= now && voucher.IsActive;
        }

        public async Task<VoucherValidationResult> XacThucVoucherAsync(string code, decimal orderAmount, int? userId = null)
        {
            var result = new VoucherValidationResult();

            try
            {
                var voucher = await LayVoucherTheoMaAsync(code);
                if (voucher == null)
                {
                    result.IsValid = false;
                    result.Message = "Mã giảm giá không tồn tại";
                    return result;
                }

                // Check validity period
                var now = DateTime.Now;
                if (voucher.ValidFrom > now)
                {
                    result.IsValid = false;
                    result.Message = "Mã giảm giá chưa có hiệu lực";
                    return result;
                }

                if (voucher.ValidTo < now)
                {
                    result.IsValid = false;
                    result.Message = "Mã giảm giá đã hết hạn";
                    return result;
                }

                // Check if voucher is active
                if (!voucher.IsActive)
                {
                    result.IsValid = false;
                    result.Message = "Mã giảm giá đã bị vô hiệu hóa";
                    return result;
                }

                // Check minimum order amount
                if (voucher.MinOrderAmount.HasValue && orderAmount < voucher.MinOrderAmount.Value)
                {
                    result.IsValid = false;
                    result.Message = $"Đơn hàng tối thiểu {voucher.MinOrderAmount.Value:N0} VND để sử dụng mã này";
                    return result;
                }

                // Check usage limit
                if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
                {
                    result.IsValid = false;
                    result.Message = "Mã giảm giá đã hết lượt sử dụng";
                    return result;
                }

                // Calculate discount amount
                decimal discountAmount = 0;
                if (voucher.DiscountType == "percentage")
                {
                    discountAmount = orderAmount * (voucher.DiscountValue / 100);
                    result.DiscountType = "percentage";
                }
                else if (voucher.DiscountType == "fixed")
                {
                    discountAmount = voucher.DiscountValue;
                    result.DiscountType = "fixed";
                }

                // Apply maximum discount limit
                if (voucher.MaxDiscountAmount.HasValue && discountAmount > voucher.MaxDiscountAmount.Value)
                {
                    discountAmount = voucher.MaxDiscountAmount.Value;
                }

                // Ensure discount doesn't exceed order amount
                discountAmount = Math.Min(discountAmount, orderAmount);

                result.IsValid = true;
                result.Voucher = voucher;
                result.DiscountAmount = discountAmount;
                result.Message = $"Áp dụng thành công: {voucher.Name}";

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Message = $"Lỗi khi xác thực mã giảm giá: {ex.Message}";
                return result;
            }
        }

        public async Task<bool> SuDungVoucherAsync(string code, int userId, int bookingId, decimal discountAmount)
        {
            try
            {
                var voucher = await LayVoucherTheoMaAsync(code);
                if (voucher == null) return false;

                // Create usage record
                var usage = new DiscountCodeUsage
                {
                    DiscountCodeId = voucher.DiscountCodeId,
                    BookingId = bookingId,
                    UserId = userId,
                    DiscountAmount = discountAmount,
                    UsedAt = DateTime.Now
                };

                _context.DiscountCodeUsages.Add(usage);

                // Update voucher usage count
                voucher.UsedCount++;
                voucher.UpdatedAt = DateTime.Now;

                _context.DiscountCodes.Update(voucher);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<DiscountCodeUsage>> LayLichSuSuDungVoucherAsync(int userId)
        {
            return await _context.DiscountCodeUsages
                .Include(dcu => dcu.DiscountCode)
                .Include(dcu => dcu.Booking)
                .Where(dcu => dcu.UserId == userId)
                .OrderByDescending(dcu => dcu.UsedAt)
                .ToListAsync();
        }

        public async Task<DiscountCode> TaoVoucherAsync(DiscountCode voucher)
        {
            voucher.CreatedAt = DateTime.Now;
            voucher.UpdatedAt = DateTime.Now;
            voucher.UsedCount = 0;

            _context.DiscountCodes.Add(voucher);
            await _context.SaveChangesAsync();
            return voucher;
        }

        public async Task<bool> CapNhatVoucherAsync(DiscountCode voucher)
        {
            try
            {
                voucher.UpdatedAt = DateTime.Now;
                _context.DiscountCodes.Update(voucher);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaVoucherAsync(int voucherId)
        {
            try
            {
                var voucher = await _context.DiscountCodes.FindAsync(voucherId);
                if (voucher != null)
                {
                    _context.DiscountCodes.Remove(voucher);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> VoHieuHoaVoucherAsync(int voucherId)
        {
            try
            {
                var voucher = await _context.DiscountCodes.FindAsync(voucherId);
                if (voucher != null)
                {
                    voucher.IsActive = false;
                    voucher.UpdatedAt = DateTime.Now;
                    _context.DiscountCodes.Update(voucher);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
