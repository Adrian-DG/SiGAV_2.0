using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <summary>
    /// Datos de <see cref="SimplificaEventosHistorico"/>: va aparte porque SQLite reconstruye las tablas
    /// al final de esa migración, y estos datos necesitan el esquema nuevo (columnas que pasan a NULL,
    /// la nueva clave de evento_tipo_evento).
    /// </summary>
    public partial class MigraDatosEventosHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- Tipos: los de cada vehículo y persona pasan a evento_tipo_evento
            migrationBuilder.Sql("""
                INSERT INTO "evento_tipo_evento" ("EventoId", "TipoEventoId", "EventoVehiculoId")
                SELECT v."EventoId", vt."TipoEventoId", vt."EventoVehiculoId"
                FROM "evento_vehiculo_tipo_evento" vt
                JOIN "evento_vehiculo" v ON v."Id" = vt."EventoVehiculoId";

                INSERT INTO "evento_tipo_evento" ("EventoId", "TipoEventoId", "EventoCiudadanoId")
                SELECT c."EventoId", ct."TipoEventoId", ct."EventoCiudadanoId"
                FROM "evento_ciudadano_tipo_evento" ct
                JOIN "evento_ciudadano" c ON c."Id" = ct."EventoCiudadanoId";

                -- La fila general sobra si un vehículo o persona del evento ya recibe ese tipo
                DELETE FROM "evento_tipo_evento"
                WHERE "EventoVehiculoId" IS NULL AND "EventoCiudadanoId" IS NULL
                  AND EXISTS (
                      SELECT 1 FROM "evento_tipo_evento" d
                      WHERE d."EventoId" = "evento_tipo_evento"."EventoId"
                        AND d."TipoEventoId" = "evento_tipo_evento"."TipoEventoId"
                        AND (d."EventoVehiculoId" IS NOT NULL OR d."EventoCiudadanoId" IS NOT NULL));
                """);

            migrationBuilder.DropTable(
                name: "evento_ciudadano_tipo_evento",
                schema: "operaciones");

            migrationBuilder.DropTable(
                name: "evento_vehiculo_tipo_evento",
                schema: "operaciones");

            // ---- Histórico de ciudadanos: lo más reciente gana (el último evento activo de cada identificación)
            migrationBuilder.Sql("""
                CREATE TEMP TABLE "ultimo_ciudadano" AS
                SELECT "Identificacion", "Nombre", "Apellido", "Sexo", "Telefono", "NacionalidadId", "Edad", "Fecha"
                FROM (
                    SELECT ec."Identificacion", ec."Nombre", ec."Apellido", ec."Sexo", ec."Telefono", ec."NacionalidadId",
                           ec."Edad", e."FechaHoraReporteUtc" AS "Fecha",
                           ROW_NUMBER() OVER (PARTITION BY ec."Identificacion" ORDER BY e."FechaHoraReporteUtc" DESC, ec."Id" DESC) AS "n"
                    FROM "evento_ciudadano" ec
                    JOIN "eventos" e ON e."Id" = ec."EventoId"
                    WHERE ec."Identificacion" IS NOT NULL AND e."IsActive" = 1)
                WHERE "n" = 1;

                UPDATE "ciudadanos" SET
                    "Nombre" = COALESCE(u."Nombre", "ciudadanos"."Nombre"),
                    "Apellido" = COALESCE(u."Apellido", "ciudadanos"."Apellido"),
                    "Sexo" = CASE WHEN u."Sexo" <> 0 THEN u."Sexo" ELSE "ciudadanos"."Sexo" END,
                    "Telefono" = COALESCE(u."Telefono", "ciudadanos"."Telefono"),
                    "NacionalidadId" = COALESCE(u."NacionalidadId", "ciudadanos"."NacionalidadId"),
                    "FechaNacimientoEstimada" = CASE WHEN u."Edad" IS NULL THEN "ciudadanos"."FechaNacimientoEstimada"
                                                     ELSE date(u."Fecha", '-' || u."Edad" || ' years') END,
                    "UltimoEventoUtc" = u."Fecha"
                FROM "ultimo_ciudadano" u
                WHERE u."Identificacion" = "ciudadanos"."Identificacion";

                INSERT INTO "ciudadanos" ("Identificacion", "Nombre", "Apellido", "Sexo", "Telefono", "NacionalidadId",
                                          "FechaNacimientoEstimada", "UltimoEventoUtc", "IsActive")
                SELECT u."Identificacion", u."Nombre", u."Apellido", u."Sexo", u."Telefono", u."NacionalidadId",
                       CASE WHEN u."Edad" IS NULL THEN NULL ELSE date(u."Fecha", '-' || u."Edad" || ' years') END,
                       u."Fecha", 1
                FROM "ultimo_ciudadano" u
                WHERE NOT EXISTS (SELECT 1 FROM "ciudadanos" c WHERE c."Identificacion" = u."Identificacion");

                DROP TABLE "ultimo_ciudadano";

                UPDATE "evento_ciudadano"
                SET "CiudadanoId" = (SELECT c."Id" FROM "ciudadanos" c
                                     WHERE c."Identificacion" = "evento_ciudadano"."Identificacion" AND c."IsActive" = 1)
                WHERE "Identificacion" IS NOT NULL AND "CiudadanoId" IS NULL;
                """);

            // ---- Histórico de vehículos: marca de los importados (antes solo a través del modelo) y lo
            // más reciente gana. Catálogo y texto libre de un mismo dato van juntos.
            migrationBuilder.Sql("""
                UPDATE "vehiculos"
                SET "MarcaId" = (SELECT m."MarcaId" FROM "modelos" m WHERE m."Id" = "vehiculos"."ModeloId")
                WHERE "MarcaId" IS NULL AND "ModeloId" IS NOT NULL;

                CREATE TEMP TABLE "ultimo_vehiculo" AS
                SELECT "Placa", "TipoVehiculoId", "MarcaId", "ModeloId", "ColorId", "MarcaTexto", "ModeloTexto", "ColorTexto", "Fecha"
                FROM (
                    SELECT ev."Placa", ev."TipoVehiculoId", ev."MarcaId", ev."ModeloId", ev."ColorId",
                           ev."MarcaTexto", ev."ModeloTexto", ev."ColorTexto", e."FechaHoraReporteUtc" AS "Fecha",
                           ROW_NUMBER() OVER (PARTITION BY ev."Placa" ORDER BY e."FechaHoraReporteUtc" DESC, ev."Id" DESC) AS "n"
                    FROM "evento_vehiculo" ev
                    JOIN "eventos" e ON e."Id" = ev."EventoId"
                    WHERE ev."Placa" IS NOT NULL AND e."IsActive" = 1)
                WHERE "n" = 1;

                UPDATE "vehiculos" SET
                    "TipoVehiculoId" = COALESCE(u."TipoVehiculoId", "vehiculos"."TipoVehiculoId"),
                    "MarcaId" = CASE WHEN u."MarcaId" IS NOT NULL OR u."MarcaTexto" IS NOT NULL THEN u."MarcaId" ELSE "vehiculos"."MarcaId" END,
                    "MarcaTexto" = CASE WHEN u."MarcaId" IS NOT NULL OR u."MarcaTexto" IS NOT NULL THEN u."MarcaTexto" ELSE "vehiculos"."MarcaTexto" END,
                    "ModeloId" = CASE WHEN u."ModeloId" IS NOT NULL OR u."ModeloTexto" IS NOT NULL THEN u."ModeloId" ELSE "vehiculos"."ModeloId" END,
                    "ModeloTexto" = CASE WHEN u."ModeloId" IS NOT NULL OR u."ModeloTexto" IS NOT NULL THEN u."ModeloTexto" ELSE "vehiculos"."ModeloTexto" END,
                    "ColorId" = CASE WHEN u."ColorId" IS NOT NULL OR u."ColorTexto" IS NOT NULL THEN u."ColorId" ELSE "vehiculos"."ColorId" END,
                    "ColorTexto" = CASE WHEN u."ColorId" IS NOT NULL OR u."ColorTexto" IS NOT NULL THEN u."ColorTexto" ELSE "vehiculos"."ColorTexto" END,
                    "UltimoEventoUtc" = u."Fecha"
                FROM "ultimo_vehiculo" u
                WHERE u."Placa" = "vehiculos"."Placa";

                INSERT INTO "vehiculos" ("Placa", "TipoVehiculoId", "MarcaId", "ModeloId", "ColorId",
                                         "MarcaTexto", "ModeloTexto", "ColorTexto", "UltimoEventoUtc", "IsActive")
                SELECT u."Placa", u."TipoVehiculoId", u."MarcaId", u."ModeloId", u."ColorId",
                       u."MarcaTexto", u."ModeloTexto", u."ColorTexto", u."Fecha", 1
                FROM "ultimo_vehiculo" u
                WHERE NOT EXISTS (SELECT 1 FROM "vehiculos" v WHERE v."Placa" = u."Placa");

                DROP TABLE "ultimo_vehiculo";

                UPDATE "evento_vehiculo"
                SET "VehiculoHistoricoId" = (SELECT v."Id" FROM "vehiculos" v
                                             WHERE v."Placa" = "evento_vehiculo"."Placa" AND v."IsActive" = 1)
                WHERE "Placa" IS NOT NULL AND "VehiculoHistoricoId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_evento_ciudadano_tipo_evento_TipoEventoId",
                schema: "operaciones",
                table: "evento_ciudadano_tipo_evento",
                column: "TipoEventoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_vehiculo_tipo_evento_TipoEventoId",
                schema: "operaciones",
                table: "evento_vehiculo_tipo_evento",
                column: "TipoEventoId");

            // Tipos por vehículo y persona de vuelta a sus tablas; la fila general vuelve a tener todos los tipos
            migrationBuilder.Sql("""
                INSERT INTO "evento_vehiculo_tipo_evento" ("EventoVehiculoId", "TipoEventoId")
                SELECT "EventoVehiculoId", "TipoEventoId" FROM "evento_tipo_evento" WHERE "EventoVehiculoId" IS NOT NULL;

                INSERT INTO "evento_ciudadano_tipo_evento" ("EventoCiudadanoId", "TipoEventoId")
                SELECT "EventoCiudadanoId", "TipoEventoId" FROM "evento_tipo_evento" WHERE "EventoCiudadanoId" IS NOT NULL;

                INSERT INTO "evento_tipo_evento" ("EventoId", "TipoEventoId")
                SELECT DISTINCT d."EventoId", d."TipoEventoId"
                FROM "evento_tipo_evento" d
                WHERE (d."EventoVehiculoId" IS NOT NULL OR d."EventoCiudadanoId" IS NOT NULL)
                  AND NOT EXISTS (
                      SELECT 1 FROM "evento_tipo_evento" g
                      WHERE g."EventoId" = d."EventoId" AND g."TipoEventoId" = d."TipoEventoId"
                        AND g."EventoVehiculoId" IS NULL AND g."EventoCiudadanoId" IS NULL);

                DELETE FROM "evento_tipo_evento" WHERE "EventoVehiculoId" IS NOT NULL OR "EventoCiudadanoId" IS NOT NULL;
                """);

            // El esquema anterior exige nombre, apellido, nacionalidad, tipo y color: lo que no los tiene sale del histórico
            migrationBuilder.Sql("""
                UPDATE "evento_ciudadano" SET "CiudadanoId" = NULL
                WHERE "CiudadanoId" IN (SELECT "Id" FROM "ciudadanos"
                                        WHERE "Nombre" IS NULL OR "Apellido" IS NULL OR "NacionalidadId" IS NULL);
                DELETE FROM "ciudadanos" WHERE "Nombre" IS NULL OR "Apellido" IS NULL OR "NacionalidadId" IS NULL;

                UPDATE "evento_vehiculo" SET "VehiculoHistoricoId" = NULL
                WHERE "VehiculoHistoricoId" IN (SELECT "Id" FROM "vehiculos" WHERE "TipoVehiculoId" IS NULL OR "ColorId" IS NULL);
                DELETE FROM "vehiculos" WHERE "TipoVehiculoId" IS NULL OR "ColorId" IS NULL;
                """);
        }
    }
}
