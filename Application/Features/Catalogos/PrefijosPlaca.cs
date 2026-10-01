using System.Text.RegularExpressions;
using Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalogos;

/// <summary>Mirrors PrefijoPlaca (Domain/Entities/Misc). TipoVehiculoIds vacío = cualquier tipo.</summary>
public record PrefijoPlacaItemViewModel(int Id, string Prefijo, string Nombre, string Patron, string Ejemplo, IReadOnlyList<int> TipoVehiculoIds);

/// <summary>
/// Formato de placa según el catálogo de prefijos. La app aplica la misma regla
/// (Mobile/src/features/events/form/placa.ts): por eso los patrones deben ser compatibles con
/// .NET y con JavaScript.
/// </summary>
public static class ReglaPlaca
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(50);

    public static async Task<IReadOnlyList<PrefijoPlacaItemViewModel>> CargarAsync(IReadDbContext db, CancellationToken cancellationToken)
        => await db.PrefijosPlaca
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Select(p => new PrefijoPlacaItemViewModel(
                p.Id,
                p.Prefijo,
                p.Nombre,
                p.Patron,
                p.Ejemplo,
                p.TiposVehiculo.OrderBy(t => t.Id).Select(t => t.Id).ToList()))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Prefijo cuyo formato cumple la placa (ya normalizada), o null si no cumple ninguno. Con
    /// prefijos que se solapan (O y OE) gana el más largo.
    /// </summary>
    public static PrefijoPlacaItemViewModel? Identificar(string placa, IEnumerable<PrefijoPlacaItemViewModel> prefijos)
        => prefijos
            .Where(p => placa.StartsWith(p.Prefijo, StringComparison.Ordinal))
            .OrderByDescending(p => p.Prefijo.Length)
            .FirstOrDefault(p => Cumple(placa, p.Patron));

    private static bool Cumple(string placa, string patron)
    {
        try { return Regex.IsMatch(placa, patron, RegexOptions.CultureInvariant, RegexTimeout); }
        catch (ArgumentException) { return false; } // patrón mal escrito en el catálogo: no valida nada
        catch (RegexMatchTimeoutException) { return false; }
    }
}

public record GetPrefijosPlacaQuery : IRequest<IReadOnlyList<PrefijoPlacaItemViewModel>>;

public class GetPrefijosPlacaQueryHandler(IReadDbContext db) : IRequestHandler<GetPrefijosPlacaQuery, IReadOnlyList<PrefijoPlacaItemViewModel>>
{
    public Task<IReadOnlyList<PrefijoPlacaItemViewModel>> Handle(GetPrefijosPlacaQuery request, CancellationToken cancellationToken)
        => ReglaPlaca.CargarAsync(db, cancellationToken);
}
