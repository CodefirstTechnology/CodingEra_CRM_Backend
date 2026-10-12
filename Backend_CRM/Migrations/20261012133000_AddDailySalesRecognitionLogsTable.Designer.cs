using CRM.DATA;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend_CRM.Migrations
{
    [DbContext(typeof(TaskDbcontext))]
    [Migration("20261012133000_AddDailySalesRecognitionLogsTable")]
    partial class AddDailySalesRecognitionLogsTable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");
#pragma warning restore 612, 618
        }
    }
}
