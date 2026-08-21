using CRM.DATA;
using CRM.DTO;
using CRM.models;
using CRM.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend_CRM.Tests
{
    public class MasterDataAdminServiceTests
    {
        private static TaskDbcontext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<TaskDbcontext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new TaskDbcontext(options);
        }

        [Fact]
        public void IsSupportedEntity_RecognizesSourcesAndLeadSources()
        {
            using var db = CreateInMemoryDbContext(nameof(IsSupportedEntity_RecognizesSourcesAndLeadSources));
            var service = new MasterDataAdminService(db);

            Assert.True(service.IsSupportedEntity("sources"));
            Assert.True(service.IsSupportedEntity("SOURCES"));
            Assert.True(service.IsSupportedEntity("lead-sources"));
            Assert.True(service.IsSupportedEntity("lead-statuses"));
            Assert.False(service.IsSupportedEntity("invalid-entity"));
        }

        [Fact]
        public async Task CreateAsync_CreatesSource_AndPreventsDuplicates()
        {
            using var db = CreateInMemoryDbContext(nameof(CreateAsync_CreatesSource_AndPreventsDuplicates));
            var service = new MasterDataAdminService(db);

            var (created, err) = await service.CreateAsync("sources", new MasterDataUpsertDto
            {
                Name = "Partner Portal",
                Description = "Partner generated leads",
                IsActive = true,
                SortOrder = 10,
            });

            Assert.Null(err);
            Assert.NotNull(created);
            Assert.Equal("Partner Portal", created.Name);
            Assert.Equal(10, created.SortOrder);

            // Duplicate name check
            var (dup, dupErr) = await service.CreateAsync("sources", new MasterDataUpsertDto
            {
                Name = "partner portal",
                Description = "Different description",
                IsActive = true,
            });

            Assert.NotNull(dupErr);
            Assert.Null(dup);
            Assert.Contains("already exists", dupErr, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteAsync_Source_SuccessWhenUnused()
        {
            using var db = CreateInMemoryDbContext(nameof(DeleteAsync_Source_SuccessWhenUnused));
            var service = new MasterDataAdminService(db);

            var (created, _) = await service.CreateAsync("sources", new MasterDataUpsertDto
            {
                Name = "Cold Call",
                Description = "Outreach",
                IsActive = true,
            });

            Assert.NotNull(created);

            var (deleted, error, notFound) = await service.DeleteAsync("sources", created.Id);
            Assert.True(deleted);
            Assert.Null(error);
            Assert.False(notFound);

            var list = await service.ListAsync("sources", activeOnly: false);
            Assert.DoesNotContain(list, s => s.Id == created.Id);
        }

        [Fact]
        public async Task DeleteAsync_Source_BlockedWhenUsedByLead()
        {
            using var db = CreateInMemoryDbContext(nameof(DeleteAsync_Source_BlockedWhenUsedByLead));
            var service = new MasterDataAdminService(db);

            var source = new LeadSource
            {
                Id = 1,
                Name = "IndiaMART",
                Description = "Marketplace",
                IsActive = true,
            };
            db.LeadSources.Add(source);

            db.Leads.Add(new Lead
            {
                Id = 101,
                LeadSource = "indiamart",
                FirstName = "Jane",
                LastName = "Doe",
            });
            await db.SaveChangesAsync();

            var (deleted, error, notFound) = await service.DeleteAsync("sources", source.Id);

            Assert.False(deleted);
            Assert.False(notFound);
            Assert.NotNull(error);
            Assert.Contains("Cannot delete: This source is assigned to existing leads", error);
        }

        [Fact]
        public async Task DeleteAsync_LeadStatus_BlockedWhenConversionStatus()
        {
            using var db = CreateInMemoryDbContext(nameof(DeleteAsync_LeadStatus_BlockedWhenConversionStatus));
            var service = new MasterDataAdminService(db);

            var status = new LeadStatus
            {
                Id = 1,
                Name = "Converted to Deal",
                IsConversionStatus = true,
                IsActive = true,
            };
            db.LeadStatuses.Add(status);
            await db.SaveChangesAsync();

            var (deleted, error, notFound) = await service.DeleteAsync("lead-statuses", status.Id);

            Assert.False(deleted);
            Assert.False(notFound);
            Assert.NotNull(error);
            Assert.Contains("conversion status", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteAsync_DealStatus_BlockedWhenUsedByDeal()
        {
            using var db = CreateInMemoryDbContext(nameof(DeleteAsync_DealStatus_BlockedWhenUsedByDeal));
            var service = new MasterDataAdminService(db);

            var status = new DealStatus
            {
                Id = 2,
                Name = "Negotiation",
                IsActive = true,
            };
            db.DealStatuses.Add(status);

            db.Deals.Add(new Deal
            {
                Id = 201,
                DealStatusId = 2,
                Status = "Negotiation",
                OrganizationName = "Global Inc",
                FirstName = "John",
                LastName = "Smith",
            });
            await db.SaveChangesAsync();

            var (deleted, error, notFound) = await service.DeleteAsync("deal-statuses", status.Id);

            Assert.False(deleted);
            Assert.False(notFound);
            Assert.NotNull(error);
            Assert.Contains("assigned to existing deals", error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteAsync_Industry_BlockedWhenUsedByOrganization()
        {
            using var db = CreateInMemoryDbContext(nameof(DeleteAsync_Industry_BlockedWhenUsedByOrganization));
            var service = new MasterDataAdminService(db);

            var industry = new Industry
            {
                Id = 5,
                Name = "Healthcare",
                IsActive = true,
            };
            db.Industries.Add(industry);

            db.Organizations.Add(new Organization
            {
                Id = 501,
                Name = "Health Plus",
                IndustryId = 5,
            });
            await db.SaveChangesAsync();

            var (deleted, error, notFound) = await service.DeleteAsync("industries", industry.Id);

            Assert.False(deleted);
            Assert.False(notFound);
            Assert.NotNull(error);
            Assert.Contains("assigned to existing organizations", error, StringComparison.OrdinalIgnoreCase);
        }
    }
}
