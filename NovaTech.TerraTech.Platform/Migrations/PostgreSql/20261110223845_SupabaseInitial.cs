using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NovaTech.TerraTech.Platform.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SupabaseInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "terratech");

            migrationBuilder.CreateTable(
                name: "products",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sensor_catalog_items",
                schema: "terratech",
                columns: table => new
                {
                    sensor_code = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    mac_address = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    is_demo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_sensor_catalog_items", x => x.sensor_code);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email_verification_exempt = table.Column<bool>(type: "boolean", nullable: false),
                    email_verified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    token_version = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    email_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_email_tokens",
                schema: "terratech",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    purpose = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    requested_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_account_email_tokens", x => new { x.user_id, x.purpose });
                    table.ForeignKey(
                        name: "f_k_account_email_tokens_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "terratech",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventories",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    owner_user_id = table.Column<int>(type: "integer", nullable: true),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    field_id = table.Column<int>(type: "int", nullable: false),
                    profile_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stock_quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    average_weight_per_unit_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_weight_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_inventories", x => x.id);
                    table.ForeignKey(
                        name: "f_k_inventories_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalSchema: "terratech",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    payment_method = table.Column<int>(type: "integer", nullable: false),
                    is_subscription = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_orders", x => x.id);
                    table.ForeignKey(
                        name: "f_k_orders_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "terratech",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    location = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    size_m2 = table.Column<double>(type: "double precision", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    fundo_name = table.Column<string>(type: "text", nullable: false),
                    contact_phone = table.Column<string>(type: "text", nullable: false),
                    moisture_threshold = table.Column<double>(type: "double precision", nullable: false),
                    temp_threshold = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_profiles", x => x.id);
                    table.ForeignKey(
                        name: "f_k_profiles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "terratech",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "comments",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    target_profile_id = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_comments", x => x.id);
                    table.ForeignKey(
                        name: "f_k_comments__profile_target_profile_id",
                        column: x => x.target_profile_id,
                        principalSchema: "terratech",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_comments_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "terratech",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_comments_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "terratech",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "community_profiles",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reputation_score = table.Column<int>(type: "integer", nullable: false),
                    public_bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    visibility_status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_community_profiles", x => x.id);
                    table.ForeignKey(
                        name: "f_k_community_profiles__profile_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "terratech",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fields",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    crop_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_m2 = table.Column<double>(type: "double precision", nullable: false),
                    soil_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    boundary = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_fields", x => x.id);
                    table.ForeignKey(
                        name: "f_k_fields__profile_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "terratech",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_id = table.Column<int>(type: "int", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    is_alert = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_notifications", x => x.id);
                    table.ForeignKey(
                        name: "f_k_notifications__profile_profile_id",
                        column: x => x.profile_id,
                        principalSchema: "terratech",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sensor_code = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    field_id = table.Column<int>(type: "integer", nullable: false),
                    mac_address = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_sync = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_devices", x => x.id);
                    table.ForeignKey(
                        name: "f_k_devices__sensor_catalog_item_sensor_code",
                        column: x => x.sensor_code,
                        principalSchema: "terratech",
                        principalTable: "sensor_catalog_items",
                        principalColumn: "sensor_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_devices_fields_field_id",
                        column: x => x.field_id,
                        principalSchema: "terratech",
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    field_id = table.Column<int>(type: "int", nullable: true),
                    crop_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    historical_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    projected_yield_kg = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    confidence_score = table.Column<double>(type: "double precision", nullable: false),
                    environmental_factor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    factor_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    mean_value = table.Column<double>(type: "double precision", nullable: false),
                    variance = table.Column<double>(type: "double precision", nullable: false),
                    standard_deviation = table.Column<double>(type: "double precision", nullable: false),
                    technical_interpretation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_reports", x => x.id);
                    table.ForeignKey(
                        name: "f_k_reports__field_field_id",
                        column: x => x.field_id,
                        principalSchema: "terratech",
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fertility_zones",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    field_id = table.Column<int>(type: "integer", nullable: false),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    boundary = table.Column<string>(type: "text", nullable: false),
                    moisture_min = table.Column<double>(type: "double precision", nullable: false, defaultValue: 25.0),
                    moisture_max = table.Column<double>(type: "double precision", nullable: false, defaultValue: 75.0),
                    nitrogen_min = table.Column<double>(type: "double precision", nullable: false, defaultValue: 20.0),
                    nitrogen_max = table.Column<double>(type: "double precision", nullable: false, defaultValue: 40.0),
                    phosphorus_min = table.Column<double>(type: "double precision", nullable: false, defaultValue: 15.0),
                    phosphorus_max = table.Column<double>(type: "double precision", nullable: false, defaultValue: 30.0),
                    potassium_min = table.Column<double>(type: "double precision", nullable: false, defaultValue: 30.0),
                    potassium_max = table.Column<double>(type: "double precision", nullable: false, defaultValue: 50.0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_fertility_zones", x => x.id);
                    table.ForeignKey(
                        name: "f_k_fertility_zones__field_field_id",
                        column: x => x.field_id,
                        principalSchema: "terratech",
                        principalTable: "fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_fertility_zones_devices_device_id",
                        column: x => x.device_id,
                        principalSchema: "terratech",
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sensor_readings",
                schema: "terratech",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    moisture_percent = table.Column<double>(type: "double precision", nullable: false),
                    soil_temperature_c = table.Column<double>(type: "double precision", nullable: false),
                    nitrogen_ppm = table.Column<double>(type: "double precision", nullable: false),
                    phosphorus_ppm = table.Column<double>(type: "double precision", nullable: false),
                    potassium_ppm = table.Column<double>(type: "double precision", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_sensor_readings", x => x.id);
                    table.ForeignKey(
                        name: "f_k_sensor_readings_devices_device_id",
                        column: x => x.device_id,
                        principalSchema: "terratech",
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_account_email_tokens_token_hash",
                schema: "terratech",
                table: "account_email_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_comments_product_id",
                schema: "terratech",
                table: "comments",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "i_x_comments_target_profile_id",
                schema: "terratech",
                table: "comments",
                column: "target_profile_id");

            migrationBuilder.CreateIndex(
                name: "i_x_comments_user_id",
                schema: "terratech",
                table: "comments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_community_profiles_profile_id",
                schema: "terratech",
                table: "community_profiles",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "i_x_devices_field_id",
                schema: "terratech",
                table: "devices",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "i_x_devices_mac_address",
                schema: "terratech",
                table: "devices",
                column: "mac_address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_devices_sensor_code",
                schema: "terratech",
                table: "devices",
                column: "sensor_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_fertility_zones_device_id",
                schema: "terratech",
                table: "fertility_zones",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "i_x_fertility_zones_field_id",
                schema: "terratech",
                table: "fertility_zones",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "i_x_fields_profile_id",
                schema: "terratech",
                table: "fields",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "i_x_inventories_owner_user_id",
                schema: "terratech",
                table: "inventories",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_notifications_profile_id",
                schema: "terratech",
                table: "notifications",
                column: "profile_id");

            migrationBuilder.CreateIndex(
                name: "i_x_orders_user_id",
                schema: "terratech",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "i_x_profiles_user_id",
                schema: "terratech",
                table: "profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_reports_field_id",
                schema: "terratech",
                table: "reports",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "i_x_sensor_catalog_items_mac_address",
                schema: "terratech",
                table: "sensor_catalog_items",
                column: "mac_address",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_sensor_readings_device_id_recorded_at",
                schema: "terratech",
                table: "sensor_readings",
                columns: new[] { "device_id", "recorded_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_users_email_address",
                schema: "terratech",
                table: "users",
                column: "email_address",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_email_tokens",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "comments",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "community_profiles",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "fertility_zones",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "inventories",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "sensor_readings",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "products",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "devices",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "sensor_catalog_items",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "fields",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "terratech");

            migrationBuilder.DropTable(
                name: "users",
                schema: "terratech");
        }
    }
}
