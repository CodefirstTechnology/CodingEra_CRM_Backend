using Microsoft.EntityFrameworkCore;

namespace HRMS.Data;

public static class AttendanceSchemaBootstrap
{
    public static async Task EnsureAsync(HRMSDbContext db, CancellationToken cancellationToken = default)
    {
        const string sql = """
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS break_duration_minutes integer;
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS is_late boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS is_early_leave boolean NOT NULL DEFAULT FALSE;
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS clock_in_device_at timestamp with time zone;
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS clock_in_server_at timestamp with time zone;
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS clock_out_device_at timestamp with time zone;
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS clock_out_server_at timestamp with time zone;
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC');
            ALTER TABLE attendance_records ADD COLUMN IF NOT EXISTS updated_at timestamp with time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC');
            """;

        await db.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}
