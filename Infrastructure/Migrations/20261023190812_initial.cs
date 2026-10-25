using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operaciones");

            migrationBuilder.EnsureSchema(
                name: "historico");

            migrationBuilder.EnsureSchema(
                name: "misc");

            migrationBuilder.EnsureSchema(
                name: "accesos");

            migrationBuilder.CreateTable(
                name: "colores",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_colores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departamentos",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "marcas",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marcas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "nacionalidades",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nacionalidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "niveles_denominacion",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Jerarquia = table.Column<int>(type: "INTEGER", nullable: false),
                    EsAmbulancia = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niveles_denominacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permisos",
                schema: "accesos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prefijos_placa",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Prefijo = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    Patron = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Ejemplo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prefijos_placa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "provincias",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provincias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rangos",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreArmada = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rangos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "regiones_asistencia",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Region = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regiones_asistencia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tipo_cierres",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_cierres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tipo_eventos",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Categoria = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_eventos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tipo_vehiculos",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipo_vehiculos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ciudadanos",
                schema: "historico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FotoIdentificacion = table.Column<string>(type: "TEXT", nullable: true),
                    NacionalidadId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Identificacion = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Apellido = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Sexo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ciudadanos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ciudadanos_nacionalidades_NacionalidadId",
                        column: x => x.NacionalidadId,
                        principalSchema: "misc",
                        principalTable: "nacionalidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_permisos_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "accesos",
                        principalTable: "permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "municipios",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProvinciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_municipios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_municipios_provincias_ProvinciaId",
                        column: x => x.ProvinciaId,
                        principalSchema: "misc",
                        principalTable: "provincias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agentes",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Institucion = table.Column<int>(type: "INTEGER", nullable: false),
                    RangoId = table.Column<int>(type: "INTEGER", nullable: false),
                    AreaOperativa = table.Column<int>(type: "INTEGER", nullable: false),
                    AccesoTotal = table.Column<bool>(type: "INTEGER", nullable: false),
                    Especialidad = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Autorizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Identificacion = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Apellido = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Sexo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agentes_rangos_RangoId",
                        column: x => x.RangoId,
                        principalSchema: "misc",
                        principalTable: "rangos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "accesos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Identificacion = table.Column<string>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: true),
                    Apellido = table.Column<string>(type: "TEXT", nullable: true),
                    Sexo = table.Column<int>(type: "INTEGER", nullable: false),
                    Institucion = table.Column<int>(type: "INTEGER", nullable: false),
                    RangoId = table.Column<int>(type: "INTEGER", nullable: false),
                    DepartamentoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usuarios_departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalSchema: "misc",
                        principalTable: "departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_rangos_RangoId",
                        column: x => x.RangoId,
                        principalSchema: "misc",
                        principalTable: "rangos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tramos",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegionAsistenciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tramos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tramos_regiones_asistencia_RegionAsistenciaId",
                        column: x => x.RegionAsistenciaId,
                        principalSchema: "operaciones",
                        principalTable: "regiones_asistencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "modelos",
                schema: "misc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MarcaId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoVehiculoId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modelos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_modelos_marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalSchema: "misc",
                        principalTable: "marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_modelos_tipo_vehiculos_TipoVehiculoId",
                        column: x => x.TipoVehiculoId,
                        principalSchema: "misc",
                        principalTable: "tipo_vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prefijo_placa_tipo_vehiculo",
                schema: "misc",
                columns: table => new
                {
                    PrefijoPlacaId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoVehiculoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prefijo_placa_tipo_vehiculo", x => new { x.PrefijoPlacaId, x.TipoVehiculoId });
                    table.ForeignKey(
                        name: "FK_prefijo_placa_tipo_vehiculo_prefijos_placa_PrefijoPlacaId",
                        column: x => x.PrefijoPlacaId,
                        principalSchema: "misc",
                        principalTable: "prefijos_placa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prefijo_placa_tipo_vehiculo_tipo_vehiculos_TipoVehiculoId",
                        column: x => x.TipoVehiculoId,
                        principalSchema: "misc",
                        principalTable: "tipo_vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_usuarios_UserId",
                        column: x => x.UserId,
                        principalSchema: "accesos",
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_usuarios_UserId",
                        column: x => x.UserId,
                        principalSchema: "accesos",
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_permisos_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "accesos",
                        principalTable: "permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_usuarios_UserId",
                        column: x => x.UserId,
                        principalSchema: "accesos",
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_usuarios_UserId",
                        column: x => x.UserId,
                        principalSchema: "accesos",
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "denominaciones",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TramoId = table.Column<int>(type: "INTEGER", nullable: false),
                    NivelDenominacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_denominaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_denominaciones_niveles_denominacion_NivelDenominacionId",
                        column: x => x.NivelDenominacionId,
                        principalSchema: "operaciones",
                        principalTable: "niveles_denominacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_denominaciones_tramos_TramoId",
                        column: x => x.TramoId,
                        principalSchema: "operaciones",
                        principalTable: "tramos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventos",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CanalReporte = table.Column<int>(type: "INTEGER", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoCierreId = table.Column<int>(type: "INTEGER", nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    MunicipioId = table.Column<int>(type: "INTEGER", nullable: false),
                    TramoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Comentario = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FechaHoraReporteUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaHoraLlegadaUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaHoraCompletadoUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    Latitud = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false),
                    Longitud = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_eventos_municipios_MunicipioId",
                        column: x => x.MunicipioId,
                        principalSchema: "misc",
                        principalTable: "municipios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_eventos_tipo_cierres_TipoCierreId",
                        column: x => x.TipoCierreId,
                        principalSchema: "operaciones",
                        principalTable: "tipo_cierres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_eventos_tramos_TramoId",
                        column: x => x.TramoId,
                        principalSchema: "operaciones",
                        principalTable: "tramos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehiculos",
                schema: "historico",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TipoId = table.Column<int>(type: "INTEGER", nullable: false),
                    ColorId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModeloId = table.Column<int>(type: "INTEGER", nullable: true),
                    Fabricacion = table.Column<int>(type: "INTEGER", nullable: true),
                    Placa = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehiculos_colores_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "misc",
                        principalTable: "colores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehiculos_modelos_ModeloId",
                        column: x => x.ModeloId,
                        principalSchema: "misc",
                        principalTable: "modelos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_vehiculos_tipo_vehiculos_TipoId",
                        column: x => x.TipoId,
                        principalSchema: "misc",
                        principalTable: "tipo_vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "denominacion_regiones",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DenominacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    RegionMacro = table.Column<int>(type: "INTEGER", nullable: true),
                    RegionAsistenciaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_denominacion_regiones", x => x.Id);
                    table.CheckConstraint("CK_denominacion_regiones_macro_o_asistencia", "([RegionMacro] IS NOT NULL AND [RegionAsistenciaId] IS NULL) OR ([RegionMacro] IS NULL AND [RegionAsistenciaId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_denominacion_regiones_denominaciones_DenominacionId",
                        column: x => x.DenominacionId,
                        principalSchema: "operaciones",
                        principalTable: "denominaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_denominacion_regiones_regiones_asistencia_RegionAsistenciaId",
                        column: x => x.RegionAsistenciaId,
                        principalSchema: "operaciones",
                        principalTable: "regiones_asistencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "denominacion_tramos",
                schema: "operaciones",
                columns: table => new
                {
                    DenominacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    TramoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_denominacion_tramos", x => new { x.DenominacionId, x.TramoId });
                    table.ForeignKey(
                        name: "FK_denominacion_tramos_denominaciones_DenominacionId",
                        column: x => x.DenominacionId,
                        principalSchema: "operaciones",
                        principalTable: "denominaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_denominacion_tramos_tramos_TramoId",
                        column: x => x.TramoId,
                        principalSchema: "operaciones",
                        principalTable: "tramos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "flotas",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", nullable: false),
                    DenominacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_flotas_denominaciones_DenominacionId",
                        column: x => x.DenominacionId,
                        principalSchema: "operaciones",
                        principalTable: "denominaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unidades",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ficha = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Placa = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    EstaDisponible = table.Column<bool>(type: "INTEGER", nullable: false),
                    DenominacionId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_unidades_denominaciones_DenominacionId",
                        column: x => x.DenominacionId,
                        principalSchema: "operaciones",
                        principalTable: "denominaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_tipo_evento",
                schema: "operaciones",
                columns: table => new
                {
                    EventoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoEventoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_tipo_evento", x => new { x.EventoId, x.TipoEventoId });
                    table.ForeignKey(
                        name: "FK_evento_tipo_evento_eventos_EventoId",
                        column: x => x.EventoId,
                        principalSchema: "operaciones",
                        principalTable: "eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evento_tipo_evento_tipo_eventos_TipoEventoId",
                        column: x => x.TipoEventoId,
                        principalSchema: "operaciones",
                        principalTable: "tipo_eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_vehiculo",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Placa = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    TipoVehiculoId = table.Column<int>(type: "INTEGER", nullable: true),
                    MarcaId = table.Column<int>(type: "INTEGER", nullable: true),
                    ModeloId = table.Column<int>(type: "INTEGER", nullable: true),
                    ColorId = table.Column<int>(type: "INTEGER", nullable: true),
                    MarcaTexto = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModeloTexto = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ColorTexto = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VehiculoHistoricoId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_vehiculo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evento_vehiculo_colores_ColorId",
                        column: x => x.ColorId,
                        principalSchema: "misc",
                        principalTable: "colores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_vehiculo_eventos_EventoId",
                        column: x => x.EventoId,
                        principalSchema: "operaciones",
                        principalTable: "eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evento_vehiculo_marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalSchema: "misc",
                        principalTable: "marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_vehiculo_modelos_ModeloId",
                        column: x => x.ModeloId,
                        principalSchema: "misc",
                        principalTable: "modelos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_vehiculo_tipo_vehiculos_TipoVehiculoId",
                        column: x => x.TipoVehiculoId,
                        principalSchema: "misc",
                        principalTable: "tipo_vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_vehiculo_vehiculos_VehiculoHistoricoId",
                        column: x => x.VehiculoHistoricoId,
                        principalSchema: "historico",
                        principalTable: "vehiculos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_unidad",
                schema: "operaciones",
                columns: table => new
                {
                    EventoId = table.Column<int>(type: "INTEGER", nullable: false),
                    UnidadId = table.Column<int>(type: "INTEGER", nullable: false),
                    DenominacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    NivelDenominacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    AgenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_unidad", x => new { x.EventoId, x.UnidadId });
                    table.ForeignKey(
                        name: "FK_evento_unidad_agentes_AgenteId",
                        column: x => x.AgenteId,
                        principalSchema: "operaciones",
                        principalTable: "agentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_unidad_denominaciones_DenominacionId",
                        column: x => x.DenominacionId,
                        principalSchema: "operaciones",
                        principalTable: "denominaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_unidad_eventos_EventoId",
                        column: x => x.EventoId,
                        principalSchema: "operaciones",
                        principalTable: "eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evento_unidad_niveles_denominacion_NivelDenominacionId",
                        column: x => x.NivelDenominacionId,
                        principalSchema: "operaciones",
                        principalTable: "niveles_denominacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_unidad_unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalSchema: "operaciones",
                        principalTable: "unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "historial_denominacion_unidad",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnidadId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoCambio = table.Column<int>(type: "INTEGER", nullable: false),
                    DenominacionAnteriorId = table.Column<int>(type: "INTEGER", nullable: true),
                    DenominacionNuevaId = table.Column<int>(type: "INTEGER", nullable: true),
                    UnidadRelacionadaId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsuarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    Observacion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historial_denominacion_unidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_historial_denominacion_unidad_denominaciones_DenominacionAnteriorId",
                        column: x => x.DenominacionAnteriorId,
                        principalSchema: "operaciones",
                        principalTable: "denominaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historial_denominacion_unidad_denominaciones_DenominacionNuevaId",
                        column: x => x.DenominacionNuevaId,
                        principalSchema: "operaciones",
                        principalTable: "denominaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historial_denominacion_unidad_unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalSchema: "operaciones",
                        principalTable: "unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_historial_denominacion_unidad_unidades_UnidadRelacionadaId",
                        column: x => x.UnidadRelacionadaId,
                        principalSchema: "operaciones",
                        principalTable: "unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unidad_posicion",
                schema: "operaciones",
                columns: table => new
                {
                    UnidadId = table.Column<int>(type: "INTEGER", nullable: false),
                    AgenteId = table.Column<int>(type: "INTEGER", nullable: false),
                    ComponenteId = table.Column<int>(type: "INTEGER", nullable: true),
                    AgenteOcupadoEnDispositivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    ComponenteOcupadoEnDispositivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrecisionMetros = table.Column<double>(type: "REAL", nullable: true),
                    Rumbo = table.Column<double>(type: "REAL", nullable: true),
                    VelocidadKmh = table.Column<double>(type: "REAL", nullable: true),
                    FechaHoraGpsUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaHoraRecibidaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SesionActiva = table.Column<bool>(type: "INTEGER", nullable: false),
                    SesionCerradaUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Latitud = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false),
                    Longitud = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidad_posicion", x => x.UnidadId);
                    table.ForeignKey(
                        name: "FK_unidad_posicion_agentes_AgenteId",
                        column: x => x.AgenteId,
                        principalSchema: "operaciones",
                        principalTable: "agentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unidad_posicion_agentes_ComponenteId",
                        column: x => x.ComponenteId,
                        principalSchema: "operaciones",
                        principalTable: "agentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unidad_posicion_unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalSchema: "operaciones",
                        principalTable: "unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_ciudadano",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rol = table.Column<int>(type: "INTEGER", nullable: false),
                    Identificacion = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Apellido = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Sexo = table.Column<int>(type: "INTEGER", nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    NacionalidadId = table.Column<int>(type: "INTEGER", nullable: true),
                    Edad = table.Column<int>(type: "INTEGER", nullable: true),
                    EventoVehiculoId = table.Column<int>(type: "INTEGER", nullable: true),
                    CiudadanoId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_ciudadano", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evento_ciudadano_ciudadanos_CiudadanoId",
                        column: x => x.CiudadanoId,
                        principalSchema: "historico",
                        principalTable: "ciudadanos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_ciudadano_evento_vehiculo_EventoVehiculoId",
                        column: x => x.EventoVehiculoId,
                        principalSchema: "operaciones",
                        principalTable: "evento_vehiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_ciudadano_eventos_EventoId",
                        column: x => x.EventoId,
                        principalSchema: "operaciones",
                        principalTable: "eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evento_ciudadano_nacionalidades_NacionalidadId",
                        column: x => x.NacionalidadId,
                        principalSchema: "misc",
                        principalTable: "nacionalidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_vehiculo_tipo_evento",
                schema: "operaciones",
                columns: table => new
                {
                    EventoVehiculoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoEventoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_vehiculo_tipo_evento", x => new { x.EventoVehiculoId, x.TipoEventoId });
                    table.ForeignKey(
                        name: "FK_evento_vehiculo_tipo_evento_evento_vehiculo_EventoVehiculoId",
                        column: x => x.EventoVehiculoId,
                        principalSchema: "operaciones",
                        principalTable: "evento_vehiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evento_vehiculo_tipo_evento_tipo_eventos_TipoEventoId",
                        column: x => x.TipoEventoId,
                        principalSchema: "operaciones",
                        principalTable: "tipo_eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_ciudadano_tipo_evento",
                schema: "operaciones",
                columns: table => new
                {
                    EventoCiudadanoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoEventoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_ciudadano_tipo_evento", x => new { x.EventoCiudadanoId, x.TipoEventoId });
                    table.ForeignKey(
                        name: "FK_evento_ciudadano_tipo_evento_evento_ciudadano_EventoCiudadanoId",
                        column: x => x.EventoCiudadanoId,
                        principalSchema: "operaciones",
                        principalTable: "evento_ciudadano",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evento_ciudadano_tipo_evento_tipo_eventos_TipoEventoId",
                        column: x => x.TipoEventoId,
                        principalSchema: "operaciones",
                        principalTable: "tipo_eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evento_evidencias",
                schema: "operaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Ubicacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TamanoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    EventoCiudadanoId = table.Column<int>(type: "INTEGER", nullable: true),
                    EventoVehiculoId = table.Column<int>(type: "INTEGER", nullable: true),
                    RegistradaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_evidencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evento_evidencias_evento_ciudadano_EventoCiudadanoId",
                        column: x => x.EventoCiudadanoId,
                        principalSchema: "operaciones",
                        principalTable: "evento_ciudadano",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_evidencias_evento_vehiculo_EventoVehiculoId",
                        column: x => x.EventoVehiculoId,
                        principalSchema: "operaciones",
                        principalTable: "evento_vehiculo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evento_evidencias_eventos_EventoId",
                        column: x => x.EventoId,
                        principalSchema: "operaciones",
                        principalTable: "eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agentes_AreaOperativa_IsActive",
                schema: "operaciones",
                table: "agentes",
                columns: new[] { "AreaOperativa", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_agentes_Identificacion",
                schema: "operaciones",
                table: "agentes",
                column: "Identificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_agentes_RangoId",
                schema: "operaciones",
                table: "agentes",
                column: "RangoId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ciudadanos_Identificacion",
                schema: "historico",
                table: "ciudadanos",
                column: "Identificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ciudadanos_NacionalidadId",
                schema: "historico",
                table: "ciudadanos",
                column: "NacionalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_denominacion_regiones_DenominacionId_RegionAsistenciaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                columns: new[] { "DenominacionId", "RegionAsistenciaId" },
                unique: true,
                filter: "[RegionAsistenciaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_denominacion_regiones_DenominacionId_RegionMacro",
                schema: "operaciones",
                table: "denominacion_regiones",
                columns: new[] { "DenominacionId", "RegionMacro" },
                unique: true,
                filter: "[RegionMacro] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_denominacion_regiones_RegionAsistenciaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                column: "RegionAsistenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_denominacion_tramos_TramoId",
                schema: "operaciones",
                table: "denominacion_tramos",
                column: "TramoId");

            migrationBuilder.CreateIndex(
                name: "IX_denominaciones_NivelDenominacionId",
                schema: "operaciones",
                table: "denominaciones",
                column: "NivelDenominacionId");

            migrationBuilder.CreateIndex(
                name: "IX_denominaciones_Nombre",
                schema: "operaciones",
                table: "denominaciones",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_denominaciones_TramoId",
                schema: "operaciones",
                table: "denominaciones",
                column: "TramoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_ciudadano_CiudadanoId",
                schema: "operaciones",
                table: "evento_ciudadano",
                column: "CiudadanoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_ciudadano_EventoId",
                schema: "operaciones",
                table: "evento_ciudadano",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_ciudadano_EventoVehiculoId",
                schema: "operaciones",
                table: "evento_ciudadano",
                column: "EventoVehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_ciudadano_Identificacion",
                schema: "operaciones",
                table: "evento_ciudadano",
                column: "Identificacion");

            migrationBuilder.CreateIndex(
                name: "IX_evento_ciudadano_NacionalidadId",
                schema: "operaciones",
                table: "evento_ciudadano",
                column: "NacionalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_ciudadano_tipo_evento_TipoEventoId",
                schema: "operaciones",
                table: "evento_ciudadano_tipo_evento",
                column: "TipoEventoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_evidencias_EventoCiudadanoId",
                schema: "operaciones",
                table: "evento_evidencias",
                column: "EventoCiudadanoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_evidencias_EventoId_RequestId",
                schema: "operaciones",
                table: "evento_evidencias",
                columns: new[] { "EventoId", "RequestId" },
                unique: true,
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_evento_evidencias_EventoId_Tipo",
                schema: "operaciones",
                table: "evento_evidencias",
                columns: new[] { "EventoId", "Tipo" });

            migrationBuilder.CreateIndex(
                name: "IX_evento_evidencias_EventoVehiculoId",
                schema: "operaciones",
                table: "evento_evidencias",
                column: "EventoVehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_tipo_evento_TipoEventoId",
                schema: "operaciones",
                table: "evento_tipo_evento",
                column: "TipoEventoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_unidad_AgenteId",
                schema: "operaciones",
                table: "evento_unidad",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_unidad_DenominacionId_EventoId",
                schema: "operaciones",
                table: "evento_unidad",
                columns: new[] { "DenominacionId", "EventoId" });

            migrationBuilder.CreateIndex(
                name: "IX_evento_unidad_NivelDenominacionId",
                schema: "operaciones",
                table: "evento_unidad",
                column: "NivelDenominacionId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_unidad_UnidadId",
                schema: "operaciones",
                table: "evento_unidad",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "UX_evento_unidad_principal",
                schema: "operaciones",
                table: "evento_unidad",
                column: "EventoId",
                unique: true,
                filter: "[Rol] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_evento_vehiculo_ColorId",
                schema: "operaciones",
                table: "evento_vehiculo",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_vehiculo_EventoId",
                schema: "operaciones",
                table: "evento_vehiculo",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_vehiculo_MarcaId",
                schema: "operaciones",
                table: "evento_vehiculo",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_vehiculo_ModeloId",
                schema: "operaciones",
                table: "evento_vehiculo",
                column: "ModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_vehiculo_Placa",
                schema: "operaciones",
                table: "evento_vehiculo",
                column: "Placa");

            migrationBuilder.CreateIndex(
                name: "IX_evento_vehiculo_TipoVehiculoId",
                schema: "operaciones",
                table: "evento_vehiculo",
                column: "TipoVehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_vehiculo_VehiculoHistoricoId",
                schema: "operaciones",
                table: "evento_vehiculo",
                column: "VehiculoHistoricoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_vehiculo_tipo_evento_TipoEventoId",
                schema: "operaciones",
                table: "evento_vehiculo_tipo_evento",
                column: "TipoEventoId");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_Estado",
                schema: "operaciones",
                table: "eventos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_IsActive_FechaHoraReporteUtc",
                schema: "operaciones",
                table: "eventos",
                columns: new[] { "IsActive", "FechaHoraReporteUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_MunicipioId",
                schema: "operaciones",
                table: "eventos",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_RequestId",
                schema: "operaciones",
                table: "eventos",
                column: "RequestId",
                unique: true,
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_TipoCierreId",
                schema: "operaciones",
                table: "eventos",
                column: "TipoCierreId");

            migrationBuilder.CreateIndex(
                name: "IX_eventos_TramoId",
                schema: "operaciones",
                table: "eventos",
                column: "TramoId");

            migrationBuilder.CreateIndex(
                name: "IX_flotas_DenominacionId",
                schema: "operaciones",
                table: "flotas",
                column: "DenominacionId");

            migrationBuilder.CreateIndex(
                name: "IX_flotas_Numero_Codigo",
                schema: "operaciones",
                table: "flotas",
                columns: new[] { "Numero", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_historial_denominacion_unidad_DenominacionAnteriorId",
                schema: "operaciones",
                table: "historial_denominacion_unidad",
                column: "DenominacionAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_historial_denominacion_unidad_DenominacionNuevaId",
                schema: "operaciones",
                table: "historial_denominacion_unidad",
                column: "DenominacionNuevaId");

            migrationBuilder.CreateIndex(
                name: "IX_historial_denominacion_unidad_UnidadId_FechaUtc",
                schema: "operaciones",
                table: "historial_denominacion_unidad",
                columns: new[] { "UnidadId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_historial_denominacion_unidad_UnidadRelacionadaId",
                schema: "operaciones",
                table: "historial_denominacion_unidad",
                column: "UnidadRelacionadaId");

            migrationBuilder.CreateIndex(
                name: "IX_modelos_MarcaId",
                schema: "misc",
                table: "modelos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_modelos_TipoVehiculoId",
                schema: "misc",
                table: "modelos",
                column: "TipoVehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_municipios_ProvinciaId",
                schema: "misc",
                table: "municipios",
                column: "ProvinciaId");

            migrationBuilder.CreateIndex(
                name: "IX_permisos_Name",
                schema: "accesos",
                table: "permisos",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "accesos",
                table: "permisos",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prefijo_placa_tipo_vehiculo_TipoVehiculoId",
                schema: "misc",
                table: "prefijo_placa_tipo_vehiculo",
                column: "TipoVehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_prefijos_placa_Prefijo",
                schema: "misc",
                table: "prefijos_placa",
                column: "Prefijo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tramos_RegionAsistenciaId",
                schema: "operaciones",
                table: "tramos",
                column: "RegionAsistenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_unidad_posicion_AgenteId",
                schema: "operaciones",
                table: "unidad_posicion",
                column: "AgenteId");

            migrationBuilder.CreateIndex(
                name: "IX_unidad_posicion_ComponenteId",
                schema: "operaciones",
                table: "unidad_posicion",
                column: "ComponenteId");

            migrationBuilder.CreateIndex(
                name: "IX_unidad_posicion_FechaHoraRecibidaUtc",
                schema: "operaciones",
                table: "unidad_posicion",
                column: "FechaHoraRecibidaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_unidades_DenominacionId",
                schema: "operaciones",
                table: "unidades",
                column: "DenominacionId");

            migrationBuilder.CreateIndex(
                name: "IX_unidades_Ficha",
                schema: "operaciones",
                table: "unidades",
                column: "Ficha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "accesos",
                table: "usuarios",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_DepartamentoId",
                schema: "accesos",
                table: "usuarios",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Identificacion_UserName",
                schema: "accesos",
                table: "usuarios",
                columns: new[] { "Identificacion", "UserName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_RangoId",
                schema: "accesos",
                table: "usuarios",
                column: "RangoId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "accesos",
                table: "usuarios",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_ColorId",
                schema: "historico",
                table: "vehiculos",
                column: "ColorId");

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_ModeloId",
                schema: "historico",
                table: "vehiculos",
                column: "ModeloId");

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_Placa",
                schema: "historico",
                table: "vehiculos",
                column: "Placa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_TipoId",
                schema: "historico",
                table: "vehiculos",
                column: "TipoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "denominacion_regiones",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "denominacion_tramos",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "evento_ciudadano_tipo_evento",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "evento_evidencias",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "evento_tipo_evento",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "evento_unidad",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "evento_vehiculo_tipo_evento",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "flotas",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "historial_denominacion_unidad",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "prefijo_placa_tipo_vehiculo",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "unidad_posicion",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "permisos",
                schema: "accesos");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "accesos");

            migrationBuilder.DropTable(
                name: "evento_ciudadano",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "tipo_eventos",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "prefijos_placa",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "agentes",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "unidades",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "departamentos",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "ciudadanos",
                schema: "historico");

            migrationBuilder.DropTable(
                name: "evento_vehiculo",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "rangos",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "denominaciones",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "nacionalidades",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "eventos",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "vehiculos",
                schema: "historico");

            migrationBuilder.DropTable(
                name: "niveles_denominacion",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "municipios",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "tipo_cierres",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "tramos",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "colores",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "modelos",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "provincias",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "regiones_asistencia",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "marcas",
                schema: "misc");

            migrationBuilder.DropTable(
                name: "tipo_vehiculos",
                schema: "misc");
        }
    }
}
