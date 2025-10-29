using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace _3TLMiniSoccer.Services
{
    public class CategoryAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryAdminService> _logger;

        public CategoryAdminService(ApplicationDbContext context, ILogger<CategoryAdminService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Main Page Data

        public async Task<CategoryListViewModel> LayDanhSachDanhMucAsync()
        {
            try
            {
                var viewModel = new CategoryListViewModel
                {
                    Stats = await LayThongKeDanhMucAsync(),
                    ProductCategories = await LayDanhSachDanhMucSanPhamAsync(),
                    FieldCategories = await LayDanhSachDanhMucSanAsync()
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category list");
                return new CategoryListViewModel();
            }
        }

        public async Task<CategoryOverviewStatsViewModel> LayThongKeDanhMucAsync()
        {
            try
            {
                var productCategories = await _context.ProductTypes
                    .Where(pt => pt.IsActive)
                    .CountAsync();
                    
                var fieldCount = await _context.FieldTypes.CountAsync();

                return new CategoryOverviewStatsViewModel
                {
                    ProductCategories = productCategories,
                    FieldCategories = fieldCount,
                    TotalCategories = productCategories + fieldCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category stats");
                return new CategoryOverviewStatsViewModel();
            }
        }

        #endregion


        #region Product Categories

        public async Task<List<ProductCategoryItemViewModel>> LayDanhSachDanhMucSanPhamAsync()
        {
            try
            {
                // Get categories from ProductTypes table (including inactive ones for display)
                var productTypes = await _context.ProductTypes.ToListAsync();

                // Get all products with their ProductTypeId
                var allProducts = await _context.Products
                    .Where(p => p.ProductTypeId != null)
                    .Select(p => p.ProductTypeId!.Value)
                    .ToListAsync();

                // Count products for each category
                var productCounts = allProducts
                    .GroupBy(ptid => ptid)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Map ProductTypes to view models
                var categories = productTypes
                    .Select(pt => new ProductCategoryItemViewModel
                    {
                        CategoryName = pt.TypeName,
                        Description = pt.Description,
                        IsActive = pt.IsActive,
                        ProductCount = productCounts.ContainsKey(pt.ProductTypeId) ? productCounts[pt.ProductTypeId] : 0
                    })
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                _logger.LogInformation("Loaded {Count} product categories", categories.Count);
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product categories");
                return new List<ProductCategoryItemViewModel>();
            }
        }

        public async Task<bool> TaoDanhMucSanPhamAsync(CreateProductCategoryViewModel model)
        {
            try
            {
                // Check if category already exists
                var existingCategory = await _context.ProductTypes
                    .AnyAsync(pt => pt.TypeName.ToLower() == model.CategoryName.ToLower());

                if (existingCategory)
                {
                    return false; // Category already exists
                }

                // Create new category in ProductTypes
                var productType = new ProductType
                {
                    TypeName = model.CategoryName.Trim(),
                    Description = model.Description?.Trim(),
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ProductTypes.Add(productType);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product category created: {CategoryName} (Active: {IsActive})", 
                    model.CategoryName, model.IsActive);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product category: {CategoryName}", model.CategoryName);
                return false;
            }
        }

        public async Task<bool> XoaDanhMucSanPhamAsync(string categoryName)
        {
            try
            {
                // Find product type by name
                var productType = await _context.ProductTypes
                    .FirstOrDefaultAsync(pt => pt.TypeName == categoryName);

                if (productType == null)
                {
                    return false; // Category not found
                }

                // Check if any products use this category
                var productsCount = await _context.Products
                    .CountAsync(p => p.ProductTypeId == productType.ProductTypeId);

                if (productsCount > 0)
                {
                    // Set all products using this category to null
                    var productsWithCategory = await _context.Products
                        .Where(p => p.ProductTypeId == productType.ProductTypeId)
                        .ToListAsync();

                    foreach (var product in productsWithCategory)
                    {
                        product.ProductTypeId = null;
                        product.UpdatedAt = DateTime.Now;
                    }
                }

                // Delete the product type
                _context.ProductTypes.Remove(productType);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Removed category {CategoryName} and updated {Count} products", 
                    categoryName, productsCount);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product category: {CategoryName}", categoryName);
                return false;
            }
        }

        #endregion

        #region Field Categories (FieldType)

        public async Task<List<FieldCategoryItemViewModel>> LayDanhSachDanhMucSanAsync()
        {
            try
            {
                var categories = await _context.FieldTypes
                    .Select(ft => new FieldCategoryItemViewModel
                    {
                        FieldTypeId = ft.FieldTypeId,
                        TypeName = ft.TypeName,
                        Description = ft.Description,
                        PlayerCount = ft.PlayerCount,
                        BasePrice = ft.BasePrice,
                        IsActive = ft.IsActive,
                        CreatedAt = ft.CreatedAt,
                        FieldCount = ft.Fields.Count()
                    })
                    .OrderBy(ft => ft.TypeName)
                    .ToListAsync();

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field categories");
                return new List<FieldCategoryItemViewModel>();
            }
        }

        public async Task<FieldCategoryItemViewModel?> LayDanhMucSanTheoIdAsync(int fieldTypeId)
        {
            try
            {
                var category = await _context.FieldTypes
                    .Where(ft => ft.FieldTypeId == fieldTypeId)
                    .Select(ft => new FieldCategoryItemViewModel
                    {
                        FieldTypeId = ft.FieldTypeId,
                        TypeName = ft.TypeName,
                        Description = ft.Description,
                        PlayerCount = ft.PlayerCount,
                        BasePrice = ft.BasePrice,
                        IsActive = ft.IsActive,
                        CreatedAt = ft.CreatedAt,
                        FieldCount = ft.Fields.Count()
                    })
                    .FirstOrDefaultAsync();

                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field category {FieldTypeId}", fieldTypeId);
                return null;
            }
        }

        public async Task<bool> TaoDanhMucSanAsync(CreateFieldCategoryViewModel model)
        {
            try
            {
                // Check if field type name already exists
                var existingFieldType = await _context.FieldTypes
                    .FirstOrDefaultAsync(ft => ft.TypeName.ToLower() == model.TypeName.ToLower());

                if (existingFieldType != null)
                {
                    return false; // Field type already exists
                }

                var fieldType = new FieldType
                {
                    TypeName = model.TypeName.Trim(),
                    Description = model.Description?.Trim(),
                    PlayerCount = model.PlayerCount,
                    BasePrice = model.BasePrice,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.FieldTypes.Add(fieldType);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created field category: {TypeName}", model.TypeName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating field category: {TypeName}", model.TypeName);
                return false;
            }
        }

        public async Task<bool> CapNhatDanhMucSanAsync(UpdateFieldCategoryViewModel model)
        {
            try
            {
                var fieldType = await _context.FieldTypes.FindAsync(model.FieldTypeId);
                if (fieldType == null)
                {
                    return false;
                }

                // Check if new name conflicts with existing field type (excluding current)
                var existingFieldType = await _context.FieldTypes
                    .FirstOrDefaultAsync(ft => ft.TypeName.ToLower() == model.TypeName.ToLower() 
                                             && ft.FieldTypeId != model.FieldTypeId);

                if (existingFieldType != null)
                {
                    return false; // Name conflict
                }

                fieldType.TypeName = model.TypeName.Trim();
                fieldType.Description = model.Description?.Trim();
                fieldType.PlayerCount = model.PlayerCount;
                fieldType.BasePrice = model.BasePrice;
                fieldType.IsActive = model.IsActive;
                fieldType.UpdatedAt = DateTime.Now;

                _context.FieldTypes.Update(fieldType);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated field category: {FieldTypeId}", model.FieldTypeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating field category: {FieldTypeId}", model.FieldTypeId);
                return false;
            }
        }

        public async Task<bool> XoaDanhMucSanAsync(int fieldTypeId)
        {
            try
            {
                var fieldType = await _context.FieldTypes
                    .Include(ft => ft.Fields)
                    .Include(ft => ft.PricingRules)
                    .FirstOrDefaultAsync(ft => ft.FieldTypeId == fieldTypeId);

                if (fieldType == null)
                {
                    return false;
                }

                // Check if field type has fields or pricing rules
                if (fieldType.Fields.Any() || fieldType.PricingRules.Any())
                {
                    // Cannot delete field type with related data
                    return false;
                }

                _context.FieldTypes.Remove(fieldType);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted field category: {FieldTypeId}", fieldTypeId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting field category: {FieldTypeId}", fieldTypeId);
                return false;
            }
        }

        #endregion

        public async Task<List<FieldCategoryItemViewModel>> LayDanhMucSanAsync()
        {
            return await _context.FieldTypes
                .Where(ft => ft.IsActive)
                .Select(ft => new FieldCategoryItemViewModel
                {
                    FieldTypeId = ft.FieldTypeId,
                    TypeName = ft.TypeName,
                    Description = ft.Description,
                    IsActive = ft.IsActive,
                    CreatedAt = ft.CreatedAt,
                    UpdatedAt = ft.UpdatedAt
                })
                .OrderBy(ft => ft.TypeName)
                .ToListAsync();
        }
    }
}
