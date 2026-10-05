using Application.Common;
using Application.Contracts;
using Application.Exceptions;
using Domain.Entities.Operaciones;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace Application.Features.Operaciones.Unidades;

public record NuevaUnidadRequest(string Ficha, string? Placa);

public record NuevaDenominacionRequest(string Nombre, int TramoId, int NivelDenominacionId);

/// <summary>
/// Asigna una denominación a una unidad. Cada lado puede ser existente (Id) o crearse en el momento
/// (Nueva…), así que cubre las cuatro combinaciones:
/// - unidad existente + denominación existente: cambio de denominación (CA-1759: Samaná 6 → Samaná Móvil I);
/// - unidad nueva + denominación existente;
/// - unidad existente + denominación nueva;
/// - unidad nueva + denominación nueva.
/// Si la denominación la tenía otra unidad activa, esa unidad queda sin denominación y No disponible
/// (AsignacionDenominacionService). La denominación anterior de la unidad queda libre. Todo se
/// guarda en una sola transacción y queda en el historial de auditoría con el usuario y el motivo.
/// </summary>
public record AsignarDenominacionCommand(
    int? UnidadId,
    NuevaUnidadRequest? NuevaUnidad,
    int? DenominacionId,
    NuevaDenominacionRequest? NuevaDenominacion,
    string? Motivo = null) : IRequest<AsignacionDenominacionResult>;

/// <param name="UnidadesLiberadas">Unidades que tenían la denominación y quedaron sin ella y No disponibles.</param>
/// <param name="DenominacionAnterior">La que tenía la unidad antes (ahora libre), si tenía otra.</param>
public record AsignacionDenominacionResult(
    int UnidadId,
    string Ficha,
    int DenominacionId,
    string Denominacion,
    string? DenominacionAnterior,
    IReadOnlyList<UnidadLiberadaViewModel> UnidadesLiberadas);

public record UnidadLiberadaViewModel(int Id, string Ficha);

public class AsignarDenominacionCommandValidator : AbstractValidator<AsignarDenominacionCommand>
{
    public AsignarDenominacionCommandValidator(IReadDbContext db)
    {
        RuleFor(x => x)
            .Must(x => x.UnidadId.HasValue != (x.NuevaUnidad is not null))
            .WithName("Unidad")
            .WithMessage("Indique una unidad existente o los datos de una unidad nueva (solo una de las dos).");
        RuleFor(x => x)
            .Must(x => x.DenominacionId.HasValue != (x.NuevaDenominacion is not null))
            .WithName("Denominacion")
            .WithMessage("Indique una denominación existente o los datos de una nueva (solo una de las dos).");

        RuleFor(x => x.UnidadId).GreaterThan(0).When(x => x.UnidadId.HasValue).WithMessage("La unidad no es válida.");
        RuleFor(x => x.DenominacionId).GreaterThan(0).When(x => x.DenominacionId.HasValue).WithMessage("La denominación no es válida.");

        RuleFor(x => x.NuevaUnidad!).SetValidator(new NuevaUnidadValidator()).When(x => x.NuevaUnidad is not null);
        RuleFor(x => x.NuevaDenominacion!).SetValidator(new NuevaDenominacionValidator(db)).When(x => x.NuevaDenominacion is not null);

        RuleFor(x => x.Motivo)
            .MaximumLength(AutorCambio.ObservacionMaxLength).WithMessage($"El motivo no puede exceder {AutorCambio.ObservacionMaxLength} caracteres.");
    }
}

public class NuevaUnidadValidator : AbstractValidator<NuevaUnidadRequest>
{
    public NuevaUnidadValidator()
    {
        RuleFor(x => x.Ficha).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("La ficha es requerida.")
            .Must(f => Unidad.FichaRegex.IsMatch(FichaUnidad.Normalizar(f)))
            .WithMessage("La ficha debe tener el formato de 1 o 2 letras, guion y 3 o 4 números (por ejemplo: CA-1759).");
        RuleFor(x => x.Placa)
            .MaximumLength(Unidad.PlacaMaxLength).WithMessage($"La placa no puede exceder {Unidad.PlacaMaxLength} caracteres.")
            .Must(p => Unidad.PlacaRegex.IsMatch(p!.Trim().ToUpperInvariant()))
            .WithMessage("La placa debe tener 1 o 2 letras y 5 o 6 números (por ejemplo: EL00101).")
            .When(x => !string.IsNullOrWhiteSpace(x.Placa));
    }
}

