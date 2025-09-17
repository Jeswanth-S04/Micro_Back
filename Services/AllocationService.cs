using BudgetManagementSystem.Api.Data;
using BudgetManagementSystem.Api.DTOs;
using BudgetManagementSystem.Api.Interfaces;
using BudgetManagementSystem.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BudgetManagementSystem.Api.Hubs;

namespace BudgetManagementSystem.Api.Services
{
    public class AllocationService : IAllocationService
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notifier;
        private readonly IHubContext<BudgetHub> _hub;

        public AllocationService(AppDbContext db, INotificationService notifier, IHubContext<BudgetHub> hub)
        {
            _db = db;
            _notifier = notifier;
            _hub = hub;
        }

        public async Task<AllocationResponseDto> CreateAsync(AllocationCreateDto dto, int userId)
        {
            var dep = await _db.Departments.FindAsync(dto.DepartmentId) ?? throw new Exception("Department not found");
            var cat = await _db.BudgetCategories.FindAsync(dto.CategoryId) ?? throw new Exception("Category not found");

            // Validate against category limit (sum of allocations)
            var currentAllocated = await _db.Allocations.Where(a => a.CategoryId == dto.CategoryId).SumAsync(a => a.Amount);
            if (currentAllocated + dto.Amount > cat.Limit)
                throw new Exception("Allocation exceeds category limit");

            var entity = new Allocation
            {
                DepartmentId = dto.DepartmentId,
                CategoryId = dto.CategoryId,
                Amount = dto.Amount
            };
            _db.Allocations.Add(entity);
            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "AllocationCreated",
                Details = $"Dept {dep.Name}, Cat {cat.Name}, Amount {dto.Amount}"
            });
            await _db.SaveChangesAsync();

            // Real-time update to department group (frontend should group by dep-{id})
            await _hub.Clients.Group($"dep-{dto.DepartmentId}").SendAsync("allocationUpdated");

            return ToDto(entity, cat.Timeframe);
        }

        public async Task<IEnumerable<AllocationResponseDto>> GetByDepartmentAsync(int departmentId)
        {
            var list = await _db.Allocations
                .Include(a => a.Category)
                .Where(a => a.DepartmentId == departmentId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return list.Select(a => ToDto(a, a.Category.Timeframe));
        }

        public async Task<DepartmentSummaryDto> GetDepartmentSummaryAsync(int departmentId)
        {
            var dep = await _db.Departments.FindAsync(departmentId) ?? throw new Exception("Department not found");

            var allocations = await _db.Allocations.Include(a => a.Category)
                .Where(a => a.DepartmentId == departmentId)
                .ToListAsync();

            var catGroups = allocations.GroupBy(a => a.Category);
            var categories = new List<CategoryBreakdownDto>();
            decimal totalAlloc = 0, totalSpent = 0;

            foreach (var g in catGroups)
            {
                var alloc = g.Sum(x => x.Amount);
                var spent = g.Sum(x => x.Spent);
                totalAlloc += alloc;
                totalSpent += spent;

                var thresholdValue = (g.Key.ThresholdPercent / 100m) * alloc;
                categories.Add(new CategoryBreakdownDto
                {
                    CategoryId = g.Key.Id,
                    CategoryName = g.Key.Name,
                    Allocation = alloc,
                    Spent = spent,
                    ThresholdPercent = g.Key.ThresholdPercent,
                    NearingLimit = spent >= thresholdValue && spent < alloc,
                    Exceeded = spent > alloc
                });
            }

            return new DepartmentSummaryDto
            {
                DepartmentId = departmentId,
                DepartmentName = dep.Name,
                TotalAllocation = totalAlloc,
                TotalSpent = totalSpent,
                Categories = categories
            };
        }

        public async Task<ManagementSummaryDto> GetManagementSummaryAsync(int? categoryId = null, int? departmentId = null)
        {
            var query = _db.Allocations.Include(a => a.Category).Include(a => a.Department).AsQueryable();
            if (categoryId.HasValue) query = query.Where(a => a.CategoryId == categoryId.Value);
            if (departmentId.HasValue) query = query.Where(a => a.DepartmentId == departmentId.Value);

            var list = await query.ToListAsync();
            var groups = list.GroupBy(a => a.Department);

            var departments = new List<DepartmentSummaryDto>();
            decimal grandAlloc = 0, grandSpent = 0;

            foreach (var g in groups)
            {
                var allocations = g.Sum(x => x.Amount);
                var spent = g.Sum(x => x.Spent);
                grandAlloc += allocations;
                grandSpent += spent;

                var catGroups = g.GroupBy(x => x.Category);
                var categories = catGroups.Select(cg =>
                {
                    var alloc = cg.Sum(x => x.Amount);
                    var sp = cg.Sum(x => x.Spent);
                    var thresholdVal = (cg.Key.ThresholdPercent / 100m) * alloc;
                    return new CategoryBreakdownDto
                    {
                        CategoryId = cg.Key.Id,
                        CategoryName = cg.Key.Name,
                        Allocation = alloc,
                        Spent = sp,
                        ThresholdPercent = cg.Key.ThresholdPercent,
                        NearingLimit = sp >= thresholdVal && sp < alloc,
                        Exceeded = sp > alloc
                    };
                }).ToList();

                departments.Add(new DepartmentSummaryDto
                {
                    DepartmentId = g.Key.Id,
                    DepartmentName = g.Key.Name,
                    TotalAllocation = allocations,
                    TotalSpent = spent,
                    Categories = categories
                });
            }

            return new ManagementSummaryDto
            {
                Departments = departments,
                GrandTotalAllocation = grandAlloc,
                GrandTotalSpent = grandSpent
            };
        }

        public async Task UpdateSpentAsync(int allocationId, decimal newSpent, int userId)
        {
            var allocation = await _db.Allocations.Include(a => a.Category).Include(a => a.Department).FirstOrDefaultAsync(a => a.Id == allocationId)
                ?? throw new Exception("Allocation not found");

            allocation.Spent = newSpent;
            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "AllocationSpentUpdated",
                Details = $"Allocation {allocationId} spent={newSpent}"
            });
            await _db.SaveChangesAsync();

            // Threshold and exceeded notifications
            var thresholdValue = allocation.Amount * (allocation.Category.ThresholdPercent / 100m);
            if (allocation.Spent >= thresholdValue && allocation.Spent < allocation.Amount)
            {
                await _notifier.SendThresholdAlertAsync(allocation.DepartmentId, allocation.CategoryId,
                    $"Nearing limit: {allocation.Department.Name} / {allocation.Category.Name}");
            }
            else if (allocation.Spent > allocation.Amount)
            {
                await _notifier.SendThresholdAlertAsync(allocation.DepartmentId, allocation.CategoryId,
                    $"Exceeded: {allocation.Department.Name} / {allocation.Category.Name}");
            }

            await _hub.Clients.Group($"dep-{allocation.DepartmentId}").SendAsync("utilizationUpdated");
        }

        private static AllocationResponseDto ToDto(Allocation a, string timeframe) => new()
        {
            Id = a.Id,
            DepartmentId = a.DepartmentId,
            CategoryId = a.CategoryId,
            Amount = a.Amount,
            Spent = a.Spent,
            CreatedAt = a.CreatedAt,
            Timeframe = timeframe
        };

        public Task<AllocationResponseDto> UpdateAsync(int id, AllocationUpdateDto dto, int userId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(int id, int userId)
        {
            throw new NotImplementedException();
        }
    }
}
