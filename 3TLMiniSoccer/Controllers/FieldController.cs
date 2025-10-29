using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using _3TLMiniSoccer.Services;
using _3TLMiniSoccer.ViewModels;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace _3TLMiniSoccer.Controllers
{
    public class BookingConfirmationData
    {
        public int FieldId { get; set; }
        public int UserId { get; set; }
        public DateTime BookingDate { get; set; }
        public int Duration { get; set; }
        public decimal TotalPrice { get; set; }
        public string? VoucherCode { get; set; }
        public decimal VoucherDiscount { get; set; }
        public string? Notes { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? FieldType { get; set; }
        public string? FieldLocation { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? Subtotal { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestEmail { get; set; }
    }

    public class FieldController : Controller
    {
        private readonly FieldService _fieldService;
        private readonly BookingService _bookingService;
        private readonly SEPayService _sepayService;
        private readonly PaymentConfigService _configService;
        private readonly SystemConfigService _systemConfigService;
        private readonly VoucherService _voucherService;
        private readonly PaymentOrderService _paymentOrderService;
        private readonly CartService _cartService;
        private readonly ApplicationDbContext _context;
        private readonly NotificationHubService _notificationHubService;
        private readonly BookingScheduleService _bookingScheduleService;
        private readonly ILogger<FieldController> _logger;

        public FieldController(FieldService fieldService, BookingService bookingService, 
            SEPayService sepayService, PaymentConfigService configService, 
            SystemConfigService systemConfigService, VoucherService voucherService, 
            PaymentOrderService paymentOrderService, CartService cartService, ApplicationDbContext context,
            NotificationHubService notificationHubService, BookingScheduleService bookingScheduleService,
            ILogger<FieldController> logger)
        {
            _fieldService = fieldService;
            _bookingService = bookingService;
            _sepayService = sepayService;
            _configService = configService;
            _systemConfigService = systemConfigService;
            _voucherService = voucherService;
            _paymentOrderService = paymentOrderService;
            _cartService = cartService;
            _context = context;
            _notificationHubService = notificationHubService;
            _bookingScheduleService = bookingScheduleService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var fields = await _fieldService.LayTatCaSanAsync();
            var fieldTypes = await _fieldService.LayTatCaLoaiSanAsync();
            
            var viewModel = new FieldIndexViewModel
            {
                Fields = fields,
                FieldTypes = fieldTypes
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var field = await _fieldService.LaySanTheoIdAsync(id);
            if (field == null)
            {
                return NotFound();
            }

            // Get booked time slots for today to display in field info
            var bookedTimeSlots = await _bookingService.LayKhungGioDaDatAsync(id, DateTime.Today);

            var viewModel = new FieldDetailsViewModel
            {
                Field = field,
                BookedTimeSlots = bookedTimeSlots,
                RecentBookings = await _bookingService.LayDatSanGanDayAsync(5),
                BookingForm = new BookingViewModel
                {
                    FieldId = id,
                    BookingDate = DateTime.Today,
                    StartTime = field.OpeningTime,
                    EndTime = field.OpeningTime.Add(TimeSpan.FromMinutes(60)),
                    Duration = 60
                }
            };

            return View(viewModel);
        }



        [HttpGet]
        public async Task<IActionResult> GetAvailableTimes(int fieldId, DateTime bookingDate, int duration = 120)
        {
            try
            {
                var field = await _fieldService.LaySanTheoIdAsync(fieldId);
                if (field == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sân bóng" });
                }

                // Use stored procedure to get available time slots
                var parameters = new[]
                {
                    new SqlParameter("@FieldId", fieldId),
                    new SqlParameter("@BookingDate", bookingDate.Date),
                    new SqlParameter("@MaxDuration", duration)
                };

                var availableSlots = await _context.Database.SqlQueryRaw<AvailableTimeSlot>(
                    "EXEC sp_CheckDynamicAvailability @FieldId, @BookingDate, @MaxDuration",
                    parameters).ToListAsync();

                var startTimes = availableSlots.Select(s => s.SuggestedStart.ToString(@"hh\:mm")).ToList();
                var endTimes = availableSlots.Select(s => s.SuggestedEnd.ToString(@"hh\:mm")).ToList();

                return Json(new
                {
                    success = true,
                    startTimes = startTimes,
                    endTimes = endTimes
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBookedTimeSlots(int fieldId, DateTime date)
        {
            try
            {
                var field = await _fieldService.LaySanTheoIdAsync(fieldId);
                if (field == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sân bóng" });
                }

                // Get booked time slots for the specified date
                var bookedTimeSlots = await _bookingService.LayKhungGioDaDatAsync(fieldId, date.Date);

                var result = bookedTimeSlots.Select(booking => new
                {
                    startTime = booking.StartTime.ToString(@"hh\:mm"),
                    endTime = booking.EndTime.ToString(@"hh\:mm"),
                    userName = $"{booking.User?.FirstName} {booking.User?.LastName}".Trim()
                }).ToList();

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckAvailability(int fieldId, DateTime bookingDate, TimeSpan startTime, TimeSpan endTime)
        {
            try
            {
                // Check field status first
                var field = await _context.Fields.FindAsync(fieldId);
                if (field == null)
                {
                    return Json(new
                    {
                        isAvailable = false,
                        message = "Sân bóng không tồn tại"
                    });
                }
                
                if (field.Status?.ToLower() == "maintenance")
                {
                    return Json(new
                    {
                        isAvailable = false,
                        message = "Sân này đang trong quá trình bảo trì và không thể đặt sân"
                    });
                }
                
                if (field.Status?.ToLower() == "inactive" || field.Status?.ToLower() == "closed")
                {
                    return Json(new
                    {
                        isAvailable = false,
                        message = "Sân này hiện không hoạt động và không thể đặt sân"
                    });
                }
                
                var overlapCheck = await CheckBookingOverlapAsync(fieldId, bookingDate, startTime, endTime);
                
                return Json(new
                {
                    isAvailable = overlapCheck.IsAvailable,
                    message = overlapCheck.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    isAvailable = false,
                    message = $"Lỗi kiểm tra khả dụng: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Book(BookingViewModel model)
        {
            // Debug logging
            System.Diagnostics.Debug.WriteLine("=== Book Action Called ===");
            System.Diagnostics.Debug.WriteLine($"FieldId: {model.FieldId}");
            System.Diagnostics.Debug.WriteLine($"BookingDate: {model.BookingDate}");
            System.Diagnostics.Debug.WriteLine($"StartTime: {model.StartTime}");
            System.Diagnostics.Debug.WriteLine($"EndTime: {model.EndTime}");
            System.Diagnostics.Debug.WriteLine($"Duration: {model.Duration}");
            System.Diagnostics.Debug.WriteLine($"TotalPrice: {model.TotalPrice}");
            System.Diagnostics.Debug.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("ModelState Errors:");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {key}: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                    }
                }
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Details", new { id = model.FieldId });
            }
            
            // Check field status - prevent booking if field is under maintenance
            var field = await _context.Fields.FindAsync(model.FieldId);
            if (field == null)
            {
                TempData["Error"] = "Sân bóng không tồn tại";
                return RedirectToAction("Details", new { id = model.FieldId });
            }
            
            if (field.Status?.ToLower() == "maintenance")
            {
                TempData["Error"] = "Sân này đang trong quá trình bảo trì và không thể đặt sân. Vui lòng chọn sân khác.";
                return RedirectToAction("Details", new { id = model.FieldId });
            }
            
            if (field.Status?.ToLower() == "inactive" || field.Status?.ToLower() == "closed")
            {
                TempData["Error"] = "Sân này hiện không hoạt động và không thể đặt sân. Vui lòng chọn sân khác.";
                return RedirectToAction("Details", new { id = model.FieldId });
            }
            
            // Additional validation for dynamic booking
            if (model.StartTime >= model.EndTime)
            {
                TempData["Error"] = "Thời gian kết thúc phải sau thời gian bắt đầu";
                return RedirectToAction("Details", new { id = model.FieldId });
            }

            // Validate duration matches time range
            var actualDuration = (int)(model.EndTime - model.StartTime).TotalMinutes;
            if (actualDuration != model.Duration)
            {
                TempData["Error"] = "Thời gian thuê không khớp với khoảng thời gian đã chọn";
                return RedirectToAction("Details", new { id = model.FieldId });
            }

            // Additional validation
            if (model.FieldId <= 0)
            {
                System.Diagnostics.Debug.WriteLine("Validation failed: FieldId <= 0");
                TempData["Error"] = "Sân bóng không hợp lệ";
                return RedirectToAction("Details", new { id = model.FieldId });
            }

            if (model.BookingDate < DateTime.Today)
            {
                System.Diagnostics.Debug.WriteLine($"Validation failed: BookingDate {model.BookingDate} < Today {DateTime.Today}");
                TempData["Error"] = "Ngày đặt sân không được trong quá khứ";
                return RedirectToAction("Details", new { id = model.FieldId });
            }

            if (model.Duration <= 0 || model.Duration > 480) // 8 hours = 480 minutes
            {
                System.Diagnostics.Debug.WriteLine($"Validation failed: Duration {model.Duration} not in range 1-480 minutes");
                TempData["Error"] = "Thời gian phải từ 1 đến 480 phút (8 giờ)";
                return RedirectToAction("Details", new { id = model.FieldId });
            }

            if (model.TotalPrice <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"Validation failed: TotalPrice {model.TotalPrice} <= 0");
                TempData["Error"] = "Tổng tiền không hợp lệ";
                return RedirectToAction("Details", new { id = model.FieldId });
            }

            // Validate dynamic time slot availability using stored procedure
            var overlapCheck = await CheckBookingOverlapAsync(model.FieldId, model.BookingDate, model.StartTime, model.EndTime);
            if (!overlapCheck.IsAvailable)
            {
                System.Diagnostics.Debug.WriteLine($"Validation failed: TimeSlot {model.StartTime}-{model.EndTime} not available for Field {model.FieldId} on {model.BookingDate}. Message: {overlapCheck.Message}");
                TempData["Error"] = overlapCheck.Message;
                return RedirectToAction("Details", new { id = model.FieldId });
            }

            // Additional validation: Check if time slot is in the past (for today's bookings)
            if (model.BookingDate.Date == DateTime.Today)
            {
                if (model.StartTime <= DateTime.Now.TimeOfDay)
                {
                    System.Diagnostics.Debug.WriteLine($"Validation failed: StartTime {model.StartTime} is in the past");
                    TempData["Error"] = "Thời gian bắt đầu đã qua, vui lòng chọn thời gian khác";
                    return RedirectToAction("Details", new { id = model.FieldId });
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = $"Vui lòng kiểm tra lại thông tin đặt sân: {string.Join(", ", errors)}";
                return RedirectToAction("Details", new { id = model.FieldId });
            }


            try
            {
                // Check if user is logged in
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    TempData["Error"] = "Vui lòng đăng nhập để đặt sân.";
                    return RedirectToAction("Login", "Account");
                }
                
                var userId = userIdClaim;

                // Get field details for confirmation
                var fieldDetails = await _fieldService.LaySanTheoIdAsync(model.FieldId);
                
                System.Diagnostics.Debug.WriteLine($"Field: {fieldDetails?.FieldName}, StartTime: {model.StartTime}, EndTime: {model.EndTime}");
                
                if (fieldDetails == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Validation failed: Field is null");
                    TempData["Error"] = "Thông tin sân không hợp lệ.";
                    return RedirectToAction("Details", new { id = model.FieldId });
                }

                // Validate and apply voucher if provided
                decimal discountAmount = 0;
                if (!string.IsNullOrEmpty(model.VoucherCode))
                {
                    // Convert duration from minutes to hours for price calculation
                    var durationInHours = model.Duration / 60.0m;
                    var subtotal = fieldDetails.FieldType.BasePrice * durationInHours;
                    var currentUserId = int.Parse(userId);
                    
                    var voucherResult = await _voucherService.XacThucVoucherAsync(model.VoucherCode, subtotal, currentUserId);
                    
                    if (voucherResult.IsValid)
                    {
                        discountAmount = voucherResult.DiscountAmount ?? 0;
                        model.VoucherDiscount = discountAmount;
                    }
                    else
                    {
                        TempData["Error"] = $"Mã giảm giá không hợp lệ: {voucherResult.Message}";
                        return RedirectToAction("Details", new { id = model.FieldId });
                    }
                }

                // Store booking data in Session for confirmation page
                var bookingData = new
                {
                    FieldId = model.FieldId,
                    UserId = int.Parse(userId),
                    BookingDate = model.BookingDate,
                    Duration = model.Duration,
                    TotalPrice = model.TotalPrice,
                    VoucherCode = model.VoucherCode,
                    VoucherDiscount = model.VoucherDiscount,
                    Notes = model.Notes,
                    FieldName = fieldDetails.FieldName,
                    FieldType = fieldDetails.FieldType?.TypeName,
                    FieldLocation = fieldDetails.Location,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    BasePrice = fieldDetails.FieldType?.BasePrice,
                    Subtotal = fieldDetails.FieldType?.BasePrice * (model.Duration / 60.0m)
                };
                
                var bookingDataJson = System.Text.Json.JsonSerializer.Serialize(bookingData);
                HttpContext.Session.SetString("BookingData", bookingDataJson);
                
                System.Diagnostics.Debug.WriteLine("====================================");
                System.Diagnostics.Debug.WriteLine("✅ BookingData saved to Session");
                System.Diagnostics.Debug.WriteLine($"Session available: {HttpContext.Session.IsAvailable}");
                System.Diagnostics.Debug.WriteLine($"BookingData JSON length: {bookingDataJson.Length}");
                System.Diagnostics.Debug.WriteLine($"BookingData preview: {bookingDataJson.Substring(0, Math.Min(200, bookingDataJson.Length))}...");
                
                // Verify Session was saved
                var verifyData = HttpContext.Session.GetString("BookingData");
                System.Diagnostics.Debug.WriteLine($"✅ Verification - BookingData retrieved: {verifyData != null}");
                System.Diagnostics.Debug.WriteLine("====================================");

                System.Diagnostics.Debug.WriteLine("Redirecting to ConfirmBooking");
                return RedirectToAction("ConfirmBooking");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in Book action: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Details", new { id = model.FieldId });
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult ConfirmBooking()
        {
            System.Diagnostics.Debug.WriteLine("====================================");
            System.Diagnostics.Debug.WriteLine("✅ ConfirmBooking GET action called");
            System.Diagnostics.Debug.WriteLine($"User authenticated: {User.Identity?.IsAuthenticated}");
            System.Diagnostics.Debug.WriteLine($"Session available: {HttpContext.Session.IsAvailable}");
            
            var bookingDataJson = HttpContext.Session.GetString("BookingData");
            System.Diagnostics.Debug.WriteLine($"BookingData from Session: {(bookingDataJson != null ? bookingDataJson.Substring(0, Math.Min(200, bookingDataJson.Length)) + "..." : "NULL")}");
            
            if (string.IsNullOrEmpty(bookingDataJson))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ BookingData is null or empty in ConfirmBooking!");
                System.Diagnostics.Debug.WriteLine($"Session Keys: {string.Join(", ", HttpContext.Session.Keys)}");
                TempData["Error"] = "Không tìm thấy thông tin đặt sân.";
                return RedirectToAction("Index");
            }
            
            System.Diagnostics.Debug.WriteLine("✅ BookingData found in Session, proceeding to ConfirmBooking view");

            try
            {
                var bookingData = System.Text.Json.JsonSerializer.Deserialize<BookingConfirmationData>(bookingDataJson);
                return View(bookingData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing booking data: {ex.Message}");
                TempData["Error"] = "Dữ liệu đặt sân không hợp lệ.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ProcessPayment(string paymentMethod)
        {
            System.Diagnostics.Debug.WriteLine("====================================");
            System.Diagnostics.Debug.WriteLine($"ProcessPayment called with method: {paymentMethod}");
            System.Diagnostics.Debug.WriteLine($"User authenticated: {User.Identity?.IsAuthenticated}");
            System.Diagnostics.Debug.WriteLine($"Session available: {HttpContext.Session.IsAvailable}");
            
            var bookingDataJson = HttpContext.Session.GetString("BookingData");
            System.Diagnostics.Debug.WriteLine($"BookingData from Session: {(bookingDataJson != null ? bookingDataJson.Substring(0, Math.Min(100, bookingDataJson.Length)) + "..." : "NULL")}");
            
            if (string.IsNullOrEmpty(bookingDataJson))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ BookingData is null or empty!");
                System.Diagnostics.Debug.WriteLine($"Session Keys: {string.Join(", ", HttpContext.Session.Keys)}");
                TempData["Error"] = "Không tìm thấy thông tin đặt sân. Vui lòng đặt lại từ đầu.";
                System.Diagnostics.Debug.WriteLine("Redirecting to Index due to missing BookingData");
                return RedirectToAction("Index");
            }

            try
            {
                var bookingData = System.Text.Json.JsonSerializer.Deserialize<BookingConfirmationData>(bookingDataJson);
                
                if (paymentMethod == "bank_transfer")
                {
                    System.Diagnostics.Debug.WriteLine("Processing bank_transfer payment method");
                    
                    // Check if there's already an active payment order in database
                    var existingOrderId = HttpContext.Session.GetString("PaymentOrderId");
                    
                    if (!string.IsNullOrEmpty(existingOrderId))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found existing payment order in session: {existingOrderId}");
                        
                        // Check if the existing order is still valid in database
                        var existingPaymentOrder = await _paymentOrderService.GetPaymentOrderByOrderIdAsync(existingOrderId);
                        
                        if (existingPaymentOrder != null)
                        {
                            // Check if order is expired
                            if (existingPaymentOrder.ExpiredAt.HasValue && existingPaymentOrder.ExpiredAt < DateTime.Now)
                            {
                                System.Diagnostics.Debug.WriteLine("Payment order is expired, creating new one");
                                await _paymentOrderService.MarkPaymentOrderAsExpiredAsync(existingOrderId);
                                HttpContext.Session.Remove("PaymentOrderId");
                            }
                            // If order is still pending, redirect to existing payment page
                            else if (existingPaymentOrder.Status == PaymentOrderStatus.PENDING)
                            {
                                System.Diagnostics.Debug.WriteLine("Payment order is still pending, redirecting to existing payment");
                                return RedirectToAction("Payment", new { orderId = existingPaymentOrder.OrderCode });
                            }
                            // If order is already paid, redirect to success
                            else if (existingPaymentOrder.Status == PaymentOrderStatus.PAID)
                            {
                                System.Diagnostics.Debug.WriteLine("Payment order is already paid, redirecting to success");
                                return RedirectToAction("BookingSuccess", new { id = existingPaymentOrder.BookingId });
                            }
                        }
                    }
                    
                    try
                    {
                        // ❌ TEMPORARILY DISABLED - CartItem table chỉ support Products, không support Field Bookings
                        // TODO: Tạo bảng BookingCartItems riêng nếu muốn giỏ hàng cho đặt sân
                        // System.Diagnostics.Debug.WriteLine($"Adding to cart - UserId: {bookingData.UserId}, FieldId: {bookingData.FieldId}");
                        // var cartItem = await _cartService.ThemVaoGioHangAsync(
                        //     bookingData.UserId,
                        //     bookingData.FieldId,
                        //     bookingData.BookingDate,
                        //     bookingData.StartTime,
                        //     bookingData.EndTime,
                        //     bookingData.Duration,
                        //     bookingData.TotalPrice,
                        //     bookingData.Notes
                        // );
                        // System.Diagnostics.Debug.WriteLine($"Cart item created with ID: {cartItem.CartItemId}");
                        
                        // Create booking directly (không qua cart)
                        var booking = new Booking
                        {
                            UserId = bookingData.UserId,
                            FieldId = bookingData.FieldId,
                            BookingDate = bookingData.BookingDate,
                            Duration = bookingData.Duration,
                            Status = "Pending", // Will be updated after payment
                            TotalPrice = bookingData.TotalPrice,
                            PaymentStatus = "Pending",
                            PaymentMethod = "VietQR",
                            Notes = bookingData.Notes
                        };
                        
                        var createdBooking = await _bookingService.TaoDatSanAsync(booking);
                        System.Diagnostics.Debug.WriteLine($"Booking created with ID: {createdBooking.BookingId}");
                        
                        // Create new payment order in database
                        var orderId = GenerateOrderId();
                        var expiresAt = DateTime.Now.AddMinutes(10); // 10 minutes timeout
                        
                        System.Diagnostics.Debug.WriteLine($"Creating payment order: {orderId}");
                        var paymentOrder = new PaymentOrder
                        {
                            OrderCode = orderId,
                            BookingId = createdBooking.BookingId, // Use actual BookingId
                            UserId = bookingData.UserId,
                            Amount = bookingData.TotalPrice,
                            Status = PaymentOrderStatus.PENDING,
                            PaymentMethod = PaymentOrderMethod.VIETQR,
                            ExpiredAt = expiresAt
                        };
                        
                        await _paymentOrderService.CreatePaymentOrderAsync(paymentOrder);
                        System.Diagnostics.Debug.WriteLine($"Payment order created successfully: {orderId}");
                        
                        // Store orderId in session for redirect
                        HttpContext.Session.SetString("PaymentOrderId", orderId);
                        
                        System.Diagnostics.Debug.WriteLine($"Redirecting to Payment with orderId: {orderId}");
                        return RedirectToAction("Payment", new { orderId = orderId });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error in ProcessPayment: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        TempData["Error"] = $"Có lỗi xảy ra khi tạo thanh toán: {ex.Message}";
                        return RedirectToAction("Index");
                    }
                }
                else if (paymentMethod == "cash")
                {
                    // Create booking directly and send to admin for approval
                    var request = new BookingCreateRequest
                    {
                        FieldId = bookingData.FieldId,
                        BookingDate = bookingData.BookingDate,
                        StartTime = bookingData.StartTime,
                        EndTime = bookingData.EndTime,
                        Duration = bookingData.Duration,
                        UserId = bookingData.UserId,
                        GuestName = bookingData.GuestName,
                        GuestPhone = bookingData.GuestPhone,
                        GuestEmail = bookingData.GuestEmail,
                        Notes = bookingData.Notes
                    };

                    var result = await _bookingService.TaoDatSanDynamicAsync(request);
                    
                    if (result.IsSuccess)
                    {
                        TempData["Success"] = "Đặt sân thành công! Chúng tôi sẽ liên hệ với bạn để xác nhận.";
                        return RedirectToAction("BookingSuccess", new { id = result.BookingId });
                    }
                    else
                    {
                        TempData["Error"] = result.Message;
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
                    return RedirectToAction("ConfirmBooking");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Payment(string orderId)
        {
            System.Diagnostics.Debug.WriteLine($"Payment action called with orderId: {orderId}");
            
            if (string.IsNullOrEmpty(orderId))
            {
                System.Diagnostics.Debug.WriteLine("OrderId is null, redirecting to Index");
                TempData["Error"] = "Không tìm thấy thông tin thanh toán.";
                return RedirectToAction("Index");
            }

            try
            {
                // Get payment order from database
                var paymentOrder = await _paymentOrderService.GetPaymentOrderByOrderIdAsync(orderId);
                
                System.Diagnostics.Debug.WriteLine($"Payment action - OrderId: {orderId}");
                System.Diagnostics.Debug.WriteLine($"Payment action - PaymentOrder exists: {paymentOrder != null}");
                
                if (paymentOrder == null)
                {
                    System.Diagnostics.Debug.WriteLine("Payment order not found in database, redirecting to Index");
                    TempData["Error"] = "Không tìm thấy thông tin thanh toán.";
                    return RedirectToAction("Index");
                }
                
                System.Diagnostics.Debug.WriteLine($"Payment action - Order Status: {paymentOrder.Status}");
                System.Diagnostics.Debug.WriteLine($"Payment action - ExpiredAt: {paymentOrder.ExpiredAt}");
                System.Diagnostics.Debug.WriteLine($"Payment action - Current Time: {DateTime.Now}");
                System.Diagnostics.Debug.WriteLine($"Payment action - Time Remaining: {(paymentOrder.ExpiredAt - DateTime.Now)?.TotalSeconds} seconds");
                
                
                // Check if order is expired
                if (paymentOrder.ExpiredAt.HasValue && paymentOrder.ExpiredAt < DateTime.Now)
                {
                    System.Diagnostics.Debug.WriteLine("Payment order is expired, redirecting to Index");
                    TempData["Error"] = "QR code đã hết hạn. Vui lòng đặt lại sân.";
                    await _paymentOrderService.MarkPaymentOrderAsExpiredAsync(orderId);
                    return RedirectToAction("Index");
                }
                
                // Check if order is already paid
                if (paymentOrder.Status == PaymentOrderStatus.PAID)
                {
                    System.Diagnostics.Debug.WriteLine("Payment order is already paid, redirecting to success");
                    return RedirectToAction("BookingSuccess", new { id = paymentOrder.BookingId });
                }
                
                // Get booking data from session
                var bookingDataJson = HttpContext.Session.GetString("BookingData");
                if (string.IsNullOrEmpty(bookingDataJson))
                {
                    System.Diagnostics.Debug.WriteLine("BookingData is null in Payment action, redirecting to Index");
                    TempData["Error"] = "Không tìm thấy thông tin đặt sân.";
                    return RedirectToAction("Index");
                }
                
                var bookingData = System.Text.Json.JsonSerializer.Deserialize<BookingConfirmationData>(bookingDataJson);
                
                // Generate QR Code URL if not already generated
                string qrCodeUrl = "";
                
                if (string.IsNullOrEmpty(qrCodeUrl))
                {
                    var amount = paymentOrder.Amount;
                    var orderInfo = $"Dat san {bookingData.FieldName} - {bookingData.BookingDate:dd/MM/yyyy}";
                    
                    System.Diagnostics.Debug.WriteLine($"Generating QR code for amount: {amount}, orderInfo: {orderInfo}");
                    
                    // Sử dụng SEPay để tạo QR code
                    if (await _sepayService.IsSEPayEnabledAsync())
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("SEPay is enabled, trying to create QR code");
                            qrCodeUrl = await _sepayService.CreateQRCodeUrlAsync(amount, orderInfo, orderId);
                            System.Diagnostics.Debug.WriteLine($"SEPay QR code created: {qrCodeUrl}");
                            
                            // Update database with QR code
                            // paymentOrder.QRCodeUrl - disabled = qrCodeUrl;
                            try
                            {
                                await _paymentOrderService.UpdatePaymentOrderQRCodeAsync(orderId, paymentOrder.Status, null, null);
                                System.Diagnostics.Debug.WriteLine("SEPay QR code updated in database successfully");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to update SEPay QR code in database: {ex.Message}");
                                // Continue without database update
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error with SEPay QR code: {ex.Message}");
                        }
                    }
                    
                    if (string.IsNullOrEmpty(qrCodeUrl))
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("Creating fallback QR code");
                            // Fallback to SEPay QR with config values
                            var sepayConfigs = await _systemConfigService.LayCauHinhTheoLoaiAsync("SEPay");
                            var sepayConfig = sepayConfigs.Where(c => c.IsActive).ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
                            var bankCode = sepayConfig.GetValueOrDefault(PaymentConfigKeys.SEPAY_BANK_CODE, "BIDV");
                            var accountNumber = sepayConfig.GetValueOrDefault(PaymentConfigKeys.SEPAY_ACCOUNT_NUMBER, "0353425450");
                            
                            System.Diagnostics.Debug.WriteLine($"Fallback config - BankCode: {bankCode}, AccountNumber: {accountNumber}");
                            
                            qrCodeUrl = $"https://qr.sepay.vn/img?bank={bankCode}&acc={accountNumber}&template=compact&amount={(int)amount}&des={Uri.EscapeDataString(orderInfo)}";
                            System.Diagnostics.Debug.WriteLine($"Fallback SEPay QR code created: {qrCodeUrl}");
                            
                            // Update database with QR code
                            // paymentOrder.QRCodeUrl - disabled = qrCodeUrl;
                            try
                            {
                                await _paymentOrderService.UpdatePaymentOrderQRCodeAsync(orderId, paymentOrder.Status, null, null);
                                System.Diagnostics.Debug.WriteLine("Fallback QR code updated in database successfully");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to update fallback QR code in database: {ex.Message}");
                                // Continue without database update
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error with fallback QR code: {ex.Message}");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Final QR Code URL: {qrCodeUrl}");
                ViewBag.QRCodeUrl = qrCodeUrl;
                ViewBag.OrderId = orderId;
                ViewBag.PaymentMethod = "bank_transfer";
                // Try different formats for JavaScript compatibility
                var expiredAtString = paymentOrder.ExpiredAt?.ToString("yyyy-MM-ddTHH:mm:ss");
                var expiredAtISO = paymentOrder.ExpiredAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                var expiredAtUniversal = paymentOrder.ExpiredAt?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                
                ViewBag.ExpiredAt = expiredAtString;
                ViewBag.ExpiredAtISO = expiredAtISO;
                ViewBag.ExpiredAtUTC = expiredAtUniversal;
                
                System.Diagnostics.Debug.WriteLine($"ViewBag.ExpiredAt (local): {expiredAtString}");
                System.Diagnostics.Debug.WriteLine($"ViewBag.ExpiredAtISO: {expiredAtISO}");
                System.Diagnostics.Debug.WriteLine($"ViewBag.ExpiredAtUTC: {expiredAtUniversal}");
                
                // Lấy thông tin ngân hàng từ config để hiển thị
                try
                {
                    var sepayConfigs = await _systemConfigService.LayCauHinhTheoLoaiAsync("SEPay");
                    var sepayConfig = sepayConfigs.Where(c => c.IsActive).ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
                    var bankCode = sepayConfig.GetValueOrDefault(PaymentConfigKeys.SEPAY_BANK_CODE, "BIDV");
                    var accountNumber = sepayConfig.GetValueOrDefault(PaymentConfigKeys.SEPAY_ACCOUNT_NUMBER, "0353425450");
                    var accountName = sepayConfig.GetValueOrDefault(PaymentConfigKeys.SEPAY_ACCOUNT_NAME, "NGUYEN VAN HUNG");
                    
                    // Lấy tên ngân hàng từ danh sách banks
                    var banks = await _sepayService.GetSupportedBanksAsync();
                    var bankInfo = banks.FirstOrDefault(b => b.Code == bankCode);
                    var bankName = bankInfo?.Name ?? bankCode;
                    
                    ViewBag.BankName = bankName;
                    ViewBag.AccountNumber = accountNumber;
                    ViewBag.AccountName = accountName;
                    
                    System.Diagnostics.Debug.WriteLine($"Bank info for display - BankName: {bankName}, AccountNumber: {accountNumber}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting bank info for display: {ex.Message}");
                    // Fallback values
                    ViewBag.BankName = "Mbbank";
                    ViewBag.AccountNumber = "0353425450";
                    ViewBag.AccountName = "NGUYEN VAN HUNG";
                }
                
                // Check if test mode is enabled
                var configuration = HttpContext.RequestServices.GetService<IConfiguration>();
                var testMode = configuration?.GetValue<bool>("PaymentSettings:TestMode") ?? false;
                ViewBag.TestMode = testMode;
                
                return View(bookingData);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Dữ liệu đặt sân không hợp lệ: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CompletePayment(string orderId)
        {
            System.Diagnostics.Debug.WriteLine($"CompletePayment action called with orderId: {orderId}");
            System.Diagnostics.Debug.WriteLine($"User authenticated: {User.Identity?.IsAuthenticated}");
            System.Diagnostics.Debug.WriteLine($"User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            
            if (string.IsNullOrEmpty(orderId))
            {
                System.Diagnostics.Debug.WriteLine("OrderId is null in CompletePayment, redirecting to Index");
                TempData["Error"] = "Không tìm thấy thông tin thanh toán.";
                return RedirectToAction("Index");
            }

            try
            {
                // Get payment order from database
                var paymentOrder = await _paymentOrderService.GetPaymentOrderByOrderIdAsync(orderId);
                if (paymentOrder == null)
                {
                    System.Diagnostics.Debug.WriteLine("Payment order not found in database, redirecting to Index");
                    TempData["Error"] = "Không tìm thấy thông tin thanh toán.";
                    return RedirectToAction("Index");
                }
                
                // Check if order is already paid
                if (paymentOrder.Status == PaymentOrderStatus.PAID)
                {
                    System.Diagnostics.Debug.WriteLine("Payment order is already paid, redirecting to success");
                    return RedirectToAction("BookingSuccess", new { id = paymentOrder.BookingId });
                }
                
                // Check if order is expired
                if (paymentOrder.ExpiredAt.HasValue && paymentOrder.ExpiredAt < DateTime.Now)
                {
                    System.Diagnostics.Debug.WriteLine("Payment order is expired, redirecting to Index");
                    TempData["Error"] = "QR code đã hết hạn. Vui lòng đặt lại sân.";
                    await _paymentOrderService.MarkPaymentOrderAsExpiredAsync(orderId);
                    return RedirectToAction("Index");
                }
                
                // Get booking data from session
                var bookingDataJson = HttpContext.Session.GetString("BookingData");
                if (string.IsNullOrEmpty(bookingDataJson))
                {
                    System.Diagnostics.Debug.WriteLine("BookingData is null in CompletePayment, redirecting to Index");
                    TempData["Error"] = "Không tìm thấy thông tin đặt sân.";
                    return RedirectToAction("Index");
                }
                
                var bookingData = System.Text.Json.JsonSerializer.Deserialize<BookingConfirmationData>(bookingDataJson);
                
                // Check payment status with payment gateway
                bool paymentVerified = false;
                string paymentReference = "";
                string paymentStatusMessage = "";
                
                // Try to verify payment with VietQR or SEPay
                var amount = paymentOrder.Amount;
                var paymentMethod = paymentOrder.PaymentMethod;
                
                System.Diagnostics.Debug.WriteLine($"Checking payment status for order: {orderId}, method: {paymentMethod}");
                
                if (paymentMethod == PaymentOrderMethod.SEPAY)
                {
                    // Check SEPay payment status
                    try
                    {
                        if (await _sepayService.IsSEPayEnabledAsync())
                        {
                            System.Diagnostics.Debug.WriteLine("Checking SEPay payment status...");
                            var sepayStatus = await _sepayService.CheckTransactionStatusAsync(orderId);
                            
                            if (sepayStatus.Success && sepayStatus.Status == "success")
                            {
                                paymentVerified = true;
                                paymentReference = sepayStatus.TransactionId ?? $"SEPAY_{orderId}_{DateTime.Now:yyyyMMddHHmmss}";
                                paymentStatusMessage = "Thanh toán SEPay thành công";
                                System.Diagnostics.Debug.WriteLine($"SEPay payment verified: {paymentReference}");
                            }
                            else
                            {
                                paymentStatusMessage = "Thanh toán SEPay chưa được xác nhận";
                                System.Diagnostics.Debug.WriteLine($"SEPay payment not verified: {sepayStatus.Message}");
                            }
                        }
                        else
                        {
                            paymentStatusMessage = "Dịch vụ SEPay không khả dụng";
                            System.Diagnostics.Debug.WriteLine("SEPay service not enabled");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error checking SEPay status: {ex.Message}");
                        paymentStatusMessage = "Lỗi kiểm tra trạng thái SEPay";
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Payment verification result: {paymentVerified}, message: {paymentStatusMessage}");
                
                if (!paymentVerified)
                {
                    TempData["Error"] = $"Giao dịch chưa hoàn thành. {paymentStatusMessage}. Vui lòng kiểm tra lại và thử lại.";
                    return RedirectToAction("Payment", new { orderId = orderId });
                }
                
                // Create booking with verified payment
                var request = new BookingCreateRequest
                {
                    FieldId = bookingData.FieldId,
                    BookingDate = bookingData.BookingDate,
                    StartTime = bookingData.StartTime,
                    EndTime = bookingData.EndTime,
                    Duration = bookingData.Duration,
                    UserId = bookingData.UserId,
                    GuestName = bookingData.GuestName,
                    GuestPhone = bookingData.GuestPhone,
                    GuestEmail = bookingData.GuestEmail,
                    Notes = bookingData.Notes
                };

                System.Diagnostics.Debug.WriteLine($"Creating booking with data: FieldId={request.FieldId}, UserId={request.UserId}, BookingDate={request.BookingDate}");
                
                var result = await _bookingService.TaoDatSanDynamicAsync(request);
                
                System.Diagnostics.Debug.WriteLine($"CreateBookingAsync result: {result.IsSuccess}, BookingId: {result?.BookingId}");
                
                if (result.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"Booking created successfully with ID: {result.BookingId}");
                    
                    // Gửi thông báo realtime cho admin
                    try
                    {
                        var booking = await _context.Bookings
                            .Include(b => b.User)
                            .Include(b => b.Field)
                            .FirstOrDefaultAsync(b => b.BookingId == result.BookingId);
                            
                        if (booking != null)
                        {
                            var customerName = booking.User != null 
                                ? $"{booking.User.FirstName} {booking.User.LastName}" 
                                : booking.GuestName ?? "Khách vãng lai";
                                
                            await _notificationHubService.guiTBDatSanMoiAsync(
                                booking.BookingId,
                                customerName,
                                booking.Field.FieldName,
                                booking.BookingDate,
                                booking.StartTime
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the booking
                        System.Diagnostics.Debug.WriteLine($"Error sending notification: {ex.Message}");
                    }
                    
                    // ❌ DISABLED - Không dùng cart cho booking nữa
                    // TODO: Xóa cart item nếu có implement BookingCartItems
                    // var cartItemId = paymentOrder.CartItemId;
                    // if (cartItemId.HasValue)
                    // {
                    //     await _cartService.RemoveFromCartAsync(cartItemId.Value);
                    // }
                    
                    // Update payment order status in database to PAID
                    System.Diagnostics.Debug.WriteLine($"Marking payment order as paid: {orderId}, BookingId: {result.BookingId}");
                    await _paymentOrderService.MarkPaymentOrderAsPaidAsync(orderId, result.BookingId ?? 0, paymentReference);
                    
                    // Clear session data
                    System.Diagnostics.Debug.WriteLine("Clearing session data");
                    HttpContext.Session.Remove("BookingData");
                    HttpContext.Session.Remove("PaymentOrderId");
                    HttpContext.Session.Remove("PaymentOrderData");
                    HttpContext.Session.Remove("PaymentExpiredAt");
                    
                    TempData["Success"] = "Thanh toán thành công! Đặt sân đã được gửi đến admin để duyệt.";
                    System.Diagnostics.Debug.WriteLine($"Redirecting to BookingSuccess with id: {result.BookingId}");
                    
                    // Log the redirect URL
                    var redirectUrl = Url.Action("BookingSuccess", new { id = result.BookingId });
                    System.Diagnostics.Debug.WriteLine($"Redirect URL: {redirectUrl}");
                    
                    return RedirectToAction("BookingSuccess", new { id = result.BookingId });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CreateBookingAsync failed: {result.Message}");
                    TempData["Error"] = result.Message;
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in CompletePayment: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> BookingSuccess(int id)
        {
            System.Diagnostics.Debug.WriteLine($"BookingSuccess action called with id: {id}");
            
            if (id <= 0)
            {
                System.Diagnostics.Debug.WriteLine("Invalid booking ID, redirecting to Index");
                TempData["Error"] = "ID đặt sân không hợp lệ.";
                return RedirectToAction("Index");
            }
            
            var booking = await _bookingService.LayDatSanTheoIdAsync(id);
            System.Diagnostics.Debug.WriteLine($"Booking found: {booking != null}");
            
            if (booking == null)
            {
                System.Diagnostics.Debug.WriteLine("Booking not found, redirecting to Index");
                TempData["Error"] = "Không tìm thấy thông tin đặt sân.";
                return RedirectToAction("Index");
            }

            System.Diagnostics.Debug.WriteLine($"Booking details: ID={booking.BookingId}, Field={booking.Field?.FieldName}, Status={booking.Status}");
            return View(booking);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> BookingHistory()
        {
            try
            {
                // Get current user ID
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    TempData["Error"] = "Vui lòng đăng nhập để xem lịch sử đặt sân.";
                    return RedirectToAction("Login", "Account");
                }

                var userId = int.Parse(userIdClaim);
                
                // Get user's booking history
                var bookings = await _bookingService.LayDatSanTheoNguoiDungAsync(userId);
                
                // Sort by booking date descending (newest first)
                bookings = bookings.OrderByDescending(b => b.BookingDate).ThenByDescending(b => b.CreatedAt).ToList();
                
                return View(bookings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in BookingHistory: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi tải lịch sử đặt sân.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingRequest? request = null)
        {
            try
            {
                // Get current user ID
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để hủy đặt sân." });
                }

                var userId = int.Parse(userIdClaim);
                
                // Get booking details
                var booking = await _bookingService.LayDatSanTheoIdAsync(id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt sân này." });
                }

                // Check if user owns this booking
                if (booking.UserId != userId)
                {
                    return Json(new { success = false, message = "Bạn không có quyền hủy đặt sân này." });
                }

                // Check if booking can be cancelled
                if (booking.Status?.ToLower() == "cancelled")
                {
                    return Json(new { success = false, message = "Đặt sân này đã được hủy trước đó." });
                }

                if (booking.Status?.ToLower() == "completed")
                {
                    return Json(new { success = false, message = "Không thể hủy đặt sân đã hoàn thành." });
                }

                // Cancel the booking
                var success = await _bookingScheduleService.HuyDatSanAsync(id, userId, request?.Reason);
                if (success)
                {
                    return Json(new { success = true, message = "Hủy đặt sân thành công" });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = $"Không thể hủy đặt sân. Trạng thái hiện tại: {GetStatusText(booking.Status)}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking {BookingId}: {Message}", id, ex.Message);
                return Json(new { success = false, message = $"Lỗi khi hủy đặt sân: {ex.Message}" });
            }
        }

        private string GetStatusText(string status)
        {
            return status?.ToLower() switch
            {
                "pending" => "Đang chờ duyệt",
                "confirmed" => "Đã xác nhận", 
                "booked" => "Đã đặt",
                "cancelled" => "Đã hủy",
                "completed" => "Hoàn thành",
                _ => status ?? "Không xác định"
            };
        }

        [HttpPost]
        public async Task<IActionResult> ValidateVoucher(string voucherCode, decimal orderAmount)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng đăng nhập để sử dụng mã giảm giá"
                    });
                }
                
                var currentUserId = int.Parse(userIdClaim);
                var result = await _voucherService.XacThucVoucherAsync(voucherCode, orderAmount, currentUserId);
                
                return Json(new
                {
                    success = result.IsValid,
                    message = result.Message,
                    discountAmount = result.DiscountAmount,
                    discountType = result.DiscountType,
                    voucher = result.Voucher != null ? new
                    {
                        name = result.Voucher.Name,
                        description = result.Voucher.Description,
                        discountType = result.Voucher.DiscountType,
                        discountValue = result.Voucher.DiscountValue
                    } : null
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Lỗi khi xác thực voucher: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public IActionResult TestBook(BookingViewModel model)
        {
            System.Diagnostics.Debug.WriteLine("TestBook action called with:");
            System.Diagnostics.Debug.WriteLine($"FieldId: {model.FieldId}");
            System.Diagnostics.Debug.WriteLine($"BookingDate: {model.BookingDate}");
            System.Diagnostics.Debug.WriteLine($"Duration: {model.Duration}");
            System.Diagnostics.Debug.WriteLine($"TotalPrice: {model.TotalPrice}");
            System.Diagnostics.Debug.WriteLine($"User.Identity.Name: {User.Identity?.Name}");
            System.Diagnostics.Debug.WriteLine($"User.IsAuthenticated: {User.Identity?.IsAuthenticated}");
            System.Diagnostics.Debug.WriteLine($"User.Identity.AuthenticationType: {User.Identity?.AuthenticationType}");
            System.Diagnostics.Debug.WriteLine($"HttpContext.User.Identity.IsAuthenticated: {HttpContext.User.Identity?.IsAuthenticated}");
            
            var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userNameClaim = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst(ClaimTypes.Email)?.Value;
            
            var isAuthenticated = User.Identity?.IsAuthenticated == true && !string.IsNullOrEmpty(userIdClaim);
            
            System.Diagnostics.Debug.WriteLine($"UserId from claim: {userIdClaim}");
            System.Diagnostics.Debug.WriteLine($"UserName from claim: {userNameClaim}");
            System.Diagnostics.Debug.WriteLine($"Final isAuthenticated: {isAuthenticated}");
            
            return Json(new { 
                success = true, 
                message = "Form submitted successfully",
                data = new {
                    FieldId = model.FieldId,
                    BookingDate = model.BookingDate,
                    Duration = model.Duration,
                    TotalPrice = model.TotalPrice,
                    IsAuthenticated = isAuthenticated,
                    UserId = userIdClaim,
                    UserName = userNameClaim,
                    AuthenticationType = User.Identity?.AuthenticationType,
                    HttpContextIsAuthenticated = HttpContext.User.Identity?.IsAuthenticated,
                    AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
                }
            });
        }

        [HttpGet]
        public IActionResult CheckAuth()
        {
            return Json(new {
                IsAuthenticated = User.Identity?.IsAuthenticated,
                UserName = User.Identity?.Name,
                AuthenticationType = User.Identity?.AuthenticationType,
                Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                HttpContextIsAuthenticated = HttpContext.User.Identity?.IsAuthenticated
            });
        }

        private string GenerateOrderId()
        {
            return $"ORDER_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        }

        [HttpPost]
        public async Task<IActionResult> CheckPaymentStatus(string orderId)
        {
            try
            {
                // Check SEPay - sử dụng API như trong script
                if (await _sepayService.IsSEPayEnabledAsync())
                {
                    var sepayStatus = await _sepayService.CheckTransactionStatusAsync(orderId);
                    if (sepayStatus.Success && sepayStatus.Status == "success")
                    {
                        return Json(new { 
                            success = true, 
                            status = "completed",
                            message = "Thanh toán thành công",
                            transactionId = sepayStatus.TransactionId,
                            amount = sepayStatus.Amount,
                            transactionDate = sepayStatus.TransactionDate
                        });
                    }
                }

                return Json(new { 
                    success = false, 
                    status = "pending",
                    message = "Đang chờ thanh toán..."
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    status = "error",
                    message = $"Lỗi kiểm tra thanh toán: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugData()
        {
            var fields = await _fieldService.LayTatCaSanAsync();
            var fieldTypes = await _fieldService.LayTatCaLoaiSanAsync();
            
            return Json(new { 
                fieldsCount = fields.Count, 
                fieldTypesCount = fieldTypes.Count,
                fields = fields.Select(f => new { 
                    f.FieldId, 
                    f.FieldName, 
                    f.Status,
                    FieldType = f.FieldType?.TypeName,
                    FieldLocation = f.Location
                }),
                fieldTypes = fieldTypes.Select(ft => new { 
                    ft.FieldTypeId, 
                    ft.TypeName, 
                    ft.BasePrice 
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> TestQR()
        {
            // Test QR code với SEPay API sử dụng config
            try
            {
                var sepayConfigs = await _systemConfigService.LayCauHinhTheoLoaiAsync("SEPay");
                var sepayConfig = sepayConfigs.Where(c => c.IsActive).ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
                var bankCode = sepayConfig.GetValueOrDefault(PaymentConfigKeys.SEPAY_BANK_CODE, "BIDV");
                var accountNumber = sepayConfig.GetValueOrDefault(PaymentConfigKeys.SEPAY_ACCOUNT_NUMBER, "0353425450");
                
                var testUrl = $"https://qr.sepay.vn/img?bank={bankCode}&acc={accountNumber}&template=compact&amount=100000&des=Test%20QR%20Code";
                ViewBag.QRCodeUrl = testUrl;
                ViewBag.OrderId = "TEST_ORDER_123";
                return View("Payment", new { FieldName = "Test Field", BookingDate = DateTime.Now, StartTime = TimeSpan.FromHours(10), EndTime = TimeSpan.FromHours(12), Duration = 2, TotalPrice = 100000 });
            }
            catch (Exception ex)
            {
                // Fallback to default values if config fails
                var testUrl = "https://qr.sepay.vn/img?bank=BIDV&acc=0353425450&template=compact&amount=100000&des=Test%20QR%20Code";
                ViewBag.QRCodeUrl = testUrl;
                ViewBag.OrderId = "TEST_ORDER_123";
                return View("Payment", new { FieldName = "Test Field", BookingDate = DateTime.Now, StartTime = TimeSpan.FromHours(10), EndTime = TimeSpan.FromHours(12), Duration = 2, TotalPrice = 100000 });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> TestPayment(string orderId)
        {
            System.Diagnostics.Debug.WriteLine($"TestPayment action called with orderId: {orderId}");
            
            if (string.IsNullOrEmpty(orderId))
            {
                TempData["Error"] = "Không tìm thấy thông tin thanh toán.";
                return RedirectToAction("Index");
            }

            try
            {
                // Get payment order from database
                var paymentOrder = await _paymentOrderService.GetPaymentOrderByOrderIdAsync(orderId);
                if (paymentOrder == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin thanh toán.";
                    return RedirectToAction("Index");
                }
                
                // Check if order is already paid
                if (paymentOrder.Status == PaymentOrderStatus.PAID)
                {
                    return RedirectToAction("BookingSuccess", new { id = paymentOrder.BookingId });
                }
                
                // Check if order is expired
                if (paymentOrder.ExpiredAt.HasValue && paymentOrder.ExpiredAt < DateTime.Now)
                {
                    TempData["Error"] = "QR code đã hết hạn. Vui lòng đặt lại sân.";
                    await _paymentOrderService.MarkPaymentOrderAsExpiredAsync(orderId);
                    return RedirectToAction("Index");
                }
                
                // Get booking data from session
                var bookingDataJson = HttpContext.Session.GetString("BookingData");
                if (string.IsNullOrEmpty(bookingDataJson))
                {
                    TempData["Error"] = "Không tìm thấy thông tin đặt sân.";
                    return RedirectToAction("Index");
                }
                
                var bookingData = System.Text.Json.JsonSerializer.Deserialize<BookingConfirmationData>(bookingDataJson);
                
                // Create booking with test payment using stored procedure
                var request = new BookingCreateRequest
                {
                    FieldId = bookingData.FieldId,
                    BookingDate = bookingData.BookingDate,
                    StartTime = bookingData.StartTime,
                    EndTime = bookingData.EndTime,
                    Duration = bookingData.Duration,
                    UserId = bookingData.UserId,
                    GuestName = bookingData.GuestName,
                    GuestPhone = bookingData.GuestPhone,
                    GuestEmail = bookingData.GuestEmail,
                    Notes = (bookingData.Notes ?? "") + " [TEST MODE]"
                };

                System.Diagnostics.Debug.WriteLine($"Creating test booking with data: FieldId={request.FieldId}, UserId={request.UserId}, BookingDate={request.BookingDate}");
                
                var result = await _bookingService.TaoDatSanDynamicAsync(request);
                
                System.Diagnostics.Debug.WriteLine($"CreateBookingAsync result: {result.IsSuccess}, BookingId: {result?.BookingId}");
                
                if (result.IsSuccess)
                {
                    System.Diagnostics.Debug.WriteLine($"Test booking created successfully with ID: {result.BookingId}");
                    
                    // Gửi thông báo realtime cho admin
                    try
                    {
                        var booking = await _context.Bookings
                            .Include(b => b.User)
                            .Include(b => b.Field)
                            .FirstOrDefaultAsync(b => b.BookingId == result.BookingId);
                            
                        if (booking != null)
                        {
                            var customerName = booking.User != null 
                                ? $"{booking.User.FirstName} {booking.User.LastName}" 
                                : booking.GuestName ?? "Khách vãng lai";
                                
                            await _notificationHubService.guiTBDatSanMoiAsync(
                                booking.BookingId,
                                customerName,
                                booking.Field.FieldName,
                                booking.BookingDate,
                                booking.StartTime
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the booking
                        System.Diagnostics.Debug.WriteLine($"Error sending notification: {ex.Message}");
                    }
                    
                    // ❌ DISABLED - Không dùng cart cho booking nữa
                    // TODO: Xóa cart item nếu có implement BookingCartItems
                    // var cartItemId = paymentOrder.CartItemId;
                    // if (cartItemId.HasValue)
                    // {
                    //     await _cartService.RemoveFromCartAsync(cartItemId.Value);
                    // }
                    
                    // Update payment order status to PAID
                    var testReference = $"TEST_{orderId}_{DateTime.Now:yyyyMMddHHmmss}";
                    await _paymentOrderService.MarkPaymentOrderAsPaidAsync(orderId, result.BookingId ?? 0, testReference);
                    
                    // Clear session data
                    HttpContext.Session.Remove("BookingData");
                    HttpContext.Session.Remove("PaymentOrderId");
                    HttpContext.Session.Remove("PaymentOrderData");
                    HttpContext.Session.Remove("PaymentExpiredAt");
                    
                    // Return JSON response for AJAX call
                    return Json(new { 
                        success = true, 
                        bookingId = result.BookingId,
                        message = "Test thanh toán thành công! Đặt sân đã được tạo (Chế độ test)."
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"CreateBookingAsync failed: {result.Message}");
                    return Json(new { 
                        success = false, 
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in TestPayment: {ex.Message}");
                return Json(new { 
                    success = false, 
                    message = "Có lỗi xảy ra: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Check booking overlap using stored procedure
        /// </summary>
        private async Task<OverlapCheckResult> CheckBookingOverlapAsync(int fieldId, DateTime bookingDate, TimeSpan startTime, TimeSpan endTime, int? excludeBookingId = null)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@FieldId", fieldId),
                    new SqlParameter("@BookingDate", bookingDate.Date),
                    new SqlParameter("@StartTime", startTime),
                    new SqlParameter("@EndTime", endTime),
                    new SqlParameter("@ExcludeBookingId", excludeBookingId ?? (object)DBNull.Value)
                };

                // Use ToListAsync() first to materialize the query, then use FirstOrDefault on client side
                var results = await _context.Database.SqlQueryRaw<OverlapCheckResult>(
                    "EXEC sp_CheckBookingOverlap @FieldId, @BookingDate, @StartTime, @EndTime, @ExcludeBookingId",
                    parameters).ToListAsync();
                
                var result = results.FirstOrDefault();

                return result ?? new OverlapCheckResult 
                { 
                    IsAvailable = false, 
                    Message = "Lỗi kiểm tra overlap" 
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CheckBookingOverlapAsync: {ex.Message}");
                return new OverlapCheckResult 
                { 
                    IsAvailable = false, 
                    Message = "Lỗi kiểm tra overlap: " + ex.Message 
                };
            }
        }
    }

    /// <summary>
    /// Result class for overlap checking
    /// </summary>
    public class OverlapCheckResult
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
        public int OverlapCount { get; set; }
    }

    /// <summary>
    /// Result class for available time slots
    /// </summary>
    public class AvailableTimeSlot
    {
        public TimeSpan SuggestedStart { get; set; }
        public TimeSpan SuggestedEnd { get; set; }
    }

    /// <summary>
    /// Request class for cancelling bookings
    /// </summary>
    public class CancelBookingRequest
    {
        public string? Reason { get; set; }
    }
}