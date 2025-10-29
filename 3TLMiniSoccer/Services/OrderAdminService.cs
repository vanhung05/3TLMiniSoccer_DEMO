using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Services
{
    public class OrderAdminService
    {
        private readonly ApplicationDbContext _context;

        public OrderAdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OrderAdminStatistics> LayThongKeAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var totalOrders = await _context.Orders.CountAsync();
            var todayOrders = await _context.Orders
                .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                .CountAsync();

            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var processingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
            var shippedOrders = await _context.Orders.CountAsync(o => o.Status == "Shipped");
            var deliveredOrders = await _context.Orders.CountAsync(o => o.Status == "Delivered");
            var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);

            var todayRevenue = await _context.Orders
                .Where(o => o.Status == "Delivered" && o.CreatedAt >= today && o.CreatedAt < tomorrow)
                .SumAsync(o => o.TotalAmount);

            return new OrderAdminStatistics
            {
                TotalOrders = totalOrders,
                TodayOrders = todayOrders,
                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders,
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue
            };
        }

        public async Task<OrderAdminStatistics> LayThongKeTheoNgayAsync(DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            var totalOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate)
                .CountAsync();

            var pendingOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate && o.Status == "Pending")
                .CountAsync();

            var processingOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate && o.Status == "Processing")
                .CountAsync();

            var shippedOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate && o.Status == "Shipped")
                .CountAsync();

            var deliveredOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate && o.Status == "Delivered")
                .CountAsync();

            var cancelledOrders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate && o.Status == "Cancelled")
                .CountAsync();

            var totalRevenue = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt < endDate && o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);

            return new OrderAdminStatistics
            {
                TotalOrders = totalOrders,
                TodayOrders = totalOrders,
                PendingOrders = pendingOrders,
                ProcessingOrders = processingOrders,
                ShippedOrders = shippedOrders,
                DeliveredOrders = deliveredOrders,
                CancelledOrders = cancelledOrders,
                TotalRevenue = totalRevenue,
                TodayRevenue = totalRevenue
            };
        }

        public async Task<OrderAdminViewModel> LayDanhSachDonHangAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? paymentStatus = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.Booking)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductType)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => 
                    o.OrderCode.Contains(search) ||
                    (o.User.FirstName + " " + o.User.LastName).Contains(search) ||
                    (o.User.PhoneNumber ?? "").Contains(search) ||
                    o.User.Email.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt < toDate.Value.Date.AddDays(1));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var orderViewModels = orders.Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                CustomerName = o.User.FirstName + " " + o.User.LastName,
                CustomerPhone = o.User.PhoneNumber ?? "",
                CustomerEmail = o.User.Email,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                OrderItems = o.OrderItems.Select(oi => new ViewModels.OrderItemViewModel
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.ProductName,
                    ProductImage = oi.Product.ImageUrl,
                    Category = oi.Product.ProductType != null ? oi.Product.ProductType.TypeName : null,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            }).ToList();

            var viewModel = new OrderAdminViewModel
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Search = search,
                Status = status,
                PaymentStatus = paymentStatus,
                FromDate = fromDate,
                ToDate = toDate
            };
            
            viewModel.Orders = orderViewModels;
            
            return viewModel;
        }

        public async Task<Order?> LayDonHangTheoIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Booking)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductType)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order?> LayDonHangTheoMaAsync(string orderCode)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Booking)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductType)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        }

        public async Task<OrderDetailViewModel?> LayChiTietDonHangAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Field)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.ProductType)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return null;

            var bookingInfo = order.Booking != null ? new BookingInfoViewModel
            {
                BookingId = order.Booking.BookingId,
                BookingCode = order.Booking.BookingCode,
                FieldName = order.Booking.Field?.FieldName ?? "N/A",
                BookingDate = order.Booking.BookingDate,
                TimeSlot = $"{order.Booking.StartTime:hh\\:mm} - {order.Booking.EndTime:hh\\:mm}",
                Duration = order.Booking.Duration,
                BookingPrice = order.Booking.TotalPrice
            } : null;

            return new OrderDetailViewModel
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                CustomerName = order.User.FirstName + " " + order.User.LastName,
                CustomerPhone = order.User.PhoneNumber ?? "",
                CustomerEmail = order.User.Email,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                OrderItems = order.OrderItems.Select(oi => new ViewModels.OrderItemViewModel
                {
                    OrderItemId = oi.OrderItemId,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.ProductName,
                    ProductImage = oi.Product.ImageUrl,
                    Category = oi.Product.ProductType != null ? oi.Product.ProductType.TypeName : null,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList(),
                BookingInfo = bookingInfo
            };
        }

        public async Task<bool> CapNhatTrangThaiDonHangAsync(int orderId, string status, string? notes = null)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return false;

                order.Status = status;
                order.UpdatedAt = DateTime.Now;
                if (!string.IsNullOrEmpty(notes))
                {
                    order.Notes = notes;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HuyDonHangAsync(int orderId, string reason)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return false;

                order.Status = "Cancelled";
                order.Notes = reason;
                order.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaDonHangAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return false;

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CoTheCapNhatTrangThaiDonHangAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            // Cannot update cancelled or delivered orders
            if (order.Status == "Cancelled" || order.Status == "Delivered")
                return false;

            return true;
        }

        public async Task<bool> CoTheHuyDonHangAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            // Can only cancel pending or processing orders
            return order.Status == "Pending" || order.Status == "Processing";
        }

        public async Task<bool> CoTheXoaDonHangAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            // Can only delete cancelled orders
            return order.Status == "Cancelled";
        }

        public async Task<List<Order>> LayTatCaDonHangAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }
    }
}