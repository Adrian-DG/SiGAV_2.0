using Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Operaciones.Agentes;

/// <summary>
/// Plantilla (.xlsx) para <see cref="ImportarAgentesCommand"/>: hoja de carga con los encabezados y
/// filas de ejemplo, más los valores válidos (rangos, instituciones y áreas operativas).
/// </summary>
public record GetPlantillaImportacionAgentesQuery : IRequest<byte[]>;

public class GetPlantillaImportacionAgentesQueryHandler(IReadDbContext db, IHojaCalculo hojaCalculo)
    : IRequestHandler<GetPlantillaImportacionAgentesQuery, byte[]>
{
    public async Task<byte[]> Handle(GetPlantillaImportacionAgentesQuery request, CancellationToken cancellationToken)
    {
        var rangos = await db.Rangos
            .Where(r => r.IsActive)
            .OrderBy(r => r.Id)
            .Select(r => new { r.Nombre, r.NombreArmada })
            .ToListAsync(cancellationToken);

        var rango = rangos.FirstOrDefault()?.Nombre ?? string.Empty;
        var areas = ColumnasImportacionAgentes.Areas.Values.ToList();

        IReadOnlyList<IReadOnlyList<string>> carga =
        [
            ColumnasImportacionAgentes.Todas,
            ["00112345678", "Juan", "Pérez", "M", "ERD", rango, areas[0], "", "No"],
            ["40212345678", "María", "Gómez", "F", "PN", rango, areas[^2], "Paramédico", "Sí"],
        ];

        IReadOnlyList<IReadOnlyList<string>> instrucciones =
        [
            ["Columna", "Qué indicar"],
            [ColumnasImportacionAgentes.Cedula, "Obligatoria. Con o sin guiones. Si no existe, se crea el agente (autorizado); si existe, se actualizan sus datos. Ponga la columna como Texto para no perder los ceros de la izquierda."],
            [ColumnasImportacionAgentes.Nombre, "Obligatorio."],
            [ColumnasImportacionAgentes.Apellido, "Obligatorio."],
            [ColumnasImportacionAgentes.Sexo, "Obligatorio. M o F."],
            [ColumnasImportacionAgentes.Institucion, $"Obligatoria. Una de: {string.Join(", ", ColumnasImportacionAgentes.Instituciones)}."],
            [ColumnasImportacionAgentes.Rango, "Obligatorio. Nombre exacto de la hoja Rangos (para la ARD, el de la columna Armada)."],
            [ColumnasImportacionAgentes.AreaOperativa, "Obligatoria. Nombre exacto de la hoja Áreas."],
            [ColumnasImportacionAgentes.Especialidad, "Opcional. Vacía: un agente existente conserva la suya."],
            [ColumnasImportacionAgentes.AccesoTotal, "Opcional. Sí o No (acceso a todos los formularios de la app). Vacío: No para los nuevos; los existentes conservan el suyo."],
            [],
            ["Reglas", "Cada cédula puede aparecer una sola vez en el archivo."],
            ["", "La autorización de un agente existente no cambia; los agentes desactivados no se pueden actualizar."],
            ["", "Si alguna fila tiene errores, no se guarda ninguna."],
        ];

        return hojaCalculo.Escribir(
        [
            new HojaNueva("Agentes", carga),
            new HojaNueva("Instrucciones", instrucciones),
            new HojaNueva("Rangos", [["Rango", "Armada"], .. rangos.Select(r => (IReadOnlyList<string>)[r.Nombre, r.NombreArmada])]),
            new HojaNueva("Áreas", [["Área operativa"], .. areas.Select(a => (IReadOnlyList<string>)[a])]),
            new HojaNueva("Instituciones", [["Institución"], .. ColumnasImportacionAgentes.Instituciones.Select(i => (IReadOnlyList<string>)[i.ToString()])]),
        ]);
    }
}
