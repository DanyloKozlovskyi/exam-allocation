using System;
using System.Collections.Generic;
using Domain.Allocations;
using Domain.Rooms;
using Domain.Rules.Parameters;
using Domain.SolveRuns;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exam_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    exam_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exam_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    name_last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_middle_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    school = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    needs_accessible_seat = table.Column<bool>(type: "boolean", nullable: false),
                    attributes = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_participants_exam_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "exam_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    parameters = table.Column<RuleParameters>(type: "jsonb", nullable: false),
                    is_hard = table.Column<bool>(type: "boolean", nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_rules_exam_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "exam_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "solve_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    options = table.Column<SolveRunOptions>(type: "jsonb", nullable: false),
                    input_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    diagnostics = table.Column<List<SolveDiagnostic>>(type: "jsonb", nullable: false),
                    stats = table.Column<SolveRunStats>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solve_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_solve_runs_exam_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "exam_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "test_centers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    location_latitude = table.Column<double>(type: "double precision", nullable: false),
                    location_longitude = table.Column<double>(type: "double precision", nullable: false),
                    google_place_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    cost_per_room = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_test_centers", x => x.id);
                    table.ForeignKey(
                        name: "fk_test_centers_exam_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "exam_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    objective = table.Column<AllocationObjective>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allocations", x => x.id);
                    table.ForeignKey(
                        name: "fk_allocations_solve_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "solve_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    center_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    layout = table.Column<RoomLayout>(type: "jsonb", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    accessible_capacity = table.Column<int>(type: "integer", nullable: false),
                    cost_override = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rooms", x => x.id);
                    table.ForeignKey(
                        name: "fk_rooms_test_centers_center_id",
                        column: x => x.center_id,
                        principalTable: "test_centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    column = table.Column<int>(type: "integer", nullable: false),
                    seat_index = table.Column<int>(type: "integer", nullable: false),
                    is_accessible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seats", x => x.id);
                    table.ForeignKey(
                        name: "fk_seats_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "allocation_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seat_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allocation_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_allocation_items_allocations_allocation_id",
                        column: x => x.allocation_id,
                        principalTable: "allocations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_allocation_items_participants_participant_id",
                        column: x => x.participant_id,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_allocation_items_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_allocation_items_seats_seat_id",
                        column: x => x.seat_id,
                        principalTable: "seats",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pins",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seat_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pins", x => x.id);
                    table.ForeignKey(
                        name: "fk_pins_exam_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "exam_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pins_participants_participant_id",
                        column: x => x.participant_id,
                        principalTable: "participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pins_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pins_seats_seat_id",
                        column: x => x.seat_id,
                        principalTable: "seats",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_allocation_items_allocation_id_participant_id",
                table: "allocation_items",
                columns: new[] { "allocation_id", "participant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_allocation_items_allocation_id_room_id",
                table: "allocation_items",
                columns: new[] { "allocation_id", "room_id" });

            migrationBuilder.CreateIndex(
                name: "ix_allocation_items_participant_id",
                table: "allocation_items",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "ix_allocation_items_room_id",
                table: "allocation_items",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_allocation_items_seat_id",
                table: "allocation_items",
                column: "seat_id");

            migrationBuilder.CreateIndex(
                name: "ix_allocations_run_id_index",
                table: "allocations",
                columns: new[] { "run_id", "index" });

            migrationBuilder.CreateIndex(
                name: "ix_exam_sessions_name",
                table: "exam_sessions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participants_session_id_external_id",
                table: "participants",
                columns: new[] { "session_id", "external_id" },
                unique: true,
                filter: "external_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_participants_session_id_group",
                table: "participants",
                columns: new[] { "session_id", "group" });

            migrationBuilder.CreateIndex(
                name: "ix_participants_session_id_school",
                table: "participants",
                columns: new[] { "session_id", "school" });

            migrationBuilder.CreateIndex(
                name: "ix_pins_participant_id",
                table: "pins",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "ix_pins_room_id",
                table: "pins",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_pins_seat_id",
                table: "pins",
                column: "seat_id");

            migrationBuilder.CreateIndex(
                name: "ix_pins_session_id_participant_id",
                table: "pins",
                columns: new[] { "session_id", "participant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rooms_center_id_name",
                table: "rooms",
                columns: new[] { "center_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rules_session_id",
                table: "rules",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_seats_room_id_key",
                table: "seats",
                columns: new[] { "room_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_solve_runs_session_id_created_at",
                table: "solve_runs",
                columns: new[] { "session_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_test_centers_session_id_name",
                table: "test_centers",
                columns: new[] { "session_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allocation_items");

            migrationBuilder.DropTable(
                name: "pins");

            migrationBuilder.DropTable(
                name: "rules");

            migrationBuilder.DropTable(
                name: "allocations");

            migrationBuilder.DropTable(
                name: "participants");

            migrationBuilder.DropTable(
                name: "seats");

            migrationBuilder.DropTable(
                name: "solve_runs");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "test_centers");

            migrationBuilder.DropTable(
                name: "exam_sessions");
        }
    }
}
