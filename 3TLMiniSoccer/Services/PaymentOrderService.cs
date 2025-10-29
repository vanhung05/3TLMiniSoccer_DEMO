using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using Microsoft.EntityFrameworkCore;

namespace _3TLMiniSoccer.Services
{
    public class PaymentOrderService
    {
        private readonly ApplicationDbContext _context;

        public PaymentOrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentOrder?> GetPaymentOrderByOrderIdAsync(string orderCode)
        {
            return await _context.PaymentOrders
                .Include(po => po.Booking)
                .Include(po => po.User)
                .FirstOrDefaultAsync(po => po.OrderCode == orderCode);
        }

        public async Task<PaymentOrder?> GetPaymentOrderByBookingIdAsync(int bookingId)
        {
            return await _context.PaymentOrders
                .Include(po => po.Booking)
                .Include(po => po.User)
                .FirstOrDefaultAsync(po => po.BookingId == bookingId);
        }

        public async Task<PaymentOrder> CreatePaymentOrderAsync(PaymentOrder paymentOrder)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Creating PaymentOrder: OrderCode={paymentOrder.OrderCode}, BookingId={paymentOrder.BookingId}, UserId={paymentOrder.UserId}, Amount={paymentOrder.Amount}");
                
                // Use raw SQL to avoid OUTPUT clause conflicts with triggers
                var sql = @"
                    INSERT INTO PaymentOrders (OrderCode, BookingId, UserId, Amount, Status, PaymentMethod, ExpiredAt, CreatedAt, UpdatedAt)
                    VALUES (@OrderCode, @BookingId, @UserId, @Amount, @Status, @PaymentMethod, @ExpiredAt, @CreatedAt, @UpdatedAt);";

                var createdAt = DateTime.Now;
                var updatedAt = createdAt;

                await _context.Database.ExecuteSqlRawAsync(sql,
                    new Microsoft.Data.SqlClient.SqlParameter("@OrderCode", paymentOrder.OrderCode),
                    new Microsoft.Data.SqlClient.SqlParameter("@BookingId", paymentOrder.BookingId),
                    new Microsoft.Data.SqlClient.SqlParameter("@UserId", paymentOrder.UserId),
                    new Microsoft.Data.SqlClient.SqlParameter("@Amount", paymentOrder.Amount),
                    new Microsoft.Data.SqlClient.SqlParameter("@Status", paymentOrder.Status ?? "Pending"),
                    new Microsoft.Data.SqlClient.SqlParameter("@PaymentMethod", paymentOrder.PaymentMethod),
                    new Microsoft.Data.SqlClient.SqlParameter("@ExpiredAt", paymentOrder.ExpiredAt.HasValue ? paymentOrder.ExpiredAt.Value : (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@CreatedAt", createdAt),
                    new Microsoft.Data.SqlClient.SqlParameter("@UpdatedAt", updatedAt));

                // Get the inserted payment order
                var insertedPaymentOrder = await _context.PaymentOrders
                    .FirstOrDefaultAsync(po => po.OrderCode == paymentOrder.OrderCode);

                if (insertedPaymentOrder != null)
                {
                    System.Diagnostics.Debug.WriteLine($"PaymentOrder created successfully with ID: {insertedPaymentOrder.PaymentOrderId}");
                    return insertedPaymentOrder;
                }
                else
                {
                    throw new Exception("Failed to retrieve created payment order");
                }
            }
            catch (DbUpdateException dbEx)
            {
                System.Diagnostics.Debug.WriteLine($"DbUpdateException: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {dbEx.InnerException.Message}");
                }
                throw new Exception($"Database error creating payment order: {dbEx.Message}. Inner: {dbEx.InnerException?.Message}", dbEx);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Exception: {ex.Message}");
                throw new Exception($"Error creating payment order: {ex.Message}", ex);
            }
        }

        public async Task<PaymentOrder?> UpdatePaymentOrderStatusAsync(string orderId, string status, string? paymentReference = null, string? paymentData = null)
        {
            var paymentOrder = await _context.PaymentOrders
                .FirstOrDefaultAsync(po => po.OrderCode == orderId);

            if (paymentOrder == null)
                return null;

            paymentOrder.Status = status;
            paymentOrder.UpdatedAt = DateTime.Now;

            if (status == "Success")
            {
                paymentOrder.ProcessedAt = DateTime.Now;
                if (!string.IsNullOrEmpty(paymentReference))
                    paymentOrder.PaymentReference = paymentReference;
                if (!string.IsNullOrEmpty(paymentData))
                    paymentOrder.PaymentData = paymentData;
            }

            await _context.SaveChangesAsync();
            return paymentOrder;
        }

        public async Task<bool> IsPaymentOrderExpiredAsync(string orderId)
        {
            var paymentOrder = await _context.PaymentOrders
                .FirstOrDefaultAsync(po => po.OrderCode == orderId);

            if (paymentOrder == null)
                return true;

            return paymentOrder.ExpiredAt < DateTime.Now;
        }

        public async Task<List<PaymentOrder>> GetExpiredPaymentOrdersAsync()
        {
            return await _context.PaymentOrders
                .Where(po => po.Status == "Pending" && po.ExpiredAt < DateTime.Now)
                .ToListAsync();
        }

        public async Task<bool> MarkPaymentOrderAsExpiredAsync(string orderId)
        {
            var paymentOrder = await _context.PaymentOrders.FirstOrDefaultAsync(po => po.OrderCode == orderId);
            if (paymentOrder != null)
            {
                paymentOrder.Status = "Cancelled";
                paymentOrder.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<PaymentOrder?> GetActivePaymentOrderByBookingIdAsync(int bookingId)
        {
            return await _context.PaymentOrders
                .Include(po => po.Booking)
                .Include(po => po.User)
                .FirstOrDefaultAsync(po => po.BookingId == bookingId && 
                    (po.Status == "Pending" || po.Status == "Success"));
        }

        public async Task<bool> CancelPaymentOrderAsync(string orderId)
        {
            var paymentOrder = await _context.PaymentOrders
                .FirstOrDefaultAsync(po => po.OrderCode == orderId);

            if (paymentOrder == null || paymentOrder.Status != "Pending")
                return false;

            paymentOrder.Status = "Cancelled";
            paymentOrder.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkPaymentOrderAsPaidAsync(string orderId, int bookingId, string paymentReference)
        {
            var paymentOrder = await _context.PaymentOrders.FirstOrDefaultAsync(po => po.OrderCode == orderId);
            if (paymentOrder != null)
            {
                paymentOrder.Status = "Success";
                paymentOrder.BookingId = bookingId;
                paymentOrder.ProcessedAt = DateTime.Now;
                paymentOrder.PaymentReference = paymentReference;
                paymentOrder.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdatePaymentOrderQRCodeAsync(string orderId, string status, int? bookingId, string? qrCodeData)
        {
            try
            {
                var paymentOrder = await _context.PaymentOrders.FirstOrDefaultAsync(po => po.OrderCode == orderId);
                if (paymentOrder != null)
                {
                    paymentOrder.Status = status;
                    paymentOrder.UpdatedAt = DateTime.Now;
                    
                    if (bookingId.HasValue)
                    {
                        paymentOrder.BookingId = bookingId.Value;
                    }
                    if (!string.IsNullOrEmpty(qrCodeData))
                    {
                        paymentOrder.PaymentData = qrCodeData;
                    }
                    
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
