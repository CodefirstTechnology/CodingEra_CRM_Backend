using CRM.DATA;
using CRM.models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Helpers
{
    /// <summary>Idempotent seed for built-in user target types (includes Custom).</summary>
    public static class UserTargetTypeSeed
    {
        public static async Task EnsureAsync(
            TaskDbcontext db,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                var seeds = new (string Name, string Description, int SortOrder)[]
                {
                    ("Hourly Target", "Sales target measured per hour", 1),
                    ("Daily Target", "Sales target measured per day", 2),
                    ("Weekly Target", "Sales target measured per week", 3),
                    ("Monthly Target", "Sales target measured per month", 4),
                    ("Custom Target", "Sales target with a custom start and end date", 5),
                };

                var existing = await db.UserTargetTypes.AsNoTracking()
                    .Select(t => t.Name.ToLower())
                    .ToListAsync(cancellationToken);
                var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

                var added = 0;
                foreach (var (name, description, sortOrder) in seeds)
                {
                    if (existingSet.Contains(name)) continue;

                    db.UserTargetTypes.Add(new UserTargetType
                    {
                        Name = name,
                        Description = description,
                        SortOrder = sortOrder,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                        LastModified = now,
                    });
                    added++;
                }

                if (added > 0)
                {
                    await db.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("User target type seed: added {Count} type(s).", added);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "User target type seed could not complete (tables may not exist yet).");
            }
        }
    }
}
