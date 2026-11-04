using Domain.Entities.Calidad;
using Domain.Enums;

namespace Application.Features.Calidad;

/// <summary>Fila de la bandeja: un evento completado y el estado de su evaluación (null = pendiente).</summary>
public record BandejaCalidadItemViewModel(
    int EventoId,
    DateTime FechaHoraReporte,
    DateTime? FechaHoraCompletado,
    IReadOnlyList<string> Tipos,
    string UnidadFicha,
    string UnidadDenominacion,
    string? Tramo,
    string? CiudadanoPrincipal,
    string? Telefono,
    int PersonasConTelefono,
    EstadoEvaluacionEnum? EstadoEvaluacion,
    int Intentos,
    DateTime? UltimoIntento,
    DateTime? Cerrada);

public record OpcionPreguntaViewModel(int Valor, string Etiqueta);

public record PreguntaEncuestaViewModel(
    int Id,
    int Orden,
    string Texto,
    TipoPreguntaEnum Tipo,
    bool Obligatoria,
    bool PermiteComentario,
    IReadOnlyList<OpcionPreguntaViewModel> Opciones);

public record EncuestaViewModel(
    int Id,
    string Nombre,
    int Version,
    EstadoEncuestaEnum Estado,
    DateTime? Publicada,
    DateTime? Retirada,
    IReadOnlyList<PreguntaEncuestaViewModel> Preguntas)
{
    public static EncuestaViewModel Desde(Encuesta encuesta) => new(
        encuesta.Id,
        encuesta.Nombre,
        encuesta.Version,
        encuesta.Estado,
        encuesta.PublicadaUtc,
        encuesta.RetiradaUtc,
        encuesta.Preguntas
            .Select(p => new PreguntaEncuestaViewModel(
                p.Id, p.Orden, p.Texto, p.Tipo, p.Obligatoria, p.PermiteComentario,
                p.Opciones.Select(o => new OpcionPreguntaViewModel(o.Valor, o.Etiqueta)).ToList()))
            .ToList());
}

public record EncuestaListItemViewModel(
    int Id,
    string Nombre,
    int Version,
    EstadoEncuestaEnum Estado,
    DateTime? Publicada,
    DateTime? Retirada,
    int TotalPreguntas,
    int TotalEvaluaciones);

public record PersonaEventoCalidadViewModel(
    int EventoCiudadanoId,
    RolCiudadanoEnum Rol,
    string Nombre,
    string? Identificacion,
    string? Telefono);

public record EventoCalidadViewModel(
    int Id,
    DateTime FechaHoraReporte,
    DateTime? FechaHoraCompletado,
    IReadOnlyList<string> Tipos,
    string? Direccion,
    string? Municipio,
    string? Provincia,
    string? Tramo,
    string UnidadFicha,
    string UnidadDenominacion,
    string? TipoCierre,
    string? Comentario,
    IReadOnlyList<PersonaEventoCalidadViewModel> Personas);

public record IntentoContactoViewModel(
    DateTime Fecha,
    CanalContactoEnum Canal,
    ResultadoContactoEnum Resultado,
    string? Usuario,
    string? Observacion);

public record RespuestaViewModel(int PreguntaId, int? Valor, string? Texto, string? Comentario);

public record EvaluacionViewModel(
    int Id,
    EstadoEvaluacionEnum Estado,
    CanalContactoEnum Canal,
    int? EventoCiudadanoId,
    string? NombreContacto,
    string TelefonoContacto,
    string? Observacion,
    DateTime Creada,
    string? CreadaPor,
    DateTime? Cerrada,
    string? CerradaPor,
    IReadOnlyList<IntentoContactoViewModel> Intentos,
    IReadOnlyList<RespuestaViewModel> Respuestas);

/// <summary>
/// Pantalla de evaluación de un evento. <see cref="Encuesta"/> es la versión de la evaluación o,
/// si aún no se inició, la vigente (null si no hay ninguna publicada).
/// </summary>
public record EvaluacionEventoViewModel(
    EventoCalidadViewModel Evento,
    EvaluacionViewModel? Evaluacion,
    EncuestaViewModel? Encuesta);
