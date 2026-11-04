using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCalidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "calidad");

            migrationBuilder.CreateTable(
                name: "encuestas",
                schema: "calidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    PublicadaUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiradaUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encuestas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evaluaciones",
                schema: "calidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventoId = table.Column<int>(type: "int", nullable: false),
                    EncuestaId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Canal = table.Column<int>(type: "int", nullable: false),
                    EventoCiudadanoId = table.Column<int>(type: "int", nullable: true),
                    NombreContacto = table.Column<string>(type: "nvarchar(101)", maxLength: 101, nullable: true),
                    TelefonoContacto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CerradaUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CerradaPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluaciones_encuestas_EncuestaId",
                        column: x => x.EncuestaId,
                        principalSchema: "calidad",
                        principalTable: "encuestas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evaluaciones_evento_ciudadano_EventoCiudadanoId",
                        column: x => x.EventoCiudadanoId,
                        principalSchema: "operaciones",
                        principalTable: "evento_ciudadano",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evaluaciones_eventos_EventoId",
                        column: x => x.EventoId,
                        principalSchema: "operaciones",
                        principalTable: "eventos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preguntas_encuesta",
                schema: "calidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EncuestaId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Obligatoria = table.Column<bool>(type: "bit", nullable: false),
                    PermiteComentario = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_preguntas_encuesta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_preguntas_encuesta_encuestas_EncuestaId",
                        column: x => x.EncuestaId,
                        principalSchema: "calidad",
                        principalTable: "encuestas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "intentos_contacto",
                schema: "calidad",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluacionCalidadId = table.Column<int>(type: "int", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Canal = table.Column<int>(type: "int", nullable: false),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferenciaExterna = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_intentos_contacto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_intentos_contacto_evaluaciones_EvaluacionCalidadId",
                        column: x => x.EvaluacionCalidadId,
                        principalSchema: "calidad",
                        principalTable: "evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "opciones_pregunta",
                schema: "calidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PreguntaEncuestaId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<int>(type: "int", nullable: false),
                    Etiqueta = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_opciones_pregunta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_opciones_pregunta_preguntas_encuesta_PreguntaEncuestaId",
                        column: x => x.PreguntaEncuestaId,
                        principalSchema: "calidad",
                        principalTable: "preguntas_encuesta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "respuestas_evaluacion",
                schema: "calidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluacionCalidadId = table.Column<int>(type: "int", nullable: false),
                    PreguntaEncuestaId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<int>(type: "int", nullable: true),
                    Texto = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Comentario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_respuestas_evaluacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_respuestas_evaluacion_evaluaciones_EvaluacionCalidadId",
                        column: x => x.EvaluacionCalidadId,
                        principalSchema: "calidad",
                        principalTable: "evaluaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_respuestas_evaluacion_preguntas_encuesta_PreguntaEncuestaId",
                        column: x => x.PreguntaEncuestaId,
                        principalSchema: "calidad",
                        principalTable: "preguntas_encuesta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_encuestas_Estado",
                schema: "calidad",
                table: "encuestas",
                column: "Estado",
                unique: true,
                filter: "[Estado] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_encuestas_Version",
                schema: "calidad",
                table: "encuestas",
                column: "Version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluaciones_EncuestaId",
                schema: "calidad",
                table: "evaluaciones",
                column: "EncuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluaciones_Estado_CerradaUtc",
                schema: "calidad",
                table: "evaluaciones",
                columns: new[] { "Estado", "CerradaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_evaluaciones_EventoCiudadanoId",
                schema: "calidad",
                table: "evaluaciones",
                column: "EventoCiudadanoId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluaciones_EventoId",
                schema: "calidad",
                table: "evaluaciones",
                column: "EventoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_intentos_contacto_EvaluacionCalidadId_FechaUtc",
                schema: "calidad",
                table: "intentos_contacto",
                columns: new[] { "EvaluacionCalidadId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_intentos_contacto_ReferenciaExterna",
                schema: "calidad",
                table: "intentos_contacto",
                column: "ReferenciaExterna",
                unique: true,
                filter: "[ReferenciaExterna] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_opciones_pregunta_PreguntaEncuestaId",
                schema: "calidad",
                table: "opciones_pregunta",
                column: "PreguntaEncuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_preguntas_encuesta_EncuestaId_Orden",
                schema: "calidad",
                table: "preguntas_encuesta",
                columns: new[] { "EncuestaId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_respuestas_evaluacion_EvaluacionCalidadId_PreguntaEncuestaId",
                schema: "calidad",
                table: "respuestas_evaluacion",
                columns: new[] { "EvaluacionCalidadId", "PreguntaEncuestaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_respuestas_evaluacion_PreguntaEncuestaId",
                schema: "calidad",
                table: "respuestas_evaluacion",
                column: "PreguntaEncuestaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "intentos_contacto",
                schema: "calidad");

            migrationBuilder.DropTable(
                name: "opciones_pregunta",
                schema: "calidad");

            migrationBuilder.DropTable(
                name: "respuestas_evaluacion",
                schema: "calidad");

            migrationBuilder.DropTable(
                name: "evaluaciones",
                schema: "calidad");

            migrationBuilder.DropTable(
                name: "preguntas_encuesta",
                schema: "calidad");

            migrationBuilder.DropTable(
                name: "encuestas",
                schema: "calidad");
        }
    }
}
