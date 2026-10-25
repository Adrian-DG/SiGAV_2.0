using Domain.Abstraction;
using Domain.Entities.Misc;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Historico;

/// <summary>
/// Vehículo conocido, por placa. Se crea al registrar un evento con una placa que aún no existe (o
/// viene importado de SiGAV 1.0) y cada evento posterior lo actualiza: lo más reciente gana. Como en
/// el evento, marca, modelo y color pueden estar fuera de catálogo (texto libre).
/// </summary>
public class Vehiculo : BaseEntityMetadata
{
    public required string Placa { get; set; }

    public int? TipoVehiculoId { get; set; }
    public virtual TipoVehiculo? TipoVehiculo { get; set; }

    public int? MarcaId { get; set; }
    public virtual Marca? Marca { get; set; }

    public int? ModeloId { get; set; }
    public virtual Modelo? Modelo { get; set; }

    public int? ColorId { get; set; }
    public virtual Color? Color { get; set; }

    public string? MarcaTexto { get; set; }
    public string? ModeloTexto { get; set; }
    public string? ColorTexto { get; set; }

    /// <summary>Año de fabricación (solo en los importados: el evento no lo registra).</summary>
    public int? Fabricacion { get; set; }

    /// <summary>Fecha de reporte del evento más reciente que actualizó los datos. null = solo importado.</summary>
    public DateTime? UltimoEventoUtc { get; set; }

    /// <summary>Vehículo nuevo a partir de un evento. Requiere placa (el resto es opcional).</summary>
    public static Vehiculo Registrar(DatosVehiculo datos, DateTime fechaEventoUtc)
    {
        ArgumentNullException.ThrowIfNull(datos);
        var vehiculo = new Vehiculo
        {
            Placa = datos.Placa ?? throw new DomainException("Solo se guarda en el histórico un vehículo con placa."),
            IsActive = true
        };
        vehiculo.Actualizar(datos, fechaEventoUtc);
        return vehiculo;
    }

    /// <summary>
    /// Toma los datos de un evento. Si el evento es más antiguo que el último aplicado (p. ej. se
    /// sincronizó tarde desde la cola offline) solo completa lo que falta, sin pisar datos nuevos.
    /// Catálogo y texto libre de un mismo dato van juntos: uno reemplaza al otro.
    /// </summary>
    public void Actualizar(DatosVehiculo datos, DateTime fechaEventoUtc)
    {
        ArgumentNullException.ThrowIfNull(datos);
        if (datos.Placa != Placa)
            throw new DomainException("Los datos no corresponden al vehículo del histórico.");

        var masReciente = UltimoEventoUtc is null || fechaEventoUtc >= UltimoEventoUtc;
        bool Aplica(bool tieneDato, bool tieneActual) => tieneDato && (masReciente || !tieneActual);

        if (Aplica(datos.TipoVehiculoId is not null, TipoVehiculoId is not null))
        {
            if (TipoVehiculo?.Id != datos.TipoVehiculoId) TipoVehiculo = null;
            TipoVehiculoId = datos.TipoVehiculoId;
        }
        if (Aplica(datos.MarcaId is not null || datos.MarcaTexto is not null, MarcaId is not null || MarcaTexto is not null))
        {
            if (Marca?.Id != datos.MarcaId) Marca = null;
            (MarcaId, MarcaTexto) = (datos.MarcaId, datos.MarcaTexto);
        }
        if (Aplica(datos.ModeloId is not null || datos.ModeloTexto is not null, ModeloId is not null || ModeloTexto is not null))
        {
            if (Modelo?.Id != datos.ModeloId) Modelo = null;
            (ModeloId, ModeloTexto) = (datos.ModeloId, datos.ModeloTexto);
        }
        if (Aplica(datos.ColorId is not null || datos.ColorTexto is not null, ColorId is not null || ColorTexto is not null))
        {
            if (Color?.Id != datos.ColorId) Color = null;
            (ColorId, ColorTexto) = (datos.ColorId, datos.ColorTexto);
        }

        if (masReciente) UltimoEventoUtc = fechaEventoUtc;
    }
}
