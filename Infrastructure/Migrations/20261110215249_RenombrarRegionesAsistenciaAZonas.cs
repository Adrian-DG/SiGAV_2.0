using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarRegionesAsistenciaAZonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_denominacion_regiones_regiones_asistencia_RegionAsistenciaId",
                schema: "operaciones",
                table: "denominacion_regiones");

            migrationBuilder.DropForeignKey(
                name: "FK_tramos_regiones_asistencia_RegionAsistenciaId",
                schema: "operaciones",
                table: "tramos");

            // Se renombra (no se borra y recrea) para conservar los datos y los Ids
            migrationBuilder.RenameTable(
                name: "regiones_asistencia",
                schema: "operaciones",
                newName: "zonas",
                newSchema: "operaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_regiones_asistencia",
                schema: "operaciones",
                table: "zonas");

            migrationBuilder.AddPrimaryKey(
                name: "PK_zonas",
                schema: "operaciones",
                table: "zonas",
                column: "Id");

            migrationBuilder.DropIndex(
                name: "IX_denominacion_regiones_DenominacionId_RegionAsistenciaId",
                schema: "operaciones",
                table: "denominacion_regiones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_denominacion_regiones_macro_o_asistencia",
                schema: "operaciones",
                table: "denominacion_regiones");

            migrationBuilder.RenameColumn(
                name: "RegionAsistenciaId",
                schema: "operaciones",
                table: "tramos",
                newName: "ZonaId");

            migrationBuilder.RenameIndex(
                name: "IX_tramos_RegionAsistenciaId",
                schema: "operaciones",
                table: "tramos",
                newName: "IX_tramos_ZonaId");

            migrationBuilder.RenameColumn(
                name: "RegionAsistenciaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                newName: "ZonaId");

            migrationBuilder.RenameIndex(
                name: "IX_denominacion_regiones_RegionAsistenciaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                newName: "IX_denominacion_regiones_ZonaId");

            migrationBuilder.CreateIndex(
                name: "IX_denominacion_regiones_DenominacionId_ZonaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                columns: new[] { "DenominacionId", "ZonaId" },
                unique: true,
                filter: "[ZonaId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_denominacion_regiones_macro_o_zona",
                schema: "operaciones",
                table: "denominacion_regiones",
                sql: "([RegionMacro] IS NOT NULL AND [ZonaId] IS NULL) OR ([RegionMacro] IS NULL AND [ZonaId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_denominacion_regiones_zonas_ZonaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                column: "ZonaId",
                principalSchema: "operaciones",
                principalTable: "zonas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tramos_zonas_ZonaId",
                schema: "operaciones",
                table: "tramos",
                column: "ZonaId",
                principalSchema: "operaciones",
                principalTable: "zonas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_denominacion_regiones_zonas_ZonaId",
                schema: "operaciones",
                table: "denominacion_regiones");

            migrationBuilder.DropForeignKey(
                name: "FK_tramos_zonas_ZonaId",
                schema: "operaciones",
                table: "tramos");

            // Se renombra (no se borra y recrea) para conservar los datos y los Ids
            migrationBuilder.RenameTable(
                name: "zonas",
                schema: "operaciones",
                newName: "regiones_asistencia",
                newSchema: "operaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_zonas",
                schema: "operaciones",
                table: "regiones_asistencia");

            migrationBuilder.AddPrimaryKey(
                name: "PK_regiones_asistencia",
                schema: "operaciones",
                table: "regiones_asistencia",
                column: "Id");

            migrationBuilder.DropIndex(
                name: "IX_denominacion_regiones_DenominacionId_ZonaId",
                schema: "operaciones",
                table: "denominacion_regiones");

            migrationBuilder.DropCheckConstraint(
                name: "CK_denominacion_regiones_macro_o_zona",
                schema: "operaciones",
                table: "denominacion_regiones");

            migrationBuilder.RenameColumn(
                name: "ZonaId",
                schema: "operaciones",
                table: "tramos",
                newName: "RegionAsistenciaId");

            migrationBuilder.RenameIndex(
                name: "IX_tramos_ZonaId",
                schema: "operaciones",
                table: "tramos",
                newName: "IX_tramos_RegionAsistenciaId");

            migrationBuilder.RenameColumn(
                name: "ZonaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                newName: "RegionAsistenciaId");

            migrationBuilder.RenameIndex(
                name: "IX_denominacion_regiones_ZonaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                newName: "IX_denominacion_regiones_RegionAsistenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_denominacion_regiones_DenominacionId_RegionAsistenciaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                columns: new[] { "DenominacionId", "RegionAsistenciaId" },
                unique: true,
                filter: "[RegionAsistenciaId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_denominacion_regiones_macro_o_asistencia",
                schema: "operaciones",
                table: "denominacion_regiones",
                sql: "([RegionMacro] IS NOT NULL AND [RegionAsistenciaId] IS NULL) OR ([RegionMacro] IS NULL AND [RegionAsistenciaId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_denominacion_regiones_regiones_asistencia_RegionAsistenciaId",
                schema: "operaciones",
                table: "denominacion_regiones",
                column: "RegionAsistenciaId",
                principalSchema: "operaciones",
                principalTable: "regiones_asistencia",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tramos_regiones_asistencia_RegionAsistenciaId",
                schema: "operaciones",
                table: "tramos",
                column: "RegionAsistenciaId",
                principalSchema: "operaciones",
                principalTable: "regiones_asistencia",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
