using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.Services
{
    public class PaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Payment Methods

        public async Task<List<PaymentMethod>> LayTatCaPhuongThucThanhToanAsync()
        {
            return await _context.PaymentMethods
                .Where(pm => pm.IsActive)
                .OrderBy(pm => pm.MethodName)
                .ToListAsync();
        }

        public async Task<PaymentMethod?> LayPhuongThucThanhToanTheoIdAsync(int paymentMethodId)
        {
            return await _context.PaymentMethods.FindAsync(paymentMethodId);
        }

        public async Task<PaymentMethod> TaoPhuongThucThanhToanAsync(PaymentMethod paymentMethod)
        {
            paymentMethod.CreatedAt = DateTime.Now;
            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();
            return paymentMethod;
        }

        public async Task<bool> CapNhatPhuongThucThanhToanAsync(PaymentMethod paymentMethod)
        {
            try
            {
                _context.PaymentMethods.Update(paymentMethod);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaPhuongThucThanhToanAsync(int paymentMethodId)
        {
            try
            {
                var paymentMethod = await _context.PaymentMethods.FindAsync(paymentMethodId);
                if (paymentMethod != null)
                {
                    paymentMethod.IsActive = false;
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Transactions

        public async Task<List<Transaction>> LayTatCaGiaoDichAsync()
        {
            return await _context.Transactions
                .Include(t => t.Booking)
                .ThenInclude(b => b.Field)
                .Include(t => t.PaymentMethod)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Transaction?> LayGiaoDichTheoIdAsync(int transactionId)
        {
            return await _context.Transactions
                .Include(t => t.Booking)
                .ThenInclude(b => b.Field)
                .Include(t => t.PaymentMethod)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<List<Transaction>> LayGiaoDichTheoDatSanAsync(int bookingId)
        {
            return await _context.Transactions
                .Include(t => t.PaymentMethod)
                .Where(t => t.BookingId == bookingId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Transaction>> LayGiaoDichTheoTrangThaiAsync(string status)
        {
            return await _context.Transactions
                .Include(t => t.Booking)
                .ThenInclude(b => b.Field)
                .Include(t => t.PaymentMethod)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Transaction> TaoGiaoDichAsync(Transaction transaction)
        {
            transaction.CreatedAt = DateTime.Now;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> CapNhatGiaoDichAsync(Transaction transaction)
        {
            try
            {
                _context.Transactions.Update(transaction);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XuLyThanhToanAsync(int transactionId, string transactionCode, string paymentData)
        {
            try
            {
                var transaction = await _context.Transactions.FindAsync(transactionId);
                if (transaction != null)
                {
                    transaction.Status = "Success";
                    transaction.TransactionCode = transactionCode;
                    transaction.PaymentData = paymentData;
                    transaction.ProcessedAt = DateTime.Now;

                    // Update booking payment status
                    var booking = await _context.Bookings.FindAsync(transaction.BookingId);
                    if (booking != null)
                    {
                        booking.PaymentStatus = "Paid";
                        booking.PaymentReference = transactionCode;
                        booking.UpdatedAt = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HoanTienGiaoDichAsync(int transactionId, string reason)
        {
            try
            {
                var transaction = await _context.Transactions.FindAsync(transactionId);
                if (transaction != null)
                {
                    transaction.Status = "Refunded";
                    transaction.PaymentData = reason;
                    transaction.ProcessedAt = DateTime.Now;

                    // Update booking payment status
                    var booking = await _context.Bookings.FindAsync(transaction.BookingId);
                    if (booking != null)
                    {
                        booking.PaymentStatus = "Refunded";
                        booking.UpdatedAt = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion


        #region Payment Processing

        public async Task<bool> XuLyThanhToanDatSanAsync(int bookingId, int paymentMethodId, decimal amount)
        {
            try
            {
                var transaction = new Transaction
                {
                    BookingId = bookingId,
                    PaymentMethodId = paymentMethodId,
                    Amount = amount,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XacThucThanhToanAsync(int transactionId)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            return transaction != null && transaction.Status == "Success";
        }

        public async Task<string> TaoMaThamChieuThanhToanAsync()
        {
            var today = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"PAY{today}{random}";
        }

        public async Task<decimal> TinhTongTienAsync(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            return booking?.TotalPrice ?? 0;
        }

        #endregion
    }
}
