namespace Domain.Enums;

/// <summary>Estado de la conexión de una unidad según su última posición.</summary>
public enum EstadoPosicionEnum
{
    /// <summary>Envió su posición hace menos de <see cref="Entities.Operaciones.UnidadPosicion.UmbralSinSenal"/>.</summary>
    EnLinea = 1,

    /// <summary>Dejó de enviar (sin cobertura, app cerrada, teléfono apagado): se muestra la última posición.</summary>
    SinSenal = 2,

    /// <summary>Cerró sesión o lleva más de <see cref="Entities.Operaciones.UnidadPosicion.UmbralDesconectada"/> sin enviar.</summary>
    Desconectada = 3,
}
