using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Azma.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HeroSlidesAndSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CoursesEnabled",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShopEnabled",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "SliderIntervalSeconds",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.CreateTable(
                name: "HeroSlides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Subtitle = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ButtonText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ButtonUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeroSlides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeroSlides_IsActive_SortOrder",
                table: "HeroSlides",
                columns: new[] { "IsActive", "SortOrder" });

            // The old one-paragraph seed terms are replaced by the built-in full terms page (admins can still set custom text).
            migrationBuilder.Sql("UPDATE [SiteSettings] SET [TermsText] = NULL WHERE [TermsText] LIKE N'%پس از پرداخت موفق و دریافت کد پیگیری قطعی می%' AND LEN([TermsText]) < 400;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeroSlides");

            migrationBuilder.DropColumn(
                name: "CoursesEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShopEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "SliderIntervalSeconds",
                table: "SiteSettings");
        }
    }
}
