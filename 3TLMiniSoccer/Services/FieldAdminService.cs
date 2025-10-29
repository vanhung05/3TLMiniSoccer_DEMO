using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.ViewModels;

namespace _3TLMiniSoccer.Services
{
    public class FieldAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FieldAdminService> _logger;
        private readonly ImageService _imageService;

        public FieldAdminService(ApplicationDbContext context, ILogger<FieldAdminService> logger, ImageService imageService)
        {
            _context = context;
            _logger = logger;
            _imageService = imageService;
        }

        public async Task<FieldListViewModel> LayDanhSachSanAsync(FieldFilterViewModel filter)
        {
            try
            {
                var query = _context.Fields
                    .Include(f => f.FieldType)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    query = query.Where(f => f.FieldName.Contains(filter.SearchTerm) ||
                                           f.Location.Contains(filter.SearchTerm) ||
                                           (f.Description != null && f.Description.Contains(filter.SearchTerm)));
                }

                if (!string.IsNullOrEmpty(filter.FieldType))
                {
                    if (int.TryParse(filter.FieldType, out int fieldTypeId))
                    {
                        query = query.Where(f => f.FieldTypeId == fieldTypeId);
                    }
                    else
                    {
                        // Handle string-based field type filtering (5v5, 7v7, 11v11)
                        var playerCount = filter.FieldType switch
                        {
                            "5v5" => 5,
                            "7v7" => 7,
                            "11v11" => 11,
                            _ => 0
                        };
                        if (playerCount > 0)
                        {
                            query = query.Where(f => f.FieldType.PlayerCount == playerCount);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(filter.Status))
                {
                    query = query.Where(f => f.Status == filter.Status);
                }

                if (!string.IsNullOrEmpty(filter.PriceRange))
                {
                    switch (filter.PriceRange)
                    {
                        case "low":
                            query = query.Where(f => f.FieldType.BasePrice < 300000);
                            break;
                        case "medium":
                            query = query.Where(f => f.FieldType.BasePrice >= 300000 && f.FieldType.BasePrice <= 500000);
                            break;
                        case "high":
                            query = query.Where(f => f.FieldType.BasePrice > 500000);
                            break;
                    }
                }

                // Get total count
                var totalFields = await query.CountAsync();

                // Apply pagination
                var fields = await query
                    .OrderByDescending(f => f.CreatedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(f => new FieldItemViewModel
                    {
                        FieldId = f.FieldId,
                        FieldName = f.FieldName,
                        FieldTypeName = f.FieldType.TypeName,
                        FieldTypeId = f.FieldTypeId,
                        PlayerCount = f.FieldType.PlayerCount,
                        BasePrice = f.FieldType.BasePrice,
                        Location = f.Location,
                        Status = f.Status,
                        Description = f.Description,
                        ImageUrl = f.ImageUrl,
                        OpeningTime = f.OpeningTime,
                        ClosingTime = f.ClosingTime,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt
                    })
                    .ToListAsync();

                // Get stats
                var stats = await LayThongKeSanAsync();

                return new FieldListViewModel
                {
                    Fields = fields,
                    Filter = filter,
                    Stats = stats,
                    TotalFields = totalFields,
                    CurrentPage = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalFields / filter.PageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fields with filter: {@Filter}", filter);
                return new FieldListViewModel { Filter = filter };
            }
        }

        public async Task<FieldItemViewModel?> LaySanTheoIdAsync(int fieldId)
        {
            try
            {
                var field = await _context.Fields
                    .Include(f => f.FieldType)
                    .Where(f => f.FieldId == fieldId)
                    .Select(f => new FieldItemViewModel
                    {
                        FieldId = f.FieldId,
                        FieldName = f.FieldName,
                        FieldTypeName = f.FieldType.TypeName,
                        FieldTypeId = f.FieldTypeId,
                        PlayerCount = f.FieldType.PlayerCount,
                        BasePrice = f.FieldType.BasePrice,
                        Location = f.Location,
                        Status = f.Status,
                        Description = f.Description,
                        ImageUrl = f.ImageUrl,
                        OpeningTime = f.OpeningTime,
                        ClosingTime = f.ClosingTime,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                return field;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field by ID: {FieldId}", fieldId);
                return null;
            }
        }

        public async Task<List<FieldType>> LayDanhSachLoaiSanAsync()
        {
            try
            {
                return await _context.FieldTypes
                    .Where(ft => ft.IsActive)
                    .OrderBy(ft => ft.PlayerCount)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field types");
                return new List<FieldType>();
            }
        }

        public async Task<FieldStatsViewModel> LayThongKeSanAsync()
        {
            try
            {
                var totalFields = await _context.Fields.CountAsync();
                var activeFields = await _context.Fields.CountAsync(f => f.Status == "Active");
                var maintenanceFields = await _context.Fields.CountAsync(f => f.Status == "Maintenance");
                var inactiveFields = await _context.Fields.CountAsync(f => f.Status == "Inactive");

                return new FieldStatsViewModel
                {
                    TotalFields = totalFields,
                    ActiveFields = activeFields,
                    MaintenanceFields = maintenanceFields,
                    InactiveFields = inactiveFields
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field stats");
                return new FieldStatsViewModel();
            }
        }

        public async Task<bool> TaoSanAsync(CreateFieldViewModel model)
        {
            try
            {
                // Validate field type exists
                var fieldType = await _context.FieldTypes.FindAsync(model.FieldTypeId);
                if (fieldType == null)
                {
                    _logger.LogWarning("Field type not found: {FieldTypeId}", model.FieldTypeId);
                    return false;
                }

                var field = new Field
                {
                    FieldName = model.FieldName,
                    FieldTypeId = model.FieldTypeId,
                    Location = model.Location,
                    Status = model.Status,
                    Description = model.Description,
                    ImageUrl = model.ImageUrl,
                    OpeningTime = model.OpeningTime,
                    ClosingTime = model.ClosingTime,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Fields.Add(field);
                await _context.SaveChangesAsync();

                // Upload image if provided
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    try
                    {
                        var imageFileName = await _imageService.TaiLenHinhAnhSanAsync(model.ImageFile, field.FieldId);
                        field.ImageUrl = imageFileName;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Field image uploaded: {ImageUrl}", imageFileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading field image for field {FieldId}", field.FieldId);
                        // Don't fail the entire operation if image upload fails
                    }
                }

                _logger.LogInformation("Field created successfully: {FieldName}", model.FieldName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating field: {@Model}", model);
                return false;
            }
        }

        public async Task<bool> CapNhatSanAsync(UpdateFieldViewModel model)
        {
            try
            {
                _logger.LogInformation("CapNhatSanAsync called with FieldId: {FieldId}, FieldName: {FieldName}", model.FieldId, model.FieldName);
                
                var field = await _context.Fields.FindAsync(model.FieldId);
                if (field == null)
                {
                    _logger.LogWarning("Field not found for update: {FieldId}", model.FieldId);
                    return false;
                }

                _logger.LogInformation("Found field: {FieldName}, current FieldTypeId: {CurrentFieldTypeId}", field.FieldName, field.FieldTypeId);

                // Validate field type exists
                var fieldType = await _context.FieldTypes.FindAsync(model.FieldTypeId);
                if (fieldType == null)
                {
                    _logger.LogWarning("Field type not found: {FieldTypeId}", model.FieldTypeId);
                    return false;
                }

                _logger.LogInformation("Found field type: {FieldTypeName}", fieldType.TypeName);

                field.FieldName = model.FieldName;
                field.FieldTypeId = model.FieldTypeId;
                field.Location = model.Location;
                field.Status = model.Status;
                field.Description = model.Description;
                field.OpeningTime = model.OpeningTime;
                field.ClosingTime = model.ClosingTime;
                field.UpdatedAt = DateTime.Now;

                // Upload new image if provided
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    try
                    {
                        var imageFileName = await _imageService.TaiLenHinhAnhSanAsync(model.ImageFile, field.FieldId);
                        field.ImageUrl = imageFileName;
                        
                        _logger.LogInformation("Field image updated: {ImageUrl}", imageFileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading field image for field {FieldId}", field.FieldId);
                        // Don't fail the entire operation if image upload fails
                    }
                }
                else if (!string.IsNullOrEmpty(model.ImageUrl))
                {
                    // Keep existing image URL if no new file uploaded
                    field.ImageUrl = model.ImageUrl;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Field updated successfully: {FieldId}", model.FieldId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating field: {@Model}", model);
                return false;
            }
        }

        public async Task<bool> XoaSanAsync(int fieldId)
        {
            try
            {
                var field = await _context.Fields.FindAsync(fieldId);
                if (field == null)
                {
                    _logger.LogWarning("Field not found for deletion: {FieldId}", fieldId);
                    return false;
                }

                // Check if field has any bookings
                var hasBookings = await _context.Bookings.AnyAsync(b => b.FieldId == fieldId);
                if (hasBookings)
                {
                    // Soft delete - just mark as inactive
                    field.Status = "Inactive";
                    field.UpdatedAt = DateTime.Now;
                    _logger.LogInformation("Field soft deleted (has bookings): {FieldId}", fieldId);
                }
                else
                {
                    // Hard delete - remove completely
                    _context.Fields.Remove(field);
                    _logger.LogInformation("Field hard deleted: {FieldId}", fieldId);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting field: {FieldId}", fieldId);
                return false;
            }
        }

        public async Task<bool> ChuyenDoiTrangThaiSanAsync(int fieldId)
        {
            try
            {
                var field = await _context.Fields.FindAsync(fieldId);
                if (field == null)
                {
                    _logger.LogWarning("Field not found for status toggle: {FieldId}", fieldId);
                    return false;
                }

                // Toggle between Active and Inactive
                field.Status = field.Status == "Active" ? "Inactive" : "Active";
                field.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Field status toggled: {FieldId} -> {Status}", fieldId, field.Status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling field status: {FieldId}", fieldId);
                return false;
            }
        }

        public async Task<List<Field>> LayTatCaSanAsync()
        {
            return await _context.Fields
                .Include(f => f.FieldType)
                .OrderBy(f => f.FieldName)
                .ToListAsync();
        }

        public async Task<List<FieldType>> LayTatCaLoaiSanAsync()
        {
            return await _context.FieldTypes
                .Where(ft => ft.IsActive)
                .OrderBy(ft => ft.TypeName)
                .ToListAsync();
        }

        #region Pricing Rules Management

        public async Task<List<PricingRuleViewModel>> LayDanhSachQuyTacGiaAsync(int? fieldTypeId = null)
        {
            try
            {
                var query = _context.PricingRules
                    .Include(pr => pr.FieldType)
                    .AsQueryable();

                if (fieldTypeId.HasValue)
                {
                    query = query.Where(pr => pr.FieldTypeId == fieldTypeId.Value);
                }

                var pricingRules = await query
                    .OrderBy(pr => pr.FieldType.TypeName)
                    .ThenBy(pr => pr.DayOfWeek)
                    .ThenBy(pr => pr.StartTime)
                    .ToListAsync();

                return pricingRules.Select(pr => new PricingRuleViewModel
                {
                    PricingRuleId = pr.PricingRuleId,
                    FieldTypeId = pr.FieldTypeId,
                    StartTime = pr.StartTime,
                    EndTime = pr.EndTime,
                    DayOfWeek = pr.DayOfWeek,
                    Price = pr.Price,
                    IsPeakHour = pr.IsPeakHour,
                    PeakMultiplier = pr.PeakMultiplier,
                    EffectiveFrom = pr.EffectiveFrom,
                    EffectiveTo = pr.EffectiveTo,
                    FieldType = pr.FieldType
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pricing rules list");
                return new List<PricingRuleViewModel>();
            }
        }

        public async Task<PricingRuleViewModel?> LayQuyTacGiaTheoIdAsync(int pricingRuleId)
        {
            try
            {
                var pricingRule = await _context.PricingRules
                    .Include(pr => pr.FieldType)
                    .FirstOrDefaultAsync(pr => pr.PricingRuleId == pricingRuleId);

                if (pricingRule == null)
                    return null;

                return new PricingRuleViewModel
                {
                    PricingRuleId = pricingRule.PricingRuleId,
                    FieldTypeId = pricingRule.FieldTypeId,
                    StartTime = pricingRule.StartTime,
                    EndTime = pricingRule.EndTime,
                    DayOfWeek = pricingRule.DayOfWeek,
                    Price = pricingRule.Price,
                    IsPeakHour = pricingRule.IsPeakHour,
                    PeakMultiplier = pricingRule.PeakMultiplier,
                    EffectiveFrom = pricingRule.EffectiveFrom,
                    EffectiveTo = pricingRule.EffectiveTo,
                    FieldType = pricingRule.FieldType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pricing rule: {PricingRuleId}", pricingRuleId);
                return null;
            }
        }

        public async Task<bool> TaoQuyTacGiaAsync(PricingRuleViewModel model)
        {
            try
            {
                // Check for overlapping rules
                var overlappingRule = await _context.PricingRules
                    .Where(pr => pr.FieldTypeId == model.FieldTypeId &&
                                pr.DayOfWeek == model.DayOfWeek &&
                                ((pr.StartTime < model.EndTime && pr.EndTime > model.StartTime) ||
                                 (model.StartTime < pr.EndTime && model.EndTime > pr.StartTime)) &&
                                (pr.EffectiveTo == null || pr.EffectiveTo > model.EffectiveFrom) &&
                                (model.EffectiveTo == null || model.EffectiveTo > pr.EffectiveFrom))
                    .FirstOrDefaultAsync();

                if (overlappingRule != null)
                {
                    _logger.LogWarning("Overlapping pricing rule found for field type {FieldTypeId}, day {DayOfWeek}", 
                        model.FieldTypeId, model.DayOfWeek);
                    return false;
                }

                var pricingRule = new PricingRule
                {
                    FieldTypeId = model.FieldTypeId,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime,
                    DayOfWeek = model.DayOfWeek,
                    Price = model.Price,
                    IsPeakHour = model.IsPeakHour,
                    PeakMultiplier = model.PeakMultiplier,
                    EffectiveFrom = model.EffectiveFrom,
                    EffectiveTo = model.EffectiveTo,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.PricingRules.Add(pricingRule);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created pricing rule: {PricingRuleId}", pricingRule.PricingRuleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pricing rule");
                return false;
            }
        }

        public async Task<bool> CapNhatQuyTacGiaAsync(PricingRuleViewModel model)
        {
            try
            {
                var pricingRule = await _context.PricingRules
                    .FirstOrDefaultAsync(pr => pr.PricingRuleId == model.PricingRuleId);

                if (pricingRule == null)
                    return false;

                // Check for overlapping rules (excluding current rule)
                var overlappingRule = await _context.PricingRules
                    .Where(pr => pr.PricingRuleId != model.PricingRuleId &&
                                pr.FieldTypeId == model.FieldTypeId &&
                                pr.DayOfWeek == model.DayOfWeek &&
                                ((pr.StartTime < model.EndTime && pr.EndTime > model.StartTime) ||
                                 (model.StartTime < pr.EndTime && model.EndTime > pr.StartTime)) &&
                                (pr.EffectiveTo == null || pr.EffectiveTo > model.EffectiveFrom) &&
                                (model.EffectiveTo == null || model.EffectiveTo > pr.EffectiveFrom))
                    .FirstOrDefaultAsync();

                if (overlappingRule != null)
                {
                    _logger.LogWarning("Overlapping pricing rule found for field type {FieldTypeId}, day {DayOfWeek}", 
                        model.FieldTypeId, model.DayOfWeek);
                    return false;
                }

                pricingRule.FieldTypeId = model.FieldTypeId;
                pricingRule.StartTime = model.StartTime;
                pricingRule.EndTime = model.EndTime;
                pricingRule.DayOfWeek = model.DayOfWeek;
                pricingRule.Price = model.Price;
                pricingRule.IsPeakHour = model.IsPeakHour;
                pricingRule.PeakMultiplier = model.PeakMultiplier;
                pricingRule.EffectiveFrom = model.EffectiveFrom;
                pricingRule.EffectiveTo = model.EffectiveTo;
                pricingRule.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated pricing rule: {PricingRuleId}", pricingRule.PricingRuleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating pricing rule: {PricingRuleId}", model.PricingRuleId);
                return false;
            }
        }

        public async Task<bool> XoaQuyTacGiaAsync(int pricingRuleId)
        {
            try
            {
                var pricingRule = await _context.PricingRules
                    .FirstOrDefaultAsync(pr => pr.PricingRuleId == pricingRuleId);

                if (pricingRule == null)
                    return false;

                _context.PricingRules.Remove(pricingRule);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted pricing rule: {PricingRuleId}", pricingRuleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting pricing rule: {PricingRuleId}", pricingRuleId);
                return false;
            }
        }

        public async Task<List<FieldType>> LayTatCaLoaiSanChoQuyTacGiaAsync()
        {
            return await _context.FieldTypes
                .Where(ft => ft.IsActive)
                .OrderBy(ft => ft.TypeName)
                .ToListAsync();
        }

        #endregion
    }
}
