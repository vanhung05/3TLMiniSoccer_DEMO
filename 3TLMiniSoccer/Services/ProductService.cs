using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;
using _3TLMiniSoccer.Services;

namespace _3TLMiniSoccer.Services
{
    public class ProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ImageService _imageService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ImageService imageService, ILogger<ProductService> logger)
        {
            _context = context;
            _imageService = imageService;
            _logger = logger;
        }

        public async Task<ProductListViewModel> LayDanhSachSanPhamAsync(ProductFilterViewModel filter)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.CreatedByUser)
                    .Include(p => p.ProductType)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    query = query.Where(p => p.ProductName.Contains(filter.SearchTerm) || 
                                           (p.Description != null && p.Description.Contains(filter.SearchTerm)));
                }

                if (!string.IsNullOrEmpty(filter.Category))
                {
                    query = query.Where(p => p.ProductTypeId != null && 
                        _context.ProductTypes.Any(pt => pt.ProductTypeId == p.ProductTypeId && pt.TypeName == filter.Category));
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    switch (filter.Status.ToLower())
                    {
                        case "active":
                            query = query.Where(p => p.IsAvailable);
                            break;
                        case "inactive":
                            query = query.Where(p => !p.IsAvailable);
                            break;
                        case "out_of_stock":
                            // Tạm thời để trống, có thể mở rộng sau khi có inventory management
                            break;
                    }
                }

                if (filter.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= filter.MinPrice.Value);
                }

                if (filter.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= filter.MaxPrice.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(p => new ProductItemViewModel
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Description = p.Description,
                        Price = p.Price,
                        Category = p.ProductType != null ? p.ProductType.TypeName : "Chưa phân loại",
                        ImageUrl = p.ImageUrl,
                        IsAvailable = p.IsAvailable,
                        CreatedAt = p.CreatedAt,
                        CreatedByName = $"{p.CreatedByUser.FirstName} {p.CreatedByUser.LastName}",
                        StockQuantity = 0 // Tạm thời để 0
                    })
                    .ToListAsync();

                return new ProductListViewModel
                {
                    Products = products,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    Filter = filter
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return new ProductListViewModel();
            }
        }

        public async Task<Product?> LaySanPhamTheoIdAsync(int id)
        {
            try
            {
                return await _context.Products
                    .Include(p => p.CreatedByUser)
                    .Include(p => p.ProductType)
                    .FirstOrDefaultAsync(p => p.ProductId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by id: {ProductId}", id);
                return null;
            }
        }

        public async Task<bool> TaoSanPhamAsync(CreateProductViewModel model)
        {
            try
            {
                // Get ProductTypeId from TypeName if Category is provided
                int? productTypeId = null;
                if (!string.IsNullOrEmpty(model.Category))
                {
                    var productType = await _context.ProductTypes
                        .FirstOrDefaultAsync(pt => pt.TypeName == model.Category);
                    
                    if (productType != null)
                    {
                        productTypeId = productType.ProductTypeId;
                    }
                }

                var product = new Product
                {
                    ProductName = model.ProductName,
                    Description = model.Description,
                    Price = model.Price,
                    ProductTypeId = productTypeId,
                    IsAvailable = model.IsAvailable,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    CreatedBy = 1 // Tạm thời hardcode, sau này sẽ lấy từ session/claims
                };

                // Handle image upload
                if (model.ImageFile != null)
                {
                    var imageUrl = await _imageService.TaiLenHinhAnhAsync(model.ImageFile, "products");
                    product.ImageUrl = imageUrl;
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product created successfully: {ProductName}", model.ProductName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {Error}", ex.Message);
                return false;
            }
        }

        public async Task<bool> CapNhatSanPhamAsync(int id, UpdateProductViewModel model)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return false;

                // Get ProductTypeId from TypeName if Category is provided
                int? productTypeId = null;
                if (!string.IsNullOrEmpty(model.Category))
                {
                    var productType = await _context.ProductTypes
                        .FirstOrDefaultAsync(pt => pt.TypeName == model.Category);
                    
                    if (productType != null)
                    {
                        productTypeId = productType.ProductTypeId;
                    }
                }

                product.ProductName = model.ProductName;
                product.Description = model.Description;
                product.Price = model.Price;
                product.ProductTypeId = productTypeId;
                product.IsAvailable = model.IsAvailable;
                product.UpdatedAt = DateTime.Now;

                // Handle image upload
                if (model.ImageFile != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        await _imageService.XoaHinhAnhAsync(product.ImageUrl);
                    }

                    var imageUrl = await _imageService.TaiLenHinhAnhAsync(model.ImageFile, "products");
                    product.ImageUrl = imageUrl;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> XoaSanPhamAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return false;

                // Check if product is used in any orders
                var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
                if (hasOrders)
                {
                    // Soft delete - just mark as unavailable
                    product.IsAvailable = false;
                    product.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Hard delete
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        await _imageService.XoaHinhAnhAsync(product.ImageUrl);
                    }
                    _context.Products.Remove(product);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return false;
            }
        }

        public async Task<bool> ChuyenDoiTrangThaiSanPhamAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return false;

                product.IsAvailable = !product.IsAvailable;
                product.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling product status: {ProductId}", id);
                return false;
            }
        }

        public async Task<List<string>> LayDanhMucAsync()
        {
            try
            {
                return await _context.ProductTypes
                    .Where(pt => pt.IsActive)
                    .Select(pt => pt.TypeName)
                    .OrderBy(name => name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product types");
                return new List<string>();
            }
        }

        // ProductType Management Methods
        public async Task<List<ProductType>> LayTatCaLoaiSanPhamAsync()
        {
            try
            {
                _logger.LogInformation("Getting all product types from database...");
                var result = await _context.ProductTypes
                    .OrderBy(pt => pt.TypeName)
                    .ToListAsync();
                _logger.LogInformation($"Retrieved {result.Count} product types from database");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all product types");
                return new List<ProductType>();
            }
        }

        public async Task<List<ProductType>> LayLoaiSanPhamHoatDongAsync()
        {
            try
            {
                return await _context.ProductTypes
                    .Where(pt => pt.IsActive)
                    .OrderBy(pt => pt.TypeName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active product types");
                return new List<ProductType>();
            }
        }

        public async Task<ProductType?> LayLoaiSanPhamTheoIdAsync(int id)
        {
            try
            {
                return await _context.ProductTypes
                    .FirstOrDefaultAsync(pt => pt.ProductTypeId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product type by id {Id}", id);
                return null;
            }
        }

        public async Task<ProductType> TaoLoaiSanPhamAsync(ProductType productType)
        {
            try
            {
                productType.CreatedAt = DateTime.Now;
                productType.UpdatedAt = DateTime.Now;

                _context.ProductTypes.Add(productType);
                await _context.SaveChangesAsync();

                return productType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product type {TypeName}", productType.TypeName);
                throw;
            }
        }

        public async Task<ProductType> CapNhatLoaiSanPhamAsync(ProductType productType)
        {
            try
            {
                productType.UpdatedAt = DateTime.Now;

                _context.ProductTypes.Update(productType);
                await _context.SaveChangesAsync();

                return productType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product type {Id}", productType.ProductTypeId);
                throw;
            }
        }

        public async Task<bool> XoaLoaiSanPhamAsync(int id)
        {
            try
            {
                var productType = await _context.ProductTypes.FindAsync(id);
                if (productType == null)
                    return false;

                // Check if any products are using this type
                var hasProducts = await _context.Products
                    .AnyAsync(p => p.ProductTypeId == id);

                if (hasProducts)
                {
                    _logger.LogWarning("Cannot delete product type {Id} because it has associated products", id);
                    return false;
                }

                _context.ProductTypes.Remove(productType);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product type {Id}", id);
                return false;
            }
        }

        public async Task<bool> ChuyenDoiTrangThaiLoaiSanPhamAsync(int id)
        {
            try
            {
                var productType = await _context.ProductTypes.FindAsync(id);
                if (productType == null)
                    return false;

                productType.IsActive = !productType.IsActive;
                productType.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling product type status {Id}", id);
                return false;
            }
        }

        public async Task<ProductStatsViewModel> LayThongKeSanPhamAsync()
        {
            try
            {
                var totalProducts = await _context.Products.CountAsync();
                var activeProducts = await _context.Products.CountAsync(p => p.IsAvailable);
                var inactiveProducts = totalProducts - activeProducts;

                var categoryStats = await _context.Products
                    .Where(p => p.ProductTypeId != null)
                    .Include(p => p.ProductType)
                    .GroupBy(p => p.ProductType!.TypeName)
                    .Select(g => new CategoryStatsViewModel
                    {
                        Category = g.Key,
                        ProductCount = g.Count()
                    })
                    .OrderByDescending(c => c.ProductCount)
                    .ToListAsync();

                return new ProductStatsViewModel
                {
                    TotalProducts = totalProducts,
                    ActiveProducts = activeProducts,
                    InactiveProducts = inactiveProducts,
                    OutOfStockProducts = 0, // Tạm thời để 0
                    CategoryStats = categoryStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product stats");
                return new ProductStatsViewModel();
            }
        }

        public async Task<List<Product>> LayTatCaSanPhamAsync()
        {
            return await _context.Products
                .Include(p => p.CreatedByUser)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        public async Task<List<string>> LayTatCaDanhMucAsync()
        {
            try
            {
                return await _context.ProductTypes
                    .Where(pt => pt.IsActive)
                    .Select(pt => pt.TypeName)
                    .OrderBy(pt => pt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories from ProductTypes");
                return new List<string>();
            }
        }
    }
}