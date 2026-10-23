using Domain.Enums;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// Fila del listado. Los nombres coinciden con EventoListItem de la app móvil
/// (Mobile/src/features/events/types.ts); de Agente en adelante son para front desk (la app los ignora).
/// Agente/UnidadId/AgenteId: la unidad principal y quien la operaba. TotalPersonas/TotalVehiculos:
/// todos los del evento (el principal incluido). Las fechas van en UTC (con "Z").
/// </summary>
public record EventoListItemViewModel(
    int Id,
    EstadoEventoEnum Estado,
    IReadOnlyList<string> Tipos,
    IReadOnlyList<CategoriaEventoEnum> Categorias,
    string? CiudadanoPrincipal,
    string? VehiculoDescripcion,
    string? Direccion,
    DateTime FechaHoraReporte,
    string UnidadFicha,
    string UnidadDenominacion,
    string? Agente,
    string? Tramo,
    int TotalPersonas,
    int TotalVehiculos,
    int? UnidadId,
    int? AgenteId);

public record EventoDetalleViewModel(
    int Id,
    Guid? RequestId,
    EstadoEventoEnum Estado,
    CanalReporteEnum CanalReporte,
    int? TipoCierreId,
    string? TipoCierre,
    bool IsActive,
    decimal Latitud,
    decimal Longitud,
    string? Direccion,
    int MunicipioId,
    string Municipio,
    string Provincia,
    int? TramoId,
    string? Tramo,
    string? Comentario,
    DateTime FechaHoraReporte,
    DateTime? FechaHoraLlegada,
    DateTime? FechaHoraCompletado,
    IReadOnlyList<EventoTipoViewModel> Tipos,
    IReadOnlyList<EventoUnidadViewModel> Unidades,
    IReadOnlyList<EventoVehiculoViewModel> Vehiculos,
    IReadOnlyList<EventoCiudadanoViewModel> Ciudadanos,
    IReadOnlyList<EventoEvidenciaViewModel> Evidencias,
    DateTime CreatedAt);

public record EventoTipoViewModel(int Id, string Nombre, CategoriaEventoEnum Categoria);

public record EventoUnidadViewModel(
    int UnidadId,
    string Ficha,
    int DenominacionId,
    string Denominacion,
    string NivelDenominacion,
    RolUnidadEventoEnum Rol,
    int AgenteId,
    string Agente);

public record EventoCiudadanoViewModel(
    int Id,
    RolCiudadanoEnum Rol,
    string? Identificacion,
    string? Nombre,
    string? Apellido,
    SexoEnum Sexo,
    string? Telefono,
    string? Nacionalidad,
    /// <summary>Id del vehículo del evento (Vehiculos) en que iba; null = sin vehículo.</summary>
    int? VehiculoId,
    /// <summary>Tipos atendidos a la persona (Tipos[].Id del evento); vacío = ninguno propio.</summary>
    IReadOnlyList<int> TipoEventoIds,
    /// <summary>Edad en años al momento del evento.</summary>
    int? Edad);

/// <summary>
/// Marca/modelo/color: el nombre del catálogo o, si no estaba en el catálogo, el texto libre.
/// TipoEventoIds: tipos atendidos al vehículo (Tipos[].Id del evento).
/// </summary>
public record EventoVehiculoViewModel(
    int Id,
    string? Placa,
    string? TipoVehiculo,
    string? Marca,
    string? Modelo,
    string? Color,
    string Descripcion,
    IReadOnlyList<int> TipoEventoIds);

/// <summary>
/// El archivo se descarga de GET /api/eventos/{eventoId}/evidencias/{id}/archivo (la clave interna del
/// almacenamiento no se expone). CiudadanoId/VehiculoId: Ciudadanos[].Id / Vehiculos[].Id del evento.
/// </summary>
public record EventoEvidenciaViewModel(
    int Id,
    TipoEvidenciaEnum Tipo,
    string ContentType,
    long TamanoBytes,
    int? CiudadanoId,
    int? VehiculoId,
    DateTime Registrada);

/// <summary>
/// VehiculoIds/CiudadanoIds: Ids que asignó la API a cada vehículo y persona, en el mismo orden del
/// request. La app los usa para asociar evidencias (foto de placa, de cédula) a cada uno.
/// </summary>
public record RegistrarEventoResult(int Id, bool EsDuplicado, IReadOnlyList<int> VehiculoIds, IReadOnlyList<int> CiudadanoIds);
