using System;
using EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace EasyPark.Platform.Shared.Infrastructure.Persistence.EFC.Migrations;

/// <summary>
///     Initial schema of the five bounded contexts (8 tables). Written by hand from the EF Core model
///     configuration because the SDK was not available where this package was prepared.
///     See docs/02-MYSQL-REMOTO.md to regenerate it with <c>dotnet ef</c> if the model changes.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261025150000_InitialCreate")]
public class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase().Annotation("MySQL:Charset", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "user_accounts",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                email = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false),
                password_hash = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                role = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                last_login_at = table.Column<DateTime?>(type: "datetime(6)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_accounts", x => x.id);
            })
            .Annotation("MySQL:Charset", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "profiles",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                user_account_id = table.Column<int>(type: "int", nullable: false),
                first_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                phone = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                photo_url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                company_name = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: true),
                job_title = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_profiles", x => x.id);
                table.ForeignKey("fk_profiles_user_accounts_user_account_id", x => x.user_account_id, "user_accounts", "id", onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySQL:Charset", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "vehicles",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                profile_id = table.Column<int?>(type: "int", nullable: true),
                plate_number = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                vehicle_type = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                color = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true),
                is_default = table.Column<bool>(type: "tinyint(1)", nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_vehicles", x => x.id);
                table.ForeignKey("fk_vehicles_profiles_profile_id", x => x.profile_id, "profiles", "id", onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySQL:Charset", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "parking_facilities",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                operator_profile_id = table.Column<int>(type: "int", nullable: false),
                name = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false),
                address = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                latitude = table.Column<decimal?>(type: "decimal(10,7)", nullable: true),
                longitude = table.Column<decimal?>(type: "decimal(10,7)", nullable: true),
                hourly_rate = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                open_time = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                close_time = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_parking_facilities", x => x.id);
                table.ForeignKey("fk_parking_facilities_profiles_operator_profile_id", x => x.operator_profile_id, "profiles", "id", onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySQL:Charset", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "parking_spots",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                facility_id = table.Column<int>(type: "int", nullable: false),
                code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                level = table.Column<int>(type: "int", nullable: false),
                spot_type = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                version = table.Column<int>(type: "int", nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_parking_spots", x => x.id);
                table.ForeignKey("fk_parking_spots_parking_facilities_facility_id", x => x.facility_id, "parking_facilities", "id", onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySQL:Charset", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "reservations",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                driver_profile_id = table.Column<int>(type: "int", nullable: false),
                vehicle_id = table.Column<int>(type: "int", nullable: false),
                parking_facility_id = table.Column<int>(type: "int", nullable: false),
                parking_spot_id = table.Column<int>(type: "int", nullable: false),
                start_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                duration_minutes = table.Column<int>(type: "int", nullable: false),
                estimated_amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_reservations", x => x.id);
                table.ForeignKey("fk_reservations_profiles_driver_profile_id", x => x.driver_profile_id, "profiles", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_reservations_vehicles_vehicle_id", x => x.vehicle_id, "vehicles", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_reservations_parking_facilities_parking_facility_id", x => x.parking_facility_id, "parking_facilities", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_reservations_parking_spots_parking_spot_id", x => x.parking_spot_id, "parking_spots", "id", onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySQL:Charset", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "access_movements",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                parking_facility_id = table.Column<int>(type: "int", nullable: false),
                vehicle_id = table.Column<int>(type: "int", nullable: false),
                plate_number = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false),
                operator_id = table.Column<int>(type: "int", nullable: false),
                movement_type = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                registration_method = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                note = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_access_movements", x => x.id);
                table.ForeignKey("fk_access_movements_parking_facilities_parking_facility_id", x => x.parking_facility_id, "parking_facilities", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_access_movements_vehicles_vehicle_id", x => x.vehicle_id, "vehicles", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_access_movements_user_accounts_operator_id", x => x.operator_id, "user_accounts", "id", onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySQL:Charset", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "parking_stays",
            columns: table => new
            {
                id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                parking_facility_id = table.Column<int>(type: "int", nullable: false),
                parking_spot_id = table.Column<int>(type: "int", nullable: false),
                vehicle_id = table.Column<int>(type: "int", nullable: false),
                reservation_id = table.Column<int?>(type: "int", nullable: true),
                entry_movement_id = table.Column<int>(type: "int", nullable: false),
                exit_movement_id = table.Column<int?>(type: "int", nullable: true),
                charged_amount = table.Column<decimal?>(type: "decimal(10,2)", nullable: true),
                currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false),
                started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                ended_at = table.Column<DateTime?>(type: "datetime(6)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_parking_stays", x => x.id);
                table.ForeignKey("fk_parking_stays_parking_facilities_parking_facility_id", x => x.parking_facility_id, "parking_facilities", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_parking_stays_parking_spots_parking_spot_id", x => x.parking_spot_id, "parking_spots", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_parking_stays_vehicles_vehicle_id", x => x.vehicle_id, "vehicles", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_parking_stays_reservations_reservation_id", x => x.reservation_id, "reservations", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_parking_stays_access_movements_entry_movement_id", x => x.entry_movement_id, "access_movements", "id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("fk_parking_stays_access_movements_exit_movement_id", x => x.exit_movement_id, "access_movements", "id", onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySQL:Charset", "utf8mb4");

        migrationBuilder.CreateIndex("ix_user_accounts_email", "user_accounts", "email", unique: true);

        migrationBuilder.CreateIndex("ix_profiles_user_account_id", "profiles", "user_account_id", unique: true);

        migrationBuilder.CreateIndex("ix_vehicles_plate_number", "vehicles", "plate_number", unique: true);

        migrationBuilder.CreateIndex("ix_vehicles_profile_id", "vehicles", "profile_id", unique: false);

        migrationBuilder.CreateIndex("ix_parking_facilities_operator_profile_id", "parking_facilities", "operator_profile_id", unique: false);

        migrationBuilder.CreateIndex("ix_parking_spots_facility_id_code", "parking_spots", new[] { "facility_id", "code" }, unique: true);

        migrationBuilder.CreateIndex("ix_reservations_code", "reservations", "code", unique: true);

        migrationBuilder.CreateIndex("ix_reservations_driver_profile_id", "reservations", "driver_profile_id", unique: false);

        migrationBuilder.CreateIndex("ix_reservations_vehicle_id_status", "reservations", new[] { "vehicle_id", "status" }, unique: false);

        migrationBuilder.CreateIndex("ix_reservations_parking_spot_id_status", "reservations", new[] { "parking_spot_id", "status" }, unique: false);

        migrationBuilder.CreateIndex("ix_reservations_parking_facility_id_start_at", "reservations", new[] { "parking_facility_id", "start_at" }, unique: false);

        migrationBuilder.CreateIndex("ix_access_movements_parking_facility_id_occurred_at", "access_movements", new[] { "parking_facility_id", "occurred_at" }, unique: false);

        migrationBuilder.CreateIndex("ix_access_movements_vehicle_id", "access_movements", "vehicle_id", unique: false);

        migrationBuilder.CreateIndex("ix_access_movements_operator_id", "access_movements", "operator_id", unique: false);

        migrationBuilder.CreateIndex("ix_parking_stays_entry_movement_id", "parking_stays", "entry_movement_id", unique: true);

        migrationBuilder.CreateIndex("ix_parking_stays_exit_movement_id", "parking_stays", "exit_movement_id", unique: true);

        migrationBuilder.CreateIndex("ix_parking_stays_vehicle_id_exit_movement_id", "parking_stays", new[] { "vehicle_id", "exit_movement_id" }, unique: false);

        migrationBuilder.CreateIndex("ix_parking_stays_parking_spot_id_exit_movement_id", "parking_stays", new[] { "parking_spot_id", "exit_movement_id" }, unique: false);

        migrationBuilder.CreateIndex("ix_parking_stays_parking_facility_id", "parking_stays", "parking_facility_id", unique: false);

        migrationBuilder.CreateIndex("ix_parking_stays_reservation_id", "parking_stays", "reservation_id", unique: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("parking_stays");
        migrationBuilder.DropTable("access_movements");
        migrationBuilder.DropTable("reservations");
        migrationBuilder.DropTable("parking_spots");
        migrationBuilder.DropTable("parking_facilities");
        migrationBuilder.DropTable("vehicles");
        migrationBuilder.DropTable("profiles");
        migrationBuilder.DropTable("user_accounts");
    }
}
