using Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Unidades;

/// <summary>
/// Plantilla (.xlsx) para <see cref="ImportarUnidadesDenominacionCommand"/>: hoja de carga con los
/// encabezados y filas de ejemplo, más los tramos y niveles activos (los nombres válidos).
/// </summary>
public record GetPlantillaImportacionUnidadesQuery : IRequest<byte[]>;

public class GetPlantillaImportacionUnidadesQueryHandler(IReadDbContext db, IHojaCalculo hojaCalculo)
    : IRequestHandler<GetPlantillaImportacionUnidadesQuery, byte[]>
{
    public async Task<byte[]> Handle(GetPlantillaImportacionUnidadesQuery request, CancellationToken cancellationToken)
    {
        var tramos = await db.Tramos
            .Where(t => t.IsActive)
            .OrderBy(t => t.Nombre)
            .Select(t => t.Nombre)
            .ToListAsync(cancellationToken);

        var niveles = await db.NivelesDenominacion
            .Where(n => n.IsActive)
            .OrderBy(n => n.Jerarquia).ThenBy(n => n.Nombre)
            .Select(n => n.Nombre)
            .ToListAsync(cancellationToken);

        var tramo = tramos.FirstOrDefault() ?? string.Empty;
        var nivel = niveles.LastOrDefault() ?? string.Empty;

        IReadOnlyList<IReadOnlyList<string>> carga =
        [
            ColumnasImportacionUnidades.Todas,
            ["CA-1759", "EL01759", "Samaná Móvil I", tramo, nivel],
            ["CA-1760", "", "Samaná Móvil II", tramo, nivel],
        ];

        IReadOnlyList<IReadOnlyList<string>> instrucciones =
        [
            ["Columna", "Qué indicar"],
            [ColumnasImportacionUnidades.Ficha, "Obligatoria. 1 o 2 letras, guion y 3 o 4 números (CA-1759). Si no existe, se crea la unidad."],
            [ColumnasImportacionUnidades.Placa, "Opcional. 1 o 2 letras y 5 o 6 números (EL01759). Si la unidad existe, se actualiza."],
            [ColumnasImportacionUnidades.Denominacion, "Obligatoria. Si no existe, se crea con el tramo y el nivel de la fila."],
            [ColumnasImportacionUnidades.Tramo, "Solo para denominaciones nuevas. Nombre exacto de la hoja Tramos."],
            [ColumnasImportacionUnidades.Nivel, "Solo para denominaciones nuevas. Nombre exacto de la hoja Niveles."],
            [],
            ["Reglas", "Cada ficha y cada denominación puede aparecer una sola vez en el archivo."],
            ["", "Si la denominación la tiene otra unidad, esa unidad queda sin denominación y No disponible."],
            ["", "Si alguna fila tiene errores, no se guarda ninguna."],
        ];

        return hojaCalculo.Escribir(
        [
            new HojaNueva("Unidades", carga),
            new HojaNueva("Instrucciones", instrucciones),
            new HojaNueva("Tramos", [["Tramo"], .. tramos.Select(t => (IReadOnlyList<string>)[t])]),
            new HojaNueva("Niveles", [["Nivel"], .. niveles.Select(n => (IReadOnlyList<string>)[n])]),
        ]);
    }
}
