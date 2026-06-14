using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "AppOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "AppOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AppOrderPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppOrderPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppOrderPayments_AppOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "AppOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreManagement_Orders_PaymentStatus",
                table: "AppOrders",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_StoreManagement_OrderPayments_CreationTime",
                table: "AppOrderPayments",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_StoreManagement_OrderPayments_OrderId_PaymentDate",
                table: "AppOrderPayments",
                columns: new[] { "OrderId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StoreManagement_OrderPayments_PaymentMethod",
                table: "AppOrderPayments",
                column: "PaymentMethod");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppOrderPayments");

            migrationBuilder.DropIndex(
                name: "IX_StoreManagement_Orders_PaymentStatus",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "AppOrders");
        }
    }
}
