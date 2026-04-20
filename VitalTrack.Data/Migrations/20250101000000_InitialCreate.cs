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
            // Users table
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id             = table.Column<int>(type: "int", nullable: false)
                                         .Annotation("SqlServer:Identity", "1, 1"),
                    Username       = table.Column<string>(type: "nvarchar(50)",  maxLength: 50,  nullable: false),
                    FullName       = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash   = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email          = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Gender         = table.Column<string>(type: "nvarchar(max)",               nullable: false),
                    Role           = table.Column<string>(type: "nvarchar(max)",               nullable: false),
                    Age            = table.Column<int>(type: "int",                            nullable: false),
                    HeightCm       = table.Column<double>(type: "float",                       nullable: false),
                    WeightKg       = table.Column<double>(type: "float",                       nullable: false),
                    BodyFatPercent = table.Column<double>(type: "float",                       nullable: false),
                    ActivityLevel  = table.Column<string>(type: "nvarchar(max)",               nullable: false),
                    CreatedAt      = table.Column<DateTime>(type: "datetime2",                 nullable: false),
                    UpdatedAt      = table.Column<DateTime>(type: "datetime2",                 nullable: false)
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

            // WorkoutSessions table
            migrationBuilder.CreateTable(
                name: "WorkoutSessions",
                columns: table => new
                {
                    Id              = table.Column<int>(type: "int",          nullable: false)
                                          .Annotation("SqlServer:Identity", "1, 1"),
                    UserId          = table.Column<int>(type: "int",          nullable: false),
                    ExerciseName    = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MuscleGroup     = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sets            = table.Column<int>(type: "int",          nullable: false),
                    Reps            = table.Column<int>(type: "int",          nullable: false),
                    WeightKg        = table.Column<double>(type: "float",     nullable: false),
                    DurationMinutes = table.Column<int>(type: "int",          nullable: false),
                    CaloriesBurned  = table.Column<double>(type: "float",     nullable: false),
                    Notes           = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoggedAt        = table.Column<DateTime>(type: "datetime2",   nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_UserId",
                table: "WorkoutSessions",
                column: "UserId");

            // NutritionLogs table
            migrationBuilder.CreateTable(
                name: "NutritionLogs",
                columns: table => new
                {
                    Id           = table.Column<int>(type: "int",      nullable: false)
                                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId       = table.Column<int>(type: "int",      nullable: false),
                    FoodName     = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MealType     = table.Column<string>(type: "nvarchar(20)",  maxLength: 20,  nullable: false),
                    Calories     = table.Column<double>(type: "float", nullable: false),
                    ProteinG     = table.Column<double>(type: "float", nullable: false),
                    CarbsG       = table.Column<double>(type: "float", nullable: false),
                    FatG         = table.Column<double>(type: "float", nullable: false),
                    ServingGrams = table.Column<double>(type: "float", nullable: false),
                    LoggedAt     = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionLogs_UserId",
                table: "NutritionLogs",
                column: "UserId");

            // ChatLogs table
            migrationBuilder.CreateTable(
                name: "ChatLogs",
                columns: table => new
                {
                    Id          = table.Column<int>(type: "int",      nullable: false)
                                       .Annotation("SqlServer:Identity", "1, 1"),
                    UserId      = table.Column<int>(type: "int",      nullable: false),
                    UserMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BotResponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt   = table.Column<DateTime>(type: "datetime2",   nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatLogs_UserId",
                table: "ChatLogs",
                column: "UserId");

            // Seed default admin — BCrypt hash of "Admin@2025"
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] {
                    "Id","Username","FullName","PasswordHash","Email",
                    "Gender","Role","Age","HeightCm","WeightKg",
                    "BodyFatPercent","ActivityLevel","CreatedAt","UpdatedAt"
                },
                values: new object[] {
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
            migrationBuilder.DropTable(name: "ChatLogs");
            migrationBuilder.DropTable(name: "NutritionLogs");
            migrationBuilder.DropTable(name: "WorkoutSessions");
            migrationBuilder.DropTable(name: "Users");
        }
    }
}
