using System.Globalization;
using Application.Common;
using Application.Contracts;
using Application.Features.Operaciones.Unidades;
using Domain.Abstraction;
using Domain.Entities.Operaciones;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Application.Exceptions.ValidationException;

namespace Application.Features.Operaciones.Agentes;

/// <summary>
/// Carga masiva de agentes desde un Excel (.xlsx). Columnas (por nombre, en cualquier orden): Cédula,
/// Nombre, Apellido, Sexo, Institución, Rango, Área operativa, Especialidad y Acceso total. Por cada fila:
/// - la cédula se busca; si no existe se crea el agente, autorizado (igual que <see cref="CreateAgenteCommand"/>);
/// - si existe se actualizan sus datos; Especialidad y Acceso total vacíos conservan lo que tiene.
/// La autorización de un agente existente no cambia. Con <see cref="Aplicar"/> = false solo se analiza
/// (vista previa). Al aplicar, si alguna fila tiene errores no se guarda ninguna.
/// </summary>
public record ImportarAgentesCommand(Stream Contenido, bool Aplicar) : IRequest<ImportacionAgentesResult>;

/// <param name="Fila">Número de fila en el Excel.</param>
/// <param name="Estado"><see cref="EstadoFilaImportacion"/>.</param>
/// <param name="Cambios">Qué pasa (o pasó, si se aplicó) con la fila, en palabras.</param>
public record FilaImportacionAgenteViewModel(
    int Fila,
    string Identificacion,
    string Nombre,
    string Estado,
    bool AgenteNuevo,
    IReadOnlyList<string> Cambios,
    IReadOnlyList<string> Errores);

/// <param name="Aplicado">true: los cambios se guardaron. false: vista previa, o hubo errores y no se guardó nada.</param>
public record ImportacionAgentesResult(
    bool Aplicado,
    int TotalFilas,
    int ConErrores,
    int SinCambios,
    int AgentesCreados,
    int AgentesActualizados,
    IReadOnlyList<FilaImportacionAgenteViewModel> Filas);

public class ImportarAgentesCommandValidator : AbstractValidator<ImportarAgentesCommand>
{
    public ImportarAgentesCommandValidator()
    {
        RuleFor(x => x.Contenido).NotNull().OverridePropertyName("Archivo").WithMessage("Seleccione el archivo de Excel (.xlsx).");
    }
}

/// <summary>Columnas del Excel de carga (también las de la plantilla) y los valores que aceptan.</summary>
public static class ColumnasImportacionAgentes
{
    public const string Cedula = "Cédula";
    public const string Nombre = "Nombre";
    public const string Apellido = "Apellido";
    public const string Sexo = "Sexo";
    public const string Institucion = "Institución";
    public const string Rango = "Rango";
    public const string AreaOperativa = "Área operativa";
    public const string Especialidad = "Especialidad";
    public const string AccesoTotal = "Acceso total";

    public static readonly string[] Todas = [Cedula, Nombre, Apellido, Sexo, Institucion, Rango, AreaOperativa, Especialidad, AccesoTotal];

    /// <summary>Como se escriben en el Excel (front desk usa los mismos nombres).</summary>
    public static readonly IReadOnlyDictionary<AreaOperativaEnum, string> Areas = new Dictionary<AreaOperativaEnum, string>
    {
        [AreaOperativaEnum.AsistenciaVial] = "Asistencia vial",
        [AreaOperativaEnum.GestionOperativa] = "Gestión operativa",
        [AreaOperativaEnum.SeguridadCiudadana] = "Seguridad ciudadana",
        [AreaOperativaEnum.Taller] = "Taller",
        [AreaOperativaEnum.Gruas] = "Grúas",
        [AreaOperativaEnum.PreHospitalaria] = "Prehospitalaria",
        [AreaOperativaEnum.Rescate] = "Rescate",
    };

    public static readonly InstitucionEnum[] Instituciones = Enum.GetValues<InstitucionEnum>().Where(i => i != InstitucionEnum.NONE).ToArray();

    public static string Sexos(SexoEnum sexo) => sexo == SexoEnum.FEMININO ? "F" : "M";
}

