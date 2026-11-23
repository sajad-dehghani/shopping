using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Azma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CardToCardAndTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Brand name: "فروشگاه آزما" (only if the admin has not renamed the site).
            migrationBuilder.Sql("UPDATE [SiteSettings] SET [SiteName] = N'فروشگاه آزما' WHERE [SiteName] = N'آموزشگاه آزما';");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardHolderName",
                table: "SiteSettings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "SiteSettings",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CardToCardEnabled",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ShebaNumber",
                table: "SiteSettings",
                type: "varchar(40)",
                unicode: false,
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PayerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PayerMobile = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    CardLast4 = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: true),
                    BankReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReceipts", x => x.Id);
                    table.CheckConstraint("CK_PaymentReceipts_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_PaymentReceipts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Mobile = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TicketMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsFromStaff = table.Column<bool>(type: "bit", nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketMessages_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_OrderId",
                table: "PaymentReceipts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_Status_CreatedAt",
                table: "PaymentReceipts",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketMessages_TicketId",
                table: "TicketMessages",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Mobile",
                table: "Tickets",
                column: "Mobile");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_OrderId",
                table: "Tickets",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PublicId",
                table: "Tickets",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Status_LastActivityAt",
                table: "Tickets",
                columns: new[] { "Status", "LastActivityAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketNumber",
                table: "Tickets",
                column: "TicketNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentReceipts");

            migrationBuilder.DropTable(
                name: "TicketMessages");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CardHolderName",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CardToCardEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShebaNumber",
                table: "SiteSettings");
        }
    }
}
