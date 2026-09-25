using Domain.Enums;

namespace Application.Features.Operaciones.Eventos;

/// <summary>
/// Fila del listado. Los nombres coinciden con EventoListItem de la app móvil
/// (Mobile/src/features/events/types.ts). Las fechas van en UTC (con "Z").
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
    string UnidadDenominacion);

public record EventoDetalleViewModel(
    int Id,
    Guid? RequestId,
    EstadoEventoEnum Estado,
    CanalReporteEnum CanalReporte,
    TipoCierreEventoEnum? TipoCierre,
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
    EventoVehiculoViewModel? Vehiculo);

/// <summary>Marca/modelo/color: el nombre del catálogo o, si no estaba en el catálogo, el texto libre.</summary>
public record EventoVehiculoViewModel(
    string? Placa,
    string? TipoVehiculo,
    string? Marca,
    string? Modelo,
    string? Color,
    string Descripcion);

public record EventoEvidenciaViewModel(int Id, TipoEvidenciaEnum Tipo, string Ubicacion, string ContentType, DateTime Registrada);

public record RegistrarEventoResult(int Id, bool EsDuplicado);
