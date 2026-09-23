namespace Domain.Abstraction;

/// <summary>
/// Registro de auditoría: una vez guardado no puede modificarse ni eliminarse.
/// La persistencia rechaza cualquier UPDATE o DELETE sobre estas entidades.
/// </summary>
public interface IRegistroInmutable;
