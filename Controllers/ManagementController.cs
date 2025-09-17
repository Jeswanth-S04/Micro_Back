using System.Security.Claims;
using BudgetManagementSystem.Api.DTOs;
using BudgetManagementSystem.Api.Enums;
using BudgetManagementSystem.Api.Helpers;
using BudgetManagementSystem.Api.Interfaces;
using BudgetManagementSystem.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/management")]
    [Authorize(Roles = $"{nameof(UserRole.Management)},{nameof(UserRole.FinanceAdmin)}")]
    public class ManagementController : ControllerBase
    {
        private readonly IAllocationService _allocationService;
        private readonly IRequestService _requestService;
        private readonly AppDbContext _db;

        public ManagementController(
            IAllocationService allocationService, 
            IRequestService requestService,
            AppDbContext db)
        {
            _allocationService = allocationService;
            _requestService = requestService;
            _db = db;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<ManagementDashboardDto>>> GetDashboard(
            [FromQuery] int? categoryId, 
            [FromQuery] int? departmentId)
        {
            var summary = await _allocationService.GetManagementSummaryAsync(categoryId, departmentId);
            var pendingRequests = await _requestService.GetPendingAsync();
            var pendingCount = pendingRequests.Count();

            var recentRequests = await _db.AdjustmentRequests
                .Include(r => r.Department)
                .Include(r => r.Category)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .Select(r => new RecentRequestDto
                {
                    Id = r.Id,
                    DepartmentName = r.Department.Name,
                    CategoryName = r.Category.Name,
                    Amount = r.Amount,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    ReviewedAt = r.ReviewedAt
                })
                .ToListAsync();

            var alerts = await GetBudgetAlertsAsync();

            var utilizationTrends = summary.Departments
                .Where(d => d.TotalAllocation > 0)
                .Select(d => new UtilizationTrendDto
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    UtilizationPercentage = Math.Round((d.TotalSpent / d.TotalAllocation) * 100, 2),
                    TotalAllocation = d.TotalAllocation,
                    TotalSpent = d.TotalSpent,
                    IsHighUtilization = (d.TotalSpent / d.TotalAllocation) >= 0.8m
                })
                .OrderByDescending(u => u.UtilizationPercentage)
                .ToList();

            var dashboard = new ManagementDashboardDto
            {
                Summary = summary,
                PendingRequestsCount = pendingCount,
                RecentRequests = recentRequests,
                BudgetAlerts = alerts,
                UtilizationTrends = utilizationTrends,
                TotalDepartments = summary.Departments.Count(),
                HighUtilizationDepartments = utilizationTrends.Count(u => u.IsHighUtilization)
            };

            return Ok(ApiResponse<ManagementDashboardDto>.Ok(dashboard));
        }

        [HttpGet("departments/performance")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentPerformanceDto>>>> GetDepartmentPerformance()
        {
            var summary = await _allocationService.GetManagementSummaryAsync();

            var performance = summary.Departments.Select(d => new DepartmentPerformanceDto
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                TotalAllocation = d.TotalAllocation,
                TotalSpent = d.TotalSpent,
                UtilizationPercentage = d.TotalAllocation > 0 ? Math.Round((d.TotalSpent / d.TotalAllocation) * 100, 2) : 0,
                CategoriesCount = d.Categories.Count(),
                CategoriesNearingLimit = d.Categories.Count(c => c.NearingLimit),
                CategoriesExceeded = d.Categories.Count(c => c.Exceeded),
                PerformanceScore = CalculatePerformanceScore(d)
            }).OrderByDescending(p => p.PerformanceScore).ToList();

            return Ok(ApiResponse<IEnumerable<DepartmentPerformanceDto>>.Ok(performance));
        }

        [HttpGet("trends/allocations")]
        public async Task<ActionResult<ApiResponse<IEnumerable<AllocationTrendDto>>>> GetAllocationTrends(
            [FromQuery] int months = 6)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            var trends = await _db.Allocations
                .Include(a => a.Department)
                .Include(a => a.Category)
                .Where(a => a.CreatedAt >= startDate)
                .GroupBy(a => new { a.CreatedAt.Year, a.CreatedAt.Month, a.DepartmentId, a.Department.Name })
                .Select(g => new AllocationTrendDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    DepartmentId = g.Key.DepartmentId,
                    DepartmentName = g.Key.Name,
                    TotalAllocated = g.Sum(a => a.Amount),
                    TotalSpent = g.Sum(a => a.Spent),
                    AllocationCount = g.Count()
                })
                .OrderBy(t => t.Year).ThenBy(t => t.Month)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<AllocationTrendDto>>.Ok(trends));
        }

        [HttpGet("analysis/categories")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CategoryAnalysisDto>>>> GetCategoryAnalysis()
        {
            var analysis = await _db.Allocations
                .Include(a => a.Category)
                .GroupBy(a => new { a.CategoryId, a.Category.Name, a.Category.Limit })
                .Select(g => new CategoryAnalysisDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    CategoryLimit = g.Key.Limit,
                    TotalAllocated = g.Sum(a => a.Amount),
                    TotalSpent = g.Sum(a => a.Spent),
                    DepartmentCount = g.Select(a => a.DepartmentId).Distinct().Count(),
                    UtilizationPercentage = g.Sum(a => a.Amount) > 0 ? Math.Round((g.Sum(a => a.Spent) / g.Sum(a => a.Amount)) * 100, 2) : 0,
                    RemainingLimit = g.Key.Limit - g.Sum(a => a.Amount)
                })
                .OrderByDescending(c => c.TotalAllocated)
                .ToListAsync();

            return Ok(ApiResponse<IEnumerable<CategoryAnalysisDto>>.Ok(analysis));
        }

        [HttpGet("analytics/requests")]
        public async Task<ActionResult<ApiResponse<RequestAnalyticsDto>>> GetRequestAnalytics(
            [FromQuery] int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var requests = await _db.AdjustmentRequests
                .Include(r => r.Department)
                .Include(r => r.Category)
                .Where(r => r.CreatedAt >= startDate)
                .ToListAsync();

            var analytics = new RequestAnalyticsDto
            {
                TotalRequests = requests.Count,
                ApprovedRequests = requests.Count(r => r.Status == RequestStatus.Approved),
                RejectedRequests = requests.Count(r => r.Status == RequestStatus.Rejected),
                PendingRequests = requests.Count(r => r.Status == RequestStatus.Pending),
                TotalAmountRequested = requests.Sum(r => r.Amount),
                TotalAmountApproved = requests.Where(r => r.Status == RequestStatus.Approved).Sum(r => r.Amount),
                ApprovalRate = requests.Count > 0 ? Math.Round((double)requests.Count(r => r.Status == RequestStatus.Approved) / requests.Count * 100, 2) : 0,
                AverageRequestAmount = requests.Count > 0 ? Math.Round(requests.Average(r => r.Amount), 2) : 0,
                TopRequestingDepartments = requests
                    .GroupBy(r => new { r.DepartmentId, r.Department.Name })
                    .Select(g => new DepartmentRequestSummaryDto
                    {
                        DepartmentId = g.Key.DepartmentId,
                        DepartmentName = g.Key.Name,
                        RequestCount = g.Count(),
                        TotalAmountRequested = g.Sum(r => r.Amount),
                        ApprovedCount = g.Count(r => r.Status == RequestStatus.Approved)
                    })
                    .OrderByDescending(d => d.RequestCount)
                    .Take(5)
                    .ToList()
            };

            return Ok(ApiResponse<RequestAnalyticsDto>.Ok(analytics));
        }

        private async Task<IEnumerable<BudgetAlertDto>> GetBudgetAlertsAsync()
        {
            var allocations = await _db.Allocations
                .Include(a => a.Department)
                .Include(a => a.Category)
                .ToListAsync();

            var alerts = new List<BudgetAlertDto>();

            foreach (var allocation in allocations)
            {
                var utilizationPercentage = allocation.Amount > 0 ? (allocation.Spent / allocation.Amount) * 100 : 0;
                var thresholdPercentage = allocation.Category.ThresholdPercent;

                if (utilizationPercentage >= thresholdPercentage)
                {
                    alerts.Add(new BudgetAlertDto
                    {
                        DepartmentId = allocation.DepartmentId,
                        DepartmentName = allocation.Department.Name,
                        CategoryId = allocation.CategoryId,
                        CategoryName = allocation.Category.Name,
                        AllocatedAmount = allocation.Amount,
                        SpentAmount = allocation.Spent,
                        UtilizationPercentage = Math.Round(utilizationPercentage, 2),
                        ThresholdPercentage = thresholdPercentage,
                        AlertType = utilizationPercentage > 100 ? "Exceeded" : "NearingLimit",
                        Severity = utilizationPercentage > 100 ? "High" : utilizationPercentage >= 90 ? "Medium" : "Low"
                    });
                }
            }

            return alerts.OrderByDescending(a => a.UtilizationPercentage);
        }

        private static decimal CalculatePerformanceScore(DepartmentSummaryDto department)
        {
            if (department.TotalAllocation == 0) return 0;

            var utilizationRate = department.TotalSpent / department.TotalAllocation;

            if (utilizationRate >= 0.7m && utilizationRate <= 0.9m)
                return 100m;
            else if (utilizationRate < 0.7m)
                return utilizationRate * 100m + 30m;
            else
                return Math.Max(0, 100m - ((utilizationRate - 0.9m) * 200m));
        }
    }
}
