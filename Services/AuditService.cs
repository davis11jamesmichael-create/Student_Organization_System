using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Services
{
    /// <summary>
    /// Appends an <see cref="AuditLog"/> row after a successful state-changing
    /// action. Called by controllers after <c>SaveChangesAsync</c> succeeds, so
    /// a failed save never produces a phantom audit entry.
    /// </summary>
    public class AuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RecordAsync(
            int? actorUserId,
            string action,
            string entityName,
            int? entityId = null,
            string? details = null)
        {
            var ip = _httpContextAccessor.HttpContext?
                .Connection.RemoteIpAddress?.ToString();

            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserID = actorUserId,
                Action = action,
                EntityName = entityName,
                EntityID = entityId,
                Details = details,
                IpAddress = ip,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}
