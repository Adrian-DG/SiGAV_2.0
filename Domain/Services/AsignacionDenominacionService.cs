using Domain.Entities.Operaciones;

namespace Domain.Services;

/// <summary>
/// Regla de negocio que involucra varios agregados: una denominación solo puede estar
/// ocupada por una unidad activa a la vez. Al asignarla, las unidades que la ocupaban
/// quedan sin denominación y no disponibles.
/// </summary>
public static class AsignacionDenominacionService
{
    public static void Asignar(Unidad unidad, Denominacion denominacion, IEnumerable<Unidad> unidadesQueLaOcupan)
    {
        foreach (var ocupante in unidadesQueLaOcupan.Where(u => u != unidad))
        {
            ocupante.LiberarDenominacion();
        }

        unidad.AsignarDenominacion(denominacion);
    }
}
