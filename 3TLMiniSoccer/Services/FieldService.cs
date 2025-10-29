using Microsoft.EntityFrameworkCore;
using _3TLMiniSoccer.Data;
using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.Services
{
    public class FieldService
    {
        private readonly ApplicationDbContext _context;

        public FieldService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Field Types

        public async Task<List<FieldType>> LayTatCaLoaiSanAsync()
        {
            return await _context.FieldTypes
                .Where(ft => ft.IsActive)
                .OrderBy(ft => ft.TypeName)
                .ToListAsync();
        }

        public async Task<FieldType?> LayLoaiSanTheoIdAsync(int fieldTypeId)
        {
            return await _context.FieldTypes.FindAsync(fieldTypeId);
        }

        public async Task<FieldType> TaoLoaiSanAsync(FieldType fieldType)
        {
            fieldType.CreatedAt = DateTime.Now;
            fieldType.UpdatedAt = DateTime.Now;
            _context.FieldTypes.Add(fieldType);
            await _context.SaveChangesAsync();
            return fieldType;
        }

        public async Task<bool> CapNhatLoaiSanAsync(FieldType fieldType)
        {
            try
            {
                fieldType.UpdatedAt = DateTime.Now;
                _context.FieldTypes.Update(fieldType);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaLoaiSanAsync(int fieldTypeId)
        {
            try
            {
                var fieldType = await _context.FieldTypes.FindAsync(fieldTypeId);
                if (fieldType != null)
                {
                    fieldType.IsActive = false;
                    fieldType.UpdatedAt = DateTime.Now;
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


        #region Fields

        public async Task<List<Field>> LayTatCaSanAsync()
        {
            return await _context.Fields
                .Include(f => f.FieldType)
                .OrderBy(f => f.FieldName)
                .ToListAsync();
        }

        public async Task<Field?> LaySanTheoIdAsync(int fieldId)
        {
            return await _context.Fields
                .Include(f => f.FieldType)
                .FirstOrDefaultAsync(f => f.FieldId == fieldId);
        }

        public async Task<List<Field>> LaySanTheoLoaiAsync(int fieldTypeId)
        {
            return await _context.Fields
                .Include(f => f.FieldType)
                .Where(f => f.FieldTypeId == fieldTypeId)
                .OrderBy(f => f.FieldName)
                .ToListAsync();
        }

        public async Task<List<Field>> GetFieldsByLocationAsync(string location)
        {
            return await _context.Fields
                .Include(f => f.FieldType)
                .Where(f => f.Location == location)
                .OrderBy(f => f.FieldName)
                .ToListAsync();
        }


        public async Task<Field> TaoSanAsync(Field field)
        {
            field.CreatedAt = DateTime.Now;
            field.UpdatedAt = DateTime.Now;
            _context.Fields.Add(field);
            await _context.SaveChangesAsync();
            return field;
        }

        public async Task<bool> CapNhatSanAsync(Field field)
        {
            try
            {
                field.UpdatedAt = DateTime.Now;
                _context.Fields.Update(field);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaSanAsync(int fieldId)
        {
            try
            {
                var field = await _context.Fields.FindAsync(fieldId);
                if (field != null)
                {
                    field.Status = "Closed";
                    field.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ThayDoiTrangThaiSanAsync(int fieldId, string status)
        {
            try
            {
                var field = await _context.Fields.FindAsync(fieldId);
                if (field != null)
                {
                    field.Status = status;
                    field.UpdatedAt = DateTime.Now;
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


        #region Pricing Rules

        public async Task<List<PricingRule>> LayTatCaQuyTacGiaAsync()
        {
            return await _context.PricingRules
                .Include(pr => pr.FieldType)
                .OrderBy(pr => pr.FieldType.TypeName)
                .ToListAsync();
        }

        public async Task<PricingRule?> LayQuyTacGiaTheoIdAsync(int pricingRuleId)
        {
            return await _context.PricingRules
                .Include(pr => pr.FieldType)
                .FirstOrDefaultAsync(pr => pr.PricingRuleId == pricingRuleId);
        }

        public async Task<List<PricingRule>> LayQuyTacGiaTheoLoaiSanAsync(int fieldTypeId)
        {
            return await _context.PricingRules
                .Include(pr => pr.FieldType)
                .Where(pr => pr.FieldTypeId == fieldTypeId)
                .ToListAsync();
        }

        public async Task<PricingRule> TaoQuyTacGiaAsync(PricingRule pricingRule)
        {
            pricingRule.CreatedAt = DateTime.Now;
            pricingRule.UpdatedAt = DateTime.Now;
            _context.PricingRules.Add(pricingRule);
            await _context.SaveChangesAsync();
            return pricingRule;
        }

        public async Task<bool> CapNhatQuyTacGiaAsync(PricingRule pricingRule)
        {
            try
            {
                pricingRule.UpdatedAt = DateTime.Now;
                _context.PricingRules.Update(pricingRule);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaQuyTacGiaAsync(int pricingRuleId)
        {
            try
            {
                var pricingRule = await _context.PricingRules.FindAsync(pricingRuleId);
                if (pricingRule != null)
                {
                    _context.PricingRules.Remove(pricingRule);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<decimal> TinhGiaAsync(int fieldTypeId, DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Convert Sunday from 0 to 7

            var pricingRule = await _context.PricingRules
                .Where(pr => pr.FieldTypeId == fieldTypeId &&
                           pr.DayOfWeek == dayOfWeek &&
                           pr.EffectiveFrom <= date &&
                           (pr.EffectiveTo == null || pr.EffectiveTo >= date))
                .FirstOrDefaultAsync();

            if (pricingRule != null)
            {
                return pricingRule.Price * pricingRule.PeakMultiplier;
            }

            // Fallback to base price if no specific rule found
            var fieldType = await _context.FieldTypes.FindAsync(fieldTypeId);
            return fieldType?.BasePrice ?? 0;
        }

        #endregion

        #region Field Schedules

        public async Task<List<FieldSchedule>> LayLichSanAsync(int fieldId, DateTime date)
        {
            return await _context.FieldSchedules
                .Include(fs => fs.Field)
                .Include(fs => fs.Booking)
                .Where(fs => fs.FieldId == fieldId && fs.Date.Date == date.Date)
                .OrderBy(fs => fs.StartTime)
                .ToListAsync();
        }

        public async Task<List<FieldSchedule>> LayLichCoSanAsync(DateTime date, int? fieldTypeId = null)
        {
            var query = _context.FieldSchedules
                .Include(fs => fs.Field)
                .ThenInclude(f => f.FieldType)
                .Where(fs => fs.Date.Date == date.Date && fs.Status == "Available");

            if (fieldTypeId.HasValue)
            {
                query = query.Where(fs => fs.Field.FieldTypeId == fieldTypeId.Value);
            }

            return await query.OrderBy(fs => fs.Field.FieldName)
                .ThenBy(fs => fs.StartTime)
                .ToListAsync();
        }

        public async Task<FieldSchedule?> LayLichSanTheoIdAsync(int scheduleId)
        {
            return await _context.FieldSchedules
                .Include(fs => fs.Field)
                .Include(fs => fs.Booking)
                .FirstOrDefaultAsync(fs => fs.ScheduleId == scheduleId);
        }

        public async Task<FieldSchedule> TaoLichSanAsync(FieldSchedule fieldSchedule)
        {
            fieldSchedule.CreatedAt = DateTime.Now;
            fieldSchedule.UpdatedAt = DateTime.Now;
            _context.FieldSchedules.Add(fieldSchedule);
            await _context.SaveChangesAsync();
            return fieldSchedule;
        }

        public async Task<bool> CapNhatLichSanAsync(FieldSchedule fieldSchedule)
        {
            try
            {
                fieldSchedule.UpdatedAt = DateTime.Now;
                _context.FieldSchedules.Update(fieldSchedule);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> XoaLichSanAsync(int scheduleId)
        {
            try
            {
                var fieldSchedule = await _context.FieldSchedules.FindAsync(scheduleId);
                if (fieldSchedule != null)
                {
                    _context.FieldSchedules.Remove(fieldSchedule);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ChanLichAsync(int fieldId, DateTime date, TimeSpan startTime, TimeSpan endTime, string reason)
        {
            try
            {
                var fieldSchedule = await _context.FieldSchedules
                    .FirstOrDefaultAsync(fs => fs.FieldId == fieldId && 
                                             fs.Date.Date == date.Date && 
                                             fs.StartTime == startTime && 
                                             fs.EndTime == endTime);

                if (fieldSchedule == null)
                {
                    fieldSchedule = new FieldSchedule
                    {
                        FieldId = fieldId,
                        Date = date.Date,
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = "Blocked",
                        Notes = reason,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _context.FieldSchedules.Add(fieldSchedule);
                }
                else
                {
                    fieldSchedule.Status = "Blocked";
                    fieldSchedule.Notes = reason;
                    fieldSchedule.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> BoChanLichAsync(int fieldId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            try
            {
                var fieldSchedule = await _context.FieldSchedules
                    .FirstOrDefaultAsync(fs => fs.FieldId == fieldId && 
                                             fs.Date.Date == date.Date && 
                                             fs.StartTime == startTime && 
                                             fs.EndTime == endTime);

                if (fieldSchedule != null)
                {
                    fieldSchedule.Status = "Available";
                    fieldSchedule.Notes = null;
                    fieldSchedule.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> KiemTraSanCoSanAsync(int fieldId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            var fieldSchedule = await _context.FieldSchedules
                .FirstOrDefaultAsync(fs => fs.FieldId == fieldId && 
                                         fs.Date.Date == date.Date && 
                                         fs.StartTime == startTime && 
                                         fs.EndTime == endTime);

            return fieldSchedule == null || fieldSchedule.Status == "Available";
        }

        #endregion

        public async Task<int> LayTongSoSanAsync()
        {
            return await _context.Fields.CountAsync(f => f.Status == "Active");
        }

        public async Task<int> LaySoSanHoatDongAsync()
        {
            return await _context.Fields.CountAsync(f => f.Status == "Active");
        }


    }
}
