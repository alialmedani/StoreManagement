using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReferenceId",
                table: "AppStockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceType",
                table: "AppStockMovements",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_StoreManagement_StockMovements_ReferenceId",
                table: "AppStockMovements",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreManagement_StockMovements_SourceType",
                table: "AppStockMovements",
                column: "SourceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreManagement_StockMovements_ReferenceId",
                table: "AppStockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StoreManagement_StockMovements_SourceType",
                table: "AppStockMovements");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "AppStockMovements");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "AppStockMovements");
        }
    }
}
