using System.Globalization;
using System.Text;
using Application.Common;
using Application.Contracts;
using Domain.Entities.Operaciones;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Features.Operaciones.Unidades;

/// <summary>
/// Carga masiva de unidades y denominaciones desde un Excel (.xlsx). Columnas (por nombre, en
/// cualquier orden): Ficha, Placa, Denominación, Tramo y Nivel. Por cada fila:
/// - la ficha se busca; si no existe se crea la unidad (la placa, si viene, se actualiza o se usa al crearla);
/// - la denominación se busca por nombre; si no existe se crea con el Tramo y el Nivel de la fila;
/// - se asigna la denominación a la unidad con las mismas reglas que <see cref="AsignarDenominacionCommand"/>:
///   si la tenía otra unidad, esa queda sin denominación y No disponible, y la anterior de la unidad queda libre.
/// Con <see cref="Aplicar"/> = false solo se analiza (vista previa, no se guarda nada). Al aplicar, si
/// alguna fila tiene errores no se guarda ninguna: todo el archivo va en una sola transacción.
/// </summary>
public record ImportarUnidadesDenominacionCommand(
    Stream Contenido,
    string? NombreArchivo,
    bool Aplicar,
    string? Motivo = null) : IRequest<ImportacionUnidadesResult>;

public static class EstadoFilaImportacion
{
    public const string Error = "error";
    public const string SinCambios = "sin-cambios";
    public const string Cambios = "cambios";
}

/// <param name="Fila">Número de fila en el Excel.</param>
/// <param name="Estado"><see cref="EstadoFilaImportacion"/>.</param>
/// <param name="Cambios">Qué pasa (o pasó, si se aplicó) con la fila, en palabras.</param>
public record FilaImportacionViewModel(
    int Fila,
    string Ficha,
    string Denominacion,
    string Estado,
    bool UnidadNueva,
    bool DenominacionNueva,
    IReadOnlyList<string> Cambios,
    IReadOnlyList<string> Errores);

/// <param name="Aplicado">true: los cambios se guardaron. false: vista previa, o hubo errores y no se guardó nada.</param>
/// <param name="UnidadesSinDenominacion">Unidades que no están en el archivo y pierden su denominación (quedan No disponibles).</param>
public record ImportacionUnidadesResult(
    bool Aplicado,
    int TotalFilas,
    int ConErrores,
    int SinCambios,
    int Asignaciones,
    int UnidadesCreadas,
    int DenominacionesCreadas,
    IReadOnlyList<FilaImportacionViewModel> Filas,
    IReadOnlyList<UnidadLiberadaViewModel> UnidadesSinDenominacion);

public class ImportarUnidadesDenominacionCommandValidator : AbstractValidator<ImportarUnidadesDenominacionCommand>
{
    public ImportarUnidadesDenominacionCommandValidator()
    {
        RuleFor(x => x.Contenido).NotNull().OverridePropertyName("Archivo").WithMessage("Seleccione el archivo de Excel (.xlsx).");
        RuleFor(x => x.Motivo)
            .MaximumLength(AutorCambio.ObservacionMaxLength).WithMessage($"El motivo no puede exceder {AutorCambio.ObservacionMaxLength} caracteres.");
    }
}

/// <summary>Columnas del Excel de carga (también las de la plantilla).</summary>
public static class ColumnasImportacionUnidades
{
    public const string Ficha = "Ficha";
    public const string Placa = "Placa";
    public const string Denominacion = "Denominación";
    public const string Tramo = "Tramo";
    public const string Nivel = "Nivel";

    public static readonly string[] Todas = [Ficha, Placa, Denominacion, Tramo, Nivel];

