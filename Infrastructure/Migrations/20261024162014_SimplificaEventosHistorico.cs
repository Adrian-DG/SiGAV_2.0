using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplificaEventosHistorico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ciudadanos_nacionalidades_NacionalidadId",
                schema: "historico",
                table: "ciudadanos");

            migrationBuilder.DropForeignKey(
                name: "FK_vehiculos_colores_ColorId",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropForeignKey(
                name: "FK_vehiculos_tipo_vehiculos_TipoId",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropIndex(
                name: "IX_vehiculos_TipoId",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_evento_tipo_evento",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.RenameColumn(
                name: "TipoId",
                schema: "historico",
                table: "vehiculos",
                newName: "TipoVehiculoId");

            // FotoIdentificacion no se usaba (las fotos van en evento_evidencias)
            migrationBuilder.DropColumn(
                name: "FotoIdentificacion",
                schema: "historico",
                table: "ciudadanos");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoEventoUtc",
                schema: "historico",
                table: "ciudadanos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ColorId",
                schema: "historico",
                table: "vehiculos",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "ColorTexto",
                schema: "historico",
                table: "vehiculos",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarcaId",
                schema: "historico",
                table: "vehiculos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarcaTexto",
                schema: "historico",
                table: "vehiculos",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeloTexto",
                schema: "historico",
                table: "vehiculos",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TipoVehiculoId",
                schema: "historico",
                table: "vehiculos",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoEventoUtc",
                schema: "historico",
                table: "vehiculos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                schema: "operaciones",
                table: "evento_tipo_evento",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "EventoCiudadanoId",
                schema: "operaciones",
                table: "evento_tipo_evento",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventoVehiculoId",
                schema: "operaciones",
                table: "evento_tipo_evento",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                schema: "historico",
                table: "ciudadanos",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "NacionalidadId",
                schema: "historico",
                table: "ciudadanos",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Apellido",
                schema: "historico",
                table: "ciudadanos",
                type: "TEXT",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaNacimientoEstimada",
                schema: "historico",
                table: "ciudadanos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                schema: "historico",
                table: "ciudadanos",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_evento_tipo_evento",
                schema: "operaciones",
                table: "evento_tipo_evento",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_MarcaId",
                schema: "historico",
                table: "vehiculos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_TipoVehiculoId",
                schema: "historico",
                table: "vehiculos",
                column: "TipoVehiculoId");

            migrationBuilder.CreateIndex(
                name: "IX_evento_tipo_evento_EventoCiudadanoId_TipoEventoId",
                schema: "operaciones",
                table: "evento_tipo_evento",
                columns: new[] { "EventoCiudadanoId", "TipoEventoId" },
                unique: true,
                filter: "\"EventoCiudadanoId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_evento_tipo_evento_EventoId_TipoEventoId",
                schema: "operaciones",
                table: "evento_tipo_evento",
                columns: new[] { "EventoId", "TipoEventoId" },
                unique: true,
                filter: "\"EventoVehiculoId\" IS NULL AND \"EventoCiudadanoId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_evento_tipo_evento_EventoVehiculoId_TipoEventoId",
                schema: "operaciones",
                table: "evento_tipo_evento",
                columns: new[] { "EventoVehiculoId", "TipoEventoId" },
                unique: true,
                filter: "\"EventoVehiculoId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_evento_tipo_evento_un_involucrado",
                schema: "operaciones",
                table: "evento_tipo_evento",
                sql: "\"EventoVehiculoId\" IS NULL OR \"EventoCiudadanoId\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ciudadanos_nacionalidades_NacionalidadId",
                schema: "historico",
                table: "ciudadanos",
                column: "NacionalidadId",
                principalSchema: "misc",
                principalTable: "nacionalidades",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_evento_tipo_evento_evento_ciudadano_EventoCiudadanoId",
                schema: "operaciones",
                table: "evento_tipo_evento",
                column: "EventoCiudadanoId",
                principalSchema: "operaciones",
                principalTable: "evento_ciudadano",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_evento_tipo_evento_evento_vehiculo_EventoVehiculoId",
                schema: "operaciones",
                table: "evento_tipo_evento",
                column: "EventoVehiculoId",
                principalSchema: "operaciones",
                principalTable: "evento_vehiculo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vehiculos_colores_ColorId",
                schema: "historico",
                table: "vehiculos",
                column: "ColorId",
                principalSchema: "misc",
                principalTable: "colores",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_vehiculos_marcas_MarcaId",
                schema: "historico",
                table: "vehiculos",
                column: "MarcaId",
                principalSchema: "misc",
                principalTable: "marcas",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_vehiculos_tipo_vehiculos_TipoVehiculoId",
                schema: "historico",
                table: "vehiculos",
                column: "TipoVehiculoId",
                principalSchema: "misc",
                principalTable: "tipo_vehiculos",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ciudadanos_nacionalidades_NacionalidadId",
                schema: "historico",
                table: "ciudadanos");

            migrationBuilder.DropForeignKey(
                name: "FK_evento_tipo_evento_evento_ciudadano_EventoCiudadanoId",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropForeignKey(
                name: "FK_evento_tipo_evento_evento_vehiculo_EventoVehiculoId",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropForeignKey(
                name: "FK_vehiculos_colores_ColorId",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropForeignKey(
                name: "FK_vehiculos_marcas_MarcaId",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropForeignKey(
                name: "FK_vehiculos_tipo_vehiculos_TipoVehiculoId",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropIndex(
                name: "IX_vehiculos_MarcaId",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropIndex(
                name: "IX_vehiculos_TipoVehiculoId",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_evento_tipo_evento",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropIndex(
                name: "IX_evento_tipo_evento_EventoCiudadanoId_TipoEventoId",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropIndex(
                name: "IX_evento_tipo_evento_EventoId_TipoEventoId",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropIndex(
                name: "IX_evento_tipo_evento_EventoVehiculoId_TipoEventoId",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropCheckConstraint(
                name: "CK_evento_tipo_evento_un_involucrado",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropColumn(
                name: "ColorTexto",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropColumn(
                name: "MarcaTexto",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropColumn(
                name: "ModeloTexto",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.AlterColumn<int>(
                name: "TipoVehiculoId",
                schema: "historico",
                table: "vehiculos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "TipoVehiculoId",
                schema: "historico",
                table: "vehiculos",
                newName: "TipoId");

            migrationBuilder.DropColumn(
                name: "UltimoEventoUtc",
                schema: "historico",
                table: "vehiculos");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropColumn(
                name: "EventoCiudadanoId",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropColumn(
                name: "EventoVehiculoId",
                schema: "operaciones",
                table: "evento_tipo_evento");

            migrationBuilder.DropColumn(
                name: "FechaNacimientoEstimada",
                schema: "historico",
                table: "ciudadanos");

            migrationBuilder.DropColumn(
                name: "Telefono",
                schema: "historico",
                table: "ciudadanos");

            migrationBuilder.DropColumn(
                name: "UltimoEventoUtc",
                schema: "historico",
                table: "ciudadanos");

            migrationBuilder.AddColumn<string>(
                name: "FotoIdentificacion",
                schema: "historico",
                table: "ciudadanos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ColorId",
                schema: "historico",
                table: "vehiculos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                schema: "historico",
                table: "ciudadanos",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "NacionalidadId",
                schema: "historico",
                table: "ciudadanos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Apellido",
                schema: "historico",
                table: "ciudadanos",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_evento_tipo_evento",
                schema: "operaciones",
                table: "evento_tipo_evento",
                columns: new[] { "EventoId", "TipoEventoId" });

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_TipoId",
                schema: "historico",
                table: "vehiculos",
                column: "TipoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ciudadanos_nacionalidades_NacionalidadId",
                schema: "historico",
                table: "ciudadanos",
                column: "NacionalidadId",
                principalSchema: "misc",
                principalTable: "nacionalidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vehiculos_colores_ColorId",
                schema: "historico",
                table: "vehiculos",
                column: "ColorId",
                principalSchema: "misc",
                principalTable: "colores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vehiculos_tipo_vehiculos_TipoId",
                schema: "historico",
                table: "vehiculos",
                column: "TipoId",
                principalSchema: "misc",
                principalTable: "tipo_vehiculos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
