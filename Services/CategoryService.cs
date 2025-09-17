using BudgetManagementSystem.Api.Data;
using BudgetManagementSystem.Api.DTOs;
using BudgetManagementSystem.Api.Interfaces;
using BudgetManagementSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetManagementSystem.Api.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _db;

        public CategoryService(AppDbContext db) => _db = db;

        public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto, int userId)
        {
            var exists = await _db.BudgetCategories.AnyAsync(c => c.Name == dto.Name && c.Timeframe == dto.Timeframe);
            if (exists) throw new Exception("Category already exists for the timeframe");

            var entity = new BudgetCategory
            {
                Name = dto.Name,
                Limit = dto.Limit,
                Timeframe = dto.Timeframe,
                ThresholdPercent = dto.ThresholdPercent
            };
            _db.BudgetCategories.Add(entity);
            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new Models.AuditLog
            {
                UserId = userId,
                Action = "CategoryCreated",
                Details = $"Category {entity.Name} ({entity.Timeframe}) limit {entity.Limit}"
            });
            await _db.SaveChangesAsync();

            return ToDto(entity);
        }

        public async Task<CategoryResponseDto> UpdateAsync(CategoryUpdateDto dto, int userId)
        {
            var entity = await _db.BudgetCategories.FindAsync(dto.Id) ?? throw new Exception("Category not found");

            entity.Name = dto.Name;
            entity.Limit = dto.Limit;
            entity.Timeframe = dto.Timeframe;
            entity.ThresholdPercent = dto.ThresholdPercent;

            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new Models.AuditLog
            {
                UserId = userId,
                Action = "CategoryUpdated",
                Details = $"Category {entity.Id} updated"
            });
            await _db.SaveChangesAsync();

            return ToDto(entity);
        }

        public async Task DeleteAsync(int id, int userId)
        {
            var entity = await _db.BudgetCategories.FindAsync(id) ?? throw new Exception("Category not found");
            _db.BudgetCategories.Remove(entity);
            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new Models.AuditLog
            {
                UserId = userId,
                Action = "CategoryDeleted",
                Details = $"Category {id}"
            });
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync()
        {
            var list = await _db.BudgetCategories.OrderBy(c => c.Name).ToListAsync();
            return list.Select(ToDto);
        }

        private static CategoryResponseDto ToDto(BudgetCategory c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Limit = c.Limit,
            Timeframe = c.Timeframe,
            ThresholdPercent = c.ThresholdPercent
        };
    }
}
