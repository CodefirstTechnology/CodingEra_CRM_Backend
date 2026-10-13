using CRM.DATA;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    public partial class AddDailySalesRecognitionLogsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS daily_sales_recognition_logs (
                    id SERIAL PRIMARY KEY,
                    date TIMESTAMPTZ NOT NULL,
                    executive_user_id INT NULL,
                    executive_name VARCHAR(150) NOT NULL,
                    new_leads INT NOT NULL DEFAULT 0,
                    customer_calls INT NOT NULL DEFAULT 0,
                    meetings INT NOT NULL DEFAULT 0,
                    follow_ups INT NOT NULL DEFAULT 0,
                    quotations INT NOT NULL DEFAULT 0,
                    lead_conversions INT NOT NULL DEFAULT 0,
                    orders_booked INT NOT NULL DEFAULT 0,
                    payment_follow_ups INT NOT NULL DEFAULT 0,
                    reporting_percentage NUMERIC(5,2) NOT NULL DEFAULT 100.0,
                    team_support INT NOT NULL DEFAULT 1,
                    lead_quality_percentage NUMERIC(5,2) NOT NULL DEFAULT 0.0,
                    daily_activity_score NUMERIC(5,2) NOT NULL DEFAULT 0.0,
                    business_result_score NUMERIC(5,2) NOT NULL DEFAULT 0.0,
                    overall_score NUMERIC(5,2) NOT NULL DEFAULT 0.0,
                    recognition VARCHAR(50) NOT NULL DEFAULT 'Improve',
                    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS ""IX_daily_sales_recognition_logs_date"" ON daily_sales_recognition_logs (date DESC);
                CREATE INDEX IF NOT EXISTS ""IX_daily_sales_recognition_logs_executive"" ON daily_sales_recognition_logs (executive_name);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS daily_sales_recognition_logs;");
        }
    }
}
