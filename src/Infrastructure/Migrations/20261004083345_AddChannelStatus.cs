using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pending_capabilities",
                table: "devices");

            migrationBuilder.AddColumn<int>(
                name: "channel_status",
                table: "sensors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "channel_status",
                table: "actuators",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "channel_status",
                table: "sensors");

            migrationBuilder.DropColumn(
                name: "channel_status",
                table: "actuators");

            migrationBuilder.AddColumn<string>(
                name: "pending_capabilities",
                table: "devices",
                type: "jsonb",
                nullable: true);
        }
    }
}
