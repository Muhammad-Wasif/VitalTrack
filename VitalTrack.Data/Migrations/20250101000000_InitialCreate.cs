using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VitalTrack.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id             = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Username       = table.Column<string>(maxLength: 50,  nullable: false),
                    FullName       = table.Column<string>(maxLength: 100, nullable: false),
                    PasswordHash   = table.Column<string>(maxLength: 256, nullable: false),
                    Email          = table.Column<string>(maxLength: 150, nullable: false),
                    Gender         = table.Column<string>(nullable: false),
                    Role           = table.Column<string>(nullable: false),
                    Age            = table.Column<int>(nullable: false),
                    HeightCm       = table.Column<double>(nullable: false),
                    WeightKg       = table.Column<double>(nullable: false),
                    BodyFatPercent = table.Column<double>(nullable: false),
                    ActivityLevel  = table.Column<string>(nullable: false),
                    CreatedAt      = table.Column<DateTime>(nullable: false),
                    UpdatedAt      = table.Column<DateTime>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateTable(
                name: "WorkoutSessions",
                columns: table => new
                {
                    Id              = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    UserId          = table.Column<int>(nullable: false),
                    ExerciseName    = table.Column<string>(maxLength: 100, nullable: false),
                    MuscleGroup     = table.Column<string>(nullable: false),
                    Sets            = table.Column<int>(nullable: false),
                    Reps            = table.Column<int>(nullable: false),
                    WeightKg        = table.Column<double>(nullable: false),
                    DurationMinutes = table.Column<int>(nullable: false),
                    CaloriesBurned  = table.Column<double>(nullable: false),
                    Notes           = table.Column<string>(nullable: false),
                    LoggedAt        = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessions", x => x.Id);
                    table.ForeignKey("FK_WorkoutSessions_Users_UserId",
                        x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NutritionLogs",
                columns: table => new
                {
                    Id           = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    UserId       = table.Column<int>(nullable: false),
                    FoodName     = table.Column<string>(maxLength: 150, nullable: false),
                    MealType     = table.Column<string>(maxLength: 20,  nullable: false),
                    Calories     = table.Column<double>(nullable: false),
                    ProteinG     = table.Column<double>(nullable: false),
                    CarbsG       = table.Column<double>(nullable: false),
                    FatG         = table.Column<double>(nullable: false),
                    ServingGrams = table.Column<double>(nullable: false),
                    LoggedAt     = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionLogs", x => x.Id);
                    table.ForeignKey("FK_NutritionLogs_Users_UserId",
                        x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatLogs",
                columns: table => new
                {
                    Id          = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    UserId      = table.Column<int>(nullable: false),
                    UserMessage = table.Column<string>(nullable: false),
                    BotResponse = table.Column<string>(nullable: false),
                    CreatedAt   = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatLogs", x => x.Id);
                    table.ForeignKey("FK_ChatLogs_Users_UserId",
                        x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
                });

            // Seed default admin user — static pre-computed BCrypt hash of "Admin@2025"
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id","Username","FullName","PasswordHash","Email","Gender","Role","Age","HeightCm","WeightKg","BodyFatPercent","ActivityLevel","CreatedAt","UpdatedAt" },
                values: new object[]
                {
                    1, "admin", "System Administrator",
                    "$2a$11$K7GQTwq5HZphRK6bRYFh3.Qa5EqT8YHN2a1dEpRmzXN6aZ9uT5jOa",
                    "admin@vitaltrack.com",
                    "Male", "Admin",
                    30, 175.0, 75.0, 15.0, "ModeratelyActive",
                    new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("ChatLogs");
            migrationBuilder.DropTable("NutritionLogs");
            migrationBuilder.DropTable("WorkoutSessions");
            migrationBuilder.DropTable("Users");
        }
    }
}
