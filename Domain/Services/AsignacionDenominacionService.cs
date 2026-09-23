using Domain.Entities.Operaciones;
using Domain.ValueObjects;

namespace Domain.Services;

/// <summary>
/// Regla de negocio que involucra varios agregados: una denominación solo puede estar
/// ocupada por una unidad activa a la vez. Al asignarla, las unidades que la ocupaban
/// quedan sin denominación y no disponibles. Cada cambio queda en el historial de auditoría
/// de las unidades involucradas, con el usuario responsable.
/// </summary>
public static class AsignacionDenominacionService
{
    public static void Asignar(Unidad unidad, Denominacion denominacion, IEnumerable<Unidad> unidadesQueLaOcupan, AutorCambio autor)
    {
        Unidad? desplazada = null;

        foreach (var ocupante in unidadesQueLaOcupan.Where(u => u != unidad))
        {
            ocupante.LiberarDenominacion(autor, unidadQueLaToma: unidad);
            desplazada ??= ocupante;
        }

        unidad.AsignarDenominacion(denominacion, autor, desplazada);
    }
}
