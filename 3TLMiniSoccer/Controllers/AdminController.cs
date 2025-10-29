using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using _3TLMiniSoccer.Services;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Authorization;

namespace _3TLMiniSoccer.Controllers
{
    public class CheckOutRequest
    {
        public int SessionId { get; set; }
    }

    public class AddProductRequest
    {
        public int SessionId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
    [AdminOnly]
    public class AdminController : Controller
    {
        private readonly PaymentConfigService _paymentConfigService;
        private readonly SystemConfigService _systemConfigService;
        private readonly SEPayService _sepayService;
        private readonly DashboardService _dashboardService;
        private readonly ProductService _productService;
        private readonly FieldAdminService _fieldAdminService;
        private readonly AccountAdminService _accountAdminService;
        private readonly CategoryAdminService _categoryAdminService;
        private readonly VoucherAdminService _voucherAdminService;
        private readonly ContactAdminService _contactAdminService;
        private readonly BookingScheduleService _bookingScheduleService;
        private readonly OrderAdminService _orderAdminService;
        private readonly BookingStatisticsService _bookingStatisticsService;
        private readonly SalesStatisticsService _salesStatisticsService;
        private readonly NotificationHubService _notificationHubService;
        private readonly SessionService _sessionService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(PaymentConfigService paymentConfigService, SystemConfigService systemConfigService, SEPayService sepayService, DashboardService dashboardService, ProductService productService, FieldAdminService fieldAdminService, AccountAdminService accountAdminService, CategoryAdminService categoryAdminService, VoucherAdminService voucherAdminService, ContactAdminService contactAdminService, BookingScheduleService bookingScheduleService, OrderAdminService orderAdminService, BookingStatisticsService bookingStatisticsService, SalesStatisticsService salesStatisticsService, NotificationHubService notificationHubService, SessionService sessionService, ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _paymentConfigService = paymentConfigService;
            _systemConfigService = systemConfigService;
            _sepayService = sepayService;
            _dashboardService = dashboardService;
            _productService = productService;
            _fieldAdminService = fieldAdminService;
            _accountAdminService = accountAdminService;
            _categoryAdminService = categoryAdminService;
            _voucherAdminService = voucherAdminService;
            _contactAdminService = contactAdminService;
            _bookingScheduleService = bookingScheduleService;
            _orderAdminService = orderAdminService;
            _bookingStatisticsService = bookingStatisticsService;
            _salesStatisticsService = salesStatisticsService;
            _notificationHubService = notificationHubService;
            _sessionService = sessionService;
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboardData = await _dashboardService.LayDuLieuTrangChuAsync();
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                // Return view with empty data on error
                return View(new DashboardViewModel());
            }
        }

