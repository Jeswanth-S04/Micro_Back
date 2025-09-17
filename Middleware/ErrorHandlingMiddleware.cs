using System.Net;
using System.Text.Json;
using BudgetManagementSystem.Api.Helpers;

namespace BudgetManagementSystem.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";
                var resp = ApiResponse<object>.Fail("An error occurred", new { ex.Message });
                await context.Response.WriteAsync(JsonSerializer.Serialize(resp));
            }
        }
    }
}
