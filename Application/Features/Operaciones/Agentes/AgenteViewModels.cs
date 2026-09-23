using Domain.Enums;

namespace Application.Features.Operaciones.Agentes;

public record AgenteViewModel(
    int Id,
    string Identificacion,
    string Nombre,
    string Apellido,
    string NombreCompleto,
    SexoEnum Sexo,
    InstitucionEnum Institucion,
    int RangoId,
    string Rango,
    AreaOperativaEnum AreaOperativa,
    bool AccesoTotal,
    string? Especialidad,
    bool Autorizado,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>
/// Se conservan los nombres de SiGAV 1.0 (miembros/confirm) para facilitar la migración de la app.
/// </summary>
public record ConfirmAgenteViewModel(bool Created, bool IsAuthorized);

public record AgenteAutoCompleteViewModel(int Id, string Identificacion, string Nombre, AreaOperativaEnum AreaOperativa);