public class ImportarAgentesCommandHandler(
    IHojaCalculo hojaCalculo,
    IAgenteRepository agentes,
    IReadDbContext db,
    IUnitOfWork uow) : IRequestHandler<ImportarAgentesCommand, ImportacionAgentesResult>
{
    public const int MaxFilas = 2000;

    private static readonly StringComparer Nombres = StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true);

    public async Task<ImportacionAgentesResult> Handle(ImportarAgentesCommand request, CancellationToken cancellationToken)
    {
        var rangos = await db.Rangos
            .Where(r => r.IsActive)
            .Select(r => new RangoItem(r.Id, r.Nombre, r.NombreArmada))
            .ToListAsync(cancellationToken);

        var filas = Leer(request.Contenido);
        foreach (var fila in filas) ValidarFormato(fila, rangos);
        MarcarRepetidas(filas);

        // Las entidades quedan en seguimiento para modificarlas
        var porCedula = (await agentes.GetByIdentificacionesAsync(
                filas.Where(f => f.Identificacion is not null).Select(f => f.Identificacion!).ToList(), cancellationToken))
            .ToDictionary(a => a.Identificacion, StringComparer.OrdinalIgnoreCase);
        var rangoPorId = rangos.ToDictionary(r => r.Id);

        foreach (var fila in filas.Where(f => f.Errores.Count == 0))
        {
            try
            {
                Procesar(fila, porCedula, rangoPorId);
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

        var correctas = filas.Where(f => f.Errores.Count == 0).ToList();
        return new ImportacionAgentesResult(
            aplicado,
            filas.Count,
            conErrores,
            correctas.Count(f => f.Cambios.Count == 0),
            correctas.Count(f => f.AgenteNuevo),
            correctas.Count(f => !f.AgenteNuevo && f.Cambios.Count > 0),
            filas.Select(f => new FilaImportacionAgenteViewModel(
                    f.Numero,
                    f.Identificacion ?? f.CedulaOriginal,
                    $"{f.Nombre} {f.Apellido}".Trim(),
                    f.Errores.Count > 0 ? EstadoFilaImportacion.Error
                        : f.Cambios.Count > 0 ? EstadoFilaImportacion.Cambios
                        : EstadoFilaImportacion.SinCambios,
                    f.AgenteNuevo,
                    f.Errores.Count > 0 ? [] : f.Cambios,
                    f.Errores))
                .ToList());
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

        var cedula = Columna("cedula", "identificacion");
        var nombre = Columna("nombre", "nombres");
        var apellido = Columna("apellido", "apellidos");
        var sexo = Columna("sexo");
        var institucion = Columna("institucion");
        var rango = Columna("rango");
        var area = Columna("areaoperativa", "area");
        var especialidad = Columna("especialidad");
        var accesoTotal = Columna("accesototal");

        if (new[] { cedula, nombre, apellido, sexo, institucion, rango, area }.Any(c => c < 0))
            throw ErrorArchivo($"La primera fila debe tener los encabezados: {string.Join(", ", ColumnasImportacionAgentes.Todas)} (Especialidad y Acceso total son opcionales).");

        var filas = hoja.Skip(1).Select(f => new FilaImportacion
        {
            Numero = f.Numero,
            CedulaOriginal = f.Celda(cedula).Trim(),
            Nombre = f.Celda(nombre).Trim(),
            Apellido = f.Celda(apellido).Trim(),
            Sexo = f.Celda(sexo).Trim(),
            Institucion = f.Celda(institucion).Trim(),
            Rango = f.Celda(rango).Trim(),
            Area = f.Celda(area).Trim(),
            Especialidad = NuloSiVacio(f.Celda(especialidad)),
            AccesoTotalTexto = NuloSiVacio(f.Celda(accesoTotal)),
        }).ToList();

        if (filas.Count == 0) throw ErrorArchivo("El archivo no tiene filas debajo de los encabezados.");
        if (filas.Count > MaxFilas) throw ErrorArchivo($"El archivo tiene {filas.Count} filas; el máximo por carga es {MaxFilas}.");

        return filas;
    }

    private static ValidationException ErrorArchivo(string mensaje) => new([new ValidationFailure("Archivo", mensaje)]);

    private static string? NuloSiVacio(string valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    // ------------------------------------------------ Validaciones

    private static void ValidarFormato(FilaImportacion f, List<RangoItem> rangos)
    {
        f.Identificacion = Cedula(f.CedulaOriginal);
        if (f.CedulaOriginal.Length == 0) f.Errores.Add("Falta la cédula.");
        else if (f.Identificacion is null)
            f.Errores.Add($"La cédula '{f.CedulaOriginal}' debe tener entre {PersonMetadata.IdentificacionMinLength} y {PersonMetadata.IdentificacionMaxLength} caracteres (sin guiones).");

        if (f.Nombre.Length == 0) f.Errores.Add("Falta el nombre.");
        else if (f.Nombre.Length > PersonMetadata.NombreMaxLength) f.Errores.Add($"El nombre no puede exceder {PersonMetadata.NombreMaxLength} caracteres.");

        if (f.Apellido.Length == 0) f.Errores.Add("Falta el apellido.");
        else if (f.Apellido.Length > PersonMetadata.ApellidoMaxLength) f.Errores.Add($"El apellido no puede exceder {PersonMetadata.ApellidoMaxLength} caracteres.");

        f.SexoValor = ColumnasImportacionUnidades.Clave(f.Sexo) switch
        {
            "m" or "masculino" or "1" => SexoEnum.MASCULINO,
            "f" or "femenino" or "2" => SexoEnum.FEMININO,
            _ => null
        };
        if (f.Sexo.Length == 0) f.Errores.Add("Falta el sexo.");
        else if (f.SexoValor is null) f.Errores.Add($"El sexo '{f.Sexo}' no es válido: use M o F.");

        f.InstitucionValor = ColumnasImportacionAgentes.Instituciones
            .Where(i => string.Equals(i.ToString(), f.Institucion, StringComparison.OrdinalIgnoreCase))
            .Select(i => (InstitucionEnum?)i)
            .FirstOrDefault();
        if (f.Institucion.Length == 0) f.Errores.Add("Falta la institución.");
        else if (f.InstitucionValor is null)
            f.Errores.Add($"La institución '{f.Institucion}' no es válida: use {string.Join(", ", ColumnasImportacionAgentes.Instituciones)}.");

        var claveArea = ColumnasImportacionUnidades.Clave(f.Area);
        f.AreaValor = ColumnasImportacionAgentes.Areas
            .Where(a => ColumnasImportacionUnidades.Clave(a.Value) == claveArea || ((int)a.Key).ToString(CultureInfo.InvariantCulture) == claveArea)
            .Select(a => (AreaOperativaEnum?)a.Key)
            .FirstOrDefault();
        if (f.Area.Length == 0) f.Errores.Add("Falta el área operativa.");
        else if (f.AreaValor is null) f.Errores.Add($"El área operativa '{f.Area}' no existe (vea la hoja Áreas de la plantilla).");

        // La Armada tiene sus propios nombres de rango: se busca primero en los de la institución de la fila
        if (f.Rango.Length == 0) f.Errores.Add("Falta el rango.");
        else
        {
            var armada = f.InstitucionValor == InstitucionEnum.ARD;
            var rango = rangos.FirstOrDefault(r => Nombres.Equals(armada ? r.NombreArmada : r.Nombre, f.Rango))
                ?? rangos.FirstOrDefault(r => Nombres.Equals(armada ? r.Nombre : r.NombreArmada, f.Rango));
            if (rango is null) f.Errores.Add($"El rango '{f.Rango}' no existe (vea la hoja Rangos de la plantilla).");
            else f.RangoId = rango.Id;
        }

        if (f.Especialidad?.Length > Agente.EspecialidadMaxLength)
            f.Errores.Add($"La especialidad no puede exceder {Agente.EspecialidadMaxLength} caracteres.");

        if (f.AccesoTotalTexto is not null)
        {
            f.AccesoTotal = ColumnasImportacionUnidades.Clave(f.AccesoTotalTexto) switch
            {
                "si" or "s" or "1" or "verdadero" or "true" or "x" => true,
                "no" or "n" or "0" or "falso" or "false" => false,
                _ => null
            };
            if (f.AccesoTotal is null) f.Errores.Add($"Acceso total '{f.AccesoTotalTexto}' no es válido: use Sí o No.");
        }
    }

    /// <summary>
    /// Como <see cref="Agente.NormalizarIdentificacion"/>, o null si el largo no es válido. Una cédula de
    /// 9 o 10 dígitos es una de 11 que Excel guardó como número y perdió los ceros de la izquierda.
    /// </summary>
    private static string? Cedula(string valor)
    {
        var cedula = new string(valor.Where(char.IsLetterOrDigit).ToArray());
        if (cedula.Length is 9 or 10 && cedula.All(char.IsAsciiDigit))
            cedula = cedula.PadLeft(PersonMetadata.IdentificacionMaxLength, '0');
        return cedula.Length is >= PersonMetadata.IdentificacionMinLength and <= PersonMetadata.IdentificacionMaxLength ? cedula : null;
    }

    /// <summary>Una cédula solo puede aparecer una vez: si no, no se sabe cuál fila manda.</summary>
    private static void MarcarRepetidas(List<FilaImportacion> filas)
    {
        foreach (var grupo in filas.Where(f => f.Identificacion is not null).GroupBy(f => f.Identificacion!, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
        {
            var numeros = string.Join(", ", grupo.Select(f => f.Numero));
            foreach (var f in grupo)
                f.Errores.Add($"La cédula '{grupo.Key}' se repite en las filas {numeros}.");
        }
    }

    // ------------------------------------------------ Cambios

    private void Procesar(FilaImportacion fila, Dictionary<string, Agente> porCedula, Dictionary<int, RangoItem> rangos)
    {
        var sexo = fila.SexoValor!.Value;
        var institucion = fila.InstitucionValor!.Value;
        var area = fila.AreaValor!.Value;
        var rangoId = fila.RangoId!.Value;

        if (!porCedula.TryGetValue(fila.Identificacion!, out var agente))
        {
            agente = Agente.Crear(
                fila.Identificacion!, fila.Nombre, fila.Apellido, sexo, institucion, rangoId, area,
                fila.AccesoTotal ?? false, fila.Especialidad, autorizado: true);
            agentes.Add(agente);
            porCedula[agente.Identificacion] = agente;
            fila.AgenteNuevo = true;
            fila.Cambios.Add("Agente nuevo, autorizado para usar la app.");
            return;
        }

        if (!agente.IsActive)
        {
            fila.Errores.Add($"El agente '{agente.Identificacion}' está desactivado.");
            return;
        }

        var accesoTotal = fila.AccesoTotal ?? agente.AccesoTotal;
        var especialidad = fila.Especialidad ?? agente.Especialidad;

        void Comparar(string campo, string? antes, string? despues)
        {
            if (!string.Equals(antes ?? string.Empty, despues ?? string.Empty, StringComparison.Ordinal))
                fila.Cambios.Add($"{campo}: {(string.IsNullOrEmpty(antes) ? "—" : antes)} → {(string.IsNullOrEmpty(despues) ? "—" : despues)}.");
        }

        string NombreRango(int id, InstitucionEnum inst) => rangos.TryGetValue(id, out var r)
            ? inst == InstitucionEnum.ARD ? r.NombreArmada : r.Nombre
            : $"#{id}";
        string NombreArea(AreaOperativaEnum a) => ColumnasImportacionAgentes.Areas.GetValueOrDefault(a, a.ToString());
        static string SiNo(bool v) => v ? "Sí" : "No";

        Comparar("Nombre", agente.Nombre, fila.Nombre);
        Comparar("Apellido", agente.Apellido, fila.Apellido);
        Comparar("Sexo", ColumnasImportacionAgentes.Sexos(agente.Sexo), ColumnasImportacionAgentes.Sexos(sexo));
        Comparar("Institución", agente.Institucion.ToString(), institucion.ToString());
        if (agente.RangoId != rangoId || agente.Institucion != institucion)
            Comparar("Rango", NombreRango(agente.RangoId, agente.Institucion), NombreRango(rangoId, institucion));
        Comparar("Área operativa", NombreArea(agente.AreaOperativa), NombreArea(area));
        Comparar("Especialidad", agente.Especialidad, especialidad);
        Comparar("Acceso total", SiNo(agente.AccesoTotal), SiNo(accesoTotal));

        if (fila.Cambios.Count == 0) return;

        agente.ActualizarDatos(agente.Identificacion, fila.Nombre, fila.Apellido, sexo, institucion, rangoId, area, accesoTotal, especialidad);
        if (!agente.Autorizado) fila.Cambios.Add("Sigue pendiente de autorización.");
    }

    private sealed record RangoItem(int Id, string Nombre, string NombreArmada);

    private sealed class FilaImportacion
    {
        public int Numero { get; init; }
        public string CedulaOriginal { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public string Apellido { get; init; } = string.Empty;
        public string Sexo { get; init; } = string.Empty;
        public string Institucion { get; init; } = string.Empty;
        public string Rango { get; init; } = string.Empty;
        public string Area { get; init; } = string.Empty;
        public string? Especialidad { get; init; }
        public string? AccesoTotalTexto { get; init; }

        /// <summary>Normalizada (sin guiones); null si no es válida.</summary>
        public string? Identificacion { get; set; }
        public SexoEnum? SexoValor { get; set; }
        public InstitucionEnum? InstitucionValor { get; set; }
        public AreaOperativaEnum? AreaValor { get; set; }
        public int? RangoId { get; set; }
        public bool? AccesoTotal { get; set; }

        public bool AgenteNuevo { get; set; }
        public List<string> Cambios { get; } = [];
        public List<string> Errores { get; } = [];
    }
}
