using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Services
{
    public class VoucherAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VoucherAdminService> _logger;

        public VoucherAdminService(ApplicationDbContext context, ILogger<VoucherAdminService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<VoucherAdminViewModel> LayDanhSachVoucherAsync(int page = 1, int pageSize = 10, string? search = null, string? discountType = null, string? status = null, DateTime? createdDate = null)
        {
            try
            {
                var query = _context.DiscountCodes.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(v => v.Code.Contains(search) || v.Name.Contains(search));
                }

                if (!string.IsNullOrEmpty(discountType) && discountType != "all")
                {
                    query = query.Where(v => v.DiscountType == discountType);
                }

                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    var now = DateTime.Now;
                    switch (status.ToLower())
                    {
                        case "active":
                            query = query.Where(v => v.IsActive && v.ValidFrom <= now && v.ValidTo >= now);
                            break;
                        case "expired":
                            query = query.Where(v => v.ValidTo < now);
                            break;
                        case "inactive":
                            query = query.Where(v => !v.IsActive);
                            break;
                        case "expiring_soon":
                            var expiringDate = now.AddDays(7);
                            query = query.Where(v => v.IsActive && v.ValidTo <= expiringDate && v.ValidTo >= now);
                            break;
                    }
                }

                if (createdDate.HasValue)
                {
                    var startDate = createdDate.Value.Date;
                    var endDate = startDate.AddDays(1);
                    query = query.Where(v => v.CreatedAt >= startDate && v.CreatedAt < endDate);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var vouchers = await query
                    .OrderByDescending(v => v.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get statistics
                var statistics = await LayThongKeVoucherAsync();

                return new VoucherAdminViewModel
                {
                    Vouchers = vouchers,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                    DiscountType = discountType,
                    Status = status,
                    CreatedDate = createdDate,
                    Statistics = statistics
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vouchers");
                return new VoucherAdminViewModel();
            }
        }

        public async Task<DiscountCode?> LayVoucherTheoIdAsync(int id)
        {
            try
            {
                return await _context.DiscountCodes
                    .Include(v => v.CreatedByUser)
                    .FirstOrDefaultAsync(v => v.DiscountCodeId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voucher by id: {Id}", id);
                return null;
            }
        }

        public async Task<DiscountCode?> LayVoucherTheoMaAsync(string code)
        {
            try
            {
                return await _context.DiscountCodes
                    .FirstOrDefaultAsync(v => v.Code == code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voucher by code: {Code}", code);
                return null;
            }
        }

        public async Task<bool> TaoVoucherAsync(DiscountCode voucher)
        {
            try
            {
                // Check if code already exists
                if (await KiemTraMaVoucherDuNhatAsync(voucher.Code))
                {
                    voucher.CreatedAt = DateTime.Now;
                    voucher.UpdatedAt = DateTime.Now;
                    
                    _context.DiscountCodes.Add(voucher);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher: {Code}", voucher.Code);
                return false;
            }
        }

        public async Task<bool> CapNhatVoucherAsync(DiscountCode voucher)
        {
            try
            {
                var existingVoucher = await _context.DiscountCodes.FindAsync(voucher.DiscountCodeId);
                if (existingVoucher == null) return false;

                // Check if code is unique (excluding current voucher)
                if (await KiemTraMaVoucherDuNhatAsync(voucher.Code, voucher.DiscountCodeId))
                {
                    existingVoucher.Code = voucher.Code;
                    existingVoucher.Name = voucher.Name;
                    existingVoucher.Description = voucher.Description;
                    existingVoucher.DiscountType = voucher.DiscountType;
                    existingVoucher.DiscountValue = voucher.DiscountValue;
                    existingVoucher.MinOrderAmount = voucher.MinOrderAmount;
                    existingVoucher.MaxDiscountAmount = voucher.MaxDiscountAmount;
                    existingVoucher.UsageLimit = voucher.UsageLimit;
                    existingVoucher.ValidFrom = voucher.ValidFrom;
                    existingVoucher.ValidTo = voucher.ValidTo;
                    existingVoucher.IsActive = voucher.IsActive;
                    existingVoucher.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating voucher: {Id}", voucher.DiscountCodeId);
                return false;
            }
        }

        public async Task<bool> XoaVoucherAsync(int id)
        {
            try
            {
                var voucher = await _context.DiscountCodes.FindAsync(id);
                if (voucher == null) return false;

                _context.DiscountCodes.Remove(voucher);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting voucher: {Id}", id);
                return false;
            }
        }

        public async Task<bool> ChuyenDoiTrangThaiVoucherAsync(int id)
        {
            try
            {
                var voucher = await _context.DiscountCodes.FindAsync(id);
                if (voucher == null) return false;

                voucher.IsActive = !voucher.IsActive;
                voucher.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling voucher status: {Id}", id);
                return false;
            }
        }

        public async Task<VoucherStatistics> LayThongKeVoucherAsync()
        {
            try
            {
                var now = DateTime.Now;
                var expiringSoonDate = now.AddDays(7);

                var totalVouchers = await _context.DiscountCodes.CountAsync();
                var activeVouchers = await _context.DiscountCodes
                    .CountAsync(v => v.IsActive && v.ValidFrom <= now && v.ValidTo >= now);
                var expiringSoonVouchers = await _context.DiscountCodes
                    .CountAsync(v => v.IsActive && v.ValidTo <= expiringSoonDate && v.ValidTo >= now);
                var expiredVouchers = await _context.DiscountCodes
                    .CountAsync(v => v.ValidTo < now);

                return new VoucherStatistics
                {
                    TotalVouchers = totalVouchers,
                    ActiveVouchers = activeVouchers,
                    ExpiringSoonVouchers = expiringSoonVouchers,
                    ExpiredVouchers = expiredVouchers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voucher statistics");
                return new VoucherStatistics();
            }
        }

        public async Task<bool> KiemTraMaVoucherDuNhatAsync(string code, int? excludeId = null)
        {
            try
            {
                var query = _context.DiscountCodes.Where(v => v.Code == code);
                if (excludeId.HasValue)
                {
                    query = query.Where(v => v.DiscountCodeId != excludeId.Value);
                }
                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking code uniqueness: {Code}", code);
                return false;
            }
        }

        public async Task<List<DiscountCode>> LayTatCaVoucherAsync()
        {
            return await _context.DiscountCodes
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsCodeUniqueAsync(string code, int? excludeId = null)
        {
            return await KiemTraMaVoucherDuNhatAsync(code, excludeId);
        }
    }
}
