using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceConfigurationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_device_configurations_device_id",
                table: "device_configurations");

            migrationBuilder.CreateIndex(
                name: "ix_device_configurations_device_id_version",
                table: "device_configurations",
                columns: new[] { "device_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_device_configurations_device_id_version",
                table: "device_configurations");

            migrationBuilder.CreateIndex(
                name: "ix_device_configurations_device_id",
                table: "device_configurations",
                column: "device_id");
        }
    }
}
