using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddCostTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostPriceAtSale",
                table: "AppOrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppProductVariantCostHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousCostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NewCostPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProductVariantCostHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreManagement_ProductVariantCostHistories_CreationTime",
                table: "AppProductVariantCostHistories",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_StoreManagement_ProductVariantCostHistories_ProductVariantId",
                table: "AppProductVariantCostHistories",
                column: "ProductVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppProductVariantCostHistories");

            migrationBuilder.DropColumn(
                name: "CostPriceAtSale",
                table: "AppOrderItems");
        }
    }
}
