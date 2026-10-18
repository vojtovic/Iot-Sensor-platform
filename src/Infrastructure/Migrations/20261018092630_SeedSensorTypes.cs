using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedSensorTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO sensor_types (code, unit, min_valid, max_valid) VALUES
                    ('temperature', '°C',  -40,   85),
                    ('humidity',    '%',     0,  100),
                    ('co2',         'ppm',   0, 5000),
                    ('sound_level', 'ADC',   0, 4095)
                ON CONFLICT (code) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