        public async Task<IActionResult> Products(ProductFilterViewModel filter)
        {
            try
            {
                var productData = await _productService.LayDanhSachSanPhamAsync(filter);
                var categories = await _productService.LayTatCaDanhMucAsync();
                
                ViewBag.Categories = categories;
                return View(productData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                return View(new ProductListViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _productService.LaySanPhamTheoIdAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                return Json(new { 
                    success = true, 
                    data = new {
                        productId = product.ProductId,
                        productName = product.ProductName,
                        description = product.Description,
                        price = product.Price,
                        productTypeId = product.ProductTypeId,
                        productTypeName = product.ProductType?.TypeName,
                        imageUrl = product.ImageUrl,
                        isAvailable = product.IsAvailable
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product: {ProductId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thông tin sản phẩm" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct()
        {
            try
            {
                var model = new CreateProductViewModel
                {
                    ProductName = Request.Form["ProductName"].ToString(),
                    Description = Request.Form["Description"].ToString(),
                    Price = decimal.TryParse(Request.Form["Price"].ToString(), out var price) ? price : 0,
                    Category = Request.Form["Category"].ToString(),
                    IsAvailable = Request.Form["IsAvailable"].ToString() == "on",
                    ImageFile = Request.Form.Files["ImageFile"]
                };

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _productService.TaoSanPhamAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Tạo sản phẩm thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Tạo sản phẩm thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo sản phẩm!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct(int id)
        {
            try
            {
                var model = new UpdateProductViewModel
                {
                    ProductName = Request.Form["ProductName"].ToString(),
                    Description = Request.Form["Description"].ToString(),
                    Price = decimal.TryParse(Request.Form["Price"].ToString(), out var price) ? price : 0,
                    Category = Request.Form["Category"].ToString(),
                    IsAvailable = Request.Form["IsAvailable"].ToString() == "on",
                    ImageFile = Request.Form.Files["ImageFile"]
                };

                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var result = await _productService.CapNhatSanPhamAsync(id, model);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật sản phẩm thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Cập nhật sản phẩm thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật sản phẩm!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var result = await _productService.XoaSanPhamAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa sản phẩm thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Xóa sản phẩm thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleProductStatus(int id)
        {
            try
            {
                var result = await _productService.ChuyenDoiTrangThaiSanPhamAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Cập nhật trạng thái thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling product status: {ProductId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái!" });
            }
        }

        public async Task<IActionResult> Fields(FieldFilterViewModel filter)
        {
            try
            {
                var fieldData = await _fieldAdminService.LayDanhSachSanAsync(filter);
                var fieldTypes = await _fieldAdminService.LayTatCaLoaiSanAsync();
                
                ViewBag.FieldTypes = fieldTypes;
                return View(fieldData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fields");
                return View(new FieldListViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetField(int id)
        {
            try
            {
                var field = await _fieldAdminService.LaySanTheoIdAsync(id);
                if (field == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sân" });
                }

                return Json(new { 
                    success = true, 
                    data = new {
                        fieldId = field.FieldId,
                        fieldName = field.FieldName,
                        fieldTypeId = field.FieldTypeId,
                        location = field.Location,
                        status = field.Status,
                        description = field.Description,
                        imageUrl = field.ImageUrl,
                        openingTime = field.OpeningTime.ToString(@"hh\:mm"),
                        closingTime = field.ClosingTime.ToString(@"hh\:mm"),
                        fieldTypeName = field.FieldTypeName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field: {FieldId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thông tin sân" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateField(CreateFieldViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var result = await _fieldAdminService.TaoSanAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Tạo sân thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể tạo sân" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating field: {@Model}", model);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo sân" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateField(UpdateFieldViewModel model)
        {
            try
            {
                _logger.LogInformation("UpdateField called with model: {@Model}", model);
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ: " + string.Join(", ", errors) });
                }

                var result = await _fieldAdminService.CapNhatSanAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật sân thành công" });
                }
                else
                {
                    _logger.LogWarning("FieldAdminService.CapNhatSanAsync returned false for FieldId: {FieldId}", model.FieldId);
                    return Json(new { success = false, message = "Không thể cập nhật sân" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating field: {@Model}", model);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật sân: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteField(int id)
        {
            try
            {
                var result = await _fieldAdminService.XoaSanAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa sân thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa sân" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting field: {FieldId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sân" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFieldStatus(int id)
        {
            try
            {
                var result = await _fieldAdminService.ChuyenDoiTrangThaiSanAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Thay đổi trạng thái sân thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể thay đổi trạng thái sân" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling field status: {FieldId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thay đổi trạng thái sân" });
            }
        }

        public async Task<IActionResult> Accounts(AccountFilterViewModel filter, int page = 1)
        {
            try
            {
                var accountList = await _accountAdminService.LayDanhSachTaiKhoanAsync(filter, page, 10);
                return View(accountList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading accounts page");
                return View(new AccountListViewModel { Filter = filter });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAccount(int id)
        {
            try
            {
                var account = await _accountAdminService.LayTaiKhoanTheoIdAsync(id);
                if (account == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản!" });
                }

                var roles = await _accountAdminService.LayTuyChonVaiTroAsync();
                
                return Json(new { 
                    success = true, 
                    account = new {
                        userId = account.UserId,
                        username = account.Username,
                        email = account.Email,
                        firstName = account.FullName.Split(' ').FirstOrDefault() ?? "",
                        lastName = string.Join(" ", account.FullName.Split(' ').Skip(1)),
                        phoneNumber = account.PhoneNumber,
                        roleId = roles.FirstOrDefault(r => r.RoleName == account.RoleName)?.RoleId ?? 0,
                        address = account.Address,
                        isActive = account.IsActive,
                        emailConfirmed = account.EmailConfirmed
                    },
                    roles = roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thông tin tài khoản!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(CreateAccountViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var result = await _accountAdminService.TaoTaiKhoanAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Tạo tài khoản thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Tên đăng nhập hoặc email đã tồn tại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo tài khoản!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAccount(UpdateAccountViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }

                var result = await _accountAdminService.CapNhatTaiKhoanAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật tài khoản thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Tên đăng nhập hoặc email đã tồn tại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật tài khoản!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            try
            {
                var result = await _accountAdminService.XoaTaiKhoanAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa tài khoản thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa tài khoản này!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa tài khoản!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAccountStatus(int id)
        {
            try
            {
                var result = await _accountAdminService.ChuyenDoiTrangThaiTaiKhoanAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Thay đổi trạng thái tài khoản thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể thay đổi trạng thái tài khoản này!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling account status: {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thay đổi trạng thái!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _accountAdminService.LayTuyChonVaiTroAsync();
                return Json(new { success = true, roles = roles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách vai trò!" });
            }
        }


        public async Task<IActionResult> Categories()
        {
            try
            {
                var viewModel = await _categoryAdminService.LayDanhSachDanhMucAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories page");
                return View(new CategoryListViewModel());
            }
        }


        #region Product Categories API

        [HttpPost]
        public async Task<IActionResult> CreateProductCategory([FromBody] CreateProductCategoryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                var result = await _categoryAdminService.TaoDanhMucSanPhamAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Thêm thể loại sản phẩm thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Thể loại đã tồn tại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product category");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm thể loại!" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteProductCategory(string categoryName)
        {
            try
            {
                var result = await _categoryAdminService.XoaDanhMucSanPhamAsync(categoryName);
                if (result)
                {
                    return Json(new { success = true, message = "Đã xóa thể loại sản phẩm và cập nhật các sản phẩm liên quan!" });
                }
                else
                {
                    return Json(new { success = false, message = "Có lỗi xảy ra khi xóa thể loại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product category {CategoryName}", categoryName);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa thể loại!" });
            }
        }

        #endregion

        #region Field Categories API

        [HttpGet]
        public async Task<IActionResult> GetFieldCategory(int id)
        {
            try
            {
                var categories = await _categoryAdminService.LayDanhMucSanAsync();
                var category = categories.FirstOrDefault(c => c.FieldTypeId == id);
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy loại sân!" });
                }

                return Json(new { success = true, data = category });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field category {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thông tin loại sân!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFieldCategory([FromBody] CreateFieldCategoryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                var result = await _categoryAdminService.TaoDanhMucSanAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Thêm loại sân thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Loại sân đã tồn tại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating field category");
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm loại sân!" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateFieldCategory([FromBody] UpdateFieldCategoryViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                var result = await _categoryAdminService.CapNhatDanhMucSanAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật loại sân thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Loại sân không tồn tại hoặc tên đã được sử dụng!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating field category");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật loại sân!" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFieldCategory(int id)
        {
            try
            {
                var result = await _categoryAdminService.XoaDanhMucSanAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa loại sân thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa loại sân có sân bóng hoặc quy tắc giá!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting field category {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa loại sân!" });
            }
        }

        #endregion

        #region Contact Management

        public async Task<IActionResult> Contact(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? type = null, DateTime? createdDate = null)
        {
            try
            {
                var contacts = await _contactAdminService.LayTatCaLienHeAsync();
                var viewModel = new ContactAdminViewModel { Contacts = contacts };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading contact page");
                return View(new ContactAdminViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetContactDetails(int id)
        {
            try
            {
                var contact = await _contactAdminService.LayLienHeTheoIdAsync(id);
                if (contact == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tin nhắn!" });
                }

                return Json(new { success = true, contact = new {
                    contactId = contact.ContactId,
                    name = contact.Name,
                    email = contact.Email,
                    phone = contact.Phone,
                    subject = contact.Subject,
                    message = contact.Message,
                    status = contact.Status,
                    response = contact.Response,
                    respondedAt = contact.RespondedAt?.ToString("dd/MM/yyyy HH:mm"),
                    respondedBy = contact.RespondedByUser != null ? $"{contact.RespondedByUser.FirstName} {contact.RespondedByUser.LastName}" : null,
                    createdAt = contact.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                }});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact details {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thông tin tin nhắn!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateContactStatus(int id, string status)
        {
            try
            {
                var result = await _contactAdminService.CapNhatTrangThaiLienHeAsync(id, status);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể cập nhật trạng thái!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contact status {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReplyToContact(int id, string response)
        {
            try
            {
                // Get current user ID (you might need to adjust this based on your authentication)
                var currentUserId = 1; // TODO: Get from current user context
                
                var result = await _contactAdminService.TraLoiLienHeAsync(id, response, currentUserId);
                if (result)
                {
                    return Json(new { success = true, message = "Phản hồi thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể gửi phản hồi!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to contact {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi phản hồi!" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteContact(int id)
        {
            try
            {
                var result = await _contactAdminService.XoaLienHeAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa tin nhắn thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa tin nhắn!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contact {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa tin nhắn!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var result = await _contactAdminService.DanhDauDaDocAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Đã đánh dấu đã đọc!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể đánh dấu đã đọc!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking contact as read {Id}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi đánh dấu đã đọc!" });
            }
        }

        #endregion

        public IActionResult Vouchers()
        {
            return View();
        }

        public async Task<IActionResult> BookingSchedule()
        {
            try
            {
                var viewModel = await _bookingScheduleService.LayDanhSachDatSanAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking schedule");
                return View(new BookingScheduleViewModel());
            }
        }

        public async Task<IActionResult> Orders()
        {
            try
            {
                var viewModel = await _orderAdminService.LayDanhSachDonHangAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                return View(new OrderAdminViewModel());
            }
        }

        #region Order Management API

        [HttpGet]
        public async Task<IActionResult> GetOrders(int page = 1, int pageSize = 10, string? search = null, string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var viewModel = await _orderAdminService.LayTatCaDonHangAsync();
                return Json(new { success = true, data = viewModel });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải danh sách đơn hàng!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            try
            {
                var orderDetail = await _orderAdminService.LayChiTietDonHangAsync(orderId);
                if (orderDetail == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }
                return Json(new { success = true, data = orderDetail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order detail {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải chi tiết đơn hàng!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status, string? notes = null)
        {
            try
            {
                var canUpdate = await _orderAdminService.CoTheCapNhatTrangThaiDonHangAsync(orderId, status);
                if (!canUpdate)
                {
                    return Json(new { success = false, message = "Không thể cập nhật trạng thái đơn hàng này!" });
                }

                var result = await _orderAdminService.CapNhatTrangThaiDonHangAsync(orderId, status, notes);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật trạng thái đơn hàng thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể cập nhật trạng thái đơn hàng!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái đơn hàng!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId, string reason)
        {
            try
            {
                var canCancel = await _orderAdminService.CoTheHuyDonHangAsync(orderId);
                if (!canCancel)
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng này!" });
                }

                var result = await _orderAdminService.HuyDonHangAsync(orderId, reason);
                if (result)
                {
                    return Json(new { success = true, message = "Hủy đơn hàng thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đơn hàng!" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            try
            {
                var canDelete = await _orderAdminService.CoTheXoaDonHangAsync(orderId);
                if (!canDelete)
                {
                    return Json(new { success = false, message = "Không thể xóa đơn hàng này!" });
                }

                var result = await _orderAdminService.XoaDonHangAsync(orderId);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa đơn hàng thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa đơn hàng!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa đơn hàng!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatistics()
        {
            try
            {
                var statistics = await _orderAdminService.LayThongKeAsync();
                return Json(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order statistics");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thống kê đơn hàng!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatisticsByDate(DateTime date)
        {
            try
            {
                var statistics = await _orderAdminService.LayThongKeTheoNgayAsync(date);
                return Json(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order statistics by date {Date}", date);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thống kê đơn hàng theo ngày!" });
            }
        }

        #endregion

        #region Sales Statistics API

        [HttpGet]
        public async Task<IActionResult> GetSalesStatistics()
        {
            try
            {
                var fromDate = DateTime.Today.AddDays(-30);
                var toDate = DateTime.Today;
                var summary = await _salesStatisticsService.LayThongKeTongQuanBanHangAsync(fromDate, toDate);
                return Json(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales statistics");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải thống kê bán hàng!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSalesStatsData([FromBody] SalesStatsFilterRequest request)
        {
            try
            {
                var fromDate = request.FromDate ?? DateTime.Today.AddDays(-30);
                var toDate = request.ToDate ?? DateTime.Today;
                var reportType = request.ReportType ?? "daily";

                var summary = await _salesStatisticsService.LayThongKeTongQuanBanHangAsync(fromDate, toDate);
                var salesRevenueChartData = await _salesStatisticsService.LayDuLieuBieuDoDoanhThuBanHangAsync(fromDate, toDate, reportType);
                var productSalesChartData = await _salesStatisticsService.LayDuLieuBieuDoBanHangSanPhamAsync(fromDate, toDate, reportType);
                var topSellingProducts = await _salesStatisticsService.LayTopSanPhamBanChayAsync(fromDate, toDate, 5);
                var topCustomers = await _salesStatisticsService.LayTopKhachHangAsync(fromDate, toDate, 5);
                var categoryAnalysis = await _salesStatisticsService.LayPhanTichDanhMucAsync(fromDate, toDate);
                var comparison = await _salesStatisticsService.LaySoSanhVoiKyTruocAsync(fromDate, toDate, reportType);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        summary,
                        salesRevenueChartData,
                        productSalesChartData,
                        topSellingProducts,
                        topCustomers,
                        categoryAnalysis,
                        comparison
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales statistics data");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải dữ liệu thống kê bán hàng!" });
            }
        }

        #endregion


        public async Task<IActionResult> SportTypes()
        {
            try
            {
                _logger.LogInformation("Loading sport types...");
                var productTypes = await _productService.LayTatCaLoaiSanPhamAsync();
                _logger.LogInformation($"Found {productTypes?.Count ?? 0} product types");
                
                // Debug: Log first few items
                if (productTypes != null && productTypes.Any())
                {
                    _logger.LogInformation($"First product type: {productTypes.First().TypeName}");
                }
                else
                {
                    _logger.LogWarning("No product types found in database");
                }
                
                return View(productTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sport types");
                return View(new List<ProductType>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugProductTypes()
        {
            try
            {
                var productTypes = await _productService.LayTatCaLoaiSanPhamAsync();
                return Json(new { 
                    success = true, 
                    count = productTypes?.Count ?? 0,
                    data = productTypes?.Select(pt => new { 
                        id = pt.ProductTypeId, 
                        name = pt.TypeName, 
                        description = pt.Description,
                        isActive = pt.IsActive 
                    }) 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in debug endpoint");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSportType(int id)
        {
            try
            {
                var productType = await _productService.LayLoaiSanPhamTheoIdAsync(id);
                if (productType == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy loại thể thao" });
                }

                return Json(new { 
                    success = true, 
                    data = new {
                        productTypeId = productType.ProductTypeId,
                        typeName = productType.TypeName,
                        description = productType.Description,
                        isActive = productType.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sport type: {SportTypeId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thông tin loại thể thao" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSportType()
        {
            try
            {
                var productType = new ProductType
                {
                    TypeName = Request.Form["TypeName"].ToString(),
                    Description = Request.Form["Description"].ToString(),
                    IsActive = Request.Form["IsActive"].ToString() == "active"
                };

                if (string.IsNullOrEmpty(productType.TypeName))
                {
                    return Json(new { success = false, message = "Tên loại thể thao không được để trống!" });
                }

                var result = await _productService.TaoLoaiSanPhamAsync(productType);
                return Json(new { success = true, message = "Tạo loại thể thao thành công!", data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sport type");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo loại thể thao!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSportType(int id)
        {
            try
            {
                var productType = new ProductType
                {
                    ProductTypeId = id,
                    TypeName = Request.Form["TypeName"].ToString(),
                    Description = Request.Form["Description"].ToString(),
                    IsActive = Request.Form["IsActive"].ToString() == "active"
                };

                if (string.IsNullOrEmpty(productType.TypeName))
                {
                    return Json(new { success = false, message = "Tên loại thể thao không được để trống!" });
                }

                var result = await _productService.CapNhatLoaiSanPhamAsync(productType);
                return Json(new { success = true, message = "Cập nhật loại thể thao thành công!", data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sport type: {SportTypeId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật loại thể thao!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSportType(int id)
        {
            try
            {
                var result = await _productService.XoaLoaiSanPhamAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa loại thể thao thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa loại thể thao vì đang được sử dụng!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting sport type: {SportTypeId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa loại thể thao!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSportTypeStatus(int id)
        {
            try
            {
                var result = await _productService.ChuyenDoiTrangThaiLoaiSanPhamAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Cập nhật trạng thái thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling sport type status: {SportTypeId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái!" });
            }
        }


        public async Task<IActionResult> SalesStats()
        {
            try
            {
                var fromDate = DateTime.Today.AddDays(-30);
                var toDate = DateTime.Today;
                var reportType = "daily";

                _logger.LogInformation("Loading Sales Statistics - FromDate: {FromDate}, ToDate: {ToDate}", fromDate, toDate);

                _logger.LogInformation("Getting sales statistics summary...");
                var summary = await _salesStatisticsService.LayThongKeTongQuanBanHangAsync(fromDate, toDate);
                _logger.LogInformation("Summary loaded successfully");

                _logger.LogInformation("Getting sales revenue chart data...");
                var salesRevenueChartData = await _salesStatisticsService.LayDuLieuBieuDoDoanhThuBanHangAsync(fromDate, toDate, reportType);
                _logger.LogInformation("Sales revenue chart data loaded successfully");

                _logger.LogInformation("Getting product sales chart data...");
                var productSalesChartData = await _salesStatisticsService.LayDuLieuBieuDoBanHangSanPhamAsync(fromDate, toDate, reportType);
                _logger.LogInformation("Product sales chart data loaded successfully");

                _logger.LogInformation("Getting top selling products...");
                var topSellingProducts = await _salesStatisticsService.LayTopSanPhamBanChayAsync(fromDate, toDate, 5);
                _logger.LogInformation("Top selling products loaded successfully");

                _logger.LogInformation("Getting top customers...");
                var topCustomers = await _salesStatisticsService.LayTopKhachHangAsync(fromDate, toDate, 5);
                _logger.LogInformation("Top customers loaded successfully");

                _logger.LogInformation("Getting category analysis...");
                var categoryAnalysis = await _salesStatisticsService.LayPhanTichDanhMucAsync(fromDate, toDate);
                _logger.LogInformation("Category analysis loaded successfully");

                _logger.LogInformation("Getting comparison data...");
                var comparison = await _salesStatisticsService.LaySoSanhVoiKyTruocAsync(fromDate, toDate, reportType);
                _logger.LogInformation("Comparison data loaded successfully");

                var viewModel = new SalesStatsViewModel
                {
                    Summary = summary,
                    SalesRevenueChartData = salesRevenueChartData,
                    ProductSalesChartData = productSalesChartData,
                    TopSellingProducts = topSellingProducts,
                    TopCustomers = topCustomers,
                    CategoryAnalysis = categoryAnalysis,
                    Comparison = comparison,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ReportType = reportType
                };

                _logger.LogInformation("Sales statistics page loaded successfully");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales statistics page: {Message} | StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
                throw; // Re-throw to see full error in browser
            }
        }

        public IActionResult Booking()
        {
            return View();
        }

        // System Configuration Management (Combined Payment & System Settings)
        public async Task<IActionResult> SystemConfigs()
        {
            var viewModel = new SystemConfigViewModel
            {
                SEPayConfigs = await _systemConfigService.GetConfigsByTypeAsync("SEPay"),
                SystemConfigs = await _systemConfigService.GetConfigsByTypeAsync("System"),
                GeneralConfigs = await _systemConfigService.GetConfigsByTypeAsync("General"),
                AllConfigs = await _systemConfigService.GetAllConfigsAsync(),
                SupportedBanks = await _sepayService.GetSupportedBanksAsync()
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSystemConfig(int configId, string configValue)
        {
            var config = await _context.SystemConfigs.FindAsync(configId);
            if (config != null)
            {
                config.ConfigValue = configValue;
                config.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật cấu hình thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy cấu hình!";
            }
            return RedirectToAction("SystemConfigs");
        }

        [HttpPost]
        public async Task<IActionResult> CreateSystemConfig(string configKey, string configValue, string description, string configType, string dataType)
        {
            var success = await _systemConfigService.DatGiaTriCauHinhAsync(
                configKey, 
                configValue, 
                description, 
                configType, 
                dataType
            );
            
            if (success)
            {
                TempData["SuccessMessage"] = "Tạo cấu hình thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo cấu hình!";
            }
            return RedirectToAction("SystemConfigs");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSystemConfig(int configId)
        {
            var success = await _systemConfigService.DeleteConfigAsync(configId);
            if (success)
            {
                TempData["SuccessMessage"] = "Xóa cấu hình thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa cấu hình!";
            }
            return RedirectToAction("SystemConfigs");
        }

        [HttpGet]
        public async Task<IActionResult> GetConfig(int id)
        {
            try
            {
                var config = await _systemConfigService.GetConfigByIdAsync(id);
                if (config != null)
                {
                    return Json(new { 
                        configId = config.ConfigId,
                        configKey = config.ConfigKey,
                        configValue = config.ConfigValue,
                        description = config.Description,
                        isActive = config.IsActive
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy cấu hình!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting config");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy cấu hình!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateConfigs(IFormCollection form)
        {
            try
            {
                var configs = new List<SystemConfig>();
                
                foreach (var key in form.Keys)
                {
                    if (key != "__RequestVerificationToken")
                    {
                        var config = await _systemConfigService.GetConfigByKeyAsync(key);
                        if (config != null)
                        {
                            config.ConfigValue = form[key];
                            config.UpdatedAt = DateTime.Now;
                            configs.Add(config);
                        }
                    }
                }

                var result = await _systemConfigService.UpdateConfigsAsync(configs);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật cấu hình thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Cập nhật cấu hình thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configs");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật cấu hình!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateConfig(SystemConfig config)
        {
            try
            {
                var result = await _systemConfigService.UpdateConfigAsync(config);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật cấu hình thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Cập nhật cấu hình thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating config");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật cấu hình!" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteConfig(int id)
        {
            try
            {
                var result = await _systemConfigService.DeleteConfigAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa cấu hình thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Xóa cấu hình thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting config");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa cấu hình!" });
            }
        }

        // =====================================================
        // VOUCHER MANAGEMENT
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Vouchers(int page = 1, int pageSize = 10, string? search = null, string? discountType = null, string? status = null, DateTime? createdDate = null)
        {
            try
            {
                var viewModel = await _voucherAdminService.LayTatCaVoucherAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vouchers page");
                return View(new VoucherAdminViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateVoucher(DiscountCode voucher)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                var result = await _voucherAdminService.TaoVoucherAsync(voucher);
                if (result)
                {
                    return Json(new { success = true, message = "Tạo mã giảm giá thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Mã giảm giá đã tồn tại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo mã giảm giá!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateVoucher(DiscountCode voucher)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                var result = await _voucherAdminService.CapNhatVoucherAsync(voucher);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật mã giảm giá thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Mã giảm giá đã tồn tại hoặc không tìm thấy!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating voucher");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật mã giảm giá!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            try
            {
                var result = await _voucherAdminService.XoaVoucherAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa mã giảm giá thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy mã giảm giá!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting voucher");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa mã giảm giá!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVoucherStatus(int id)
        {
            try
            {
                var result = await _voucherAdminService.ChuyenDoiTrangThaiVoucherAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy mã giảm giá!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling voucher status");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật trạng thái!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVoucherDetails(int id)
        {
            try
            {
                var voucher = await _voucherAdminService.LayVoucherTheoIdAsync(id);
                if (voucher == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy mã giảm giá!" });
                }

                return Json(new { 
                    success = true, 
                    voucher = new {
                        id = voucher.DiscountCodeId,
                        code = voucher.Code,
                        name = voucher.Name,
                        description = voucher.Description,
                        discountType = voucher.DiscountType,
                        discountValue = voucher.DiscountValue,
                        minOrderAmount = voucher.MinOrderAmount,
                        maxDiscountAmount = voucher.MaxDiscountAmount,
                        usageLimit = voucher.UsageLimit,
                        usedCount = voucher.UsedCount,
                        validFrom = voucher.ValidFrom.ToString("yyyy-MM-dd"),
                        validTo = voucher.ValidTo.ToString("yyyy-MM-dd"),
                        isActive = voucher.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voucher details");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thông tin mã giảm giá!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckCodeUnique(string code, int? excludeId = null)
        {
            try
            {
                var isUnique = await _voucherAdminService.IsCodeUniqueAsync(code, excludeId);
                return Json(new { isUnique = isUnique });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking code uniqueness");
                return Json(new { isUnique = false });
            }
        }

        // =====================================================
        // BOOKING SCHEDULE ACTIONS
        // =====================================================


        [HttpGet]
        public async Task<IActionResult> GetBookingDetails(int id)
        {
            try
            {
                var booking = await _bookingScheduleService.LayDatSanTheoIdAsync(id);
                if (booking == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đặt sân" });
                }

                var result = new
                {
                    success = true,
                    booking = new
                    {
                        bookingId = booking.BookingId,
                        bookingCode = booking.BookingCode,
                        customerName = booking.User != null ? $"{booking.User.FirstName} {booking.User.LastName}" : booking.GuestName,
                        customerPhone = booking.User?.PhoneNumber ?? booking.GuestPhone,
                        customerEmail = booking.User?.Email ?? booking.GuestEmail,
                        fieldName = booking.Field.FieldName,
                        fieldType = booking.Field.FieldType.TypeName,
                        bookingDate = booking.BookingDate.ToString("dd/MM/yyyy"),
                        startTime = booking.StartTime.ToString(@"hh\:mm"),
                        endTime = booking.EndTime.ToString(@"hh\:mm"),
                        duration = booking.Duration,
                        totalPrice = booking.TotalPrice,
                        status = booking.Status,
                        paymentStatus = booking.PaymentStatus,
                        paymentMethod = booking.PaymentMethod,
                        notes = booking.Notes,
                        createdAt = booking.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        confirmedAt = booking.ConfirmedAt?.ToString("dd/MM/yyyy HH:mm"),
                        confirmedBy = booking.ConfirmedByUser != null ? $"{booking.ConfirmedByUser.FirstName} {booking.ConfirmedByUser.LastName}" : null,
                        cancelledAt = booking.CancelledAt?.ToString("dd/MM/yyyy HH:mm"),
                        cancelledBy = booking.CancelledByUser != null ? $"{booking.CancelledByUser.FirstName} {booking.CancelledByUser.LastName}" : null
                    }
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking details");
                return Json(new { success = false, message = "Lỗi khi tải thông tin đặt sân" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            try
            {
                _logger.LogInformation("Creating booking with status: {Status}", request.Status);
                
                if (request.FieldId <= 0 || request.Duration <= 0)
                {
                    return Json(new { success = false, message = "Thông tin đặt sân không hợp lệ" });
                }

                // Đảm bảo trạng thái được set đúng
                if (string.IsNullOrEmpty(request.Status))
                {
                    request.Status = "Pending";
                    _logger.LogInformation("Set default status to Pending for booking");
                }

                var isAvailable = await _bookingScheduleService.KiemTraSanCoSanAsync(request.FieldId, request.BookingDate, request.StartTime, request.EndTime);
                if (!isAvailable)
                {
                    return Json(new { success = false, message = "Sân đã được đặt trong khoảng thời gian này" });
                }

                var success = await _bookingScheduleService.TaoDatSanAsync(request);
                if (success)
                {
                    _logger.LogInformation("Successfully created booking with status: {Status}", request.Status);
                    return Json(new { success = true, message = "Tạo đặt sân thành công" });
                }
                else
                {
                    _logger.LogError("Failed to create booking");
                    return Json(new { success = false, message = "Lỗi khi tạo đặt sân" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking: {Message}", ex.Message);
                return Json(new { success = false, message = "Lỗi khi tạo đặt sân" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking([FromBody] ConfirmBookingRequest request)
        {
            try
            {
                var id = request.Id;
                _logger.LogInformation("=== CONFIRM BOOKING DEBUG ===");
                _logger.LogInformation("Received booking ID: {BookingId} (type: {Type})", id, id.GetType().Name);
                
                // Kiểm tra booking có tồn tại không trước khi xác nhận
                var booking = await _bookingScheduleService.LayDatSanTheoIdAsync(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", id);
                    return Json(new { success = false, message = "Không tìm thấy đặt sân này." });
                }

                _logger.LogInformation("Booking {BookingId} current status: {Status}", id, booking.Status);

                var confirmedBy = 1; // TODO: Get from current user context
                var success = await _bookingScheduleService.XacNhanDatSanAsync(id, confirmedBy);
                if (success)
                {
                    _logger.LogInformation("Successfully confirmed booking {BookingId}", id);
                    return Json(new { success = true, message = "Xác nhận đặt sân thành công" });
                }
                else
                {
                    _logger.LogWarning("Failed to confirm booking {BookingId} with status: {Status}", id, booking.Status);
                    return Json(new { success = false, message = $"Không thể xác nhận đặt sân. Trạng thái hiện tại: {booking.Status}. Chỉ có thể xác nhận đặt sân đang chờ xác nhận." });
                }
            }
            catch (Exception ex)
            {
                var id = request?.Id ?? 0;
                _logger.LogError(ex, "Error confirming booking {BookingId}: {Message}", id, ex.Message);
                return Json(new { success = false, message = $"Lỗi khi xác nhận đặt sân: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingRequest? request = null)
        {
            try
            {
                _logger.LogInformation("Attempting to cancel booking {BookingId}", id);
                
                // Kiểm tra booking có tồn tại không
                var booking = await _bookingScheduleService.LayDatSanTheoIdAsync(id);
                if (booking == null)
                {
                    _logger.LogWarning("Booking not found: {BookingId}", id);
                    return Json(new { success = false, message = "Không tìm thấy đặt sân này." });
                }

                _logger.LogInformation("Booking {BookingId} current status: {Status}", id, booking.Status);

                var cancelledBy = 1; // TODO: Get from current user context
                var success = await _bookingScheduleService.HuyDatSanAsync(id, cancelledBy, request?.Reason);
                if (success)
                {
                    _logger.LogInformation("Booking {BookingId} cancelled successfully", id);
                    return Json(new { success = true, message = "Hủy đặt sân thành công" });
                }
                else
                {
                    _logger.LogWarning("Failed to cancel booking {BookingId} with status: {Status}", id, booking.Status);
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

        [HttpGet]
        public async Task<IActionResult> GetAvailableFields()
        {
            try
            {
                var fields = await _bookingScheduleService.LaySanCoSanAsync();
                var result = fields.Select(f => new
                {
                    fieldId = f.FieldId,
                    fieldName = f.FieldName,
                    fieldType = f.FieldType.TypeName,
                    location = f.Location,
                    description = f.Description
                }).ToList();

                return Json(new { success = true, fields = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available fields");
                return Json(new { success = false, message = "Lỗi khi tải danh sách sân" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBookingStatistics()
        {
            try
            {
                var statistics = await _bookingScheduleService.LayThongKeAsync();
                return Json(new { success = true, statistics = statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking statistics");
                return Json(new { success = false, message = "Lỗi khi tải thống kê" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFieldSchedule(DateTime date)
        {
            try
            {
                var fields = await _context.Fields
                    .Include(f => f.FieldType)
                    .Where(f => f.Status != "Maintenance")
                    .OrderBy(f => f.FieldName)
                    .ToListAsync();

                var bookings = await _context.Bookings
                    .Include(b => b.User)
                    .Where(b => b.BookingDate.Date == date.Date && 
                               (b.Status == "Confirmed" || b.Status == "Pending"))
                    .ToListAsync();

                var schedule = fields.Select(field => new
                {
                    fieldId = field.FieldId,
                    fieldName = field.FieldName,
                    fieldType = field.FieldType.TypeName,
                    timeSlots = GenerateTimeSlots(field, bookings, date)
                }).ToList();

                return Json(new { success = true, schedule = schedule });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field schedule for date {Date}", date);
                return Json(new { success = false, message = "Lỗi khi tải lịch sân" });
            }
        }

        private List<object> GenerateTimeSlots(Field field, List<Booking> bookings, DateTime date)
        {
            var slots = new List<object>();
            var fieldBookings = bookings.Where(b => b.FieldId == field.FieldId).ToList();

            // Generate slots for each hour from 6:00 to 22:00
            for (int hour = 6; hour < 23; hour++)
            {
                var hourStart = new TimeSpan(hour, 0, 0);
                var hourEnd = new TimeSpan(hour + 1, 0, 0);

                // Check if any booking overlaps with this hour
                var booking = fieldBookings.FirstOrDefault(b =>
                    b.StartTime < hourEnd && b.EndTime > hourStart);

                if (booking != null)
                {
                    var customerName = booking.User != null
                        ? $"{booking.User.FirstName} {booking.User.LastName}"
                        : booking.GuestName;

                    slots.Add(new
                    {
                        hour = hour,
                        startTime = booking.StartTime.ToString(@"hh\:mm"),
                        endTime = booking.EndTime.ToString(@"hh\:mm"),
                        status = booking.Status,
                        customerName = customerName,
                        bookingId = booking.BookingId
                    });
                }
            }

            return slots;
        }

        // Booking Statistics Actions
        [HttpGet]
        public async Task<IActionResult> BookingStats()
        {
            try
            {
                var fromDate = DateTime.Today.AddDays(-30);
                var toDate = DateTime.Today;
                var reportType = "daily";

                var summary = await _bookingStatisticsService.LayThongKeTongQuanDatSanAsync(fromDate, toDate);
                var bookingChartData = await _bookingStatisticsService.LayDuLieuBieuDoDatSanAsync(fromDate, toDate, reportType);
                var revenueChartData = await _bookingStatisticsService.LayDuLieuBieuDoDoanhThuAsync(fromDate, toDate, reportType);
                var fieldPerformance = await _bookingStatisticsService.LayHieuSuatSanAsync(fromDate, toDate);
                var topCustomers = await _bookingStatisticsService.LayTopKhachHangAsync(fromDate, toDate, 5);
                var topFields = await _bookingStatisticsService.LayTopSanAsync(fromDate, toDate, 5);
                var timeAnalysis = await _bookingStatisticsService.LayPhanTichThoiGianAsync(fromDate, toDate);
                var comparison = await _bookingStatisticsService.LaySoSanhVoiKyTruocAsync(fromDate, toDate, reportType);

                var viewModel = new BookingStatsViewModel
                {
                    Summary = summary,
                    BookingChartData = bookingChartData,
                    RevenueChartData = revenueChartData,
                    FieldPerformance = fieldPerformance,
                    TopCustomers = topCustomers,
                    TopFields = topFields,
                    TimeAnalysis = timeAnalysis,
                    Comparison = comparison,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ReportType = reportType
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking statistics page");
                return View("Error", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetBookingStatsData([FromBody] BookingStatsFilterRequest request)
        {
            try
            {
                var fromDate = request.FromDate ?? DateTime.Today.AddDays(-30);
                var toDate = request.ToDate ?? DateTime.Today;
                var reportType = request.ReportType ?? "daily";

                var summary = await _bookingStatisticsService.LayThongKeTongQuanDatSanAsync(fromDate, toDate);
                var bookingChartData = await _bookingStatisticsService.LayDuLieuBieuDoDatSanAsync(fromDate, toDate, reportType);
                var revenueChartData = await _bookingStatisticsService.LayDuLieuBieuDoDoanhThuAsync(fromDate, toDate, reportType);
                var fieldPerformance = await _bookingStatisticsService.LayHieuSuatSanAsync(fromDate, toDate);
                var topCustomers = await _bookingStatisticsService.LayTopKhachHangAsync(fromDate, toDate, 5);
                var topFields = await _bookingStatisticsService.LayTopSanAsync(fromDate, toDate, 5);
                var timeAnalysis = await _bookingStatisticsService.LayPhanTichThoiGianAsync(fromDate, toDate);
                var comparison = await _bookingStatisticsService.LaySoSanhVoiKyTruocAsync(fromDate, toDate, reportType);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        summary,
                        bookingChartData,
                        revenueChartData,
                        fieldPerformance,
                        topCustomers,
                        topFields,
                        timeAnalysis,
                        comparison
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting booking statistics data");
                return Json(new { success = false, message = "Lỗi khi tải dữ liệu thống kê" });
            }
        }

        public class ConfirmBookingRequest
        {
            public int Id { get; set; }
        }

        public class CancelBookingRequest
        {
            public string? Reason { get; set; }
        }

        #region Pricing Rules Management

        [HttpGet]
        public async Task<IActionResult> GetPricingRules(int? fieldTypeId = null)
        {
            try
            {
                var pricingRules = await _fieldAdminService.LayDanhSachQuyTacGiaAsync(fieldTypeId);
                return Json(new { success = true, data = pricingRules });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pricing rules");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy danh sách quy tắc giá" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPricingRule(int id)
        {
            try
            {
                var pricingRule = await _fieldAdminService.LayQuyTacGiaTheoIdAsync(id);
                if (pricingRule == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy quy tắc giá" });
                }

                return Json(new { 
                    success = true, 
                    data = new {
                        pricingRuleId = pricingRule.PricingRuleId,
                        fieldTypeId = pricingRule.FieldTypeId,
                        startTime = pricingRule.StartTime.ToString(@"hh\:mm"),
                        endTime = pricingRule.EndTime.ToString(@"hh\:mm"),
                        dayOfWeek = pricingRule.DayOfWeek,
                        price = pricingRule.Price,
                        isPeakHour = pricingRule.IsPeakHour,
                        peakMultiplier = pricingRule.PeakMultiplier,
                        effectiveFrom = pricingRule.EffectiveFrom.ToString("yyyy-MM-dd"),
                        effectiveTo = pricingRule.EffectiveTo?.ToString("yyyy-MM-dd"),
                        fieldTypeName = pricingRule.FieldType?.TypeName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pricing rule: {PricingRuleId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thông tin quy tắc giá" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePricingRule(PricingRuleViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
                }

                var result = await _fieldAdminService.TaoQuyTacGiaAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Tạo quy tắc giá thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể tạo quy tắc giá. Có thể do trùng lặp thời gian hoặc lỗi hệ thống." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pricing rule");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo quy tắc giá" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePricingRule(PricingRuleViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
                }

                var result = await _fieldAdminService.CapNhatQuyTacGiaAsync(model);
                if (result)
                {
                    return Json(new { success = true, message = "Cập nhật quy tắc giá thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể cập nhật quy tắc giá. Có thể do trùng lặp thời gian hoặc lỗi hệ thống." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pricing rule: {PricingRuleId}", model.PricingRuleId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật quy tắc giá" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePricingRule(int id)
        {
            try
            {
                var result = await _fieldAdminService.XoaQuyTacGiaAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa quy tắc giá thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể xóa quy tắc giá" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pricing rule: {PricingRuleId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa quy tắc giá" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFieldTypesForPricing()
        {
            try
            {
                var fieldTypes = await _fieldAdminService.LayTatCaLoaiSanChoQuyTacGiaAsync();
                return Json(new { success = true, data = fieldTypes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field types for pricing");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy danh sách loại sân" });
            }
        }

        #endregion

        #region Session Management

        public async Task<IActionResult> Sessions()
        {
            try
            {
                var sessions = await _sessionService.LayTatCaSessionAsync();
                return View(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sessions");
                return View(new List<BookingSession>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckIn()
        {
            try
            {
                // Lấy danh sách booking đang chờ check-in
                var pendingBookings = await _context.Bookings
                    .Where(b => b.Status == "Confirmed" && !b.BookingSessions.Any())
                    .Include(b => b.User)
                    .Include(b => b.Field)
                    .OrderBy(b => b.BookingDate)
                    .ThenBy(b => b.StartTime)
                    .ToListAsync();

                return View(pendingBookings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading check-in page");
                return View(new List<Booking>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn(string bookingCode, string? notes = null)
        {
            try
            {
                _logger.LogInformation($"Attempting check-in for booking code: {bookingCode}");
                
                // Tìm booking theo mã booking (BookingCode)
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);
                
                if (booking == null)
                {
                    _logger.LogWarning($"Booking not found for code: {bookingCode}");
                    TempData["Error"] = $"Không tìm thấy booking với mã: {bookingCode}";
                    return RedirectToAction("CheckIn");
                }

                _logger.LogInformation($"Found booking: ID={booking.BookingId}, Status={booking.Status}");

                // Kiểm tra trạng thái booking
                if (booking.Status != "Confirmed")
                {
                    _logger.LogWarning($"Booking {bookingCode} status is {booking.Status}, not Confirmed");
                    TempData["Error"] = $"Booking {bookingCode} chưa được xác nhận. Trạng thái hiện tại: {booking.Status}";
                    return RedirectToAction("CheckIn");
                }

                // Kiểm tra xem đã check-in chưa
                var existingSession = await _context.BookingSessions
                    .FirstOrDefaultAsync(bs => bs.BookingId == booking.BookingId);
                
                if (existingSession != null)
                {
                    _logger.LogWarning($"Booking {bookingCode} already checked in");
                    TempData["Error"] = $"Booking {bookingCode} đã được check-in rồi!";
                    return RedirectToAction("CheckIn");
                }

                // Kiểm tra xem có admin user nào không
                var adminExists = await _context.Users.AnyAsync(u => u.RoleId == 1); // RoleId = 1 là admin
                if (!adminExists)
                {
                    _logger.LogError("No admin user found in database");
                    TempData["Error"] = "Không tìm thấy admin trong hệ thống!";
                    return RedirectToAction("CheckIn");
                }

                // Lấy admin đầu tiên làm mặc định
                var defaultAdmin = await _context.Users.FirstAsync(u => u.RoleId == 1);
                _logger.LogInformation($"Using default admin: ID={defaultAdmin.UserId}, Name={defaultAdmin.FirstName} {defaultAdmin.LastName}");

                // Thực hiện check-in bằng BookingId và UserId (admin)
                var result = await _sessionService.CheckInAsync(booking.BookingId, defaultAdmin.UserId, notes);
                if (result.Success)
                {
                    _logger.LogInformation($"Check-in successful for booking {bookingCode}");
                    TempData["Success"] = $"Check-in thành công cho booking {bookingCode}!";
                }
                else
                {
                    _logger.LogError($"Check-in failed for booking {bookingCode}: {result.Message}");
                    TempData["Error"] = $"Check-in thất bại: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-in for booking {BookingCode}", bookingCode);
                TempData["Error"] = $"Có lỗi xảy ra khi check-in: {ex.Message}";
            }
            return RedirectToAction("CheckIn");
        }

        [HttpPost]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
        {
            try
            {
                // Tìm admin user đầu tiên
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 1);
                if (adminUser == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy admin user!" });
                }

                var result = await _sessionService.CheckOutAsync(request.SessionId, adminUser.UserId, null);
                if (result.Success)
                {
                    return Json(new { success = true, message = result.Message, finalAmount = result.FinalAmount });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during check-out for session {SessionId}", request.SessionId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi check-out!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] AddProductRequest request)
        {
            try
            {
                var result = await _sessionService.ThemSanPhamVaoSessionAsync(request.SessionId, request.ProductId, request.Quantity);
                if (result)
                {
                    return Json(new { success = true, message = "Thêm sản phẩm thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Thêm sản phẩm thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to session {SessionId}", request.SessionId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm sản phẩm!" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveProduct(int orderItemId)
        {
            try
            {
                var result = await _sessionService.XoaSanPhamKhoiSessionAsync(orderItemId);
                if (result)
                {
                    return Json(new { success = true, message = "Xóa sản phẩm thành công!" });
                }
                else
                {
                    return Json(new { success = false, message = "Xóa sản phẩm thất bại!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product from session {OrderItemId}", orderItemId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSessionDetails(int sessionId)
        {
            try
            {
                var session = await _sessionService.LaySessionTheoIdAsync(sessionId);
                if (session == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy session!" });
                }

                return Json(new { 
                    success = true, 
                    session = new {
                        sessionId = session.SessionId,
                        bookingId = session.BookingId,
                        checkInTime = session.CheckInTime?.ToString("dd/MM/yyyy HH:mm"),
                        checkOutTime = session.CheckOutTime?.ToString("dd/MM/yyyy HH:mm"),
                        status = session.Status,
                        notes = session.Notes,
                        totalAmount = session.Booking?.TotalPrice ?? 0,
                        booking = session.Booking != null ? new {
                            bookingCode = session.Booking.BookingCode,
                            customerName = session.Booking.User != null ? 
                                $"{session.Booking.User.FirstName} {session.Booking.User.LastName}" : 
                                session.Booking.GuestName,
                            fieldName = session.Booking.Field?.FieldName
                        } : null,
                        orderItems = session.SessionOrders?.SelectMany(so => so.SessionOrderItems?.Select(soi => new {
                            sessionOrderItemId = soi.SessionOrderItemId,
                            productName = soi.Product?.ProductName,
                            quantity = soi.Quantity,
                            unitPrice = soi.UnitPrice,
                            totalPrice = soi.TotalPrice
                        }) ?? Enumerable.Empty<object>()).ToList() ?? new List<object>()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session details {SessionId}", sessionId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thông tin session!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableProducts()
        {
            try
            {
                var products = await _sessionService.LaySanPhamCoSanAsync();
                return Json(new { 
                    success = true, 
                    products = products.Select(p => new {
                        productId = p.ProductId,
                        productName = p.ProductName,
                        price = p.Price,
                        productTypeId = p.ProductTypeId,
                        productTypeName = p.ProductType != null ? p.ProductType.TypeName : null
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available products");
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy danh sách sản phẩm!" });
            }
        }

        #endregion


    }
}