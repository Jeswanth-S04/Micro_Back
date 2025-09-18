using BudgetManagementSystem.Api.Data;
using BudgetManagementSystem.Api.DTOs;
using BudgetManagementSystem.Api.Enums;
using BudgetManagementSystem.Api.Interfaces;
using BudgetManagementSystem.Api.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BudgetManagementSystem.Api.Hubs;

namespace BudgetManagementSystem.Api.Services
{
    public class RequestService : IRequestService
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notifier;
        private readonly IHubContext<BudgetHub> _hub;

        public RequestService(AppDbContext db, INotificationService notifier, IHubContext<BudgetHub> hub)
        {
            _db = db;
            _notifier = notifier;
            _hub = hub;
        }

        public async Task<AdjustmentRequestResponseDto> CreateAsync(AdjustmentRequestCreateDto dto, int userId)
        {
            var dep = await _db.Departments.FindAsync(dto.DepartmentId) ?? throw new Exception("Department not found");
            var cat = await _db.BudgetCategories.FindAsync(dto.CategoryId) ?? throw new Exception("Category not found");

            var entity = new AdjustmentRequest
            {
                DepartmentId = dto.DepartmentId,
                CategoryId = dto.CategoryId,
                Reason = dto.Reason,
                Amount = dto.Amount
            };
            _db.AdjustmentRequests.Add(entity);
            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = "AdjustmentRequested",
                Details = $"Dept {dep.Name}, Cat {cat.Name}, Amount {dto.Amount}"
            });
            await _db.SaveChangesAsync();

            // FIX: Pass enum name as string
            await _notifier.NotifyRoleAsync(nameof(UserRole.FinanceAdmin), $"New Request by {dep.Name}",
                $"Department {dep.Name} requested {dto.Amount} for {cat.Name}");

            await _hub.Clients.Group("role-FinanceAdmin").SendAsync("requestsUpdated");

            return ToDto(entity);
        }

        public async Task<AdjustmentRequestResponseDto> ReviewAsync(AdjustmentReviewDto dto, int reviewerUserId)
        {
            var req = await _db.AdjustmentRequests.Include(r => r.Department).Include(r => r.Category)
                .FirstOrDefaultAsync(r => r.Id == dto.RequestId) ?? throw new Exception("Request not found");

            if (req.Status != RequestStatus.Pending) throw new Exception("Request already reviewed");

            req.Status = dto.Approve ? RequestStatus.Approved : RequestStatus.Rejected;
            req.ReviewerNote = dto.ReviewerNote;
            req.ReviewedAt = DateTime.UtcNow;
            req.ReviewedByUserId = reviewerUserId;

            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = reviewerUserId,
                Action = dto.Approve ? "RequestApproved" : "RequestRejected",
                Details = $"Request {req.Id} {req.Status}"
            });
            await _db.SaveChangesAsync();

            // If approved, increase allocation for the department-category
            if (dto.Approve)
            {
                var allocation = await _db.Allocations
                    .FirstOrDefaultAsync(a => a.DepartmentId == req.DepartmentId && a.CategoryId == req.CategoryId);
                if (allocation == null)
                {
                    allocation = new Allocation
                    {
                        DepartmentId = req.DepartmentId,
                        CategoryId = req.CategoryId,
                        Amount = req.Amount
                    };
                    _db.Allocations.Add(allocation);
                }
                else
                {
                    allocation.Amount += req.Amount;
                }
                await _db.SaveChangesAsync();
            }

            // Notify department head(s) - FIX: Use enum directly
            var head = await _db.Users.FirstOrDefaultAsync(u => u.DepartmentId == req.DepartmentId && u.Role == UserRole.DepartmentHead);
            if (head != null)
            {
                int emailamount = (int)Math.Floor(req.Amount); 
                var subject = dto.Approve ? "Request approved" : "Request rejected";
                var body = dto.Approve
                    ? $"Your request #{req.Id} for {emailamount} in {req.Category.Name} has been approved by Finance Admin."
                    : $"Your request #{req.Id} for {emailamount} in {req.Category.Name} was rejected. Note: {dto.ReviewerNote}";
                await _notifier.NotifyUserAsync(head.Id, subject, body);
            }

            await _hub.Clients.Group($"dep-{req.DepartmentId}").SendAsync("requestsUpdated");

            return ToDto(req);
        }

        public async Task<IEnumerable<AdjustmentRequestResponseDto>> GetPendingAsync()
        {
            var list = await _db.AdjustmentRequests.Where(r => r.Status == RequestStatus.Pending)
                .OrderBy(r => r.CreatedAt).ToListAsync();
            return list.Select(ToDto);
        }

        public async Task<IEnumerable<AdjustmentRequestResponseDto>> GetByDepartmentAsync(int departmentId)
        {
            var list = await _db.AdjustmentRequests.Where(r => r.DepartmentId == departmentId)
                .OrderByDescending(r => r.CreatedAt).ToListAsync();
            return list.Select(ToDto);
        }

        private static AdjustmentRequestResponseDto ToDto(AdjustmentRequest r) => new()
        {
            Id = r.Id,
            DepartmentId = r.DepartmentId,
            CategoryId = r.CategoryId,
            Reason = r.Reason,
            Amount = r.Amount,
            Status = r.Status,
            ReviewerNote = r.ReviewerNote,
            CreatedAt = r.CreatedAt,
            ReviewedAt = r.ReviewedAt
        };
    }
}