    /// <summary>"Denominación " → "denominacion": sin acentos, espacios ni mayúsculas.</summary>
    public static string Clave(string encabezado)
    {
        var sb = new StringBuilder();
        foreach (var ch in encabezado.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }
}

public class ImportarUnidadesDenominacionCommandHandler(
    IHojaCalculo hojaCalculo,
    IUnidadRepository unidades,
    IDenominacionRepository denominaciones,
    IReadDbContext db,
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    TimeProvider timeProvider) : IRequestHandler<ImportarUnidadesDenominacionCommand, ImportacionUnidadesResult>
{
    public const int MaxFilas = 2000;

    private static readonly StringComparer Nombres = StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true);

    public async Task<ImportacionUnidadesResult> Handle(ImportarUnidadesDenominacionCommand request, CancellationToken cancellationToken)
    {
        var autor = currentUser.RequerirAutorWeb(timeProvider, Motivo(request));

        var filas = Leer(request.Contenido);
        ValidarFormato(filas);
        MarcarRepetidas(filas);

        // Las filas con errores de formato también se revisan contra los datos, para mostrar todos sus errores juntos
        var conDatos = filas.Where(f => f.Ficha.Length > 0 && f.Denominacion.Length > 0).ToList();

        // Todo lo que hace falta se carga de una vez (las entidades quedan en seguimiento para modificarlas)
        var porFicha = (await unidades.GetByFichasAsync(filas.Where(f => f.Ficha.Length > 0).Select(f => f.Ficha).ToList(), cancellationToken))
            .ToDictionary(u => u.Ficha, StringComparer.OrdinalIgnoreCase);
        var porNombre = (await denominaciones.GetByNombresAsync(filas.Where(f => f.Denominacion.Length > 0).Select(f => f.Denominacion).ToList(), cancellationToken))
            .GroupBy(d => d.Nombre, Nombres)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.IsActive).First(), Nombres);
        var ocupantes = await unidades.GetActivasConDenominacionesAsync(
            porNombre.Values.Where(d => d.IsActive).Select(d => d.Id).ToList(), cancellationToken);

        var tramos = await CatalogoAsync(db.Tramos.Where(t => t.IsActive).Select(t => new { t.Id, t.Nombre }).ToListAsync(cancellationToken), t => t.Id, t => t.Nombre);
        var niveles = await CatalogoAsync(db.NivelesDenominacion.Where(n => n.IsActive).Select(n => new { n.Id, n.Nombre }).ToListAsync(cancellationToken), n => n.Id, n => n.Nombre);

        // Nombre de la denominación que tiene hoy cada unidad del archivo (para describir el cambio)
        var idsAnteriores = porFicha.Values.Where(u => u.DenominacionId.HasValue).Select(u => u.DenominacionId!.Value).Distinct().ToList();
        var nombresAnteriores = await db.Denominaciones
            .Where(d => idsAnteriores.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Nombre, cancellationToken);

        foreach (var fila in conDatos)
            ValidarContraDatos(fila, porFicha, porNombre, tramos, niveles);

        var liberadas = new List<Unidad>();
        foreach (var fila in filas.Where(f => f.Errores.Count == 0))
        {
            try
            {
                Procesar(fila, autor, porFicha, porNombre, ocupantes, tramos, niveles, nombresAnteriores, liberadas);
            }
            catch (DomainException ex)
            {
                fila.Errores.Add(ex.Message);
            }
        }

        var conErrores = filas.Count(f => f.Errores.Count > 0);
        var aplicado = request.Aplicar && conErrores == 0;

        // Un único SaveChanges es transaccional: todo el archivo o nada
        if (aplicado) await uow.SaveChangesAsync(cancellationToken);

        var fichasDelArchivo = filas.Select(f => f.Ficha).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sinDenominacion = liberadas
            .Distinct()
            .Where(u => u.DenominacionId is null && u.Denominacion is null && !fichasDelArchivo.Contains(u.Ficha))
            .OrderBy(u => u.Ficha)
            .Select(u => new UnidadLiberadaViewModel(u.Id, u.Ficha))
            .ToList();

        var resultado = filas.Select(f => new FilaImportacionViewModel(
                f.Numero,
                f.Ficha,
                f.NombreDenominacion ?? f.Denominacion,
                f.Errores.Count > 0 ? EstadoFilaImportacion.Error
                    : f.Cambios.Count > 0 ? EstadoFilaImportacion.Cambios
                    : EstadoFilaImportacion.SinCambios,
                f.UnidadNueva,
                f.DenominacionNueva,
                f.Errores.Count > 0 ? [] : f.Cambios,
                f.Errores))
            .ToList();
        var correctas = filas.Where(f => f.Errores.Count == 0).ToList();

        return new ImportacionUnidadesResult(
            aplicado,
            filas.Count,
            conErrores,
            correctas.Count(f => f.Cambios.Count == 0),
            correctas.Count(f => f.Asignada),
            correctas.Count(f => f.UnidadNueva),
            correctas.Count(f => f.DenominacionNueva),
            resultado,
            sinDenominacion);
    }

    private static string Motivo(ImportarUnidadesDenominacionCommand request)
    {
        if (!string.IsNullOrWhiteSpace(request.Motivo)) return request.Motivo.Trim();

        var motivo = string.IsNullOrWhiteSpace(request.NombreArchivo)
            ? "Carga desde Excel"
            : $"Carga desde Excel: {Path.GetFileName(request.NombreArchivo)}";
        return motivo.Length > AutorCambio.ObservacionMaxLength ? motivo[..AutorCambio.ObservacionMaxLength] : motivo;
    }

    // ------------------------------------------------ Lectura del archivo

    private List<FilaImportacion> Leer(Stream contenido)
    {
        IReadOnlyList<FilaHoja> hoja;
        try
        {
            hoja = hojaCalculo.LeerPrimeraHoja(contenido);
        }
        catch (InvalidDataException ex)
        {
            throw ErrorArchivo(ex.Message);
        }

        if (hoja.Count == 0) throw ErrorArchivo("El archivo está vacío.");

        // Primera fila con datos: encabezados
        var encabezado = hoja[0].Celdas.Select(ColumnasImportacionUnidades.Clave).ToList();
        int Columna(params string[] nombres) => encabezado.FindIndex(e => nombres.Contains(e));

        var ficha = Columna("ficha");
        var placa = Columna("placa");
        var denominacion = Columna("denominacion");
        var tramo = Columna("tramo");
        var nivel = Columna("nivel", "niveldenominacion", "niveldeladenominacion");

        if (ficha < 0 || denominacion < 0)
            throw ErrorArchivo($"La primera fila debe tener los encabezados: {string.Join(", ", ColumnasImportacionUnidades.Todas)} (Ficha y Denominación son obligatorias).");

        var filas = hoja.Skip(1).Select(f => new FilaImportacion
        {
            Numero = f.Numero,
            Ficha = FichaUnidad.Normalizar(f.Celda(ficha)),
            Placa = NuloSiVacio(f.Celda(placa))?.ToUpperInvariant(),
            Denominacion = f.Celda(denominacion).Trim(),
            Tramo = NuloSiVacio(f.Celda(tramo)),
            Nivel = NuloSiVacio(f.Celda(nivel))
        }).ToList();

        if (filas.Count == 0) throw ErrorArchivo("El archivo no tiene filas debajo de los encabezados.");
        if (filas.Count > MaxFilas) throw ErrorArchivo($"El archivo tiene {filas.Count} filas; el máximo por carga es {MaxFilas}.");

        return filas;
    }

    private static ValidationException ErrorArchivo(string mensaje) => new([new ValidationFailure("Archivo", mensaje)]);

    private static string? NuloSiVacio(string valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    // ------------------------------------------------ Validaciones

    private static void ValidarFormato(List<FilaImportacion> filas)
    {
        foreach (var f in filas)
        {
            if (f.Ficha.Length == 0) f.Errores.Add("Falta la ficha.");
            else if (!Unidad.FichaRegex.IsMatch(f.Ficha))
                f.Errores.Add($"La ficha '{f.Ficha}' no tiene el formato de 1 o 2 letras, guion y 3 o 4 números (por ejemplo: CA-1759).");

            if (f.Placa is not null && (f.Placa.Length > Unidad.PlacaMaxLength || !Unidad.PlacaRegex.IsMatch(f.Placa)))
                f.Errores.Add($"La placa '{f.Placa}' debe tener 1 o 2 letras y 5 o 6 números (por ejemplo: EL00101).");

            if (f.Denominacion.Length == 0) f.Errores.Add("Falta la denominación.");
            else if (f.Denominacion.Length > Denominacion.NombreMaxLength)
                f.Errores.Add($"La denominación no puede exceder {Denominacion.NombreMaxLength} caracteres.");
        }
    }

    /// <summary>Una ficha o una denominación solo puede aparecer una vez: si no, no se sabe cuál fila manda.</summary>
    private static void MarcarRepetidas(List<FilaImportacion> filas)
    {
        void Marcar(Func<FilaImportacion, string> clave, IEqualityComparer<string> comparador, string que)
        {
            foreach (var grupo in filas.Where(f => clave(f).Length > 0).GroupBy(clave, comparador).Where(g => g.Count() > 1))
            {
                var numeros = string.Join(", ", grupo.Select(f => f.Numero));
                foreach (var f in grupo)
                    f.Errores.Add($"{que} '{grupo.Key}' se repite en las filas {numeros}.");
            }
        }

        Marcar(f => f.Ficha, StringComparer.OrdinalIgnoreCase, "La ficha");
        Marcar(f => f.Denominacion, Nombres, "La denominación");
    }

    private static void ValidarContraDatos(
        FilaImportacion fila,
        Dictionary<string, Unidad> porFicha,
        Dictionary<string, Denominacion> porNombre,
        Dictionary<string, (int Id, string Nombre)> tramos,
        Dictionary<string, (int Id, string Nombre)> niveles)
    {
        if (porFicha.TryGetValue(fila.Ficha, out var unidad) && !unidad.IsActive)
            fila.Errores.Add($"La unidad '{unidad.Ficha}' está desactivada.");

        if (porNombre.TryGetValue(fila.Denominacion, out var denominacion))
        {
            if (!denominacion.IsActive)
                fila.Errores.Add($"La denominación '{denominacion.Nombre}' está inactiva.");
            return;
        }

        // Denominación nueva: el tramo y el nivel son obligatorios
        if (fila.Tramo is null) fila.Errores.Add("La denominación no existe: indique el tramo para crearla.");
        else if (!tramos.ContainsKey(fila.Tramo)) fila.Errores.Add($"El tramo '{fila.Tramo}' no existe.");

        if (fila.Nivel is null) fila.Errores.Add("La denominación no existe: indique el nivel para crearla.");
        else if (!niveles.ContainsKey(fila.Nivel)) fila.Errores.Add($"El nivel '{fila.Nivel}' no existe.");
    }

    // ------------------------------------------------ Cambios

    private void Procesar(
        FilaImportacion fila,
        AutorCambio autor,
        Dictionary<string, Unidad> porFicha,
        Dictionary<string, Denominacion> porNombre,
        List<Unidad> ocupantes,
        Dictionary<string, (int Id, string Nombre)> tramos,
        Dictionary<string, (int Id, string Nombre)> niveles,
        Dictionary<int, string> nombresAnteriores,
        List<Unidad> liberadas)
    {
        // Unidad
        if (!porFicha.TryGetValue(fila.Ficha, out var unidad))
        {
            unidad = Unidad.Crear(fila.Ficha, fila.Placa, autor);
            unidades.Add(unidad);
            porFicha[unidad.Ficha] = unidad;
            fila.UnidadNueva = true;
            fila.Cambios.Add(fila.Placa is null ? "Unidad nueva." : $"Unidad nueva con placa {fila.Placa}.");
        }
        else if (fila.Placa is not null && fila.Placa != unidad.Placa)
        {
            fila.Cambios.Add($"Placa: {unidad.Placa ?? "sin placa"} → {fila.Placa}.");
            unidad.ActualizarDatos(unidad.Ficha, fila.Placa);
        }

        // Denominación
        if (!porNombre.TryGetValue(fila.Denominacion, out var denominacion))
        {
            var tramo = tramos[fila.Tramo!];
            var nivel = niveles[fila.Nivel!];
            denominacion = Denominacion.Crear(fila.Denominacion, tramo.Id, nivel.Id);
            denominaciones.Add(denominacion);
            porNombre[denominacion.Nombre] = denominacion;
            fila.DenominacionNueva = true;
            fila.Cambios.Add($"Denominación nueva en el tramo {tramo.Nombre}, nivel {nivel.Nombre}.");
        }
        else if (fila.Tramo is not null || fila.Nivel is not null)
        {
            var tramoDistinto = fila.Tramo is not null && (!tramos.TryGetValue(fila.Tramo, out var t) || t.Id != denominacion.TramoId);
            var nivelDistinto = fila.Nivel is not null && (!niveles.TryGetValue(fila.Nivel, out var n) || n.Id != denominacion.NivelDenominacionId);
            if (tramoDistinto || nivelDistinto)
                fila.Cambios.Add("El tramo y el nivel del archivo se ignoran: la denominación ya existe y conserva los suyos.");
        }

        fila.NombreDenominacion = denominacion.Nombre;

        if (unidad.Id > 0 && unidad.TieneDenominacion(denominacion.Id))
        {
            // Solo una nota no es un cambio
            fila.Cambios.RemoveAll(c => c.StartsWith("El tramo y el nivel", StringComparison.Ordinal));
            return;
        }

        // Asignación. Los ocupantes se filtran en memoria: una fila anterior pudo cambiarles la denominación.
        var anterior = unidad.DenominacionId is { } anteriorId && nombresAnteriores.TryGetValue(anteriorId, out var nombreAnterior)
            ? nombreAnterior
            : null;
        var quienesLaTienen = fila.DenominacionNueva
            ? []
            : ocupantes.Where(o => o != unidad && o.IsActive && o.DenominacionId == denominacion.Id).ToList();

        AsignacionDenominacionService.Asignar(unidad, denominacion, quienesLaTienen, autor);

        fila.Asignada = true;
        fila.Cambios.Add(anterior is null
            ? $"Se le asigna {denominacion.Nombre}."
            : porNombre.ContainsKey(anterior)
                ? $"Cambia de {anterior} a {denominacion.Nombre}."
                : $"Cambia de {anterior} a {denominacion.Nombre} ({anterior} queda libre).");
        foreach (var otra in quienesLaTienen)
            fila.Cambios.Add($"{otra.Ficha} pierde {denominacion.Nombre} y queda sin denominación (No disponible).");
        liberadas.AddRange(quienesLaTienen);
    }

    private static async Task<Dictionary<string, (int Id, string Nombre)>> CatalogoAsync<T>(Task<List<T>> consulta, Func<T, int> id, Func<T, string> nombre)
        => (await consulta)
            .GroupBy(nombre, Nombres)
            .ToDictionary(g => g.Key, g => (id(g.First()), nombre(g.First())), Nombres);

    private sealed class FilaImportacion
    {
        public int Numero { get; init; }
        public string Ficha { get; init; } = string.Empty;
        public string? Placa { get; init; }
        public string Denominacion { get; init; } = string.Empty;
        public string? Tramo { get; init; }
        public string? Nivel { get; init; }

        /// <summary>Como está registrada (el archivo puede traerla con otras mayúsculas).</summary>
        public string? NombreDenominacion { get; set; }
        public bool UnidadNueva { get; set; }
        public bool DenominacionNueva { get; set; }
        public bool Asignada { get; set; }
        public List<string> Cambios { get; } = [];
        public List<string> Errores { get; } = [];
    }
}
