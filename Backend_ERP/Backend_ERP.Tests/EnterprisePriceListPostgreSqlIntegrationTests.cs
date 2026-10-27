using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Backend_ERP.Tests
{
    public class EnterprisePriceListPostgreSqlIntegrationTests
    {
        private const string ConnectionString = "Host=localhost;Port=5432;Database=ERP;User Id=postgres;Password=pravin@45";

        [Fact]
        public async Task Postgres_GiST_Exclusion_Rejects_Overlapping_Active_Price_Lists()
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var uniqueNumber1 = "TEST-PL-GIST-1-" + Guid.NewGuid().ToString("N")[..8];
            var uniqueNumber2 = "TEST-PL-GIST-2-" + Guid.NewGuid().ToString("N")[..8];
            var testTenant = "TEST-TENANT-" + Guid.NewGuid().ToString("N")[..6];

            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Insert List 1 (Active, 2026-01-01 to 2026-12-31, INR, Direct, Standard)
                await using (var cmd1 = conn.CreateCommand())
                {
                    cmd1.Transaction = tx;
                    cmd1.CommandText = @"
                        INSERT INTO public.price_lists (
                            ""TenantId"", ""PriceListNumber"", ""PriceListName"", ""CustomerCategory"", 
                            ""Channel"", ""Currency"", ""EffectiveFrom"", ""EffectiveTo"", ""Status"", 
                            ""ValidityPeriod"", ""CreatedDate"", ""UpdatedDate""
                        ) VALUES (
                            @tenant, @num, 'GiST Test 1', 'Standard', 
                            'Direct', 'INR', '2026-01-01', '2026-12-31', 'Active', 
                            daterange('2026-01-01'::date, '2026-12-31'::date, '[]'), NOW(), NOW()
                        );";
                    cmd1.Parameters.AddWithValue("tenant", testTenant);
                    cmd1.Parameters.AddWithValue("num", uniqueNumber1);
                    await cmd1.ExecuteNonQueryAsync();
                }

                // Insert List 2 (Active, OVERLAPPING: 2026-06-01 to 2027-06-01, identical Tenant, Category, Channel, Currency)
                await using (var cmd2 = conn.CreateCommand())
                {
                    cmd2.Transaction = tx;
                    cmd2.CommandText = @"
                        INSERT INTO public.price_lists (
                            ""TenantId"", ""PriceListNumber"", ""PriceListName"", ""CustomerCategory"", 
                            ""Channel"", ""Currency"", ""EffectiveFrom"", ""EffectiveTo"", ""Status"", 
                            ""ValidityPeriod"", ""CreatedDate"", ""UpdatedDate""
                        ) VALUES (
                            @tenant, @num, 'GiST Test 2 (Overlap)', 'Standard', 
                            'Direct', 'INR', '2026-06-01', '2027-06-01', 'Active', 
                            daterange('2026-06-01'::date, '2027-06-01'::date, '[]'), NOW(), NOW()
                        );";
                    cmd2.Parameters.AddWithValue("tenant", testTenant);
                    cmd2.Parameters.AddWithValue("num", uniqueNumber2);

                    var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
                    {
                        await cmd2.ExecuteNonQueryAsync();
                    });

                    // 23P01 is the PostgreSQL exclusion_violation error code
                    Assert.Equal("23P01", ex.SqlState);
                    Assert.Contains("exc_price_list_no_overlap", ex.ConstraintName);
                }
            }
            finally
            {
                await tx.RollbackAsync(); // Always rollback so no test data remains
            }
        }

        [Fact]
        public async Task Postgres_MultiTenant_Trigger_Rejects_Invalid_Item_Reference()
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var testTenant = "TEST-TENANT-TRIGGER";
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Create a temporary parent price list
                int parentListId;
                await using (var cmdParent = conn.CreateCommand())
                {
                    cmdParent.Transaction = tx;
                    cmdParent.CommandText = @"
                        INSERT INTO public.price_lists (
                            ""TenantId"", ""PriceListNumber"", ""PriceListName"", ""CustomerCategory"", 
                            ""Channel"", ""Currency"", ""EffectiveFrom"", ""Status"", 
                            ""ValidityPeriod"", ""CreatedDate"", ""UpdatedDate""
                        ) VALUES (
                            @tenant, 'PL-PARENT-TRIG', 'Trigger Test Header', 'Standard', 
                            'Direct', 'INR', '2026-01-01', 'Draft', 
                            daterange('2026-01-01'::date, '2026-12-31'::date, '[]'), NOW(), NOW()
                        ) RETURNING ""Id"";";
                    cmdParent.Parameters.AddWithValue("tenant", testTenant);
                    parentListId = Convert.ToInt32(await cmdParent.ExecuteScalarAsync());
                }

                // Attempt to insert an item pointing to non-existent or cross-tenant FinishedGood ID 999999
                await using (var cmdItem = conn.CreateCommand())
                {
                    cmdItem.Transaction = tx;
                    cmdItem.CommandText = @"
                        INSERT INTO public.price_list_items (
                            ""TenantId"", ""PriceListId"", ""ItemType"", ""ItemId"", ""ItemCode"", ""ItemName"",
                            ""BasePrice"", ""SellingPrice"", ""FloorPrice"", ""BaseUom"", ""PricingUom""
                        ) VALUES (
                            @tenant, @plId, 'FinishedGood', 999999, 'INVALID-ITEM-CODE', 'NonExistent Item',
                            100, 100, 80, 'Nos', 'Nos'
                        );";
                    cmdItem.Parameters.AddWithValue("tenant", testTenant);
                    cmdItem.Parameters.AddWithValue("plId", parentListId);

                    var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
                    {
                        await cmdItem.ExecuteNonQueryAsync();
                    });

                    // Trigger raises custom exception
                    Assert.Contains("FinishedGood ID 999999", ex.MessageText);
                }
            }
            finally
            {
                await tx.RollbackAsync();
            }
        }

        [Fact]
        public async Task Postgres_Real_GetAll_And_Dashboard_Diagnose()
        {
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ERP.Infrastructure.Data.ERPDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            using var db = new ERP.Infrastructure.Data.ERPDbContext(options);
            var repo = new ERP.Infrastructure.Sales.PriceListRepository(db);
            var numbering = new ERP.Infrastructure.Sales.PriceListNumberingService(db);
            var service = new ERP.Infrastructure.Sales.PriceListService(repo, numbering);

            // Test GetAllAsync
            var list = await service.GetAllAsync(null);
            Assert.NotNull(list);

            // Test GetDashboardAsync
            var dashboard = await service.GetDashboardAsync();
            Assert.NotNull(dashboard);
        }
    }
}
