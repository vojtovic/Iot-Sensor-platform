using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buildings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_buildings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sensor_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    min_valid = table.Column<double>(type: "double precision", nullable: false),
                    max_valid = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensor_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    building_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    floor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rooms", x => x.id);
                    table.ForeignKey(
                        name: "fk_rooms_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    hardware_id = table.Column<string>(type: "text", nullable: false),
                    room_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    firmware_version = table.Column<string>(type: "text", nullable: true),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    credentials_issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_devices_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "actuators",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actuators", x => x.id);
                    table.ForeignKey(
                        name: "fk_actuators_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_configurations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_device_configurations", x => x.id);
                    table.ForeignKey(
                        name: "fk_device_configurations_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sensors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    sensor_type_id = table.Column<int>(type: "integer", nullable: false),
                    calibration_offset = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sensors", x => x.id);
                    table.ForeignKey(
                        name: "fk_sensors_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sensors_sensor_types_sensor_type_id",
                        column: x => x.sensor_type_id,
                        principalTable: "sensor_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "actuator_commands",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    actuator_id = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actuator_commands", x => x.id);
                    table.ForeignKey(
                        name: "fk_actuator_commands_actuators_actuator_id",
                        column: x => x.actuator_id,
                        principalTable: "actuators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "measurements",
                columns: table => new
                {
                    time = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    sensor_id = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false),
                    quality = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_measurements", x => new { x.sensor_id, x.time });
                    table.ForeignKey(
                        name: "fk_measurements_sensors_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_actuator_commands_actuator_id",
                table: "actuator_commands",
                column: "actuator_id");

            migrationBuilder.CreateIndex(
                name: "ix_actuators_device_id_channel",
                table: "actuators",
                columns: new[] { "device_id", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_configurations_device_id",
                table: "device_configurations",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_devices_hardware_id",
                table: "devices",
                column: "hardware_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_room_id",
                table: "devices",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_rooms_building_id",
                table: "rooms",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_types_code",
                table: "sensor_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sensors_device_id_channel",
                table: "sensors",
                columns: new[] { "device_id", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sensors_sensor_type_id",
                table: "sensors",
                column: "sensor_type_id");

            migrationBuilder.Sql("SELECT create_hypertable('measurements', by_range('time'), if_not_exists => TRUE);");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "actuator_commands");

            migrationBuilder.DropTable(
                name: "device_configurations");

            migrationBuilder.DropTable(
                name: "measurements");

            migrationBuilder.DropTable(
                name: "actuators");

            migrationBuilder.DropTable(
                name: "sensors");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "sensor_types");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "buildings");
        }
    }
}