public class NuevaDenominacionValidator : AbstractValidator<NuevaDenominacionRequest>
{
    public NuevaDenominacionValidator(IReadDbContext db)
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la denominación es requerido.")
            .MaximumLength(Denominacion.NombreMaxLength).WithMessage($"El nombre no puede exceder {Denominacion.NombreMaxLength} caracteres.");
        RuleFor(x => x.TramoId).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("El tramo es requerido.")
            .MustAsync((id, ct) => db.Tramos.ExisteActivoAsync(id, ct)).WithMessage("El tramo especificado no existe.");
        RuleFor(x => x.NivelDenominacionId).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("El nivel de la denominación es requerido.")
            .MustAsync((id, ct) => db.NivelesDenominacion.ExisteActivoAsync(id, ct)).WithMessage("El nivel de denominación especificado no existe.");
    }
}

/// <summary>Las fichas se guardan en mayúsculas ("ca-1759" → "CA-1759").</summary>
public static class FichaUnidad
{
    public static string Normalizar(string? ficha) => (ficha ?? string.Empty).Trim().ToUpperInvariant();
}

public class AsignarDenominacionCommandHandler(
    IUnidadRepository unidades,
    IDenominacionRepository denominaciones,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IRequestHandler<AsignarDenominacionCommand, AsignacionDenominacionResult>
{
    public async Task<AsignacionDenominacionResult> Handle(AsignarDenominacionCommand request, CancellationToken cancellationToken)
    {
        var autor = currentUser.RequerirAutorWeb(timeProvider, request.Motivo);

        var unidad = await ObtenerUnidadAsync(request, autor, cancellationToken);
        var (denominacion, esNueva) = await ObtenerDenominacionAsync(request, cancellationToken);

        var anterior = unidad.Id > 0 && unidad.DenominacionId is { } anteriorId && anteriorId != denominacion.Id
            ? (await denominaciones.GetByIdAsync(anteriorId, cancellationToken))?.Nombre
            : null;

        // Una denominación nueva no la ocupa nadie; si la unidad ya la tenía tampoco hay a quién liberar
        List<Unidad> ocupantes = esNueva || unidad.TieneDenominacion(denominacion.Id)
            ? []
            : await unidades.GetActivasConDenominacionAsync(denominacion.Id, cancellationToken);
        var liberadas = ocupantes.Where(o => o != unidad).ToList();

        AsignacionDenominacionService.Asignar(unidad, denominacion, ocupantes, autor);

        // Un único SaveChanges es transaccional: unidad, denominación, liberaciones e historial, o nada
        await uow.SaveChangesAsync(cancellationToken);

        return new AsignacionDenominacionResult(
            unidad.Id,
            unidad.Ficha,
            denominacion.Id,
            denominacion.Nombre,
            anterior,
            liberadas.Select(u => new UnidadLiberadaViewModel(u.Id, u.Ficha)).ToList());
    }

    private async Task<Unidad> ObtenerUnidadAsync(AsignarDenominacionCommand request, AutorCambio autor, CancellationToken cancellationToken)
    {
        if (request.UnidadId is { } unidadId)
            return await unidades.GetByIdAsync(unidadId, cancellationToken)
                ?? throw new NotFoundException("La unidad", unidadId);

        var nueva = request.NuevaUnidad!;
        var ficha = FichaUnidad.Normalizar(nueva.Ficha);
        if (await unidades.ExisteFichaAsync(ficha, cancellationToken: cancellationToken))
            throw new ConflictException($"La ficha '{ficha}' ya está registrada. Elíjala como unidad existente.");

        var unidad = Unidad.Crear(ficha, nueva.Placa, autor);
        unidades.Add(unidad);
        return unidad;
    }

    private async Task<(Denominacion Denominacion, bool EsNueva)> ObtenerDenominacionAsync(AsignarDenominacionCommand request, CancellationToken cancellationToken)
    {
        if (request.DenominacionId is { } denominacionId)
            return (await denominaciones.GetByIdAsync(denominacionId, cancellationToken)
                ?? throw new NotFoundException("La denominación", denominacionId), false);

        var nueva = request.NuevaDenominacion!;
        var denominacion = Denominacion.Crear(nueva.Nombre, nueva.TramoId, nueva.NivelDenominacionId);
        if (await denominaciones.ExisteNombreAsync(denominacion.Nombre, cancellationToken))
            throw new ConflictException($"La denominación '{denominacion.Nombre}' ya existe. Elíjala como denominación existente.");

        denominaciones.Add(denominacion);
        return (denominacion, true);
    }
}
